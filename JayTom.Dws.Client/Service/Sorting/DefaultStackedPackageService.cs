using System;
using DryIoc;
using S7.Net;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using FluentFTP.Helpers;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using JayTom.Dws.Plugin.SerialPort;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Manager;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Dto.PackageExitLockDto;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.Service.Sorting.Communication.TcpComm;
using JayTom.Dws.Client.Service.Sorting.Communication.SerialComm;

namespace JayTom.Dws.Client.Service.Sorting {

    public class DefaultStackedPackageService : IStackedPackageService {
        private readonly IConfigRepository _configRepository;
        private readonly IPackageDetectionSerialPort _packageDetectionSerialPort;
        private readonly IPackageDetectionTcp _packageDetectionTcp;
        private StackedPackageDetectionSettingsDto? _stackedPackageDetectionSettingsDto = new();
        private ConcurrentQueue<PackageInfo> _stackedPackageItems = new();
        private static Task? _clearThread;
        private static CancellationTokenSource? _cancellationTokenSource;

        public DefaultStackedPackageService(IConfigRepository configRepository,
            IPackageDetectionSerialPort packageDetectionSerialPort, IPackageDetectionTcp packageDetectionTcp) {
            _configRepository = configRepository;
            _packageDetectionSerialPort = packageDetectionSerialPort;
            _packageDetectionTcp = packageDetectionTcp;
            _packageDetectionSerialPort.DataReceived += (sender, args) => {
                //串口接收内容
                if (_stackedPackageDetectionSettingsDto is not null) {
                    try {
                        var isStacked = false;
                        var tryDequeue = _stackedPackageItems.TryDequeue(out var result);
                        if (tryDequeue && result is not null) {
                            isStacked = Regex.IsMatch(args.AsciiMessage, _stackedPackageDetectionSettingsDto.RegularExpression);
                        }

                        OnStackedPackageReturned(new StackedPackageEventArgs() {
                            PackageInfo = result,
                            PackageTime = result?.CreateTime ?? DateTime.MinValue,
                            IsStacked = isStacked,
                            StackedContent = args.AsciiMessage
                        });
                    }
                    catch (Exception e) {
                        OnExceptionOccurred(new ExceptionEventArgs() {
                            ExceptionMessage = $"接收内容解析错误:{args.AsciiMessage}"
                        });
                    }
                }
            };
            _packageDetectionSerialPort.ErrorOccurred += (sender, args) => {
                //串口报错
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = $"监测异常:{args.Exception.Message}"
                });
            };
            _packageDetectionTcp.Communication += async (sender, info) => {
                //Tcp接收内容

                if (_stackedPackageDetectionSettingsDto is not null) {
                    try {
                        var isStacked = false;
                        var tryDequeue = _stackedPackageItems.TryDequeue(out var result);
                        if (tryDequeue && result is not null) {
                            isStacked = Regex.IsMatch(info.Content, _stackedPackageDetectionSettingsDto.RegularExpression);
                        }

                        OnStackedPackageReturned(new StackedPackageEventArgs() {
                            PackageInfo = result,
                            PackageTime = result?.CreateTime ?? DateTime.MinValue,
                            IsStacked = isStacked,
                            StackedContent = info.Content
                        });
                    }
                    catch (Exception e) {
                        OnExceptionOccurred(new ExceptionEventArgs() {
                            ExceptionMessage = $"接收内容解析错误:{info.Content}"
                        });
                    }
                }
            };
            _packageDetectionTcp.Exception += (sender, exception) => {
                //Tcp报错
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = $"监测异常:{exception.Message}"
                });
            };
            _packageDetectionTcp.ConnectionException += (sender, s) => {
                //Tcp连接报错
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = $"监测Tcp连接异常:{s}"
                });
            };
            EventAggregator.Instance.Subscribe<TriggerPositionEvent>(async item => {
                if (item is TriggerPositionEvent { TriggerPosition: TriggerPositionEnum.PackageTrigger } info) {
                    _stackedPackageItems.Enqueue(info.PackageInfo ?? new PackageInfo());
                    //NLog.LogManager.GetCurrentClassLogger().Error($"队列数:{_stackedPackageItems.Count}");
                }
            });
        }

        public event EventHandler<EventArgs>? Connected;

        public event EventHandler<EventArgs>? Disconnected;

        public event EventHandler<StackedPackageEventArgs>? StackedPackageReturned;

        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        public bool IsConnected { get; private set; }

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken token = default) {
            _stackedPackageDetectionSettingsDto = await _configRepository.FirstOrDefaultEntity<StackedPackageDetectionSettingsDto>(
                "StackedPackageDetectionSettings", token) ?? new StackedPackageDetectionSettingsDto();
            //创建清理线程
            if (_clearThread is null && _stackedPackageDetectionSettingsDto.IsStackedPackageDetection) {
                _cancellationTokenSource = new CancellationTokenSource();
                _clearThread = Task.Run(async () => {
                    while (!_cancellationTokenSource.IsCancellationRequested) {
                        while (_stackedPackageItems.TryPeek(out var oldestPackage) &&
                               DateTime.Now.Subtract(oldestPackage.CreateTime).TotalMilliseconds >
                               _stackedPackageDetectionSettingsDto.Timeout) {
                            _stackedPackageItems.TryDequeue(out _);
                        }

                        await Task.Delay(20, token);
                    }
                }, token);
            }

            //连接叠包
            if (_stackedPackageDetectionSettingsDto.IsStackedPackageDetection) {
                if (_stackedPackageDetectionSettingsDto.CommunicationType == CommunicationsType.SerialPort) {
                    //串口
                    if (_stackedPackageDetectionSettingsDto.SerialPortConfigInfo is null) {
                        OnExceptionOccurred(new ExceptionEventArgs() {
                            ExceptionMessage = "串口参数为空,无法连接"
                        });
                        return new KeyValuePair<bool, string>(false, "串口参数为空,无法连接");
                    }

                    var connect = _packageDetectionSerialPort.Connect(
                        _stackedPackageDetectionSettingsDto.SerialPortConfigInfo.PortName,
                        _stackedPackageDetectionSettingsDto.SerialPortConfigInfo.BaudRate,
                        _stackedPackageDetectionSettingsDto.SerialPortConfigInfo.DataBits,
                        _stackedPackageDetectionSettingsDto.SerialPortConfigInfo.Parity,
                        _stackedPackageDetectionSettingsDto.SerialPortConfigInfo.StopBits,
                        (SerialPortFormat)_stackedPackageDetectionSettingsDto.SerialPortConfigInfo.DataFormat
                    );
                    if (connect) {
                        OnConnected();
                    }
                    else {
                        OnDisconnected();
                    }
                    return new KeyValuePair<bool, string>(connect, $"叠包监测连接:{(connect ? "成功" : "失败")}");
                }
                else if (_stackedPackageDetectionSettingsDto.CommunicationType == CommunicationsType.TCP) {
                    //Tcp
                    if (_stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo is null) {
                        OnExceptionOccurred(new ExceptionEventArgs() {
                            ExceptionMessage = "Tcp参数为空,无法连接"
                        });
                        return new KeyValuePair<bool, string>(false, "Tcp参数为空,无法连接");
                    }
                    if (_stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.ConnectionMode == TcpConnectionMode.Server) {
                        var connect = await _packageDetectionTcp.Connect(
                            _stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.ServerConfig.IpAddress,
                            _stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.ServerConfig.Port,
                            ConnectionType.Server,
                            1000,
                            (FormatType)_stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.DataFormat,
                            0, token);
                        if (connect) {
                            OnConnected();
                        }
                        else {
                            OnDisconnected();
                        }
                        return new KeyValuePair<bool, string>(connect, $"叠包监测连接:{(connect ? "成功" : "失败")}");
                    }
                    else if (_stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.ConnectionMode == TcpConnectionMode.Client) {
                        var connect = await _packageDetectionTcp.Connect(
                            _stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.ClientConfig.IpAddress,
                            _stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.ClientConfig.Port,
                            ConnectionType.Client,
                            1000,
                            (FormatType)_stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.DataFormat,
                            0, token);
                        if (connect) {
                            OnConnected();
                        }
                        else {
                            OnDisconnected();
                        }
                        return new KeyValuePair<bool, string>(connect, $"叠包监测连接:{(connect ? "成功" : "失败")}");
                    }
                }
                return new KeyValuePair<bool, string>(false, "未知连接方式");
            }
            else {
                return new KeyValuePair<bool, string>(true, "无需监测叠包状态");
            }
        }

        public async Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default) {
            try {
                //停止线程
                await Task.Yield();
                _cancellationTokenSource?.Cancel();
                if (_clearThread != null) {
                    await _clearThread;
                    _clearThread?.Dispose();
                }

                _clearThread = null;
                if (_packageDetectionTcp.ConnectionStatus == ConnectionStatus.Connected) {
                    _packageDetectionTcp.Close();
                }

                if (_packageDetectionSerialPort.Status == SerialPortStatus.Running) {
                    _packageDetectionTcp.Close();
                }
                OnDisconnected();
                return new KeyValuePair<bool, string>(true, "停止成功");
            }
            catch (Exception e) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = $"监测连接停止异常:{e}"
                });
            }

            return new KeyValuePair<bool, string>(false, "停止失败");
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            throw new NotImplementedException();
        }

        protected virtual async void OnStackedPackageReturned(StackedPackageEventArgs e) {
            await Task.Yield();
            if (e.IsStacked) {
                EventAggregator.Instance.Publish(new SortingLogInfoModel {
                    CreateTime = DateTime.Now,
                    Message = $"包裹:[{e.PackageInfo?.Guid:X4}]-[{e.PackageInfo?.BarCodeInfo?.Barcode}],叠包",
                    Type = LogType.Information
                });
            }

            StackedPackageReturned?.Invoke(this, e);
        }

        protected virtual async void OnExceptionOccurred(ExceptionEventArgs e) {
            await Task.Yield();
            ExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnConnected() {
            await Task.Yield();
            Connected?.Invoke(this, EventArgs.Empty);
        }

        protected virtual async void OnDisconnected() {
            await Task.Yield();
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}