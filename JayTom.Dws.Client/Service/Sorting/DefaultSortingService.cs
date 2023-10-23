using Polly;
using System;
using DryIoc;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Text.Json;
using JayTom.Dws.Interface;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Windows.Controls;
using System.Linq.Dynamic.Core;
using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Service.Sorting.Communication.TcpComm;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Service.Sorting.Communication.SerialComm;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.DownstreamProtocols.CommunicationProtocols;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.RuleConfig;

namespace JayTom.Dws.Client.Service.Sorting {

    public class DefaultSortingService : ISortingService {
        private readonly IConfigRepository _configRepository;
        private readonly ISortingSerialPort _sortingSerialPort;
        private readonly ISortingTcp _sortingTcp;
        private readonly ILogisticsRegexRepository _logisticsRegexRepository;
        private readonly ILogisticsCodeRecognitionRepository _logisticsCodeRecognitionRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly IBarCodeSortingRepository _barCodeSortingRepository;
        private readonly IBarCodeRegexRepository _barCodeRegexRepository;
        private readonly ILogisticsSortingRepository _logisticsSortingRepository;
        private readonly ILogisticsRuleRepository _logisticsRuleRepository;
        private readonly IOcrSortingRepository _ocrSortingRepository;
        private readonly IOcrRuleRepository _ocrRuleRepository;
        private readonly ISortingInstructionBindingRepository _sortingInstructionBindingRepository;
        private readonly ISortingInstructionRepository _sortingInstructionRepository;
        private readonly IVolumeSortingRepository _volumeSortingRepository;
        private readonly IWeightSortingRepository _weightSortingRepository;
        private readonly IVolumeRuleRepository _volumeRuleRepository;
        private readonly IWeightRuleRepository _weightRuleRepository;
        private readonly IApiSortingRepository _apiSortingRepository;
        private readonly IApiRuleRepository _apiRuleRepository;
        private IDeviceCommunicationProtocol? _deviceCommunicationProtocol = null;
        private SemaphoreSlim _semaphore = new(1);
        private CommunicationsSettingsDto _communicationsSettingsDto = new();
        private SortingMethodDto _sortingMethodDto = new();
        private ConcurrentQueue<string> _replyContentQueue = new();

        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        #region 配置

        private List<LogisticsRegexInfoModel> _logisticsRegexInfos = new();
        private List<LogisticsCodeRecognitionInfoModel> _logisticsCodeRecognitionInfos = new();
        private List<PackageExitDefinitionInfoModel> _packageExitDefinitionInfos = new();
        private List<BarCodeSortingInfoModel> _barCodeSortingInfoModels = new();
        private List<BarCodeRegexInfoModel> _barCodeRegexInfos = new();
        private List<LogisticsSortingInfoModel> _logisticsSortingInfoModels = new();
        private List<LogisticsRuleInfoModel> _logisticsRuleInfoModels = new();
        private List<OcrSortingInfoModel> _ocrSortingInfoModels = new();
        private List<OcrRuleInfoModel> _ocrRuleInfoModels = new();
        private List<SortingInstructionBindingInfoModel> _sortingInstructionBindingInfoModels = new();
        private List<SortingInstructionInfoModel> _sortingInstructionInfoModels = new();
        private List<VolumeSortingInfoModel> _volumeSortingInfoModels = new();
        private List<WeightSortingInfoModel> _weightSortingInfoModels = new();
        private List<VolumeRuleInfoModel> _volumeRuleInfoModels = new();
        private List<WeightRuleInfoModel> _weightRuleInfoModels = new();
        private List<ApiRuleInfoModel> _apiRuleInfoModels = new();
        private List<ApiSortingInfoModel> _apiSortingInfoModels = new();

        #endregion 配置

        public event EventHandler<RetryEventArgs>? RetryOccurred;

        public event EventHandler<LogEventArgs>? LogReceived;

        public event EventHandler<Exception>? HeartbeatError;

        public event EventHandler<ExceptionEventArgs>? SendError;

        public event EventHandler<string>? CreatePackageEvent;

