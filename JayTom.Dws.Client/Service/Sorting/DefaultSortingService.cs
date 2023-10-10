using System;
using DryIoc;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Service.Sorting.Communication.TcpComm;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Service.Sorting.Communication.SerialComm;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;

namespace JayTom.Dws.Client.Service.Sorting {

    public class DefaultSortingService : ISortingService {
        private readonly IConfigRepository _configRepository;
        private readonly ISortingSerialPort _sortingSerialPort;
        private readonly ISortingTcp _sortingTcp;
        private readonly ILogisticsRegexRepository _logisticsRegexRepository;
        private readonly ILogisticsCodeRecognitionRepository _logisticsCodeRecognitionRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private SemaphoreSlim _semaphore = new(1);
        private CommunicationsSettingsDto _communicationsSettingsDto = new();
        private List<LogisticsRegexInfoModel>? _logisticsRegexInfos = new();
        private List<LogisticsCodeRecognitionInfoModel>? _logisticsCodeRecognitionInfos = new();
        private List<PackageExitDefinitionInfoModel>? _packageExitDefinitionInfos = new();

        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        public event EventHandler<RetryEventArgs>? RetryOccurred;

        public event EventHandler<LogEventArgs>? LogReceived;

        public event EventHandler<Exception>? HeartbeatError;

        public event EventHandler<ExceptionEventArgs>? SendError;

