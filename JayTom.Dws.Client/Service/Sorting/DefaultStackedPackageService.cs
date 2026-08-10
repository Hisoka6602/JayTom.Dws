using JayTom.Dws.Application.Configuration;
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
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using JayTom.Dws.Plugin.SerialPort;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Dto.PackageExitLockDto;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.Service.Sorting.Communication.TcpComm;
using JayTom.Dws.Client.Service.Sorting.Communication.SerialComm;
using TriggerPositionEvent = JayTom.Dws.Client.EventMediators.TriggerPositionEvent;

namespace JayTom.Dws.Client.Service.Sorting
{

    public class DefaultStackedPackageService : IStackedPackageService
    {
        private readonly ISettingsStore _settingsStore;
        private readonly IPackageDetectionSerialPort _packageDetectionSerialPort;
        private readonly IPackageDetectionTcp _packageDetectionTcp;
        private StackedPackageDetectionSettingsDto? _stackedPackageDetectionSettingsDto = new();
        private readonly Queue<PackageInfo> _stackedPackageItems = new();
        private readonly System.Threading.Lock _queueSync = new();
        private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
        private Task? _clearThread;
        private CancellationTokenSource? _cancellationTokenSource;
        private Regex? _stackedRegex;
        private int _isConnected;

        public DefaultStackedPackageService(ISettingsStore settingsStore,
            IPackageDetectionSerialPort packageDetectionSerialPort, IPackageDetectionTcp packageDetectionTcp)
        {
            _settingsStore = settingsStore;
            _packageDetectionSerialPort = packageDetectionSerialPort;
            _packageDetectionTcp = packageDetectionTcp;
            _packageDetectionSerialPort.DataReceived += (sender, args) =>
            {
                //串口接收内容
                var regex = Volatile.Read(ref _stackedRegex);
                if (regex is not null)
                {
                    try
                    {
                        var isStacked = false;
                        PackageInfo? result = null;
                        lock (_queueSync)
                        {
                            if (_stackedPackageItems.Count > 0)
                            {
                                result = _stackedPackageItems.Dequeue();
                            }
                        }
                        if (result is not null)
                        {
                            isStacked = regex.IsMatch(args.AsciiMessage);
                        }

                        OnStackedPackageReturned(new StackedPackageEventArgs()
                        {
                            PackageInfo = result,
                            PackageTime = result?.CreateTime ?? DateTime.MinValue,
                            IsStacked = isStacked,
                            StackedContent = args.AsciiMessage
                        });
                    }
                    catch (Exception e)
                    {
                        OnExceptionOccurred(new ExceptionEventArgs()
                        {
                            ExceptionMessage = $"接收内容解析错误:{args.AsciiMessage}"
                        });
                    }
                }
            };
            _packageDetectionSerialPort.ErrorOccurred += (sender, args) =>
            {
                //串口报错
                OnExceptionOccurred(new ExceptionEventArgs()
                {
                    ExceptionMessage = $"监测异常:{args.Exception.Message}"
                });
            };
            _packageDetectionTcp.Communication += (sender, info) =>
            {
                //Tcp接收内容

                var regex = Volatile.Read(ref _stackedRegex);
                if (regex is not null)
                {
                    try
                    {
                        var isStacked = false;
                        PackageInfo? result = null;
                        lock (_queueSync)
                        {
                            if (_stackedPackageItems.Count > 0)
                            {
                                result = _stackedPackageItems.Dequeue();
                            }
                        }
                        if (result is not null)
                        {
                            isStacked = regex.IsMatch(info.Content);
                        }

                        OnStackedPackageReturned(new StackedPackageEventArgs()
                        {
                            PackageInfo = result,
                            PackageTime = result?.CreateTime ?? DateTime.MinValue,
                            IsStacked = isStacked,
                            StackedContent = info.Content
                        });
                    }
                    catch (Exception e)
                    {
                        OnExceptionOccurred(new ExceptionEventArgs()
                        {
                            ExceptionMessage = $"接收内容解析错误:{info.Content}"
                        });
                    }
                }
            };
            _packageDetectionTcp.Exception += (sender, exception) =>
            {
                //Tcp报错
                OnExceptionOccurred(new ExceptionEventArgs()
                {
                    ExceptionMessage = $"监测异常:{exception.Message}"
                });
            };
            _packageDetectionTcp.ConnectionException += (sender, s) =>
            {
                //Tcp连接报错
                OnExceptionOccurred(new ExceptionEventArgs()
                {
                    ExceptionMessage = $"监测Tcp连接异常:{s}"
                });
            };
            EventAggregator.Instance.Subscribe<TriggerPositionEvent>(item =>
            {
                if (item is TriggerPositionEvent { TriggerPosition: TriggerPositionEnum.PackageTrigger } info)
                {
                    if (Volatile.Read(ref _stackedRegex) is not null)
                    {
                        lock (_queueSync)
                        {
                            _stackedPackageItems.Enqueue(info.PackageInfo ?? new PackageInfo());
                        }
                    }
                    //NLog.LogManager.GetCurrentClassLogger().Error($"队列数:{_stackedPackageItems.Count}");
                }
            });
        }

