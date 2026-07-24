using System;
using DryIoc;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Text.Json;
using System.Threading;
using System.Diagnostics;
using JayTom.Dws.Interface;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Linq.Expressions;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Model;
using System.Linq.Dynamic.Core;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using JayTom.Dws.PluginInterface;
using JayTom.Dws.Interface.Cloud;
using MathNet.Numerics.RootFinding;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Dto.PackageExitLockDto;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using UploadResponse = JayTom.Dws.Interface.UploadResponse;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using SortingExitType = JayTom.Dws.Client.EventMediators.SortingExitType;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;
using PushAlternateExitSorterEvent = JayTom.Dws.Client.EventMediators.PushAlternateExitSorterEvent;

namespace JayTom.Dws.Client.Service.Sorting {

    public class DefaultSortingService : ISortingService {
        private readonly IConfigRepository _configRepository;
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
        private readonly ISortingConnectionService _sortingConnectionService;
        private readonly ICommunicationConnectionConfigRepository _communicationConnectionConfigRepository;

        private readonly IApiRuleRepository _apiRuleRepository;
        private readonly IExitMonitor _exitMonitor;
        private readonly IStackedPackageService _stackedPackageService;
        private readonly IGrayscaleService _grayscaleService;

        private readonly SemaphoreSlim _sortingExecutionGate = new(1, 1);

        //private ConcurrentDictionary<DateTime, long> _stackedPackageGuidItems = new();
        private SortingMethodDto _sortingMethodDto = new();

        private PackageExitLockSettingsDto _packageExitLockSettingsDto = new();

        private StackedPackageDetectionSettingsDto? _stackedPackageDetectionSettingsDto = new();

        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        #region 配置

        private ApiSettingsDto _apiSettingsDto = new();
        private LogisticsRegexInfoModel[] _logisticsRegexInfos = Array.Empty<LogisticsRegexInfoModel>();
        private LogisticsCodeRecognitionInfoModel[] _logisticsCodeRecognitionInfos =
            Array.Empty<LogisticsCodeRecognitionInfoModel>();
        private PackageExitDefinitionInfoModel[] _packageExitDefinitionInfos =
            Array.Empty<PackageExitDefinitionInfoModel>();
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
        private List<CommunicationConnectionConfigInfoModel> _connectionConfigInfoModels = new();

        #endregion 配置

        public event EventHandler<RetryEventArgs>? RetryOccurred;

        public event EventHandler<LogEventArgs>? LogReceived;

        public event EventHandler<Exception>? HeartbeatError;

        public event EventHandler<ExceptionEventArgs>? SendError;

        public event EventHandler<PackageInstructionEventArgs>? CreatePackageEvent;

        public event EventHandler<PackageInstructionEventArgs>? RemovePackageEvent;

        public event EventHandler<PackageInstructionEventArgs>? PackageException;

        public event EventHandler<PackageInstructionEventArgs>? PackageExceptionEx;

        public event EventHandler<PackageInstructionEventArgs>? PreSignalReplyReceived;

        public event EventHandler<PackageInstructionEventArgs>? SendInstruction;

        public event EventHandler<PackageInstructionEventArgs>? SequenceBinding;

        public event EventHandler<PackageInstructionEventArgs>? ResetButtonTrigger;

        public event EventHandler<PackageInstructionEventArgs>? FlowToEndOrException;

        public event EventHandler<string>? ClearExceptionEvent;