        public DefaultSortingService(IConfigRepository configRepository,
            ISortingSerialPort sortingSerialPort,
            ISortingTcp sortingTcp, ILogisticsRegexRepository logisticsRegexRepository,
            ILogisticsCodeRecognitionRepository logisticsCodeRecognitionRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository) {
            _configRepository = configRepository;
            _sortingSerialPort = sortingSerialPort;
            _sortingTcp = sortingTcp;
            _logisticsRegexRepository = logisticsRegexRepository;
            _logisticsCodeRecognitionRepository = logisticsCodeRecognitionRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            //事件
            _sortingSerialPort.Disconnected += delegate (object? sender, ISortingSerialPort port) {
                IsConnected = false;
            };
            _sortingSerialPort.ConnectionChanged += delegate (object? sender, ISortingSerialPort port) {
                IsConnected = true;
            };
            _sortingSerialPort.ErrorOccurred +=
                delegate (object? sender, Communication.SerialComm.ExceptionEventArgs args) {
                    OnExceptionOccurred(new ExceptionEventArgs() {
                        ExceptionMessage = args.Exception?.ToString() ?? string.Empty
                    });
                };
            _sortingSerialPort.DataReceived += delegate (object? sender, MessageEventArgs args) {
                //接收的数据
            };
            _sortingSerialPort.HeartbeatError += delegate (object? sender, Exception exception) {
                OnHeartbeatError(exception);
            };
            _sortingSerialPort.SendError += delegate (object? sender, Communication.SerialComm.ExceptionEventArgs args) {
                OnSendError(new ExceptionEventArgs() {
                    ExceptionMessage = args.Exception.Message
                });
            };
            //TCP
            _sortingTcp.Exception += delegate (object? sender, Exception exception) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = exception.ToString() ?? string.Empty
                });
            };
            _sortingTcp.Disconnected += delegate (object? sender, string s) {
                IsConnected = false;
            };
            _sortingTcp.Connected += delegate (object? sender, string s) {
                IsConnected = true;
            };
            _sortingTcp.ConnectionException += delegate (object? sender, string s) {
                IsConnected = false;
            };
            _sortingTcp.Communication += delegate (object? sender, CommunicationInfo info) {
                if (info.Type == CommunicationType.Receive) {
                    //接收消息
                }
            };
            _sortingTcp.HeartbeatError += delegate (object? sender, Exception exception) {
                OnHeartbeatError(exception);
            };
            _sortingTcp.SendError += delegate (object? sender, Exception exception) {
                OnSendError(new ExceptionEventArgs() {
                    ExceptionMessage = exception.Message
                });
            };
            //判断通讯方式
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async settings => {
                if (settings is SettingsChangedEvent { SettingsName: "CommunicationsSettings" }) {
                    try {
                        await _semaphore.WaitAsync();

                        var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("CommunicationsSettings"));
                        _communicationsSettingsDto =
                            JsonConvert.DeserializeObject<CommunicationsSettingsDto>(configInfoModel.Value) ?? new CommunicationsSettingsDto();
                        if (_communicationsSettingsDto is not null) {
                            IsSortingEnabled = _communicationsSettingsDto.Type != CommunicationsType.None;
                            await Disconnect();
                        }
                    }
                    catch (Exception e) {
                        OnExceptionOccurred(new ExceptionEventArgs() {
                            ExceptionMessage = e.Message
                        });
                    }
                    finally {
                        _semaphore.Release();
                    }
                }
            });
            //格口更改
            EventAggregator.Instance.Subscribe<LogisticsCodeRecognitionInfoModel>(async models => {
                _logisticsCodeRecognitionInfos = await _logisticsCodeRecognitionRepository.Select(s => s.Id > 0,
                    o => o.Id);
            });
            EventAggregator.Instance.Subscribe<LogisticsRegexInfoModel>(async models => {
                _logisticsRegexInfos = await _logisticsRegexRepository.Select(s => s.Id > 0,
                    o => o.CreateTime);
            });
            EventAggregator.Instance.Subscribe<PackageExitDefinitionInfoModel>(async models => {
                _packageExitDefinitionInfos = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                    o => o.Id);
            });
            //触发初始化
            EventAggregator.Instance.Publish(new SettingsChangedEvent {
                SettingsName = "CommunicationsSettings"
            });
        }

        public async void ExecuteSorting(string barCode, DateTime scanTime, object sortingType, string apiResponseContent) {
            //需要传入更多元素、重量、体积、
            //获取分拣判断类型[物流、Ocr、接口、条码]
            //获取分拣格口
            //发送指令(当所有条件都不满足,则判断有无异常口,如果有则发送异常口指令)
            throw new NotImplementedException();
        }

        public async void SendInstructions(List<string> instructions, TimeSpan interval) {
            //判断是否连接，如果未连接则连接
            if (!IsConnected) {
                await Connect(false);
            }
            if (_communicationsSettingsDto.Type == CommunicationsType.SerialPort) {
                //串口
                if (_sortingSerialPort.Status == SortingSerialPortStatus.Running &&
                    instructions?.Any() == true) {
                    foreach (var instruction in instructions) {
                        _sortingSerialPort.Send(instruction);

                        await Task.Delay(interval);
                    }
                }
                else {
                    OnExceptionOccurred(new ExceptionEventArgs() {
                        ExceptionMessage = "下位机未连接!"
                    });
                }
            }
            else if (_communicationsSettingsDto.Type == CommunicationsType.TCP) {
                //tcp
                if (_sortingTcp.ConnectionStatus == ConnectionStatus.Connected &&
                    instructions?.Any() == true) {
                    foreach (var instruction in instructions) {
                        await _sortingTcp.SendMessage(instruction);
                        await Task.Delay(interval);
                    }
                }
                else {
                    OnExceptionOccurred(new ExceptionEventArgs() {
                        ExceptionMessage = "下位机未连接!"
                    });
                }
            }
        }

        public bool IsConnected { get; private set; } = false;
        public bool IsSortingEnabled { get; private set; }

        public async Task<LogisticsCodeRecognitionInfoModel?> GetLogisticsInfo(string barCode) {
            if (_logisticsCodeRecognitionInfos?.Any() != true) {
                _logisticsCodeRecognitionInfos = await _logisticsCodeRecognitionRepository.
                     Select(s => s.Id > 0,
                         o => o.Id);
            }

            if (_logisticsRegexInfos?.Any() != true) {
                _logisticsRegexInfos = await _logisticsRegexRepository.Select(s => s.Id > 0,
                    o => o.CreateTime);
            }

            if (_logisticsRegexInfos?.Any() == true) {
                var logisticsRegexInfoModel = _logisticsRegexInfos.FirstOrDefault(f => {
                    try {
                        var isMatch = Regex.IsMatch(barCode, f.RegexPattern);
                        if (isMatch) {
                            return true;
                        }
                    }
                    catch (Exception e) {
                        return false;
                    }

                    return false;
                });
                if (logisticsRegexInfoModel is not null) {
                    return _logisticsCodeRecognitionInfos?.FirstOrDefault(f =>
                        f.Id.Equals(logisticsRegexInfoModel.LogisticsId));
                }
            }

            return null;
        }

        public bool RunningStatus { get; private set; }

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken token = default) {
            //连接
            //心跳包
            if (!RunningStatus) {
                await Disconnect();
                await Connect();
                RunningStatus = true;
                return new KeyValuePair<bool, string>(true, "已启动");
            }
            return new KeyValuePair<bool, string>(true, "已启动,无需再次启动");
        }

        public async Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default) {
            //停止
            //关闭心跳包
            await Disconnect();
            RunningStatus = false;
            return new KeyValuePair<bool, string>(true, "已停止");
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        private async Task Connect(bool isUseHeartbeat = true) {
            if (_communicationsSettingsDto.Type == CommunicationsType.SerialPort) {
                //连接
                _sortingSerialPort.Connect(_communicationsSettingsDto.SerialPortSettingsInfo.PortName,
                    _communicationsSettingsDto.SerialPortSettingsInfo.BaudRate,
                    _communicationsSettingsDto.SerialPortSettingsInfo.DataBits,
                    _communicationsSettingsDto.SerialPortSettingsInfo.Parity,
                    _communicationsSettingsDto.SerialPortSettingsInfo.StopBits,
                    (SortingSerialPortFormat)_communicationsSettingsDto.SerialPortSettingsInfo
                        .DataFormat);
                //心跳包
                if (isUseHeartbeat) {
                    if (_communicationsSettingsDto.HeartbeatInfo.IsHeartbeatEnabled) {
                        _sortingSerialPort.StartHeartbeat(_communicationsSettingsDto.HeartbeatInfo.HeartbeatData, TimeSpan.FromMilliseconds(_communicationsSettingsDto.HeartbeatInfo.HeartbeatInterval));
                    }
                }
            }
            else if (_communicationsSettingsDto.Type == CommunicationsType.TCP) {
                if (_communicationsSettingsDto.TcpSettingsInfo.ConnectionMode ==
                    TcpConnectionMode.Server) {
                    await _sortingTcp.Connect(
                        _communicationsSettingsDto.TcpSettingsInfo.ServerConfig.IpAddress,
                        _communicationsSettingsDto.TcpSettingsInfo.ServerConfig.Port,
                        ConnectionType.Server);
                }
                else {
                    await _sortingTcp.Connect(
                        _communicationsSettingsDto.TcpSettingsInfo.ClientConfig.IpAddress,
                        _communicationsSettingsDto.TcpSettingsInfo.ClientConfig.Port,
                        ConnectionType.Client);
                }
                //心跳包
                if (isUseHeartbeat) {
                    if (_communicationsSettingsDto.HeartbeatInfo.IsHeartbeatEnabled) {
                        _sortingTcp.StartHeartbeat(_communicationsSettingsDto.HeartbeatInfo.HeartbeatData, TimeSpan.FromMilliseconds(_communicationsSettingsDto.HeartbeatInfo.HeartbeatInterval));
                    }
                }
            }
        }

        /// <summary>
        /// 断开
        /// </summary>
        /// <returns></returns>
        private async Task Disconnect() {
            //断开全部通讯
            //串口通讯
            if (_sortingSerialPort.Status == SortingSerialPortStatus.Running) {
                _sortingSerialPort.Dispose();
                await Task.Delay(600);
            }
            //Tcp通讯
            if (_sortingTcp.ConnectionStatus == ConnectionStatus.Connected) {
                _sortingTcp.Dispose();
                await Task.Delay(600);
            }
        }

        protected virtual async void OnExceptionOccurred(ExceptionEventArgs e) {
            await Task.Yield();
            ExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnHeartbeatError(Exception e) {
            await Task.Yield();
            HeartbeatError?.Invoke(this, e);
        }

        protected virtual async void OnSendError(ExceptionEventArgs e) {
            await Task.Yield();
            SendError?.Invoke(this, e);
        }
    }
}