        public event EventHandler<string>? RemovePackageEvent;

        public event EventHandler<string>? ClearExceptionEvent;

        public DefaultSortingService(IConfigRepository configRepository,
            ISortingSerialPort sortingSerialPort,
            ISortingTcp sortingTcp, ILogisticsRegexRepository logisticsRegexRepository,
            ILogisticsCodeRecognitionRepository logisticsCodeRecognitionRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IBarCodeSortingRepository barCodeSortingRepository,
            IBarCodeRegexRepository barCodeRegexRepository,
            ILogisticsSortingRepository logisticsSortingRepository,
            ILogisticsRuleRepository logisticsRuleRepository,
            IOcrSortingRepository ocrSortingRepository,
            IOcrRuleRepository ocrRuleRepository,
            ISortingInstructionBindingRepository sortingInstructionBindingRepository,
            ISortingInstructionRepository sortingInstructionRepository,
            IVolumeSortingRepository volumeSortingRepository,
            IWeightSortingRepository weightSortingRepository,
            IVolumeRuleRepository volumeRuleRepository,
            IWeightRuleRepository weightRuleRepository,
            IApiSortingRepository apiSortingRepository,
            IApiRuleRepository apiRuleRepository) {
            _configRepository = configRepository;
            _sortingSerialPort = sortingSerialPort;
            _sortingTcp = sortingTcp;
            _logisticsRegexRepository = logisticsRegexRepository;
            _logisticsCodeRecognitionRepository = logisticsCodeRecognitionRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _barCodeSortingRepository = barCodeSortingRepository;
            _barCodeRegexRepository = barCodeRegexRepository;
            _logisticsSortingRepository = logisticsSortingRepository;
            _logisticsRuleRepository = logisticsRuleRepository;
            _ocrSortingRepository = ocrSortingRepository;
            _ocrRuleRepository = ocrRuleRepository;
            _sortingInstructionBindingRepository = sortingInstructionBindingRepository;
            _sortingInstructionRepository = sortingInstructionRepository;
            _volumeSortingRepository = volumeSortingRepository;
            _weightSortingRepository = weightSortingRepository;
            _volumeRuleRepository = volumeRuleRepository;
            _weightRuleRepository = weightRuleRepository;
            _apiSortingRepository = apiSortingRepository;
            _apiRuleRepository = apiRuleRepository;
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
                if (_deviceCommunicationProtocol is not null) {
                    var deviceDecodeResult = _deviceCommunicationProtocol.DecodeData(args.AsciiMessage);
                    if (deviceDecodeResult is not null) {
                        if (deviceDecodeResult.Type == FunctionType.CreatePackage) {
                            //创建包裹
                            OnCreatePackageEvent(deviceDecodeResult.Keyword);
                        }
                        else if (deviceDecodeResult.Type == FunctionType.RemovePackage) {
                            //移除包裹
                            OnCreatePackageEvent(deviceDecodeResult.Keyword);
                        }
                        else if (deviceDecodeResult.Type == FunctionType.Heartbeat) {
                            //心跳包
                            OnCreatePackageEvent(deviceDecodeResult.Keyword);
                        }
                        else if (deviceDecodeResult.Type == FunctionType.ClearException) {
                            //清空异常
                        }
                    }
                }
                _replyContentQueue.Enqueue(args.AsciiMessage);
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
                    if (_deviceCommunicationProtocol is not null) {
                        var deviceDecodeResult = _deviceCommunicationProtocol.DecodeData(info.Content);
                        if (deviceDecodeResult is not null) {
                            if (deviceDecodeResult.Type == FunctionType.CreatePackage) {
                                //创建包裹
                                OnCreatePackageEvent(deviceDecodeResult.Keyword);
                            }
                            else if (deviceDecodeResult.Type == FunctionType.RemovePackage) {
                                //移除包裹
                                OnCreatePackageEvent(deviceDecodeResult.Keyword);
                            }
                            else if (deviceDecodeResult.Type == FunctionType.Heartbeat) {
                                //心跳包
                                OnCreatePackageEvent(deviceDecodeResult.Keyword);
                            }
                            else if (deviceDecodeResult.Type == FunctionType.ClearException) {
                                //清空异常
                            }
                        }
                    }

                    _replyContentQueue.Enqueue(info.Content);
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
                            JsonConvert.DeserializeObject<CommunicationsSettingsDto>(configInfoModel?.Value ?? string.Empty) ?? new CommunicationsSettingsDto();
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
            //触发方式
            EventAggregator.Instance.Subscribe<PackageInfo>(async item => {
                if (item is PackageInfo model) {
                    await Task.Yield();
                    //不包含Api
                    if (_sortingMethodDto.SortMode != SortMode.None &&
                        _sortingMethodDto.SortMode != SortMode.ApiResponseSorting &&
                        _sortingMethodDto.SortMode != SortMode.CombinedWorkflowSorting) {
                        ExecuteSorting(new SortingParam() {
                            Guid = model.Guid,
                            BarCode = model.BarCode ?? string.Empty,
                            Height = (float)(model.Height ?? 0),
                            Length = (float)(model.Length ?? 0),
                            ScanTime = model.ScanTime,
                            Volume = (float)(model.Volume ?? 0),
                            Width = (float)(model.Width ?? 0),
                            Weight = (float)(model.Weight ?? 0),
                            //三段码未完成
                            OcrCode = string.Empty,
                        });
                    }
                }
            });
            //Api触发
            EventAggregator.Instance.Subscribe<ApiResponseReceived>(async item => {
                if (item is ApiResponseReceived model) {
                    if (_sortingMethodDto.SortMode == SortMode.ApiResponseSorting) {
                        ExecuteSorting(new SortingParam() {
                            Guid = model.Guid,
                            BarCode = model.Barcode ?? string.Empty,
                            ScanTime = model.ScanTime,
                            OcrCode = string.Empty,
                            ApiResponse = model.UploadResponse ?? new UploadResponse()
                        });
                    }
                }
            });
            //
            //触发初始化
            /*EventAggregator.Instance.Publish(new SettingsChangedEvent {
                SettingsName = "CommunicationsSettings"
            });*/
        }

