using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Application.SortingInstructions;
using JayTom.Dws.Application.PackageExits;
using JayTom.Dws.Application.Communications;
using JayTom.Dws.Application.SortingConfigurations;
using JayTom.Dws.Application.Packages;
using JayTom.Dws.Application.Workflows;
using System;
using DryIoc;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using JayTom.Dws.Integrations;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using System.Linq.Expressions;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Legacy.Contracts.Model;
using System.Linq.Dynamic.Core;
using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Legacy.Contracts.Packages;
using System.Collections.Generic;
using JayTom.Dws.PluginInterface;
using JayTom.Dws.Integrations.Cloud;
using MathNet.Numerics.RootFinding;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Legacy.Contracts.DownstreamProtocols;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Legacy.Contracts.Dto.PackageExitLockDto;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using UploadResponse = JayTom.Dws.Integrations.Contracts.UploadResponse;
using FormulaNumber = System.Decimal;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;
using SortingExitType = JayTom.Dws.Application.Events.SortingExitType;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig.ConnectionParams;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;
using PushAlternateExitSorterEvent = JayTom.Dws.Application.Events.PushAlternateExitSorterEvent;

namespace JayTom.Dws.Client.Service.Sorting
{

    public class DefaultSortingService : ISortingService, IAsyncDisposable
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;
        private readonly ISettingsStore _settingsStore;
        /// <summary>提供运行期包裹会话，用于核对 API 响应和待发送格口指令的包裹身份。</summary>
        private readonly IPackageSessionStore _packageSessionStore;
        private readonly ISortingRuleCatalog<LogisticsRegexInfoModel> _logisticsRegexRepository;
        private readonly ISortingConfigurationCatalog<LogisticsCodeRecognitionInfoModel> _logisticsCodeRecognitionRepository;
        private readonly IPackageExitManagement _packageExitDefinitionRepository;
        private readonly ISortingConfigurationCatalog<BarCodeSortingInfoModel> _barCodeSortingRepository;
        private readonly ISortingRuleCatalog<BarCodeRegexInfoModel> _barCodeRegexRepository;
        private readonly ISortingConfigurationCatalog<LogisticsSortingInfoModel> _logisticsSortingRepository;
        private readonly ISortingRuleCatalog<LogisticsRuleInfoModel> _logisticsRuleRepository;
        private readonly ISortingConfigurationCatalog<OcrSortingInfoModel> _ocrSortingRepository;
        private readonly ISortingRuleCatalog<OcrRuleInfoModel> _ocrRuleRepository;
        private readonly ISortingInstructionBindingCatalog _sortingInstructionBindingRepository;
        private readonly ISortingInstructionBindingCatalog _sortingInstructionRepository;
        private readonly ISortingConfigurationCatalog<VolumeSortingInfoModel> _volumeSortingRepository;
        private readonly ISortingConfigurationCatalog<WeightSortingInfoModel> _weightSortingRepository;
        private readonly ISortingRuleCatalog<VolumeRuleInfoModel> _volumeRuleRepository;
        private readonly ISortingRuleCatalog<WeightRuleInfoModel> _weightRuleRepository;
        private readonly ISortingConfigurationCatalog<ApiSortingInfoModel> _apiSortingRepository;
        private readonly ISortingConnectionService _sortingConnectionService;
        private readonly ICommunicationConfigurationCatalog _communicationConnectionConfigRepository;

        private readonly ISortingRuleCatalog<ApiRuleInfoModel> _apiRuleRepository;
        private readonly IExitMonitor _exitMonitor;
        private readonly IStackedPackageService _stackedPackageService;
        private readonly IGrayscaleService _grayscaleService;

        private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
        /// <summary>
        /// 按到期时间调度分拣工作的通道，避免一个包裹的配置延迟阻塞后续包裹。
        /// </summary>
        private readonly MonotonicDeadlineScheduler _sortingDeadlineScheduler;
        /// <summary>按提交顺序执行已经到期的分拣工作，避免线程池饥饿影响格口响应。</summary>
        private readonly AsyncOrderedDispatcher<Func<Task>> _sortingDispatcher;
        /// <summary>最近一次报告分拣队列性能水位的单调时钟时间戳。</summary>
        private long _lastSortingPerformanceReportTimestamp = Stopwatch.GetTimestamp();
        /// <summary>
        /// 用户配置正则的编译缓存。
        /// </summary>
        private readonly ConcurrentDictionary<string, Regex> _regexCache =
            new(StringComparer.Ordinal);
        /// <summary>
        /// 重量分拣公式的已编译委托缓存。
        /// </summary>
        private readonly ConcurrentDictionary<string, Func<FormulaNumber, bool>> _weightFormulaCache =
            new(StringComparer.Ordinal);
        /// <summary>
        /// 体积分拣公式的已编译委托缓存。
        /// </summary>
        private readonly ConcurrentDictionary<string, Func<FormulaNumber, FormulaNumber, FormulaNumber, FormulaNumber, bool>> _volumeFormulaCache =
            new(StringComparer.Ordinal);
        /// <summary>保护 API 响应的一次性消费状态。</summary>
        /// <summary>记录已经消费 API 响应的包裹创建时间，防止重试或重复响应再次赋格口。</summary>
        private readonly ConcurrentDictionary<long, byte> _consumedApiCorrelations = new();
        /// <summary>暂存设备已启动但分拣配置尚未就绪期间到达的完成与 API 事件。</summary>
        private readonly Queue<PendingSortingEvent> _pendingStartupEvents = new();
        /// <summary>仅在启动切换期间保护暂存事件和就绪标记。</summary>
        private readonly object _startupEventGate = new();
        /// <summary>一表示分拣配置和物理连接已经就绪，可以消费事件。</summary>
        private int _eventProcessingReady;
        /// <summary>
        /// 用户可配正则的最长单次匹配时间。
        /// </summary>
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

        //private ConcurrentDictionary<DateTime, long> _stackedPackageGuidItems = new();
        private SortingMethodDto _sortingMethodDto = new();

        private SortingMethodDto CurrentSortingMethod =>
            Volatile.Read(ref _sortingMethodDto);

        /// <summary>获取当前分拣模式是否要求写入物理格口指令。</summary>
        public bool RequiresSortingInstruction =>
            Volatile.Read(ref _eventProcessingReady) == 0 ||
            CurrentSortingMethod.SortMode != SortMode.None;

        private PackageExitLockSettingsDto _packageExitLockSettingsDto = new();

        private StackedPackageDetectionSettingsDto? _stackedPackageDetectionSettingsDto = new();

        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        #region 配置

        private ApiSettingsDto _apiSettingsDto = new();
        private LogisticsRegexInfoModel[] _logisticsRegexInfos = [];
        private LogisticsCodeRecognitionInfoModel[] _logisticsCodeRecognitionInfos =
            [];
        /// <summary>一次性发布的格口配置与索引快照，杜绝配置切换时的新旧版本混读。</summary>
        private SortingExitSnapshot _exitSnapshot = SortingExitSnapshot.Empty;
        private List<BarCodeSortingInfoModel> _barCodeSortingInfoModels = new();
        private List<BarCodeRegexInfoModel> _barCodeRegexInfos = new();
        private List<LogisticsSortingInfoModel> _logisticsSortingInfoModels = new();
        private List<LogisticsRuleInfoModel> _logisticsRuleInfoModels = new();
        private List<OcrSortingInfoModel> _ocrSortingInfoModels = new();
        private List<OcrRuleInfoModel> _ocrRuleInfoModels = new();
        /// <summary>按格口编号直接定位已启用的指令绑定。</summary>
        private IReadOnlyDictionary<long, SortingInstructionBindingInfoModel>
            _sortingInstructionBindingByExit =
                new Dictionary<long, SortingInstructionBindingInfoModel>();
        /// <summary>按绑定编号保存预分组的指令数组。</summary>
        private IReadOnlyDictionary<long, SortingInstructionInfoModel[]>
            _sortingInstructionsByBinding =
                new Dictionary<long, SortingInstructionInfoModel[]>();
        private List<VolumeSortingInfoModel> _volumeSortingInfoModels = new();
        private List<WeightSortingInfoModel> _weightSortingInfoModels = new();
        private List<VolumeRuleInfoModel> _volumeRuleInfoModels = new();
        private List<WeightRuleInfoModel> _weightRuleInfoModels = new();
        /// <summary>拥有 API 规则解析和目标格口索引的独立组件。</summary>
        private readonly ApiSortingRuleEvaluator _apiRuleEvaluator = new();
        /// <summary>按模式定位兼容分拣策略的注册表。</summary>
        private readonly LegacySortingStrategyRegistry _strategyRegistry;
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