        public event EventHandler<EventArgs>? Connected;

        public event EventHandler<EventArgs>? Disconnected;

        public event EventHandler<StackedPackageEventArgs>? StackedPackageReturned;

        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        public bool IsConnected => Volatile.Read(ref _isConnected) != 0;

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken token = default)
        {
            await _lifecycleGate.WaitAsync(token);
            try
            {
                return await StartCore(token);
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }

        private async Task<KeyValuePair<bool, string>> StartCore(CancellationToken token)
        {
            _stackedPackageDetectionSettingsDto = await _settingsStore.GetAsync<StackedPackageDetectionSettingsDto>(
                "StackedPackageDetectionSettings", token) ?? new StackedPackageDetectionSettingsDto();
            Volatile.Write(
                ref _stackedRegex,
                _stackedPackageDetectionSettingsDto.IsStackedPackageDetection
                    ? new Regex(
                        _stackedPackageDetectionSettingsDto.RegularExpression,
                        RegexOptions.Compiled | RegexOptions.CultureInvariant,
                        TimeSpan.FromMilliseconds(100))
                    : null);
            //创建清理线程
            if (_clearThread is null && _stackedPackageDetectionSettingsDto.IsStackedPackageDetection)
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                var clearToken = _cancellationTokenSource.Token;
                var timeout = _stackedPackageDetectionSettingsDto.Timeout;
                _clearThread = Task.Run(async () =>
                {
                    while (!clearToken.IsCancellationRequested)
                    {
                        lock (_queueSync)
                        {
                            while (_stackedPackageItems.Count > 0 &&
                                   DateTime.Now.Subtract(_stackedPackageItems.Peek().CreateTime)
                                       .TotalMilliseconds > timeout)
                            {
                                _stackedPackageItems.Dequeue();
                            }
                        }

                        await Task.Delay(20, clearToken);
                    }
                }, clearToken);
            }

            //连接叠包
            if (_stackedPackageDetectionSettingsDto.IsStackedPackageDetection)
            {
                if (_stackedPackageDetectionSettingsDto.CommunicationType == CommunicationsType.SerialPort)
                {
                    //串口
                    if (_stackedPackageDetectionSettingsDto.SerialPortConfigInfo is null)
                    {
                        OnExceptionOccurred(new ExceptionEventArgs()
                        {
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
                    if (connect)
                    {
                        OnConnected();
                    }
                    else
                    {
                        OnDisconnected();
                    }
                    return new KeyValuePair<bool, string>(connect, $"叠包监测连接:{(connect ? "成功" : "失败")}");
                }
                else if (_stackedPackageDetectionSettingsDto.CommunicationType == CommunicationsType.TCP)
                {
                    //Tcp
                    if (_stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo is null)
                    {
                        OnExceptionOccurred(new ExceptionEventArgs()
                        {
                            ExceptionMessage = "Tcp参数为空,无法连接"
                        });
                        return new KeyValuePair<bool, string>(false, "Tcp参数为空,无法连接");
                    }
                    if (_stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.ConnectionMode == TcpConnectionMode.Server)
                    {
                        var connect = await _packageDetectionTcp.Connect(
                            _stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.ServerConfig.IpAddress,
                            _stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.ServerConfig.Port,
                            ConnectionType.Server,
                            1000,
                            (FormatType)_stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.DataFormat,
                            0, token);
                        if (connect)
                        {
                            OnConnected();
                        }
                        else
                        {
                            OnDisconnected();
                        }
                        return new KeyValuePair<bool, string>(connect, $"叠包监测连接:{(connect ? "成功" : "失败")}");
                    }
                    else if (_stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.ConnectionMode == TcpConnectionMode.Client)
                    {
                        var connect = await _packageDetectionTcp.Connect(
                            _stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.ClientConfig.IpAddress,
                            _stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.ClientConfig.Port,
                            ConnectionType.Client,
                            1000,
                            (FormatType)_stackedPackageDetectionSettingsDto.TcpConnectionConfigInfo.DataFormat,
                            0, token);
                        if (connect)
                        {
                            OnConnected();
                        }
                        else
                        {
                            OnDisconnected();
                        }
                        return new KeyValuePair<bool, string>(connect, $"叠包监测连接:{(connect ? "成功" : "失败")}");
                    }
                }
                return new KeyValuePair<bool, string>(false, "未知连接方式");
            }
            else
            {
                return new KeyValuePair<bool, string>(true, "无需监测叠包状态");
            }
        }

        public async Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default)
        {
            await _lifecycleGate.WaitAsync(token);
            try
            {
                //停止线程
                _cancellationTokenSource?.Cancel();
                if (_clearThread != null)
                {
                    try
                    {
                        await _clearThread;
                    }
                    catch (OperationCanceledException) when (_cancellationTokenSource?.IsCancellationRequested == true)
                    {
                        // 正常停止后台清理任务。
                    }
                    _clearThread?.Dispose();
                }

                _clearThread = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                Volatile.Write(ref _stackedRegex, null);
                lock (_queueSync)
                {
                    _stackedPackageItems.Clear();
                }
                if (_packageDetectionTcp.ConnectionStatus == ConnectionStatus.Connected)
                {
                    _packageDetectionTcp.Close();
                }

                if (_packageDetectionSerialPort.Status == SerialPortStatus.Running)
                {
                    _packageDetectionSerialPort.Dispose();
                }
                OnDisconnected();
                return new KeyValuePair<bool, string>(true, "停止成功");
            }
            catch (Exception e)
            {
                OnExceptionOccurred(new ExceptionEventArgs()
                {
                    ExceptionMessage = $"监测连接停止异常:{e}"
                });
            }
            finally
            {
                _lifecycleGate.Release();
            }

            return new KeyValuePair<bool, string>(false, "停止失败");
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnStackedPackageReturned(StackedPackageEventArgs e)
        {
            if (e.IsStacked)
            {
                EventAggregator.Instance.Publish(new SortingLogInfoModel
                {
                    CreateTime = DateTime.Now,
                    Message = $"包裹:[{e.PackageInfo?.Guid:X4}]-[{e.PackageInfo?.BarCodeInfo?.Barcode}],叠包",
                    Type = LogType.Information
                });
            }

            StackedPackageReturned?.Invoke(this, e);
        }

        protected virtual void OnExceptionOccurred(ExceptionEventArgs e)
        {
            ExceptionOccurred?.Invoke(this, e);
        }

        protected virtual void OnConnected()
        {
            Interlocked.Exchange(ref _isConnected, 1);
            Connected?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDisconnected()
        {
            Interlocked.Exchange(ref _isConnected, 0);
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}