        public DefaultSortingService(IConfigRepository configRepository,
           ILogisticsRegexRepository logisticsRegexRepository,
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
           ISortingConnectionService sortingConnectionService,
           ICommunicationConnectionConfigRepository communicationConnectionConfigRepository,
            IApiRuleRepository apiRuleRepository,
            IExitMonitor exitMonitor,
            IStackedPackageService stackedPackageService,
            IGrayscaleService grayscaleService) {
            _configRepository = configRepository;
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
            _sortingConnectionService = sortingConnectionService;
            _communicationConnectionConfigRepository = communicationConnectionConfigRepository;
            _apiRuleRepository = apiRuleRepository;
            _exitMonitor = exitMonitor;
            _stackedPackageService = stackedPackageService;
            _grayscaleService = grayscaleService;

            //事件
            _sortingConnectionService.HeartbeatError += delegate (object? sender, Exception exception) {
                OnHeartbeatError(exception);
            };
            _sortingConnectionService.SendError += delegate (object? sender, ExceptionEventArgs args) {
                OnSendError(new ExceptionEventArgs() {
                    ExceptionMessage = args.ExceptionMessage,
                });
            };
            _sortingConnectionService.CommunicationExceptionEvent += delegate (object? sender, Exception exception) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = exception.ToString() ?? string.Empty
                });
            };
            _sortingConnectionService.ReceivedInstructionsEvent += delegate (object? sender, DeviceDecodeResult result) {
                switch (result.Type) {
                    case FunctionType.CreatePackage:
                        //创建包裹
                        OnCreatePackageEvent(new PackageInstructionEventArgs() {
                            Keyword = result.Keyword,
                            Instruction = result.RawContent,
                            InstructionTime = result.Time,
                            ConnectionName = result.ConnectionName
                        });
                        break;

                    case FunctionType.RemovePackage:
                        //移除包裹
                        OnRemovePackageEvent(new PackageInstructionEventArgs() {
                            Keyword = result.Keyword,
                            Instruction = result.RawContent,
                            InstructionTime = result.Time,
                            ConnectionName = result.ConnectionName
                        });
                        break;

                    case FunctionType.PackageException:
                        //包裹异常
                        OnPackageException(new PackageInstructionEventArgs() {
                            Keyword = result.Keyword,
                            Instruction = result.RawContent,
                            InstructionTime = result.Time,
                            ConnectionName = result.ConnectionName
                        });
                        break;

                    case FunctionType.Heartbeat:
                        //心跳包

                        //OnCreatePackageEvent(result.Keyword);
                        break;

                    case FunctionType.ClearException:
                        //清空异常
                        break;

                    case FunctionType.ReceivePreSignalReply:
                        //前置信号回复
                        OnPreSignalReplyReceived(new PackageInstructionEventArgs() {
                            Keyword = result.Keyword,
                            Instruction = result.RawContent,
                            InstructionTime = result.Time,
                            ConnectionName = result.ConnectionName
                        });
                        break;

                    case FunctionType.SequenceBindingReply:
                        //车号绑定回复
                        OnSequenceBinding(new PackageInstructionEventArgs() {
                            Keyword = result.Keyword,
                            Instruction = result.RawContent,
                            InstructionTime = result.Time,
                            ConnectionName = result.ConnectionName
                        });
                        break;

                    case FunctionType.ResetButtonTrigger:
                        //复位按钮触发
                        OnResetButtonTrigger(new PackageInstructionEventArgs() {
                            Keyword = result.Keyword,
                            Instruction = result.RawContent,
                            InstructionTime = result.Time,
                            ConnectionName = result.ConnectionName
                        });
                        break;

                    case FunctionType.PackageExceptionEx:

                        OnPackageExceptionEx(new PackageInstructionEventArgs() {
                            Keyword = result.Keyword,
                            Instruction = result.RawContent,
                            InstructionTime = result.Time,
                            ConnectionName = result.ConnectionName
                        });
                        break;
                }
            };
            //格口更改
            EventAggregator.Instance.Subscribe<LogisticsCodeRecognitionInfoModel>(models => {
                _ = ReloadLogisticsCodeRecognitionAsync();
            });
            EventAggregator.Instance.Subscribe<LogisticsRegexInfoModel>(models => {
                _ = ReloadLogisticsRegexAsync();
            });
            EventAggregator.Instance.Subscribe<PackageExitDefinitionInfoModel>(models => {
                _ = ReloadPackageExitDefinitionsAsync();
            });
            //触发方式
            EventAggregator.Instance.Subscribe<PackageInfo>(item => {
                if (item is { } model) {
                    //不包含Api
                    if (_sortingMethodDto.SortMode != SortMode.None &&
                        _sortingMethodDto.SortMode != SortMode.ApiResponseSorting &&
                        _sortingMethodDto.SortMode != SortMode.CombinedWorkflowSorting &&
                        _sortingMethodDto.SortMode != SortMode.OcrSorting) {
                        ExecuteSorting(new SortingParam() {
                            Guid = model.Guid,
                            BarCode = model.BarCodeInfo?.Barcode ?? string.Empty,
                            Height = (float)(model.VolumeInfo?.FormattedHeight ?? 0),
                            ScanTime = model.BarCodeInfo?.ScanTime ?? DateTime.Now,
                            Weight = (float)(model.WeightInfo?.FormattedWeight ?? 0),
                            Length = (float)(model.VolumeInfo?.FormattedLength ?? 0),
                            Width = (float)(model.VolumeInfo?.FormattedWidth ?? 0),
                            Volume = (float)(model.VolumeInfo?.FormattedVolume ?? 0),
                            PackageCreationTime = model.CreateTime,
                            PackageCreationInstruction = model.PackageCreationInstruction,
                            IsCreatedByLowerMachine = model.IsCreatedByLowerMachine,
                            IsStackedPackage = model.IsStackedPackage ?? false,
                            LinkedCarCount = model.LinkedCarCount,
                            Timestamp = model.Timestamp,
                        });
                    }
                }
            });
            //Api触发
            EventAggregator.Instance.Subscribe<ApiResponseReceived>(item => {
                if (item is { } model) {
                    if (_sortingMethodDto.SortMode == SortMode.ApiResponseSorting) {
                        ExecuteSorting(new SortingParam {
                            Timestamp = model.Timestamp,
                            Guid = model.Guid,
                            BarCode = model.Barcode ?? string.Empty,
                            ScanTime = model.ScanTime,
                            PackageCreationTime = model.PackageCreationTime,
                            PackageCreationInstruction = model.PackageCreationInstruction,
                            IsCreatedByLowerMachine = model.IsCreatedByLowerMachine,
                            ApiResponse = model.UploadResponse ?? new UploadResponse(),
                            IsStackedPackage = model.IsStackedPackage,
                            LinkedCarCount = model.LinkedCarCount
                        });
                    }
                }
            });
            //Ocr触发
            EventAggregator.Instance.Subscribe<PackageOcrInfo>(item => {
                if (item is { } model) {
                    if (_sortingMethodDto.SortMode == SortMode.OcrSorting) {
                        ExecuteSorting(new SortingParam {
                            Guid = model.RecognitionTimestamp,
                            BarCode = model.BarCode ?? string.Empty,
                            ScanTime = model.RecognitionTime,
                            PackageCreationTime = model.RecognitionTime,
                            PackageCreationInstruction = null,
                            IsCreatedByLowerMachine = false,
                            OcrInfo = model,
                            IsStackedPackage = model.IsStackedPackage,
                            Timestamp = model.RecognitionTimestamp,
                        });
                    }
                }
            });

            //备用格口分拣
            EventAggregator.Instance.Subscribe<PushAlternateExitSorterEvent>(item => {
                if (item is { } model) {
                    SubSorting(new SortingParam {
                        Timestamp = model.PackageInfo.Timestamp,
                        Guid = model.PackageInfo.Guid,
                        BarCode = model.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty,
                        ScanTime = model.PackageInfo.BarCodeInfo?.ScanTime,
                        PackageCreationTime = model.PackageInfo.CreateTime,
                        PackageCreationInstruction = model.PackageInfo.PackageCreationInstruction,
                        IsCreatedByLowerMachine = true,
                        ExitId = model.OriginalExitId,
                        IsAlternateExitInstruction = true,
                    });
                }
            });
            //锁格
            _exitMonitor.LockExitEvent += (sender, model) => {
                ReplaceExitLockStatus(model);
            };
            //解锁
            _exitMonitor.UnLockExitEvent += (sender, model) => {
                ReplaceExitLockStatus(model);
            };
        }

        public void ExecuteSorting(SortingParam param, CancellationToken token = default) {
            switch (_sortingMethodDto.SortMode) {
                case SortMode.BarcodeSorting:
                    BarcodeSorting(param, token);
                    break;

                case SortMode.WeightSorting:
                    WeightSorting(param, token);
                    break;

                case SortMode.VolumeSorting:
                    VolumeSorting(param, token);
                    break;

                case SortMode.LogisticsSorting:
                    LogisticsSorting(param, token);
                    break;

                case SortMode.OcrSorting:
                    OcrSorting(param, token);
                    break;

                case SortMode.ApiResponseSorting:
                    ApiResponseSorting(param, token);
                    break;

                case SortMode.CombinedWorkflowSorting:
                    CombinedWorkflowSorting(param, token);
                    break;
            }
        }

        public bool IsConnected => true;

        public async Task<LogisticsCodeRecognitionInfoModel?> GetLogisticsInfo(string barCode) {
            var codeRecognitionInfos = Volatile.Read(ref _logisticsCodeRecognitionInfos);
            if (codeRecognitionInfos.Length == 0) {
                await ReloadLogisticsCodeRecognitionAsync();
                codeRecognitionInfos = Volatile.Read(ref _logisticsCodeRecognitionInfos);
            }

            var regexInfos = Volatile.Read(ref _logisticsRegexInfos);
            if (regexInfos.Length == 0) {
                await ReloadLogisticsRegexAsync();
                regexInfos = Volatile.Read(ref _logisticsRegexInfos);
            }

            if (regexInfos.Length > 0) {
                var logisticsRegexInfoModel = regexInfos.FirstOrDefault(f => {
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
                    return codeRecognitionInfos.FirstOrDefault(f =>
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

                #region 读取配置信息

                try {
                    _apiSettingsDto = await _configRepository.FirstOrDefaultEntity<ApiSettingsDto>("ApiSettings", token) ?? new ApiSettingsDto();
                    _sortingMethodDto = await _configRepository.FirstOrDefaultEntity<SortingMethodDto>("SortingMethodSettings", token) ?? new SortingMethodDto();
                    _stackedPackageDetectionSettingsDto = await _configRepository.FirstOrDefaultEntity<StackedPackageDetectionSettingsDto>("StackedPackageDetectionSettings", token) ?? new StackedPackageDetectionSettingsDto();

                    _packageExitLockSettingsDto = await _configRepository.FirstOrDefaultEntity<PackageExitLockSettingsDto>("PackageExitLockSettings", token) ?? new PackageExitLockSettingsDto();

                    Volatile.Write(
                        ref _packageExitDefinitionInfos,
                        (await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.Id, token))
                        .ToArray());

                    Volatile.Write(
                        ref _logisticsCodeRecognitionInfos,
                        (await _logisticsCodeRecognitionRepository.Select(s => s.Id > 0, o => o.Id, token))
                        .ToArray());
                    Volatile.Write(
                        ref _logisticsRegexInfos,
                        (await _logisticsRegexRepository.Select(s => s.Id > 0, o => o.CreateTime, token))
                        .ToArray());
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

                    _connectionConfigInfoModels = await _communicationConnectionConfigRepository.CommunicationConnectionConfigItems(
                        s => s.Id > 0, token);

                    await _sortingConnectionService.ConfigurationInitializer();
                    var (key, value) = await _exitMonitor.Start(token);
                    if (!key) {
                        OnExceptionOccurred(new ExceptionEventArgs() {
                            ExceptionMessage = $"{value}"
                        });
                    }
                    var (b, value1) = await _stackedPackageService.Start(token);
                    if (!b) {
                        OnExceptionOccurred(new ExceptionEventArgs() {
                            ExceptionMessage = $"{value1}"
                        });
                    }

                    var (key1, s1) = await _grayscaleService.StartSensor();
                    if (!key1) {
                        OnExceptionOccurred(new ExceptionEventArgs() {
                            ExceptionMessage = $"{s1}"
                        });
                    }
                }
                catch (Exception e) {
                    OnExceptionOccurred(new ExceptionEventArgs() {
                        ExceptionMessage = $"分拣配置读取异常:{e}"
                    });
                }

                #endregion 读取配置信息

                await _sortingConnectionService.DisconnectAll();
                foreach (var infoModel in _connectionConfigInfoModels) {
                    var communicationsType = (CommunicationsType)Enum.Parse(typeof(CommunicationsType), infoModel.CommunicationType.ToString());
                    var communicationProtocol = (CommunicationProtocol)Enum.Parse(typeof(CommunicationProtocol), infoModel.CommunicationProtocol.ToString());
                    if (communicationsType == CommunicationsType.SerialPort) {
                        await _sortingConnectionService.AddConnection(communicationsType, communicationProtocol,
                             infoModel.ConnectionName, infoModel.SerialPortConfigInfo);
                    }
                    else if (communicationsType == CommunicationsType.TCP) {
                        var (key, value) = await _sortingConnectionService.AddConnection(communicationsType, communicationProtocol,
                            infoModel.ConnectionName, infoModel.TcpConnectionConfigInfo);
                    }
                }

                RunningStatus = true;
                return new KeyValuePair<bool, string>(true, "已启动");
            }
            return new KeyValuePair<bool, string>(true, "已启动,无需再次启动");
        }

        public async Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default) {
            //停止
            //关闭心跳包
            await _sortingConnectionService.DisconnectAll();
            await _exitMonitor.Stop(token);
            await _stackedPackageService.Stop(token);
            await _grayscaleService.StopSensor();
            RunningStatus = false;
            return new KeyValuePair<bool, string>(true, "已停止");
        }

        public void ExceptionSorting(
            SortingParam param,
            PackageCloudAbnormalSortingType abnormalSortingType,
            CancellationToken token = default) {
            QueueSortingWork(() => ExceptionSortingAsync(param, abnormalSortingType, token));
        }

        private async Task ExceptionSortingAsync(
            SortingParam param,
            PackageCloudAbnormalSortingType abnormalSortingType,
            CancellationToken token) {
            EventAggregator.Instance.Publish(new ExceptionSortingReceived {
                ScanTime = param.ScanTime,
                BarCode = param.BarCode,
                Timestamp = param.Timestamp,
                PackageCloudAbnormalSortingType = abnormalSortingType
            });
            var packageExitDefinitions = Volatile.Read(ref _packageExitDefinitionInfos);
            var packageExitDefinitionInfoModel = packageExitDefinitions.FirstOrDefault(f =>
                f is { Type: ExitType.AbnormalExit, IsActive: true });
            if (packageExitDefinitionInfoModel is not null) {
                //执行分拣
                //判断指令提交方式
                var sortingInstructionBindingInfoModel = _sortingInstructionBindingInfoModels.FirstOrDefault(f =>
                    f.ExitId.Equals(packageExitDefinitionInfoModel.Id));

                if (sortingInstructionBindingInfoModel is not null) {
                    EventAggregator.Instance.Publish(new SortingExitReceived {
                        ScanTime = param.ScanTime,
                        BarCode = param.BarCode,
                        Timestamp = param.Timestamp,
                        ExitName = packageExitDefinitionInfoModel.ExitName,
                        ExitId = packageExitDefinitionInfoModel.Id,
                        ExitType = SortingExitType.PhysicalExit,
                        Type = packageExitDefinitionInfoModel.Type,
                        SortingParam = param
                    });
                    var sortingInstructionInfoModels = _sortingInstructionInfoModels.Where(w =>
                            w.InstructionBindingId.Equals(sortingInstructionBindingInfoModel.Id))
                        ?.ToList();

                    await Task.Delay(sortingInstructionBindingInfoModel.DelaySendMilliseconds, token);
                    _sortingConnectionService.SendInstructions(param.Tag ?? new object(), sortingInstructionBindingInfoModel.ExitId ?? 0,
                        sortingInstructionInfoModels ?? new List<SortingInstructionInfoModel>(),
                        TimeSpan.FromMilliseconds(sortingInstructionBindingInfoModel.SendIntervalMilliseconds),
                        new InstructionsAttach {
                            BarCode = param.BarCode,
                            ExitName = packageExitDefinitionInfoModel.ExitName,
                            Guid = param.Guid,
                            Height = param.Height,
                            Length = param.Length,
                            Timestamp = param.Timestamp,
                            Volume = param.Volume,
                            Width = param.Width,
                            Weight = param.Weight,
                            ScanTime = param.ScanTime,
                            ExitId = param.ExitId,
                            //先忽略物流
                            SortingMode = _sortingMethodDto?.SortMode ?? SortMode.None,
                            PackageCreationTime = param.PackageCreationTime,
                            PackageCreationInstruction = param.PackageCreationInstruction ?? string.Empty,
                            IsCreatedByLowerMachine = param.IsCreatedByLowerMachine,
                            LinkedCarCount = param.LinkedCarCount
                        });
                    //回调分拣消息
                }
            }
        }

        public void BarcodeSorting(SortingParam param, CancellationToken token = default) {
            try {
                var barCodeRegexInfoModel = _barCodeRegexInfos.FirstOrDefault(f => Regex.IsMatch(param.BarCode, f.RegexPattern));
                if (barCodeRegexInfoModel is not null) {
                    //取出对于格口id
                    var barCodeSortingInfoModel = _barCodeSortingInfoModels.FirstOrDefault(f =>
                        f.Id.Equals(barCodeRegexInfoModel.BarCodeSortingId));
                    if (barCodeSortingInfoModel is not null) {
                        param.ExitId = barCodeSortingInfoModel.ExitId;
                        SubSorting(param, token);
                    }
                    else {
                        //走异常口
                        ExceptionSorting(param, PackageCloudAbnormalSortingType.NoPhysicalMailbox, token);
                    }
                }
                else {
                    //走异常口
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule, token);
                }
            }
            catch (Exception e) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void WeightSorting(SortingParam param, CancellationToken token = default) {
            //取出重量规则
            try {
                var weightRuleInfoModel = _weightRuleInfoModels.FirstOrDefault(f => ValidateWeight(f.Formula, param.Weight));
                if (weightRuleInfoModel is not null) {
                    //取出格口
                    var weightSortingInfoModel = _weightSortingInfoModels.FirstOrDefault(f =>
                        f.Id.Equals(weightRuleInfoModel.WeightSortingId));
                    if (weightSortingInfoModel is not null) {
                        param.ExitId = weightSortingInfoModel.ExitId;
                        SubSorting(param, token);
                    }
                    else {
                        //走异常口
                        ExceptionSorting(param, PackageCloudAbnormalSortingType.NoPhysicalMailbox, token);
                    }
                }
                else {
                    //走异常口
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule, token);
                }
            }
            catch (Exception e) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void VolumeSorting(SortingParam param, CancellationToken token = default) {
            try {
                var volumeRuleInfoModel = _volumeRuleInfoModels.FirstOrDefault(f => ValidateVolume(f.Formula, param.Length, param.Width, param.Height, param.Volume));
                if (volumeRuleInfoModel is not null) {
                    var volumeSortingInfoModel = _volumeSortingInfoModels.FirstOrDefault(f => f.Id.Equals(volumeRuleInfoModel.VolumeSortingId));
                    if (volumeSortingInfoModel is not null) {
                        param.ExitId = volumeSortingInfoModel.ExitId;
                        SubSorting(param, token);
                    }
                    else {
                        //走异常口
                        ExceptionSorting(param, PackageCloudAbnormalSortingType.NoPhysicalMailbox, token);
                    }
                }
                else {
                    //走异常口
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule, token);
                }
            }
            catch (Exception e) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void LogisticsSorting(SortingParam param, CancellationToken token = default) {
            try {
                var logisticsRegexInfos = Volatile.Read(ref _logisticsRegexInfos);
                var logisticsRegexInfoModel = logisticsRegexInfos.FirstOrDefault(
                    f => Regex.IsMatch(param.BarCode, f.RegexPattern));
                if (logisticsRegexInfoModel is not null) {
                    //取出物流
                    var logisticsRuleInfoModel = _logisticsRuleInfoModels.FirstOrDefault(f => f.LogisticsId.Equals(logisticsRegexInfoModel.LogisticsId));
                    if (logisticsRuleInfoModel is not null) {
                        var logisticsSortingInfoModel = _logisticsSortingInfoModels.FirstOrDefault(f => f.Id.Equals(logisticsRuleInfoModel.LogisticsId));
                        if (logisticsSortingInfoModel is not null) {
                            param.ExitId = logisticsSortingInfoModel.ExitId;
                            SubSorting(param, token);
                        }
                        else {
                            //走异常口
                            ExceptionSorting(param, PackageCloudAbnormalSortingType.NoPhysicalMailbox, token);
                        }
                    }
                    else {
                        //走异常口
                        ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule, token);
                    }
                }
                else {
                    //走异常口
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule, token);
                }
            }
            catch (Exception e) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void OcrSorting(SortingParam param, CancellationToken token = default) {
            try {
                var ocrRuleInfoModel = _ocrRuleInfoModels.FirstOrDefault(f =>
                    ValidateOcrRule(param.OcrInfo, f.JsonContent));

                if (ocrRuleInfoModel != null) {
                    var ocrSortingInfoModel = _ocrSortingInfoModels.
                        FirstOrDefault(f =>
                            f.Id.Equals(ocrRuleInfoModel.OcrSortingId));

                    if (ocrSortingInfoModel is not null) {
                        param.ExitId = ocrSortingInfoModel.ExitId;
                        SubSorting(param, token);
                    }
                    else {
                        //走异常口
                        ExceptionSorting(param, PackageCloudAbnormalSortingType.NoPhysicalMailbox, token);
                    }
                }
                else {
                    //走异常口
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule, token);
                }
            }
            catch (Exception e) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void ApiResponseSorting(SortingParam param, CancellationToken token = default) {
            //先判断返回内容
            if (param.ApiResponse.ResponseContent.Contains("返回超时")) {
                ExceptionSorting(param, PackageCloudAbnormalSortingType.NetworkTimeout, token);
                return;
            }
            if (_apiSettingsDto.Type == ApiType.RoutDataApi) {

                #region 邮政额外定制

                if (param.ApiResponse.ResponseContent.Contains("段道")) {
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.PostSegmentNotFound, token);
                    return;
                }
                else if (param.ApiResponse.ResponseContent.Contains("非本机构")) {
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.PostNonLocalBarcode, token);
                    return;
                }

                #endregion 邮政额外定制
            }
            var apiRuleInfoModel = _apiRuleInfoModels
                ?.Select(o => {
                    try {
                        var apiRuleJsonDto = JsonConvert.DeserializeObject<ApiRuleJsonDto>(o.JsonContent);
                        return new { ApiRuleInfo = o, ApiRuleJsonDto = apiRuleJsonDto };
                    }
                    catch (Exception e) {
                        return new { ApiRuleInfo = o, ApiRuleJsonDto = (ApiRuleJsonDto)null };
                    }
                })
                ?.OrderByDescending(x => x.ApiRuleJsonDto?.IsUseStringComparison ?? false)
                ?.ThenByDescending(x => x.ApiRuleJsonDto?.IsUseJsonField ?? false)
                ?.ThenByDescending(x => x.ApiRuleJsonDto?.IsUseStringSearch ?? false)
                ?.ThenBy(x => x.ApiRuleJsonDto?.ResponseStatus ?? UploadStatus.NotUploaded)
                ?.Select(x => x.ApiRuleInfo)
                ?.ToList()
                ?.FirstOrDefault(f =>
                    ValidateApiRule(param.ApiResponse, f.JsonContent));
            if (apiRuleInfoModel != null) {
                var apiSortingInfoModel = _apiSortingInfoModels.FirstOrDefault(f => f.Id.Equals(apiRuleInfoModel.ApiSortingId));
                if (apiSortingInfoModel is not null) {
                    param.ExitId = apiSortingInfoModel.ExitId;
                    SubSorting(param, token);
                }
                else {
                    //走异常口
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.NoPhysicalMailbox, token);
                }
            }
            else {
                //走异常口
                ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule, token);
            }
        }

        public void CombinedWorkflowSorting(SortingParam param, CancellationToken token = default) {
        }

        public void SendPreSignal(int num, InstructionsAttach attach, CancellationToken token = default) {
            _sortingConnectionService.SendPreSignal(num, attach, token);
        }

        public void SendPackageInfoCompletedSignal(int num, InstructionsAttach attach, CancellationToken token = default) {
            _sortingConnectionService.SendPackageInfoCompletedSignal(num, attach, token);
        }

        public void SendPackageCenter(int num, InstructionsAttach attach, CancellationToken token = default) {
            _sortingConnectionService.SendPackageCenter(num, attach, token);
        }

        protected virtual void OnExceptionOccurred(ExceptionEventArgs e) {
            ExceptionOccurred?.Invoke(this, e);
        }

        protected virtual void OnHeartbeatError(Exception e) {
            HeartbeatError?.Invoke(this, e);
        }

        protected virtual void OnSendError(ExceptionEventArgs e) {
            SendError?.Invoke(this, e);
        }

        /// <summary>
        /// 具体分拣
        /// </summary>
        /// <param name="param"></param>
        /// <param name="token"></param>
        private void SubSorting(SortingParam param, CancellationToken token = default) {
            QueueSortingWork(() => SubSortingAsync(param, token));
        }

        private async Task SubSortingAsync(
            SortingParam param,
            CancellationToken token) {
            //取出格口指令
            //判断格口是否生效
            PackageExitDefinitionInfoModel? exitDefinitionInfoModel = null;
            if (param.IsStackedPackage && _stackedPackageDetectionSettingsDto?.IsAutoExceptionSorting == true) {
                //走异常口
                ExceptionSorting(param, PackageCloudAbnormalSortingType.StackedPackage, token);
                return;
            }
            var packageExitDefinitions = Volatile.Read(ref _packageExitDefinitionInfos);
            var packageExitDefinitionInfoModel = packageExitDefinitions.FirstOrDefault(f => f.Id.Equals(param.ExitId) &&
                f is { IsActive: true });
            if (packageExitDefinitionInfoModel is not null) {
                if (packageExitDefinitionInfoModel.IsLockExit || param.IsAlternateExitInstruction) {
                    //判断备用格口

                    exitDefinitionInfoModel = packageExitDefinitions.FirstOrDefault(f => f is { IsLockExit: false, IsActive: true } &&
                        f.Pid == packageExitDefinitionInfoModel.Id);
                    /*if (exitDefinitionInfoModel is null || _packageExitLockSettingsDto.IsAutoExceptionSorting) {
                        ExceptionSorting(param, PackageCloudAbnormalSortingType.LockExit, token);
                        return;
                    }
                    param.ExitId = exitDefinitionInfoModel.Id;*/
                    if (exitDefinitionInfoModel is not null) {
                        param.ExitId = exitDefinitionInfoModel.Id;
                    }
                    else {
                        //判断去异常口
                        if (_packageExitLockSettingsDto.IsAutoExceptionSorting) {
                            ExceptionSorting(param, PackageCloudAbnormalSortingType.LockExit, token);
                            return;
                        }
                    }
                }

                var sortingInstructionBindingInfoModel = _sortingInstructionBindingInfoModels.FirstOrDefault(f =>
                    f.ExitId.Equals(param.ExitId));
                if (sortingInstructionBindingInfoModel is not null) {
                    //回调格口信息
                    EventAggregator.Instance.Publish(new SortingExitReceived {
                        ScanTime = param.ScanTime,
                        BarCode = param.BarCode,
                        Timestamp = param.Timestamp,
                        ExitName = exitDefinitionInfoModel != null ? exitDefinitionInfoModel.ExitName : packageExitDefinitionInfoModel.ExitName ?? string.Empty,
                        ExitId = param.ExitId,
                        ExitType = SortingExitType.PhysicalExit,
                        Type = packageExitDefinitionInfoModel.Type,
                        SortingParam = param
                    });

                    //执行分拣
                    //判断指令提交方式
                    var sortingInstructionInfoModels = _sortingInstructionInfoModels.Where(w =>
                            w.InstructionBindingId.Equals(sortingInstructionBindingInfoModel.Id))
                        ?.ToList();
                    await Task.Delay(sortingInstructionBindingInfoModel.DelaySendMilliseconds, token);

                    _sortingConnectionService.SendInstructions(param.Tag ?? new object(), sortingInstructionBindingInfoModel.ExitId ?? 0,
                        sortingInstructionInfoModels ?? new List<SortingInstructionInfoModel>(),
                        TimeSpan.FromMilliseconds(sortingInstructionBindingInfoModel.SendIntervalMilliseconds),
                        new InstructionsAttach {
                            BarCode = param.BarCode,
                            ExitName = packageExitDefinitionInfoModel.ExitName ?? string.Empty,
                            Guid = param.Guid,
                            Height = param.Height,
                            Length = param.Length,
                            Timestamp = param.Timestamp,
                            Volume = param.Volume,
                            Width = param.Width,
                            Weight = param.Weight,
                            ScanTime = param.ScanTime,
                            ExitId = param.ExitId,
                            //先忽略物流
                            SortingMode = _sortingMethodDto?.SortMode ?? SortMode.None,
                            PackageCreationTime = param.PackageCreationTime,
                            PackageCreationInstruction = param.PackageCreationInstruction ?? string.Empty,
                            IsCreatedByLowerMachine = param.IsCreatedByLowerMachine,
                            LinkedCarCount = param.LinkedCarCount
                        });
                    //回调分拣消息
                    //NLog.LogManager.GetCurrentClassLogger().Error($"SubSorting:{param.LinkedCarCount}");
                }
                else {
                    //走异常口
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.NoSortingInstruction, token);
                }
            }
            else {
                //走异常口
                ExceptionSorting(param, PackageCloudAbnormalSortingType.NoPhysicalMailbox, token);
            }
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
                                return apiRuleJsonDto.SearchDirection == SearchDirection.Forward
                                    ? apiResponse.ResponseContent.IndexOf(apiRuleJsonDto.SearchStringContent, StringComparison.Ordinal) >= 0
                                    : apiResponse.ResponseContent.LastIndexOf(apiRuleJsonDto.SearchStringContent, StringComparison.Ordinal) >= 0;
                            }
                            else if (apiRuleJsonDto.IsUseJsonField) {
                                var resultContent = Regex.Unescape(apiResponse.ResponseContent);
                                var replace = Regex.Replace(resultContent, @"[\u0000-\u001f\b]", "");
                                var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(replace));
                                var tryParseValue = JsonDocument.TryParseValue(ref reader, out var document);
                                if (tryParseValue && document is not null) {
                                    var fieldValue = FindFieldValue(document.RootElement, apiRuleJsonDto.JsonField, apiRuleJsonDto.SearchDirection);
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
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                Console.WriteLine(e);
            }
            return false;
        }

        private bool ValidateOcrRule(PackageOcrInfo ocrInfo, string json) {
            try {
                var isValid = true;
                var ocrRuleJsonDto = JsonConvert.DeserializeObject<OcrRuleJsonDto>(json);
                if (ocrRuleJsonDto is not null) {
                    //判断三段码
                    if (isValid && ocrRuleJsonDto.IsUseThreeSegmentCodeValidation) {
                        isValid = ocrInfo.ThreeSegmentCode.Contains(ocrRuleJsonDto.ThreeSegmentCodeContainsChars);
                    }
                    //是否使用发件人地址
                    if (isValid && ocrRuleJsonDto.IsUseSenderAddressValidation) {
                        isValid = ocrInfo.SenderAddress.Contains(ocrRuleJsonDto.SenderAddressContainsChars);
                    }
                    //是否使用收件人地址
                    if (isValid && ocrRuleJsonDto.IsUseRecipientAddressValidation) {
                        isValid = ocrInfo.RecipientAddress.Contains(ocrRuleJsonDto.RecipientAddressContainsChars);
                    }
                    //是否使用发件人手机号码
                    if (isValid && ocrRuleJsonDto.IsUseSenderPhoneNumberValidation) {
                        isValid = ocrInfo.SenderPhone.EndsWith(ocrRuleJsonDto.SenderPhoneNumberEndsWith);
                    }

                    return isValid;
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

        public void PublishExceptionSortingInfo1(SortingParam param) {
            if (param.ApiResponse.ExceptionMsg.Contains("接口访问返回超时")) {
                EventAggregator.Instance.Publish(new ExceptionSortingReceived {
                    ScanTime = param.ScanTime,
                    BarCode = param.BarCode,
                    Timestamp = param.Timestamp,
                    PackageCloudAbnormalSortingType = PackageCloudAbnormalSortingType.NetworkTimeout
                });
            }
            else if (param.ApiResponse.ExceptionMsg.Contains("接口访问异常")) {
                EventAggregator.Instance.Publish(new ExceptionSortingReceived {
                    ScanTime = param.ScanTime,
                    BarCode = param.BarCode,
                    Timestamp = param.Timestamp,
                    PackageCloudAbnormalSortingType = PackageCloudAbnormalSortingType.ApiAccessError
                });
            }
            else if (param.BarCode.ToLower().Equals("noread")) {
                EventAggregator.Instance.Publish(new ExceptionSortingReceived {
                    ScanTime = param.ScanTime,
                    BarCode = param.BarCode,
                    Timestamp = param.Timestamp,
                    PackageCloudAbnormalSortingType = PackageCloudAbnormalSortingType.NoRead
                });
            }
        }

        private static JsonElement? FindFieldValue(JsonElement root, string fieldName, SearchDirection direction = SearchDirection.Forward) {
            try {
                var stack = new Stack<JsonElement>();
                stack.Push(root);

                JsonElement? lastMatch = null;

                while (stack.Count > 0) {
                    var element = stack.Pop();

                    switch (element.ValueKind) {
                        case JsonValueKind.Object when element.TryGetProperty(fieldName, out var field):
                            lastMatch = field;
                            if (direction == SearchDirection.Forward) {
                                continue;
                            }
                            break;

                        case JsonValueKind.Object: {
                                foreach (var property in element.EnumerateObject()) {
                                    stack.Push(direction == SearchDirection.Forward
                                        ? property.Value
                                        : property.Value.Clone());
                                }

                                break;
                            }
                        case JsonValueKind.Array: {
                                var array = element.EnumerateArray().ToList();
                                if (direction == SearchDirection.Backward) {
                                    array.Reverse();
                                }
                                foreach (var arrayElement in array) {
                                    stack.Push(arrayElement);
                                }

                                break;
                            }
                    }
                }

                return lastMatch;
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

            return null;
        }

        private async Task ReloadLogisticsCodeRecognitionAsync() {
            try {
                var items = await _logisticsCodeRecognitionRepository.Select(
                    item => item.Id > 0,
                    item => item.Id);
                Volatile.Write(ref _logisticsCodeRecognitionInfos, items.ToArray());
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger()
                    .Error(e, "刷新物流识别配置失败");
            }
        }

        private async Task ReloadLogisticsRegexAsync() {
            try {
                var items = await _logisticsRegexRepository.Select(
                    item => item.Id > 0,
                    item => item.CreateTime);
                Volatile.Write(ref _logisticsRegexInfos, items.ToArray());
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger()
                    .Error(e, "刷新物流正则配置失败");
            }
        }

        private async Task ReloadPackageExitDefinitionsAsync() {
            try {
                var items = await _packageExitDefinitionRepository.Select(
                    item => item.Id > 0,
                    item => item.Id);
                Volatile.Write(ref _packageExitDefinitionInfos, items.ToArray());
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger()
                    .Error(e, "刷新格口配置失败");
            }
        }

        private void ReplaceExitLockStatus(PackageExitDefinitionInfoModel changedStatus) {
            PackageExitDefinitionInfoModel[] current;
            PackageExitDefinitionInfoModel[] updated;
            do {
                current = Volatile.Read(ref _packageExitDefinitionInfos);
                updated = current
                    .Select(item => item.Id == changedStatus.Id
                        ? CloneExitDefinition(item, changedStatus.IsLockExit)
                        : item)
                    .ToArray();
            } while (!ReferenceEquals(
                         Interlocked.CompareExchange(
                             ref _packageExitDefinitionInfos,
                             updated,
                             current),
                         current));
        }

        private static PackageExitDefinitionInfoModel CloneExitDefinition(
            PackageExitDefinitionInfoModel source,
            bool isLockExit) {
            return new PackageExitDefinitionInfoModel {
                Id = source.Id,
                CommunicationConnectionId = source.CommunicationConnectionId,
                CommunicationConnectionConfigInfo = source.CommunicationConnectionConfigInfo,
                Pid = source.Pid,
                ExitName = source.ExitName,
                Type = source.Type,
                IsActive = source.IsActive,
                IsLockExit = isLockExit,
                PackageExitLockBindingInfo = source.PackageExitLockBindingInfo,
                Remarks = source.Remarks,
                CreateTime = source.CreateTime,
                ModifyTime = source.ModifyTime
            };
        }

        private void QueueSortingWork(Func<Task> work) {
            _ = RunSortingWorkAsync(work);
        }

        private async Task RunSortingWorkAsync(Func<Task> work) {
            await _sortingExecutionGate.WaitAsync();
            try {
                await work();
            }
            catch (OperationCanceledException) {
                // 调用方取消时停止当前发送，不污染后续分拣任务。
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger()
                    .Error(e, "分拣任务执行失败");
                OnExceptionOccurred(new ExceptionEventArgs {
                    ExceptionMessage = e.Message
                });
            }
            finally {
                _sortingExecutionGate.Release();
            }
        }

        protected virtual void OnCreatePackageEvent(PackageInstructionEventArgs e) {
            CreatePackageEvent?.Invoke(this, e);
        }

        protected virtual void OnRemovePackageEvent(PackageInstructionEventArgs e) {
            RemovePackageEvent?.Invoke(this, e);
        }

        protected virtual void OnClearExceptionEvent(string e) {
            ClearExceptionEvent?.Invoke(this, e);
        }

        protected virtual void OnSendInstruction(PackageInstructionEventArgs e) {
            SendInstruction?.Invoke(this, e);
        }

        protected virtual void OnPackageException(PackageInstructionEventArgs e) {
            PackageException?.Invoke(this, e);
        }

        protected virtual void OnPreSignalReplyReceived(PackageInstructionEventArgs e) {
            PreSignalReplyReceived?.Invoke(this, e);
        }

        protected virtual void OnSequenceBinding(PackageInstructionEventArgs e) {
            SequenceBinding?.Invoke(this, e);
        }

        protected virtual void OnResetButtonTrigger(PackageInstructionEventArgs e) {
            ResetButtonTrigger?.Invoke(this, e);
        }

        protected virtual void OnFlowToEndOrException(PackageInstructionEventArgs e) {
            FlowToEndOrException?.Invoke(this, e);
        }

        protected virtual void OnPackageExceptionEx(PackageInstructionEventArgs e) {
            PackageExceptionEx?.Invoke(this, e);
        }
    }
}