        public DefaultSortingService(ISettingsStore settingsStore,
            IPackageSessionStore packageSessionStore,
           ISortingRuleCatalog<LogisticsRegexInfoModel> logisticsRegexRepository,
            ISortingConfigurationCatalog<LogisticsCodeRecognitionInfoModel> logisticsCodeRecognitionRepository,
            IPackageExitManagement packageExitDefinitionRepository,
            ISortingConfigurationCatalog<BarCodeSortingInfoModel> barCodeSortingRepository,
            ISortingRuleCatalog<BarCodeRegexInfoModel> barCodeRegexRepository,
            ISortingConfigurationCatalog<LogisticsSortingInfoModel> logisticsSortingRepository,
            ISortingRuleCatalog<LogisticsRuleInfoModel> logisticsRuleRepository,
            ISortingConfigurationCatalog<OcrSortingInfoModel> ocrSortingRepository,
            ISortingRuleCatalog<OcrRuleInfoModel> ocrRuleRepository,
            ISortingInstructionBindingCatalog sortingInstructionBindingRepository,
            ISortingInstructionBindingCatalog sortingInstructionRepository,
            ISortingConfigurationCatalog<VolumeSortingInfoModel> volumeSortingRepository,
            ISortingConfigurationCatalog<WeightSortingInfoModel> weightSortingRepository,
            ISortingRuleCatalog<VolumeRuleInfoModel> volumeRuleRepository,
            ISortingRuleCatalog<WeightRuleInfoModel> weightRuleRepository,
            ISortingConfigurationCatalog<ApiSortingInfoModel> apiSortingRepository,
           ISortingConnectionService sortingConnectionService,
           ICommunicationConfigurationCatalog communicationConnectionConfigRepository,
            ISortingRuleCatalog<ApiRuleInfoModel> apiRuleRepository,
            IExitMonitor exitMonitor,
            IStackedPackageService stackedPackageService,
            IGrayscaleService grayscaleService,
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            _settingsStore = settingsStore;
            _packageSessionStore = packageSessionStore;
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
            _strategyRegistry = new LegacySortingStrategyRegistry(
            [
                new(SortMode.BarcodeSorting, BarcodeSorting),
                new(SortMode.WeightSorting, WeightSorting),
                new(SortMode.VolumeSorting, VolumeSorting),
                new(SortMode.LogisticsSorting, LogisticsSorting),
                new(SortMode.OcrSorting, OcrSorting),
                new(SortMode.ApiResponseSorting, ApiResponseSorting),
                new(SortMode.CombinedWorkflowSorting, CombinedWorkflowSorting)
            ]);
            _sortingDeadlineScheduler = new MonotonicDeadlineScheduler(
                "SortingDeadlines",
                ThreadPriority.AboveNormal);
            _sortingDispatcher = new AsyncOrderedDispatcher<Func<Task>>(
                static work => work(),
                (_, exception) =>
                {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Error(exception, "分拣任务执行失败");
                    OnExceptionOccurred(new ExceptionEventArgs
                    {
                        ExceptionMessage = exception.Message
                    });
                });
            _packageSessionStore.PackageRemoved += (_, args) =>
                RemoveConsumedApiCorrelation(args.RemovedPackage.CreateTime.Ticks);

            //事件
            _sortingConnectionService.HeartbeatError += delegate (object? sender, Exception exception)
            {
                OnHeartbeatError(exception);
            };
            _sortingConnectionService.SendError += delegate (object? sender, ExceptionEventArgs args)
            {
                OnSendError(new ExceptionEventArgs()
                {
                    ExceptionMessage = args.ExceptionMessage,
                });
            };
            _sortingConnectionService.CommunicationExceptionEvent += delegate (object? sender, Exception exception)
            {
                OnExceptionOccurred(new ExceptionEventArgs()
                {
                    ExceptionMessage = exception.ToString() ?? string.Empty
                });
            };
            _sortingConnectionService.ReceivedInstructionsEvent += delegate (object? sender, DeviceDecodeResult result)
            {
                switch (result.Type)
                {
                    case FunctionType.CreatePackage:
                        //创建包裹
                        OnCreatePackageEvent(new PackageInstructionEventArgs()
                        {
                            Keyword = result.Keyword,
                            Instruction = result.RawContent,
                            InstructionTime = result.Time,
                            ConnectionName = result.ConnectionName
                        });
                        break;

                    case FunctionType.RemovePackage:
                        //移除包裹
                        OnRemovePackageEvent(new PackageInstructionEventArgs()
                        {
                            Keyword = result.Keyword,
                            Instruction = result.RawContent,
                            InstructionTime = result.Time,
                            ConnectionName = result.ConnectionName
                        });
                        break;

                    case FunctionType.PackageException:
                        //包裹异常
                        OnPackageException(new PackageInstructionEventArgs()
                        {
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
                        OnPreSignalReplyReceived(new PackageInstructionEventArgs()
                        {
                            Keyword = result.Keyword,
                            Instruction = result.RawContent,
                            InstructionTime = result.Time,
                            ConnectionName = result.ConnectionName
                        });
                        break;

                    case FunctionType.SequenceBindingReply:
                        //车号绑定回复
                        OnSequenceBinding(new PackageInstructionEventArgs()
                        {
                            Keyword = result.Keyword,
                            Instruction = result.RawContent,
                            InstructionTime = result.Time,
                            ConnectionName = result.ConnectionName
                        });
                        break;

                    case FunctionType.ResetButtonTrigger:
                        //复位按钮触发
                        OnResetButtonTrigger(new PackageInstructionEventArgs()
                        {
                            Keyword = result.Keyword,
                            Instruction = result.RawContent,
                            InstructionTime = result.Time,
                            ConnectionName = result.ConnectionName
                        });
                        break;

                    case FunctionType.PackageExceptionEx:

                        OnPackageExceptionEx(new PackageInstructionEventArgs()
                        {
                            Keyword = result.Keyword,
                            Instruction = result.RawContent,
                            InstructionTime = result.Time,
                            ConnectionName = result.ConnectionName
                        });
                        break;
                }
            };
            //格口更改
            _eventBus.Subscribe<LogisticsCodeRecognitionInfoModel>(models =>
            {
                ReloadLogisticsCodeRecognitionAsync()
                    .Forget("重新加载物流识别配置");
            });
            _eventBus.Subscribe<LogisticsRegexInfoModel>(models =>
            {
                ReloadLogisticsRegexAsync()
                    .Forget("重新加载物流正则配置");
            });
            _eventBus.Subscribe<PackageExitDefinitionInfoModel>(models =>
            {
                ReloadPackageExitDefinitionsAsync()
                    .Forget("重新加载格口配置");
            });
            _eventBus.Subscribe<SortingInstructionBindingInfoModel>(changedBinding =>
            {
                ReloadInstructionLookupsAsync()
                    .Forget("重新加载指令索引");
            });
            _eventBus.Subscribe<SortingInstructionInfoModel>(changedInstruction =>
            {
                ReloadInstructionLookupsAsync()
                    .Forget("重新加载指令绑定索引");
            });
            //触发方式
            _eventBus.SubscribePackage<PackageInfo>(item =>
            {
                if (item is { } model)
                {
                    if (!TryQueueStartupEvent(new PendingSortingEvent(model, null)))
                    {
                        ProcessCompletedPackageForSorting(model);
                    }
                }
            });
            //Api触发
            _eventBus.SubscribePackage<ApiResponseReceived>(item =>
            {
                if (item is { } model)
                {
                    if (!TryQueueStartupEvent(new PendingSortingEvent(null, model)))
                    {
                        ProcessApiResponseForSorting(model);
                    }
                }
            });
            //Ocr触发
            _eventBus.Subscribe<PackageOcrInfo>(item =>
            {
                if (item is { } model)
                {
                    if (!TryQueueStartupEvent(new PendingSortingEvent(null, null, model)))
                    {
                        ProcessOcrForSorting(model);
                    }
                }
            });

            //备用格口分拣
            _eventBus.Subscribe<PushAlternateExitSorterEvent>(item =>
            {
                if (item is { } model)
                {
                    SubSorting(new SortingParam
                    {
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
            _exitMonitor.LockExitEvent += (sender, model) =>
            {
                ReplaceExitLockStatus(model);
            };
            //解锁
            _exitMonitor.UnLockExitEvent += (sender, model) =>
            {
                ReplaceExitLockStatus(model);
            };
        }

        public void ExecuteSorting(SortingParam param, CancellationToken token = default)
        {
            _strategyRegistry.TryExecute(CurrentSortingMethod.SortMode, param, token);
        }

        public bool IsConnected => _sortingConnectionService.IsConnected;

        public async Task<LogisticsCodeRecognitionInfoModel?> GetLogisticsInfo(string barCode)
        {
            var codeRecognitionInfos = Volatile.Read(ref _logisticsCodeRecognitionInfos);
            if (codeRecognitionInfos.Length == 0)
            {
                await ReloadLogisticsCodeRecognitionAsync();
                codeRecognitionInfos = Volatile.Read(ref _logisticsCodeRecognitionInfos);
            }

            var regexInfos = Volatile.Read(ref _logisticsRegexInfos);
            if (regexInfos.Length == 0)
            {
                await ReloadLogisticsRegexAsync();
                regexInfos = Volatile.Read(ref _logisticsRegexInfos);
            }

            if (regexInfos.Length > 0)
            {
                var logisticsRegexInfoModel = regexInfos.FirstOrDefault(f =>
                {
                    try
                    {
                        var isMatch = IsRegexMatch(barCode, f.RegexPattern);
                        if (isMatch)
                        {
                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        return false;
                    }

                    return false;
                });
                if (logisticsRegexInfoModel is not null)
                {
                    return codeRecognitionInfos.FirstOrDefault(f =>
                        f.Id.Equals(logisticsRegexInfoModel.LogisticsId));
                }
            }

            return null;
        }

        public bool RunningStatus { get; private set; }

        /// <summary>在分拣服务就绪前按到达顺序暂存完成、API 和 OCR 事件。</summary>
        private bool TryQueueStartupEvent(PendingSortingEvent pendingEvent)
        {
            if (Volatile.Read(ref _eventProcessingReady) == 1)
            {
                return false;
            }

            lock (_startupEventGate)
            {
                if (_eventProcessingReady == 1)
                {
                    return false;
                }

                _pendingStartupEvents.Enqueue(pendingEvent);
                return true;
            }
        }

        /// <summary>原子切换到运行状态并在当前线程按到达顺序排空启动期事件。</summary>
        private void ActivateSortingEventProcessing()
        {
            lock (_startupEventGate)
            {
                Volatile.Write(ref _eventProcessingReady, 2);
            }

            while (true)
            {
                PendingSortingEvent pendingEvent;
                lock (_startupEventGate)
                {
                    if (_pendingStartupEvents.Count == 0)
                    {
                        Volatile.Write(ref _eventProcessingReady, 1);
                        return;
                    }
                    pendingEvent = _pendingStartupEvents.Dequeue();
                }

                try
                {
                    if (pendingEvent.CompletedPackage is not null)
                    {
                        ProcessCompletedPackageForSorting(pendingEvent.CompletedPackage);
                    }
                    else if (pendingEvent.ApiResponse is not null)
                    {
                        ProcessApiResponseForSorting(pendingEvent.ApiResponse);
                    }
                    else if (pendingEvent.OcrInfo is not null)
                    {
                        ProcessOcrForSorting(pendingEvent.OcrInfo);
                    }
                }
                catch (Exception exception)
                {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Error(exception, "消费启动期分拣事件失败");
                }
            }
        }

        /// <summary>按当前配置消费一个已完成包裹；无分拣模式时明确释放生命周期等待。</summary>
        private void ProcessCompletedPackageForSorting(PackageInfo model)
        {
            var sortingMethod = CurrentSortingMethod;
            if (sortingMethod.SortMode == SortMode.None)
            {
                model.MarkSortingInstructionNotRequired();
                return;
            }

            model.MarkSortingInstructionExpected();
            if (sortingMethod.SortMode != SortMode.ApiResponseSorting &&
                sortingMethod.SortMode != SortMode.CombinedWorkflowSorting &&
                sortingMethod.SortMode != SortMode.OcrSorting)
            {
                ExecuteSorting(new SortingParam
                {
                    Guid = model.Guid,
                    BarCode = model.BarCodeInfo?.Barcode ?? string.Empty,
                    Height = (decimal)(model.VolumeInfo?.FormattedHeight ?? 0),
                    ScanTime = model.BarCodeInfo?.ScanTime ?? DateTime.Now,
                    Weight = (decimal)(model.WeightInfo?.FormattedWeight ?? 0),
                    Length = (decimal)(model.VolumeInfo?.FormattedLength ?? 0),
                    Width = (decimal)(model.VolumeInfo?.FormattedWidth ?? 0),
                    Volume = (decimal)(model.VolumeInfo?.FormattedVolume ?? 0),
                    PackageCreationTime = model.CreateTime,
                    PackageCreationInstruction = model.PackageCreationInstruction,
                    IsCreatedByLowerMachine = model.IsCreatedByLowerMachine,
                    IsStackedPackage = model.IsStackedPackage ?? false,
                    LinkedCarCount = model.LinkedCarCount,
                    Timestamp = model.Timestamp
                });
            }
        }

        /// <summary>消费 API 响应前执行活动包裹的完整身份与一次性消费校验。</summary>
        private void ProcessApiResponseForSorting(ApiResponseReceived model)
        {
            if (CurrentSortingMethod.SortMode != SortMode.ApiResponseSorting)
            {
                return;
            }
            if (!TryConsumeApiCorrelation(model, out var rejectionReason))
            {
                NLog.LogManager.GetCurrentClassLogger().Error(
                    $"拒绝未通过包裹身份校验的 API 格口响应:{rejectionReason}");
                OnExceptionOccurred(new ExceptionEventArgs
                {
                    ExceptionMessage = $"API 格口响应与运行包裹不匹配，已禁止发送:{rejectionReason}"
                });
                return;
            }

            ExecuteSorting(new SortingParam
            {
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

        /// <summary>按当前 OCR 分拣模式消费识别结果。</summary>
        private void ProcessOcrForSorting(PackageOcrInfo model)
        {
            if (CurrentSortingMethod.SortMode != SortMode.OcrSorting)
            {
                return;
            }

            ExecuteSorting(new SortingParam
            {
                Guid = model.RecognitionTimestamp,
                BarCode = model.BarCode ?? string.Empty,
                ScanTime = model.RecognitionTime,
                PackageCreationTime = model.RecognitionTime,
                PackageCreationInstruction = null,
                IsCreatedByLowerMachine = false,
                OcrInfo = model,
                IsStackedPackage = model.IsStackedPackage,
                Timestamp = model.RecognitionTimestamp
            });
        }

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

        /// <summary>
        /// 在生命周期锁内启动分拣子服务和通信连接。
        /// </summary>
        private async Task<KeyValuePair<bool, string>> StartCore(CancellationToken token)
        {
            //连接
            //心跳包
            if (!RunningStatus)
            {

                #region 读取配置信息

                try
                {
                    _apiSettingsDto = await _settingsStore.GetAsync<ApiSettingsDto>("ApiSettings", token) ?? new ApiSettingsDto();
                    Volatile.Write(
                        ref _sortingMethodDto,
                        await _settingsStore.GetAsync<SortingMethodDto>("SortingMethodSettings", token) ??
                        new SortingMethodDto());
                    _stackedPackageDetectionSettingsDto = await _settingsStore.GetAsync<StackedPackageDetectionSettingsDto>("StackedPackageDetectionSettings", token) ?? new StackedPackageDetectionSettingsDto();

                    _packageExitLockSettingsDto = await _settingsStore.GetAsync<PackageExitLockSettingsDto>("PackageExitLockSettings", token) ?? new PackageExitLockSettingsDto();

                    PublishExitDefinitionSnapshot(
                        [.. (await _packageExitDefinitionRepository.ListAsync(token))]);

                    Volatile.Write(
                        ref _logisticsCodeRecognitionInfos,
                        [.. (await _logisticsCodeRecognitionRepository.ListAsync(token))]);
                    Volatile.Write(
                        ref _logisticsRegexInfos,
                        [.. (await _logisticsRegexRepository.ListAsync(token))]);
                    _barCodeSortingInfoModels = [.. await _barCodeSortingRepository.ListAsync(token)];
                    _barCodeRegexInfos = [.. await _barCodeRegexRepository.ListAsync(token)];
                    _logisticsSortingInfoModels = [.. await _logisticsSortingRepository.ListAsync(token)];
                    _logisticsRuleInfoModels = [.. await _logisticsRuleRepository.ListAsync(token)];
                    _ocrSortingInfoModels = [.. await _ocrSortingRepository.ListAsync(token)];
                    _ocrRuleInfoModels = [.. await _ocrRuleRepository.ListAsync(token)];

                    var sortingInstructionBindings =
                        await _sortingInstructionBindingRepository.ListAsync(token);
                    var sortingInstructions =
                        await _sortingInstructionRepository.ListInstructionsAsync(token);
                    RebuildInstructionLookup(
                        sortingInstructionBindings,
                        sortingInstructions);
                    _volumeSortingInfoModels = [.. await _volumeSortingRepository.ListAsync(token)];
                    _volumeRuleInfoModels = [.. await _volumeRuleRepository.ListAsync(token)];
                    _weightSortingInfoModels = [.. await _weightSortingRepository.ListAsync(token)];
                    _weightRuleInfoModels = [.. await _weightRuleRepository.ListAsync(token)];
                    var apiRules = await _apiRuleRepository.ListAsync(token);
                    var apiSorting = await _apiSortingRepository.ListAsync(token);
                    _apiRuleEvaluator.Replace(apiRules, apiSorting);

                    _connectionConfigInfoModels =
                        [.. await _communicationConnectionConfigRepository.ListWithDetailsAsync(token)];

                    await _sortingConnectionService.ConfigurationInitializer();
                    var (key, value) = await _exitMonitor.Start(token);
                    if (!key)
                    {
                        OnExceptionOccurred(new ExceptionEventArgs()
                        {
                            ExceptionMessage = $"{value}"
                        });
                        await StopCore(token);
                        return new KeyValuePair<bool, string>(false, value);
                    }
                    var (b, value1) = await _stackedPackageService.Start(token);
                    if (!b)
                    {
                        OnExceptionOccurred(new ExceptionEventArgs()
                        {
                            ExceptionMessage = $"{value1}"
                        });
                        await StopCore(token);
                        return new KeyValuePair<bool, string>(false, value1);
                    }

                    var (key1, s1) = await _grayscaleService.StartSensor();
                    if (!key1)
                    {
                        OnExceptionOccurred(new ExceptionEventArgs()
                        {
                            ExceptionMessage = $"{s1}"
                        });
                        await StopCore(token);
                        return new KeyValuePair<bool, string>(false, s1);
                    }
                }
                catch (Exception e)
                {
                    OnExceptionOccurred(new ExceptionEventArgs()
                    {
                        ExceptionMessage = $"分拣配置读取异常:{e}"
                    });
                    await StopCore(token);
                    return new KeyValuePair<bool, string>(false, $"分拣配置读取异常:{e.Message}");
                }

                #endregion 读取配置信息

                await _sortingConnectionService.DisconnectAll();
                foreach (var infoModel in _connectionConfigInfoModels)
                {
                    var communicationsType = (CommunicationsType)Enum.Parse(typeof(CommunicationsType), infoModel.CommunicationType.ToString());
                    var communicationProtocol = (CommunicationProtocol)Enum.Parse(typeof(CommunicationProtocol), infoModel.CommunicationProtocol.ToString());
                    if (communicationsType == CommunicationsType.SerialPort)
                    {
                        var (connected, message) = await _sortingConnectionService.AddConnection(communicationsType, communicationProtocol,
                             infoModel.ConnectionName, infoModel.SerialPortConfigInfo);
                        if (!connected)
                        {
                            await StopCore(token);
                            return new KeyValuePair<bool, string>(false, message);
                        }
                    }
                    else if (communicationsType == CommunicationsType.TCP)
                    {
                        var (connected, message) = await _sortingConnectionService.AddConnection(communicationsType, communicationProtocol,
                            infoModel.ConnectionName, infoModel.TcpConnectionConfigInfo);
                        if (!connected)
                        {
                            await StopCore(token);
                            return new KeyValuePair<bool, string>(false, message);
                        }
                    }
                }

                RunningStatus = true;
                ActivateSortingEventProcessing();
                return new KeyValuePair<bool, string>(true, "已启动");
            }
            return new KeyValuePair<bool, string>(true, "已启动,无需再次启动");
        }

        public async Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default)
        {
            await _lifecycleGate.WaitAsync(token);
            try
            {
                return await StopCore(token);
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }

        /// <summary>
        /// 在生命周期锁内停止分拣子服务和通信连接。
        /// </summary>
        private async Task<KeyValuePair<bool, string>> StopCore(CancellationToken token)
        {
            Volatile.Write(ref _eventProcessingReady, 0);
            try
            {
                //停止
                //关闭心跳包
                await _sortingConnectionService.DisconnectAll();
                await _exitMonitor.Stop(token);
                await _stackedPackageService.Stop(token);
                await _grayscaleService.StopSensor();
                RunningStatus = false;
                return new KeyValuePair<bool, string>(true, "已停止");
            }
            catch (Exception exception)
            {
                RunningStatus = false;
                return new KeyValuePair<bool, string>(false, $"停止分拣服务失败:{exception.Message}");
            }
        }

        public void ExceptionSorting(
            SortingParam param,
            PackageCloudAbnormalSortingType abnormalSortingType,
            CancellationToken token = default)
        {
            QueueSortingWork(() => ExceptionSortingAsync(param, abnormalSortingType, token));
        }

        private Task ExceptionSortingAsync(
            SortingParam param,
            PackageCloudAbnormalSortingType abnormalSortingType,
            CancellationToken token)
        {
            _eventBus.Publish(new ExceptionSortingReceived
            {
                ScanTime = param.ScanTime,
                BarCode = param.BarCode,
                Timestamp = param.Timestamp,
                PackageCloudAbnormalSortingType = abnormalSortingType
            });
            var packageExitDefinitionInfoModel = Volatile.Read(ref _exitSnapshot).ActiveAbnormalExit;
            if (packageExitDefinitionInfoModel is not null)
            {
                //执行分拣
                //判断指令提交方式
                Volatile.Read(ref _sortingInstructionBindingByExit).TryGetValue(
                    packageExitDefinitionInfoModel.Id,
                    out var sortingInstructionBindingInfoModel);

                if (sortingInstructionBindingInfoModel is not null)
                {
                    _eventBus.Publish(new SortingExitReceived
                    {
                        ScanTime = param.ScanTime,
                        BarCode = param.BarCode,
                        Timestamp = param.Timestamp,
                        ExitName = packageExitDefinitionInfoModel.ExitName,
                        ExitId = packageExitDefinitionInfoModel.Id,
                        ExitType = SortingExitType.PhysicalExit,
                        Type = packageExitDefinitionInfoModel.Type,
                        SortingParam = param
                    });
                    Volatile.Read(ref _sortingInstructionsByBinding).TryGetValue(
                        sortingInstructionBindingInfoModel.Id,
                        out var sortingInstructionInfoModels);

                    var attach = new InstructionsAttach
                        {
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
                            ExitId = packageExitDefinitionInfoModel.Id,
                            //先忽略物流
                            SortingMode = CurrentSortingMethod.SortMode,
                            PackageCreationTime = param.PackageCreationTime,
                            PackageCreationInstruction = param.PackageCreationInstruction ?? string.Empty,
                            IsCreatedByLowerMachine = param.IsCreatedByLowerMachine,
                            LinkedCarCount = param.LinkedCarCount
                        };
                    attach.ValidateBeforeSend = () =>
                        IsCurrentPackageIdentity(attach, out var reason) ? null : reason;
                    attach.OnSendSucceeded = () => MarkSortingInstructionSent(attach);
                    QueueSortingWork(
                        () =>
                        {
                            if (!IsCurrentPackageIdentity(attach, out var rejectionReason))
                            {
                                NLog.LogManager.GetCurrentClassLogger().Error(
                                    $"发送前包裹身份复核失败，已禁止异常格口指令:{rejectionReason}");
                                return Task.CompletedTask;
                            }

                            _sortingConnectionService.SendInstructions(
                                param.Tag ?? new object(),
                                sortingInstructionBindingInfoModel.ExitId ?? 0,
                            sortingInstructionInfoModels ?? [],
                                TimeSpan.FromMilliseconds(sortingInstructionBindingInfoModel.SendIntervalMilliseconds),
                                attach);
                            return Task.CompletedTask;
                        },
                        TimeSpan.FromMilliseconds(sortingInstructionBindingInfoModel.DelaySendMilliseconds));
                    //回调分拣消息
                }
            }

            return Task.CompletedTask;
        }

        public void BarcodeSorting(SortingParam param, CancellationToken token = default)
        {
            try
            {
                var barCodeRegexInfoModel = _barCodeRegexInfos.FirstOrDefault(f =>
                    IsRegexMatch(param.BarCode, f.RegexPattern));
                if (barCodeRegexInfoModel is not null)
                {
                    //取出对于格口id
                    var barCodeSortingInfoModel = _barCodeSortingInfoModels.FirstOrDefault(f =>
                        f.Id.Equals(barCodeRegexInfoModel.BarCodeSortingId));
                    if (barCodeSortingInfoModel is not null)
                    {
                        param.ExitId = barCodeSortingInfoModel.ExitId;
                        SubSorting(param, token);
                    }
                    else
                    {
                        //走异常口
                        ExceptionSorting(param, PackageCloudAbnormalSortingType.NoPhysicalMailbox, token);
                    }
                }
                else
                {
                    //走异常口
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule, token);
                }
            }
            catch (Exception e)
            {
                OnExceptionOccurred(new ExceptionEventArgs()
                {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void WeightSorting(SortingParam param, CancellationToken token = default)
        {
            //取出重量规则
            try
            {
                var weightRuleInfoModel = _weightRuleInfoModels.FirstOrDefault(f => ValidateWeight(f.Formula, param.Weight));
                if (weightRuleInfoModel is not null)
                {
                    //取出格口
                    var weightSortingInfoModel = _weightSortingInfoModels.FirstOrDefault(f =>
                        f.Id.Equals(weightRuleInfoModel.WeightSortingId));
                    if (weightSortingInfoModel is not null)
                    {
                        param.ExitId = weightSortingInfoModel.ExitId;
                        SubSorting(param, token);
                    }
                    else
                    {
                        //走异常口
                        ExceptionSorting(param, PackageCloudAbnormalSortingType.NoPhysicalMailbox, token);
                    }
                }
                else
                {
                    //走异常口
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule, token);
                }
            }
            catch (Exception e)
            {
                OnExceptionOccurred(new ExceptionEventArgs()
                {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void VolumeSorting(SortingParam param, CancellationToken token = default)
        {
            try
            {
                var volumeRuleInfoModel = _volumeRuleInfoModels.FirstOrDefault(f => ValidateVolume(f.Formula, param.Length, param.Width, param.Height, param.Volume));
                if (volumeRuleInfoModel is not null)
                {
                    var volumeSortingInfoModel = _volumeSortingInfoModels.FirstOrDefault(f => f.Id.Equals(volumeRuleInfoModel.VolumeSortingId));
                    if (volumeSortingInfoModel is not null)
                    {
                        param.ExitId = volumeSortingInfoModel.ExitId;
                        SubSorting(param, token);
                    }
                    else
                    {
                        //走异常口
                        ExceptionSorting(param, PackageCloudAbnormalSortingType.NoPhysicalMailbox, token);
                    }
                }
                else
                {
                    //走异常口
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule, token);
                }
            }
            catch (Exception e)
            {
                OnExceptionOccurred(new ExceptionEventArgs()
                {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void LogisticsSorting(SortingParam param, CancellationToken token = default)
        {
            try
            {
                var logisticsRegexInfos = Volatile.Read(ref _logisticsRegexInfos);
                var logisticsRegexInfoModel = logisticsRegexInfos.FirstOrDefault(
                    f => IsRegexMatch(param.BarCode, f.RegexPattern));
                if (logisticsRegexInfoModel is not null)
                {
                    //取出物流
                    var logisticsRuleInfoModel = _logisticsRuleInfoModels.FirstOrDefault(f => f.LogisticsId.Equals(logisticsRegexInfoModel.LogisticsId));
                    if (logisticsRuleInfoModel is not null)
                    {
                        var logisticsSortingInfoModel = _logisticsSortingInfoModels.FirstOrDefault(f =>
                            f.Id.Equals(logisticsRuleInfoModel.LogisticsSortingId));
                        if (logisticsSortingInfoModel is not null)
                        {
                            param.ExitId = logisticsSortingInfoModel.ExitId;
                            SubSorting(param, token);
                        }
                        else
                        {
                            //走异常口
                            ExceptionSorting(param, PackageCloudAbnormalSortingType.NoPhysicalMailbox, token);
                        }
                    }
                    else
                    {
                        //走异常口
                        ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule, token);
                    }
                }
                else
                {
                    //走异常口
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule, token);
                }
            }
            catch (Exception e)
            {
                OnExceptionOccurred(new ExceptionEventArgs()
                {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void OcrSorting(SortingParam param, CancellationToken token = default)
        {
            try
            {
                var ocrRuleInfoModel = _ocrRuleInfoModels.FirstOrDefault(f =>
                    ValidateOcrRule(param.OcrInfo, f.JsonContent));

                if (ocrRuleInfoModel != null)
                {
                    var ocrSortingInfoModel = _ocrSortingInfoModels.
                        FirstOrDefault(f =>
                            f.Id.Equals(ocrRuleInfoModel.OcrSortingId));

                    if (ocrSortingInfoModel is not null)
                    {
                        param.ExitId = ocrSortingInfoModel.ExitId;
                        SubSorting(param, token);
                    }
                    else
                    {
                        //走异常口
                        ExceptionSorting(param, PackageCloudAbnormalSortingType.NoPhysicalMailbox, token);
                    }
                }
                else
                {
                    //走异常口
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule, token);
                }
            }
            catch (Exception e)
            {
                OnExceptionOccurred(new ExceptionEventArgs()
                {
                    ExceptionMessage = $"分拣异常:{e}"
                });
            }
        }

        public void ApiResponseSorting(SortingParam param, CancellationToken token = default)
        {
            //先判断返回内容
            if (param.ApiResponse.ResponseContent.Contains("返回超时"))
            {
                ExceptionSorting(param, PackageCloudAbnormalSortingType.NetworkTimeout, token);
                return;
            }
            if (_apiSettingsDto.Type == ApiType.RoutDataApi)
            {

                #region 邮政额外定制

                if (param.ApiResponse.ResponseContent.Contains("段道"))
                {
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.PostSegmentNotFound, token);
                    return;
                }
                else if (param.ApiResponse.ResponseContent.Contains("非本机构"))
                {
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.PostNonLocalBarcode, token);
                    return;
                }

                #endregion 邮政额外定制
            }
            long? exitId = _apiRuleEvaluator.ResolveExitId(param.ApiResponse);
            if (exitId is not null)
            {
                param.ExitId = exitId.Value;
                SubSorting(param, token);
            }
            else
            {
                //走异常口
                ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule, token);
            }
        }

        public void CombinedWorkflowSorting(SortingParam param, CancellationToken token = default)
        {
        }

        public void SendPreSignal(int num, InstructionsAttach attach, CancellationToken token = default)
        {
            _sortingConnectionService.SendPreSignal(num, attach, token);
        }

        public void SendPackageInfoCompletedSignal(int num, InstructionsAttach attach, CancellationToken token = default)
        {
            _sortingConnectionService.SendPackageInfoCompletedSignal(num, attach, token);
        }

        public void SendPackageCenter(int num, InstructionsAttach attach, CancellationToken token = default)
        {
            _sortingConnectionService.SendPackageCenter(num, attach, token);
        }

        protected virtual void OnExceptionOccurred(ExceptionEventArgs e)
        {
            ExceptionOccurred?.Invoke(this, e);
        }

        protected virtual void OnHeartbeatError(Exception e)
        {
            HeartbeatError?.Invoke(this, e);
        }

        protected virtual void OnSendError(ExceptionEventArgs e)
        {
            SendError?.Invoke(this, e);
        }

        /// <summary>
        /// 具体分拣
        /// </summary>
        /// <param name="param"></param>
        /// <param name="token"></param>
        private void SubSorting(SortingParam param, CancellationToken token = default)
        {
            QueueSortingWork(() => SubSortingAsync(param, token));
        }

        private Task SubSortingAsync(
            SortingParam param,
            CancellationToken token)
        {
            //取出格口指令
            //判断格口是否生效
            if (param.IsStackedPackage && _stackedPackageDetectionSettingsDto?.IsAutoExceptionSorting == true)
            {
                //走异常口
                ExceptionSorting(param, PackageCloudAbnormalSortingType.StackedPackage, token);
                return Task.CompletedTask;
            }
            var exitSnapshot = Volatile.Read(ref _exitSnapshot);
            exitSnapshot.ExitLookup.TryGetValue(
                param.ExitId,
                out var packageExitDefinitionInfoModel);
            if (packageExitDefinitionInfoModel is { IsActive: false })
            {
                packageExitDefinitionInfoModel = null;
            }
            if (packageExitDefinitionInfoModel is not null)
            {
                var effectiveExitDefinition = packageExitDefinitionInfoModel;
                var effectiveExitIdentifier = packageExitDefinitionInfoModel.Id;
                if (packageExitDefinitionInfoModel.IsLockExit || param.IsAlternateExitInstruction)
                {
                    //判断备用格口

                    exitSnapshot.AlternateExitByParent.TryGetValue(
                        packageExitDefinitionInfoModel.Id,
                        out var alternateExitDefinition);
                    /*if (exitDefinitionInfoModel is null || _packageExitLockSettingsDto.IsAutoExceptionSorting) {
                        ExceptionSorting(param, PackageCloudAbnormalSortingType.LockExit, token);
                        return;
                    }
                    param.ExitId = exitDefinitionInfoModel.Id;*/
                    if (alternateExitDefinition is not null)
                    {
                        effectiveExitDefinition = alternateExitDefinition;
                        effectiveExitIdentifier = alternateExitDefinition.Id;
                        param.ExitId = effectiveExitIdentifier;
                    }
                    else
                    {
                        //判断去异常口
                        if (_packageExitLockSettingsDto.IsAutoExceptionSorting)
                        {
                            ExceptionSorting(param, PackageCloudAbnormalSortingType.LockExit, token);
                            return Task.CompletedTask;
                        }
                    }
                }

                Volatile.Read(ref _sortingInstructionBindingByExit).TryGetValue(
                    effectiveExitIdentifier,
                    out var sortingInstructionBindingInfoModel);
                if (sortingInstructionBindingInfoModel is not null)
                {
                    //回调格口信息
                    _eventBus.Publish(new SortingExitReceived
                    {
                        ScanTime = param.ScanTime,
                        BarCode = param.BarCode,
                        Timestamp = param.Timestamp,
                        ExitName = effectiveExitDefinition.ExitName ?? string.Empty,
                        ExitId = effectiveExitIdentifier,
                        ExitType = SortingExitType.PhysicalExit,
                        Type = effectiveExitDefinition.Type,
                        SortingParam = param
                    });

                    //执行分拣
                    //判断指令提交方式
                    Volatile.Read(ref _sortingInstructionsByBinding).TryGetValue(
                        sortingInstructionBindingInfoModel.Id,
                        out var sortingInstructionInfoModels);
                    var attach = new InstructionsAttach
                        {
                            BarCode = param.BarCode,
                            ExitName = effectiveExitDefinition.ExitName ?? string.Empty,
                            Guid = param.Guid,
                            Height = param.Height,
                            Length = param.Length,
                            Timestamp = param.Timestamp,
                            Volume = param.Volume,
                            Width = param.Width,
                            Weight = param.Weight,
                            ScanTime = param.ScanTime,
                            ExitId = effectiveExitIdentifier,
                            //先忽略物流
                            SortingMode = CurrentSortingMethod.SortMode,
                            PackageCreationTime = param.PackageCreationTime,
                            PackageCreationInstruction = param.PackageCreationInstruction ?? string.Empty,
                            IsCreatedByLowerMachine = param.IsCreatedByLowerMachine,
                            LinkedCarCount = param.LinkedCarCount
                        };
                    attach.ValidateBeforeSend = () =>
                        IsCurrentPackageIdentity(attach, out var reason) ? null : reason;
                    attach.OnSendSucceeded = () => MarkSortingInstructionSent(attach);
                    QueueSortingWork(
                        () =>
                        {
                            if (!IsCurrentPackageIdentity(attach, out var rejectionReason))
                            {
                                NLog.LogManager.GetCurrentClassLogger().Error(
                                    $"发送前包裹身份复核失败，已禁止格口指令:{rejectionReason}");
                                return Task.CompletedTask;
                            }

                            _sortingConnectionService.SendInstructions(
                                param.Tag ?? new object(),
                                effectiveExitIdentifier,
                                sortingInstructionInfoModels ?? [],
                                TimeSpan.FromMilliseconds(sortingInstructionBindingInfoModel.SendIntervalMilliseconds),
                                attach);
                            return Task.CompletedTask;
                        },
                        TimeSpan.FromMilliseconds(sortingInstructionBindingInfoModel.DelaySendMilliseconds));
                    //回调分拣消息
                    //NLog.LogManager.GetCurrentClassLogger().Error($"SubSorting:{param.LinkedCarCount}");
                }
                else
                {
                    //走异常口
                    ExceptionSorting(param, PackageCloudAbnormalSortingType.NoSortingInstruction, token);
                }
            }
            else
            {
                //走异常口
                ExceptionSorting(param, PackageCloudAbnormalSortingType.NoPhysicalMailbox, token);
            }

            return Task.CompletedTask;
        }

        /// <summary>严格核对 API 响应与活动包裹，并原子地确保同一包裹只消费一次响应。</summary>
        private bool TryConsumeApiCorrelation(ApiResponseReceived response, out string rejectionReason)
        {
            var activePackage = _packageSessionStore.GetPackage(response.PackageCreationTime);
            if (activePackage is null)
            {
                rejectionReason = $"创建时间未指向活动包裹:Timestamp={response.Timestamp}";
                return false;
            }

            if (!IsCurrentPackageIdentity(activePackage, response, out rejectionReason))
            {
                return false;
            }

            if (!_consumedApiCorrelations.TryAdd(response.PackageCreationTime.Ticks, 0))
            {
                rejectionReason = $"同一包裹的 API 响应已消费:Timestamp={response.Timestamp}";
                return false;
            }

            rejectionReason = string.Empty;
            return true;
        }

        /// <summary>核对活动包裹与 API 响应携带的完整身份。</summary>
        private static bool IsCurrentPackageIdentity(
            PackageInfo? package,
            ApiResponseReceived response,
            out string rejectionReason)
        {
            if (package is null)
            {
                rejectionReason = $"活动包裹已不存在:Timestamp={response.Timestamp}";
                return false;
            }

            lock (package.SyncRoot)
            {
                if (!package.IsCompleted ||
                    package.Timestamp != response.Timestamp ||
                    package.Guid != response.Guid ||
                    package.CreateTime.Ticks != response.PackageCreationTime.Ticks ||
                    package.BarCodeInfo is null ||
                    !string.Equals(package.BarCodeInfo.Barcode, response.Barcode, StringComparison.Ordinal) ||
                    package.BarCodeInfo.ScanTime.Ticks != response.ScanTime.Ticks)
                {
                    rejectionReason = $"活动包裹身份已变化:Timestamp={response.Timestamp},Guid={response.Guid},Barcode={response.Barcode}";
                    return false;
                }
            }

            rejectionReason = string.Empty;
            return true;
        }

        /// <summary>在配置延时结束后，根据指令附件再次核对活动包裹身份。</summary>
        private bool IsCurrentPackageIdentity(InstructionsAttach attach, out string rejectionReason)
        {
            var package = _packageSessionStore.GetPackage(attach.PackageCreationTime);
            if (package is null)
            {
                rejectionReason = $"活动包裹已不存在:Timestamp={attach.Timestamp}";
                return false;
            }

            lock (package.SyncRoot)
            {
                if (!package.IsCompleted ||
                    package.Timestamp != attach.Timestamp ||
                    package.Guid != attach.Guid ||
                    package.CreateTime.Ticks != attach.PackageCreationTime.Ticks ||
                    package.BarCodeInfo is null ||
                    !string.Equals(package.BarCodeInfo.Barcode, attach.BarCode, StringComparison.Ordinal) ||
                    package.BarCodeInfo.ScanTime.Ticks != attach.ScanTime?.Ticks)
                {
                    rejectionReason = $"活动包裹身份与待发指令不一致:Timestamp={attach.Timestamp},Guid={attach.Guid},Barcode={attach.BarCode}";
                    return false;
                }
            }

            rejectionReason = string.Empty;
            return true;
        }

        /// <summary>在物理连接确认全部指令已写入后更新对应活动包裹的生命周期状态。</summary>
        private void MarkSortingInstructionSent(InstructionsAttach attach)
        {
            var package = _packageSessionStore.GetPackage(attach.PackageCreationTime);
            if (package is not null &&
                IsCurrentPackageIdentity(attach, out _))
            {
                package.MarkSortingInstructionSent();
            }
        }

        /// <summary>包裹离开活动会话后清理一次性 API 消费状态。</summary>
        private void RemoveConsumedApiCorrelation(long creationTimeTicks)
        {
            _consumedApiCorrelations.TryRemove(creationTimeTicks, out _);
        }


        private bool ValidateOcrRule(PackageOcrInfo ocrInfo, string json)
        {
            try
            {
                var isValid = true;
                var ocrRuleJsonDto = JsonConvert.DeserializeObject<OcrRuleJsonDto>(json);
                if (ocrRuleJsonDto is not null)
                {
                    //判断三段码
                    if (isValid && ocrRuleJsonDto.IsUseThreeSegmentCodeValidation)
                    {
                        isValid = ocrInfo.ThreeSegmentCode.Contains(ocrRuleJsonDto.ThreeSegmentCodeContainsChars);
                    }
                    //是否使用发件人地址
                    if (isValid && ocrRuleJsonDto.IsUseSenderAddressValidation)
                    {
                        isValid = ocrInfo.SenderAddress.Contains(ocrRuleJsonDto.SenderAddressContainsChars);
                    }
                    //是否使用收件人地址
                    if (isValid && ocrRuleJsonDto.IsUseRecipientAddressValidation)
                    {
                        isValid = ocrInfo.RecipientAddress.Contains(ocrRuleJsonDto.RecipientAddressContainsChars);
                    }
                    //是否使用发件人手机号码
                    if (isValid && ocrRuleJsonDto.IsUseSenderPhoneNumberValidation)
                    {
                        isValid = ocrInfo.SenderPhone.EndsWith(ocrRuleJsonDto.SenderPhoneNumberEndsWith);
                    }

                    return isValid;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return false;
        }

        public bool ValidateWeight(string formula, FormulaNumber weight)
        {
            try
            {
                var validator = _weightFormulaCache.GetOrAdd(formula, static value =>
                {
                    var expression = DynamicExpressionParser.ParseLambda(
                        [Expression.Parameter(typeof(FormulaNumber), "weight")],
                        typeof(bool),
                        value);
                    return (Func<FormulaNumber, bool>)expression.Compile();
                });
                return validator(weight);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool ValidateVolume(
            string formula,
            FormulaNumber length,
            FormulaNumber width,
            FormulaNumber height,
            FormulaNumber volume)
        {
            try
            {
                var validator = _volumeFormulaCache.GetOrAdd(formula, static value =>
                {
                    ParameterExpression[] parameters =
                    [
                        Expression.Parameter(typeof(FormulaNumber), "Length"),
                        Expression.Parameter(typeof(FormulaNumber), "Width"),
                        Expression.Parameter(typeof(FormulaNumber), "Height"),
                        Expression.Parameter(typeof(FormulaNumber), "Volume")
                    ];
                    var expression = DynamicExpressionParser.ParseLambda(parameters, typeof(bool), value);
                    return (Func<FormulaNumber, FormulaNumber, FormulaNumber, FormulaNumber, bool>)
                        expression.Compile();
                });
                return validator(length, width, height, volume);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public void PublishExceptionSortingInfo1(SortingParam param)
        {
            if (param.ApiResponse.ExceptionMsg.Contains("接口访问返回超时"))
            {
                _eventBus.Publish(new ExceptionSortingReceived
                {
                    ScanTime = param.ScanTime,
                    BarCode = param.BarCode,
                    Timestamp = param.Timestamp,
                    PackageCloudAbnormalSortingType = PackageCloudAbnormalSortingType.NetworkTimeout
                });
            }
            else if (param.ApiResponse.ExceptionMsg.Contains("接口访问异常"))
            {
                _eventBus.Publish(new ExceptionSortingReceived
                {
                    ScanTime = param.ScanTime,
                    BarCode = param.BarCode,
                    Timestamp = param.Timestamp,
                    PackageCloudAbnormalSortingType = PackageCloudAbnormalSortingType.ApiAccessError
                });
            }
            else if (param.BarCode.ToLower().Equals("noread"))
            {
                _eventBus.Publish(new ExceptionSortingReceived
                {
                    ScanTime = param.ScanTime,
                    BarCode = param.BarCode,
                    Timestamp = param.Timestamp,
                    PackageCloudAbnormalSortingType = PackageCloudAbnormalSortingType.NoRead
                });
            }
        }

        private async Task ReloadLogisticsCodeRecognitionAsync()
        {
            try
            {
                var items = await _logisticsCodeRecognitionRepository.ListAsync();
                Volatile.Write(ref _logisticsCodeRecognitionInfos, [.. items]);
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger()
                    .Error(e, "刷新物流识别配置失败");
            }
        }

        private async Task ReloadLogisticsRegexAsync()
        {
            try
            {
                var items = await _logisticsRegexRepository.ListAsync();
                Volatile.Write(ref _logisticsRegexInfos, [.. items]);
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger()
                    .Error(e, "刷新物流正则配置失败");
            }
        }

        private async Task ReloadPackageExitDefinitionsAsync()
        {
            try
            {
                var items = await _packageExitDefinitionRepository.ListAsync();
                PublishExitDefinitionSnapshot([.. items]);
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger()
                    .Error(e, "刷新格口配置失败");
            }
        }

        /// <summary>在配置变化后异步重建格口指令只读索引。</summary>
        private async Task ReloadInstructionLookupsAsync()
        {
            try
            {
                var bindingsTask = _sortingInstructionBindingRepository.ListAsync();
                var instructionsTask = _sortingInstructionRepository.ListInstructionsAsync();
                await Task.WhenAll(bindingsTask, instructionsTask).ConfigureAwait(false);
                RebuildInstructionLookup(
                    await bindingsTask.ConfigureAwait(false),
                    await instructionsTask.ConfigureAwait(false));
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger()
                    .Error(e, "刷新格口指令索引失败");
            }
        }

        private void ReplaceExitLockStatus(PackageExitDefinitionInfoModel changedStatus)
        {
            SortingExitSnapshot current;
            SortingExitSnapshot updated;
            do
            {
                current = Volatile.Read(ref _exitSnapshot);
                var updatedItems = current.Items
                    .Select(item => item.Id == changedStatus.Id
                        ? CloneExitDefinition(item, changedStatus.IsLockExit)
                        : item)
                    .ToArray();
                updated = BuildExitDefinitionSnapshot(updatedItems);
            } while (!ReferenceEquals(
                         Interlocked.CompareExchange(
                             ref _exitSnapshot,
                             updated,
                             current),
                         current));
        }

        /// <summary>一次性编译格口到绑定及绑定到指令的查询快照。</summary>
        private void RebuildInstructionLookup(
            IEnumerable<SortingInstructionBindingInfoModel> bindings,
            IEnumerable<SortingInstructionInfoModel> instructions)
        {
            var bindingByExit = bindings
                .Where(binding => binding is { IsActive: true, ExitId: not null })
                .GroupBy(binding => binding.ExitId!.Value)
                .ToDictionary(group => group.Key, group => group.Last());
            var instructionsByBinding = instructions
                .GroupBy(instruction => instruction.InstructionBindingId)
                .ToDictionary(group => group.Key, group => group.ToArray());
            Volatile.Write(ref _sortingInstructionBindingByExit, bindingByExit);
            Volatile.Write(ref _sortingInstructionsByBinding, instructionsByBinding);
        }

        /// <summary>原子发布格口数组及其派生查询索引。</summary>
        private void PublishExitDefinitionSnapshot(PackageExitDefinitionInfoModel[] items)
        {
            Volatile.Write(ref _exitSnapshot, BuildExitDefinitionSnapshot(items));
        }

        /// <summary>根据格口数组构建可一次性原子发布的完整查询快照。</summary>
        private static SortingExitSnapshot BuildExitDefinitionSnapshot(
            PackageExitDefinitionInfoModel[] items)
        {
            var exitLookup = items
                .GroupBy(item => item.Id)
                .ToDictionary(group => group.Key, group => group.Last());
            var alternateExitByParent = items
                .Where(item => item is { IsLockExit: false, IsActive: true })
                .GroupBy(item => item.Pid)
                .ToDictionary(group => group.Key, group => group.First());
            var activeAbnormalExit = items.FirstOrDefault(item => item is
                { Type: ExitType.AbnormalExit, IsActive: true });
            return new SortingExitSnapshot(
                items,
                exitLookup,
                alternateExitByParent,
                activeAbnormalExit);
        }

        private static PackageExitDefinitionInfoModel CloneExitDefinition(
            PackageExitDefinitionInfoModel source,
            bool isLockExit)
        {
            return new PackageExitDefinitionInfoModel
            {
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

        private void QueueSortingWork(Func<Task> work, TimeSpan delay = default)
        {
            var normalizedDelay = delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
            if (normalizedDelay == TimeSpan.Zero)
            {
                EnqueueSortingWork(work);
                return;
            }
            _sortingDeadlineScheduler.Schedule(
                normalizedDelay,
                () => EnqueueSortingWork(work));
        }

        /// <summary>将已经到期的分拣任务移交到独立高优先级工作线程。</summary>
        private void EnqueueSortingWork(Func<Task> work)
        {
            if (!_sortingDispatcher.TryEnqueue(work))
            {
                OnExceptionOccurred(new ExceptionEventArgs
                {
                    ExceptionMessage = "分拣任务队列已停止，任务未执行"
                });
            }
            ReportSortingPerformanceWatermarkIfDue();
        }

        /// <summary>低频报告分拣计算队列排队、执行和积压水位。</summary>
        private void ReportSortingPerformanceWatermarkIfDue()
        {
            var now = Stopwatch.GetTimestamp();
            var previous = Volatile.Read(ref _lastSortingPerformanceReportTimestamp);
            if (Stopwatch.GetElapsedTime(previous, now) < TimeSpan.FromMinutes(1) ||
                Interlocked.CompareExchange(
                    ref _lastSortingPerformanceReportTimestamp,
                    now,
                    previous) != previous)
            {
                return;
            }

            var queueDelay = _sortingDispatcher.TakeMaximumQueueDelayMicroseconds();
            var handlerDuration = _sortingDispatcher.TakeMaximumHandlerDurationMicroseconds();
            var pending = _sortingDispatcher.PendingCount;
            if (queueDelay < 50_000 && handlerDuration < 50_000 && pending <= 32)
            {
                return;
            }

            NLog.LogManager.GetCurrentClassLogger().Warn(
                $"分拣热路径性能水位(us):排队={queueDelay},执行={handlerDuration},待处理={pending}");
        }

        /// <summary>
        /// 停止分拣工作通道并等待消费者退出。
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            _sortingDeadlineScheduler.Dispose();
            await _sortingDispatcher.DisposeAsync().ConfigureAwait(false);
            _lifecycleGate.Dispose();
        }

        /// <summary>
        /// 使用带超时的已编译正则执行匹配。
        /// </summary>
        private bool IsRegexMatch(string input, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return false;
            }

            var regex = _regexCache.GetOrAdd(
                pattern,
                static value => new Regex(
                    value,
                    RegexOptions.CultureInvariant | RegexOptions.Compiled,
                    RegexTimeout));
            return regex.IsMatch(input ?? string.Empty);
        }


        protected virtual void OnCreatePackageEvent(PackageInstructionEventArgs e)
        {
            CreatePackageEvent?.Invoke(this, e);
        }

        protected virtual void OnRemovePackageEvent(PackageInstructionEventArgs e)
        {
            RemovePackageEvent?.Invoke(this, e);
        }

        protected virtual void OnClearExceptionEvent(string e)
        {
            ClearExceptionEvent?.Invoke(this, e);
        }

        protected virtual void OnSendInstruction(PackageInstructionEventArgs e)
        {
            SendInstruction?.Invoke(this, e);
        }

        protected virtual void OnPackageException(PackageInstructionEventArgs e)
        {
            PackageException?.Invoke(this, e);
        }

        protected virtual void OnPreSignalReplyReceived(PackageInstructionEventArgs e)
        {
            PreSignalReplyReceived?.Invoke(this, e);
        }

        protected virtual void OnSequenceBinding(PackageInstructionEventArgs e)
        {
            SequenceBinding?.Invoke(this, e);
        }

        protected virtual void OnResetButtonTrigger(PackageInstructionEventArgs e)
        {
            ResetButtonTrigger?.Invoke(this, e);
        }

        protected virtual void OnFlowToEndOrException(PackageInstructionEventArgs e)
        {
            FlowToEndOrException?.Invoke(this, e);
        }

        protected virtual void OnPackageExceptionEx(PackageInstructionEventArgs e)
        {
            PackageExceptionEx?.Invoke(this, e);
        }
    }
}