        public void ExecuteSorting(SortingParam param, CancellationToken token = default) {
            if (_sortingMethodDto is not null) {
                switch (_sortingMethodDto.SortMode) {
                    case SortMode.BarcodeSorting:
                        BarcodeSorting(param.BarCode, param.Guid, token);
                        break;

                    case SortMode.WeightSorting:
                        WeightSorting(param.Weight, param.Guid, token);
                        break;

                    case SortMode.VolumeSorting:
                        VolumeSorting(param.Length, param.Width, param.Height, param.Volume, param.Guid, token);
                        break;

                    case SortMode.LogisticsSorting:
                        LogisticsSorting(param.BarCode, param.Guid, token);
                        break;

                    case SortMode.OcrSorting:
                        OcrSorting(param.OcrCode, param.Guid, token);
                        break;

                    case SortMode.ApiResponseSorting:
                        ApiResponseSorting(param.ApiResponse, param.Guid, token);
                        break;

                    case SortMode.CombinedWorkflowSorting:
                        CombinedWorkflowSorting(param.Guid, token);
                        break;

                    default:
                        break;
                }
            }
        }

        public async void SendInstructions(long grid, List<string> instructions, TimeSpan interval) {
            //判断是否连接，如果未连接则连接
            if (!IsConnected) {
                await Connect(false);
            }
            if (_communicationsSettingsDto.Type == CommunicationsType.SerialPort) {
                //串口
                if (_sortingSerialPort.Status == SortingSerialPortStatus.Running &&
                    instructions?.Any() == true) {
                    foreach (var instruction in instructions) {
                        //效验协议

                        var message = instruction;
                        if (_deviceCommunicationProtocol is not null) {
                            message = _deviceCommunicationProtocol.EncodeData(FunctionType.SendExit, (int)grid,
                                instruction, null);
                        }
                        _sortingSerialPort.Send(message);

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
                        //效验协议

                        var message = instruction;
                        if (_deviceCommunicationProtocol is not null) {
                            message = _deviceCommunicationProtocol.EncodeData(FunctionType.SendExit, (int)grid,
                                instruction, null);
                        }

                        var sendMessage = await _sortingTcp.SendMessage(message);
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

        public async void SendInstructions(long grid, List<SortingInstructionInfoModel> sortingInstructionInfoModels, TimeSpan interval) {
            //判断是否连接，如果未连接则连接
            if (!IsConnected) {
                await Connect(false);
            }
            if (_communicationsSettingsDto.Type == CommunicationsType.SerialPort) {
                //串口
                if (_sortingSerialPort.Status == SortingSerialPortStatus.Running &&
                    sortingInstructionInfoModels?.Any() == true) {
                    foreach (var instruction in sortingInstructionInfoModels) {
                        if (_communicationsSettingsDto.MachineReplyInfo.IsVerificationEnabled) {
                            var retryPolicy = Policy.HandleResult<bool>(result => !result)
                                .RetryAsync(_communicationsSettingsDto.MachineReplyInfo.MaxRetryCount, (a, b) => {
                                });

                            var executeAsync = await retryPolicy.ExecuteAsync(async () => {
                                //效验协议
                                var message = instruction.Instruction;
                                if (_deviceCommunicationProtocol is not null) {
                                    message = _deviceCommunicationProtocol.EncodeData(FunctionType.SendExit, (int)grid,
                                        instruction.Instruction, null);
                                }
                                _sortingSerialPort.Send(message);
                                return await WaitForReply(instruction.ReplyContent,
                                    TimeSpan.FromMilliseconds(_communicationsSettingsDto.MachineReplyInfo.Timeout));
                            });
                            if (!executeAsync) {
                                OnExceptionOccurred(new ExceptionEventArgs() {
                                    ExceptionMessage = "未收到应答信息!"
                                });
                                break;
                            }
                        }
                        else {
                            //不使用应答
                            var message = instruction.Instruction;
                            if (_deviceCommunicationProtocol is not null) {
                                message = _deviceCommunicationProtocol.EncodeData(FunctionType.SendExit, (int)grid,
                                    instruction.Instruction, null);
                            }
                            _sortingSerialPort.Send(message);
                        }
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
                    sortingInstructionInfoModels?.Any() == true) {
                    foreach (var instruction in sortingInstructionInfoModels) {
                        //使用应答
                        if (_communicationsSettingsDto.MachineReplyInfo.IsVerificationEnabled) {
                            var retryPolicy = Policy.HandleResult<bool>(result => !result)
                               .RetryAsync(_communicationsSettingsDto.MachineReplyInfo.MaxRetryCount, (a, b) => {
                               });

                            var executeAsync = await retryPolicy.ExecuteAsync(async () => {
                                //效验协议
                                var message = instruction.Instruction;
                                if (_deviceCommunicationProtocol is not null) {
                                    message = _deviceCommunicationProtocol.EncodeData(FunctionType.SendExit, (int)grid,
                                        instruction.Instruction, null);
                                }

                                var sendMessage = await _sortingTcp.SendMessage(message);
                                if (sendMessage) {
                                    return await WaitForReply(instruction.ReplyContent,
                                        TimeSpan.FromMilliseconds(_communicationsSettingsDto.MachineReplyInfo.Timeout));
                                }
                                return false;
                            });
                            if (!executeAsync) {
                                OnExceptionOccurred(new ExceptionEventArgs() {
                                    ExceptionMessage = "未收到应答信息!"
                                });
                                break;
                            }
                        }
                        else {
                            //不使用应答

                            //效验协议
                            var message = instruction.Instruction;
                            if (_deviceCommunicationProtocol is not null) {
                                message = _deviceCommunicationProtocol.EncodeData(FunctionType.SendExit, (int)grid,
                                    instruction.Instruction, null);
                            }

                            var sendMessage = await _sortingTcp.SendMessage(message);
                            if (!sendMessage) {
                                OnExceptionOccurred(new ExceptionEventArgs() {
                                    ExceptionMessage = "发送失败!"
                                });
                                break;
                            }
                        }
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

                #region 读取配置信息

                try {
                    _deviceCommunicationProtocol = null;
                    var configInfoModel = await _configRepository.FirstOrDefault(f =>
                        f.ConfigName.Equals("SortingMethodSettings"), token);
                    if (configInfoModel is not null) {
                        _sortingMethodDto = JsonConvert.DeserializeObject<SortingMethodDto>(configInfoModel.Value) ?? new SortingMethodDto();
                    }

                    configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("CommunicationsSettings"), token);
                    _communicationsSettingsDto =
                        JsonConvert.DeserializeObject<CommunicationsSettingsDto>(configInfoModel?.Value ?? string.Empty) ?? new CommunicationsSettingsDto();
                    if (_communicationsSettingsDto is not null) {
                        IsSortingEnabled = _communicationsSettingsDto.Type != CommunicationsType.None;
                        await Disconnect();
                    }
                    _packageExitDefinitionInfos = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                        o => o.Id);

                    _logisticsCodeRecognitionInfos = await _logisticsCodeRecognitionRepository.Select(s => s.Id > 0,
                    o => o.Id, token);
                    _logisticsRegexInfos = await _logisticsRegexRepository.Select(s => s.Id > 0,
                        o => o.CreateTime, token);
                    _barCodeSortingInfoModels = await _barCodeSortingRepository.Select(s => s.Id > 0,
                        o => o.CreateTime, token);
                    _barCodeRegexInfos = await _barCodeRegexRepository.Select(s => s.Id > 0,
                        o => o.Id, token);
                    _logisticsSortingInfoModels = await _logisticsSortingRepository.Select(s => s.Id > 0, o => o.CreateTime, token);
                    _logisticsRuleInfoModels = await _logisticsRuleRepository.Select(s => s.Id > 0,
                        o => o.CreateTime, token);
                    _ocrSortingInfoModels = await _ocrSortingRepository.Select(s => s.Id > 0,
                        o => o.CreateTime, token);
                    _ocrRuleInfoModels = await _ocrRuleRepository.Select(s => s.Id > 0,
                        o => o.CreateTime, token);

                    _sortingInstructionBindingInfoModels = await _sortingInstructionBindingRepository.Select(s => s.Id > 0,
                        o => o.CreateTime, token);
                    _sortingInstructionInfoModels = await _sortingInstructionRepository.Select(s => s.Id > 0,
                        o => o.CreateTime, token);
                    _volumeSortingInfoModels = await _volumeSortingRepository.Select(s => s.Id > 0,
                        o => o.CreateTime, token);
                    _volumeRuleInfoModels = await _volumeRuleRepository.Select(s => s.Id > 0, o => o.CreateTime, token);
                    _weightSortingInfoModels = await _weightSortingRepository.Select(s => s.Id > 0,
                        o => o.CreateTime, token);
                    _weightRuleInfoModels = await _weightRuleRepository.Select(s => s.Id > 0, o => o.CreateTime, token);
                    _apiRuleInfoModels = await _apiRuleRepository.Select(s => s.Id > 0, o => o.CreateTime, token);
                    _apiSortingInfoModels = await _apiSortingRepository.Select(s => s.Id > 0, o => o.CreateTime, token);

                    //读协议
                    if (_communicationsSettingsDto?.Protocol == CommunicationProtocol.Wxkc) {
                        //无限科创协议
                        _deviceCommunicationProtocol = new WxkcCommunicationProtocol();
                    }
                    //其他协议
                }
                catch (Exception e) {
                    OnExceptionOccurred(new ExceptionEventArgs() {
                        ExceptionMessage = $"分拣配置读取异常:{e}"
                    });
                }

                #endregion 读取配置信息

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

        public async void ExceptionSorting(long guid = 0, CancellationToken token = default) {
            var packageExitDefinitionInfoModel = _packageExitDefinitionInfos.FirstOrDefault(f =>
                f is { Type: ExitType.AbnormalExit, IsActive: true });
            if (packageExitDefinitionInfoModel is not null) {
                //执行分拣
                //判断指令提交方式
                var sortingInstructionInfoModels = _sortingInstructionInfoModels.Where(w =>
                        w.InstructionBindingId.Equals(packageExitDefinitionInfoModel.Id))
                    ?.ToList();
                var sortingInstructionBindingInfoModel = _sortingInstructionBindingInfoModels.FirstOrDefault(f =>
                    f.ExitId.Equals(packageExitDefinitionInfoModel.Id));
                if (sortingInstructionBindingInfoModel is not null) {
                    await Task.Delay(sortingInstructionBindingInfoModel.DelaySendMilliseconds, token);
                    SendInstructions(guid, sortingInstructionInfoModels ?? new List<SortingInstructionInfoModel>(),
                        TimeSpan.FromMilliseconds(sortingInstructionBindingInfoModel.SendIntervalMilliseconds));
                }
            }
        }

        public void BarcodeSorting(string barcode, long guid, CancellationToken token = default) {
            try {
                var barCodeRegexInfoModel = _barCodeRegexInfos.FirstOrDefault(f => Regex.IsMatch(barcode, f.RegexPattern));
                if (barCodeRegexInfoModel is not null) {
                    //取出对于格口id
                    var barCodeSortingInfoModel = _barCodeSortingInfoModels.FirstOrDefault(f =>
                        f.Id.Equals(barCodeRegexInfoModel.BarCodeSortingId));
                    if (barCodeSortingInfoModel is not null) {
                        SubSorting(barCodeSortingInfoModel.ExitId, guid, token);
                    }
                    else {
                        //走异常口
                        ExceptionSorting(guid, token);
                    }
                }
                else {
                    //走异常口
                    ExceptionSorting(guid, token);
                }
            }
            catch (Exception e) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void WeightSorting(float weight, long guid, CancellationToken token = default) {
            //取出重量规则
            try {
                var weightRuleInfoModel = _weightRuleInfoModels.FirstOrDefault(f => ValidateWeight(f.Formula, weight));
                if (weightRuleInfoModel is not null) {
                    //取出格口
                    var weightSortingInfoModel = _weightSortingInfoModels.FirstOrDefault(f =>
                        f.Id.Equals(weightRuleInfoModel.WeightSortingId));
                    if (weightSortingInfoModel is not null) {
                        SubSorting(weightSortingInfoModel.ExitId, guid, token);
                    }
                    else {
                        //走异常口
                        ExceptionSorting(guid, token);
                    }
                }
                else {
                    //走异常口
                    ExceptionSorting(guid, token);
                }
            }
            catch (Exception e) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void VolumeSorting(double length, double width, double height, double volume, long guid, CancellationToken token = default) {
            try {
                var volumeRuleInfoModel = _volumeRuleInfoModels.FirstOrDefault(f => ValidateVolume(f.Formula, length, width, height, volume));
                if (volumeRuleInfoModel is not null) {
                    var volumeSortingInfoModel = _volumeSortingInfoModels.FirstOrDefault(f => f.Id.Equals(volumeRuleInfoModel.VolumeSortingId));
                    if (volumeSortingInfoModel is not null) {
                        SubSorting(volumeSortingInfoModel.ExitId, guid, token);
                    }
                    else {
                        //走异常口
                        ExceptionSorting(guid, token);
                    }
                }
                else {
                    //走异常口
                    ExceptionSorting(guid, token);
                }
            }
            catch (Exception e) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void LogisticsSorting(string barcode, long guid, CancellationToken token = default) {
            try {
                var logisticsRegexInfoModel = _logisticsRegexInfos.FirstOrDefault(f => Regex.IsMatch(barcode, f.RegexPattern));
                if (logisticsRegexInfoModel is not null) {
                    //取出物流
                    var logisticsRuleInfoModel = _logisticsRuleInfoModels.FirstOrDefault(f => f.LogisticsId.Equals(logisticsRegexInfoModel.LogisticsId));
                    if (logisticsRuleInfoModel is not null) {
                        var logisticsSortingInfoModel = _logisticsSortingInfoModels.FirstOrDefault(f => f.Id.Equals(logisticsRuleInfoModel.LogisticsId));
                        if (logisticsSortingInfoModel is not null) {
                            SubSorting(logisticsSortingInfoModel.ExitId, guid, token);
                        }
                        else {
                            //走异常口
                            ExceptionSorting(guid, token);
                        }
                    }
                    else {
                        //走异常口
                        ExceptionSorting(guid, token);
                    }
                }
                else {
                    //走异常口
                    ExceptionSorting(guid, token);
                }
            }
            catch (Exception e) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void OcrSorting(string ocrContent, long guid, CancellationToken token = default) {
            try {
                var ocrRuleInfoModel = _ocrRuleInfoModels.FirstOrDefault(f => Regex.IsMatch(ocrContent, f.RegexPattern));
                if (ocrRuleInfoModel is not null) {
                    var ocrSortingInfoModel = _ocrSortingInfoModels.FirstOrDefault(f => f.Id.Equals(ocrRuleInfoModel.OcrSortingId));
                    if (ocrSortingInfoModel is not null) {
                        SubSorting(ocrSortingInfoModel.ExitId, guid, token);
                    }
                    else {
                        //走异常口
                        ExceptionSorting(guid, token);
                    }
                }
                else {
                    //走异常口
                    ExceptionSorting(guid, token);
                }
            }
            catch (Exception e) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void ApiResponseSorting(UploadResponse apiResponse, long guid, CancellationToken token = default) {
            var apiRuleInfoModel = _apiRuleInfoModels.FirstOrDefault(f =>
                ValidateApiRule(apiResponse, f.JsonContent));
            if (apiRuleInfoModel != null) {
                var apiSortingInfoModel = _apiSortingInfoModels.FirstOrDefault(f => f.Id.Equals(apiRuleInfoModel.ApiSortingId));
                if (apiSortingInfoModel is not null) {
                    SubSorting(apiSortingInfoModel.ExitId, guid, token);
                }
                else {
                    //走异常口
                    ExceptionSorting(guid, token);
                }
            }
            else {
                //走异常口
                ExceptionSorting(guid, token);
            }
        }

        public void CombinedWorkflowSorting(long guid = 0, CancellationToken token = default) {
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

        /// <summary>
        /// 分拣
        /// </summary>
        /// <param name="exitId"></param>
        /// <param name="guid"></param>
        /// <param name="token"></param>
        private async void SubSorting(long exitId, long guid, CancellationToken token = default) {
            //取出格口指令
            //判断格口是否生效
            var packageExitDefinitionInfoModel = _packageExitDefinitionInfos.FirstOrDefault(f => f.Id.Equals(exitId) &&
                f.IsActive);
            if (packageExitDefinitionInfoModel is not null) {
                var sortingInstructionBindingInfoModel = _sortingInstructionBindingInfoModels.FirstOrDefault(f =>
                    f.ExitId.Equals(exitId));
                if (sortingInstructionBindingInfoModel is not null) {
                    //执行分拣
                    //判断指令提交方式
                    var sortingInstructionInfoModels = _sortingInstructionInfoModels.Where(w =>
                            w.InstructionBindingId.Equals(sortingInstructionBindingInfoModel.Id))
                        ?.ToList();
                    await Task.Delay(sortingInstructionBindingInfoModel.DelaySendMilliseconds, token);
                    SendInstructions(guid, sortingInstructionInfoModels ?? new List<SortingInstructionInfoModel>(),
                        TimeSpan.FromMilliseconds(sortingInstructionBindingInfoModel.SendIntervalMilliseconds));
                }
                else {
                    //走异常口
                    ExceptionSorting(guid, token);
                }
            }
            else {
                //走异常口
                ExceptionSorting(guid, token);
            }
        }

        private async Task<bool> WaitForReply(string replyContent, TimeSpan timeOut) {
            await Task.Yield();
            var startTime = DateTime.Now;
            do {
                var tryDequeue = _replyContentQueue.TryDequeue(out var result);
                if (tryDequeue &&
                   replyContent.Equals(result)) {
                    return true;
                }
                await Task.Delay(5);
            } while (DateTime.Now.Subtract(startTime) < timeOut);
            return false;
        }

        private bool ValidateApiRule(UploadResponse apiResponse, string json) {
            try {
                var apiRuleJsonDto = JsonConvert.DeserializeObject<ApiRuleJsonDto>(json);

                if (apiRuleJsonDto is not null) {
                    if (apiRuleJsonDto.ResponseStatus == (apiResponse.IsSuccess ? UploadStatus.Succeeded : UploadStatus.Failed)) {
                        //判断查找方式
                        if (!apiRuleJsonDto.IsUseStringComparison) {
                            return true;
                        }
                        else {
                            if (apiRuleJsonDto.IsUseStringSearch) {
                                return apiResponse.ResponseContent.Contains(apiRuleJsonDto.SearchStringContent);
                            }
                            else if (apiRuleJsonDto.IsUseJsonField) {
                                var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(apiResponse.ResponseContent));
                                var tryParseValue = JsonDocument.TryParseValue(ref reader, out var document);
                                if (tryParseValue && document is not null) {
                                    var fieldValue = FindFieldValue(document.RootElement, apiRuleJsonDto.JsonField);
                                    if (fieldValue.HasValue) {
                                        return fieldValue.Value.ToString()?.Equals(apiRuleJsonDto.JsonFieldValue) == true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
            return false;
        }

        public bool ValidateWeight(string formula, double weight) {
            try {
                // 解析并计算表达式
                var expression = DynamicExpressionParser
                    .ParseLambda(new[] { Expression.Parameter(typeof(double), "weight") }, typeof(bool), formula);
                // 编译并执行表达式
                return (bool)(expression.Compile().DynamicInvoke(weight) ?? false);
            }
            catch (Exception e) {
                return false;
            }
        }

        public bool ValidateVolume(string formula, double length, double width, double height, double volume) {
            try {
                // 解析并计算表达式
                ParameterExpression[] parameters = {
                    Expression.Parameter(typeof(double), "Length"),
                    Expression.Parameter(typeof(double), "Width"),
                    Expression.Parameter(typeof(double), "Height"),
                    Expression.Parameter(typeof(double), "Volume")
                };
                var expression = DynamicExpressionParser.ParseLambda(parameters, typeof(bool), formula);

                // 编译并执行表达式
                return (bool)(expression.Compile().DynamicInvoke(length, width, height, volume) ?? false);
            }
            catch (Exception e) {
                return false;
            }
        }

        private JsonElement? FindFieldValue(JsonElement root, string fieldName) {
            try {
                var stack = new Stack<JsonElement>();
                stack.Push(root);

                while (stack.Count > 0) {
                    var element = stack.Pop();

                    switch (element.ValueKind) {
                        case JsonValueKind.Object when element.TryGetProperty(fieldName, out var field):
                            return field;

                        case JsonValueKind.Object: {
                                foreach (var property in element.EnumerateObject()) {
                                    stack.Push(property.Value);
                                }

                                break;
                            }
                        case JsonValueKind.Array: {
                                foreach (var arrayElement in element.EnumerateArray()) {
                                    stack.Push(arrayElement);
                                }

                                break;
                            }
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

            return null;
        }

        protected virtual async void OnCreatePackageEvent(string e) {
            await Task.Yield();
            CreatePackageEvent?.Invoke(this, e);
        }

        protected virtual async void OnRemovePackageEvent(string e) {
            await Task.Yield();
            RemovePackageEvent?.Invoke(this, e);
        }

        protected virtual async void OnClearExceptionEvent(string e) {
            await Task.Yield();
            ClearExceptionEvent?.Invoke(this, e);
        }
    }
}