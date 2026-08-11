using System;
using DryIoc;
using System.Linq;
using System.Text;
using JayTom.Dws.Ocr;
using System.Threading;
using JayTom.Dws.Camera;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Plugin.Scale;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Domain.Repository;
using JayTom.Dws.Domain.Repository.LocalLog;
using JayTom.Dws.Client.Service.ExternalDataService;
using WindowsAction = JayTom.Dws.Domain.EventMediators.WindowsAction;
using WindowsActionType = JayTom.Dws.Domain.EventMediators.WindowsActionType;
using SettingsChangedEvent = JayTom.Dws.Domain.EventMediators.SettingsChangedEvent;
using TriggerPositionEvent = JayTom.Dws.Domain.EventMediators.TriggerPositionEvent;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    /// <summary>
    /// 日志处理器
    /// </summary>
    public class LogProcessingService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IAppLogRepository _appLogRepository;
        private readonly ICameraLogRepository _cameraLogRepository;
        private readonly ISortingLogRepository _sortingLogRepository;
        private readonly IWeighingLogRepository _weighingLogRepository;
        private readonly IVolumeLogRepository _volumeLogRepository;
        private readonly IApiLogRepository _apiLogRepository;
        private readonly IOutputLogRepository _outputLogRepository;
        private readonly IInputLogRepository _inputLogRepository;
        private readonly IOcrLogRepository _ocrLogRepository;
        private readonly IFtpLogRepository _ftpLogRepository;
        private readonly ICleanupLogRepository _cleanupLogRepository;
        private readonly IExceptionLogRepository _exceptionLogRepository;
        private readonly IDeviceService _deviceService;
        private readonly IExternalDataService _externalDataService;
        private readonly ISortingService _sortingService;
        private readonly IExitMonitor _exitMonitor;
        private readonly IStackedPackageService _stackedPackageService;
        /// <summary>每一种数据库日志在内存中允许等待写出的最大数量。</summary>
        private const int MaxPendingLogsPerCategory = 4096;
        /// <summary>诊断文本日志在内存中允许等待写出的最大数量。</summary>
        private const int MaxPendingDiagnosticLogs = 8192;
        /// <summary>日志丢弃汇总的最短报告间隔。</summary>
        private static readonly TimeSpan DroppedLogReportInterval = TimeSpan.FromMinutes(1);
        private readonly BoundedLogQueue<ExceptionLogInfoModel> _exceptionItems = new(MaxPendingLogsPerCategory);
        private readonly BoundedLogQueue<AppLogInfoModel> _appLogItems = new(MaxPendingLogsPerCategory);
        private readonly BoundedLogQueue<CameraLogInfoModel> _cameraLogItems = new(MaxPendingLogsPerCategory);
        private readonly BoundedLogQueue<SortingLogInfoModel> _sortingLogItems = new(MaxPendingLogsPerCategory);
        private readonly BoundedLogQueue<WeighingLogInfoModel> _weighingLogItems = new(MaxPendingLogsPerCategory);
        private readonly BoundedLogQueue<VolumeLogInfoModel> _volumeLogItems = new(MaxPendingLogsPerCategory);
        private readonly BoundedLogQueue<ApiLogInfoModel> _apiLogInfoItems = new(MaxPendingLogsPerCategory);
        private readonly BoundedLogQueue<OutputLogInfoModel> _outputLogItems = new(MaxPendingLogsPerCategory);
        private readonly BoundedLogQueue<InputLogInfoModel> _inputLogItems = new(MaxPendingLogsPerCategory);
        private readonly BoundedLogQueue<OcrLogInfoModel> _ocrLogItems = new(MaxPendingLogsPerCategory);
        private readonly BoundedLogQueue<FtpLogInfoModel> _ftpLogItems = new(MaxPendingLogsPerCategory);
        private readonly BoundedLogQueue<LogCleaningLogInfoModel> _logCleaningLogItems = new(MaxPendingLogsPerCategory);

        /// <summary>
        /// 缓存需要由后台循环写出的诊断日志文本。
        /// </summary>
        private readonly BoundedLogQueue<string> _diagnosticLogItems = new(MaxPendingDiagnosticLogs);
        private int _isWindowsClose;
        /// <summary>最近一次统计日志丢弃数量的单调时钟时间戳。</summary>
        private long _lastDroppedLogReportTimestamp;

        //LogCleaningLogInfoModel
        public LogProcessingService(IAppLogRepository appLogRepository,
            ICameraLogRepository cameraLogRepository,
            ISortingLogRepository sortingLogRepository,
            IWeighingLogRepository weighingLogRepository,
            IVolumeLogRepository volumeLogRepository,
            IApiLogRepository apiLogRepository,
            IOutputLogRepository outputLogRepository,
            IInputLogRepository inputLogRepository,
            IOcrLogRepository ocrLogRepository,
            IFtpLogRepository ftpLogRepository,
            ICleanupLogRepository cleanupLogRepository,
            IExceptionLogRepository exceptionLogRepository,
            IDeviceService deviceService,
            IExternalDataService externalDataService,
            ISortingService sortingService,
            IExitMonitor exitMonitor,
            IStackedPackageService stackedPackageService)
        {
            _appLogRepository = appLogRepository;
            _cameraLogRepository = cameraLogRepository;
            _sortingLogRepository = sortingLogRepository;
            _weighingLogRepository = weighingLogRepository;
            _volumeLogRepository = volumeLogRepository;
            _apiLogRepository = apiLogRepository;
            _outputLogRepository = outputLogRepository;
            _inputLogRepository = inputLogRepository;
            _ocrLogRepository = ocrLogRepository;
            _ftpLogRepository = ftpLogRepository;
            _cleanupLogRepository = cleanupLogRepository;
            _exceptionLogRepository = exceptionLogRepository;
            _deviceService = deviceService;
            _externalDataService = externalDataService;
            _sortingService = sortingService;
            _exitMonitor = exitMonitor;
            _stackedPackageService = stackedPackageService;
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(item =>
            {
                if (item is { } model)
                {
                    _appLogItems.Enqueue(new AppLogInfoModel()
                    {
                        Type = LogType.Information,
                        Message = $"更改配置:{model.SettingsName}"
                    });
                }
            });
            //异常日志
            EventAggregator.Instance.Subscribe<ExceptionLogInfoModel>(item =>
            {
                if (item is { } model)
                {
                    //添加
                    _exceptionItems.Enqueue(model);
                }
            });
            //程序运行日志
            EventAggregator.Instance.Subscribe<AppLogInfoModel>(item =>
            {
                if (item is { } model)
                {
                    //添加

                    _appLogItems.Enqueue(model);
                }
            });
            //相机日志
            EventAggregator.Instance.Subscribe<CameraLogInfoModel>(item =>
            {
                if (item is { } model)
                {
                    //添加
                    _cameraLogItems.Enqueue(model);
                }
            });
            //分拣日志
            EventAggregator.Instance.Subscribe<SortingLogInfoModel>(item =>
            {
                if (item is { } model)
                {
                    //添加

                    _sortingLogItems.Enqueue(model);
                }
            });

            //称重日志队列
            EventAggregator.Instance.Subscribe<WeighingLogInfoModel>(item =>
            {
                if (item is { } model)
                {
                    //添加

                    _weighingLogItems.Enqueue(model);
                }
            });
            //体积日志队列
            EventAggregator.Instance.Subscribe<VolumeLogInfoModel>(item =>
            {
                if (item is { } model)
                {
                    //添加

                    _volumeLogItems.Enqueue(model);
                }
            });
            //Api日志队列
            EventAggregator.Instance.Subscribe<ApiLogInfoModel>(item =>
            {
                if (item is { } model)
                {
                    //添加

                    _apiLogInfoItems.Enqueue(model);
                    _diagnosticLogItems.Enqueue(
                        $"{model.RequestTime:yyyy-MM-dd HH:mm:ss.fff}--[Api请求]-{model.RequestContent}");
                    _diagnosticLogItems.Enqueue(
                        $"{model.ResponseTime:yyyy-MM-dd HH:mm:ss.fff}--[Api响应]-{model.ResponseContent}");
                }
            });
            //输出日志队列
            EventAggregator.Instance.Subscribe<OutputLogInfoModel>(item =>
            {
                if (item is { } model)
                {
                    //添加

                    _outputLogItems.Enqueue(model);
                }
            });
            //输入日志队列
            EventAggregator.Instance.Subscribe<InputLogInfoModel>(item =>
            {
                if (item is { } model)
                {
                    //添加

                    _inputLogItems.Enqueue(model);

                    _diagnosticLogItems.Enqueue(
                        $"{model.CreateTime:yyyy-MM-dd HH:mm:ss.fff}--[输入]-{model.Message}");
                }
            });
            //Ocr日志队列
            EventAggregator.Instance.Subscribe<OcrLogInfoModel>(item =>
            {
                if (item is { } model)
                {
                    //添加

                    _ocrLogItems.Enqueue(model);
                }
            });
            //Ftp日志队列
            EventAggregator.Instance.Subscribe<FtpLogInfoModel>(item =>
            {
                if (item is { } model)
                {
                    //添加

                    _ftpLogItems.Enqueue(model);
                }
            });
            //清理记录队列
            EventAggregator.Instance.Subscribe<LogCleaningLogInfoModel>(item =>
            {
                if (item is { } model)
                {
                    //添加

                    _logCleaningLogItems.Enqueue(model);
                }
            });
            EventAggregator.Instance.Subscribe<WindowsAction>(item =>
            {
                if (item is { Type: WindowsActionType.Close })
                {
                    EventAggregator.Instance.Publish(new AppLogInfoModel
                    {
                        CreateTime = DateTime.Now,
                        Message = "程序关闭",
                        Type = LogType.Information
                    });
                    Interlocked.Exchange(ref _isWindowsClose, 1);
                }
            });

            _deviceService.BarcodeScanned += delegate (object? sender, BarcodeReadEventArgs args)
            {
                EventAggregator.Instance.Publish(new CameraLogInfoModel()
                {
                    Type = LogType.Information,
                    Message = $"相机:{args.CameraSerialNumber}获取到条码[{args.Barcode}]",
                    CameraSerialNumber = args.CameraSerialNumber,
                });
            };
            _deviceService.VolumeCaptured += delegate (object? sender, VolumeCapturedEventArgs args)
            {
                EventAggregator.Instance.Publish(new CameraLogInfoModel()
                {
                    Type = LogType.Information,
                    Message = $"相机,获取体积信息:{args.Length},{args.Width},{args.Height}",
                });
                EventAggregator.Instance.Publish(new VolumeLogInfoModel()
                {
                    Type = LogType.Information,
                    Message = $"获取体积信息:{args.Length},{args.Width},{args.Height}",
                    DataSourceType = DataSourceType.DeviceInput
                });
            };
            _deviceService.CameraBound += delegate (object? sender, CameraFinderItemInfoModel model)
            {
                EventAggregator.Instance.Publish(new CameraLogInfoModel()
                {
                    Type = LogType.Information,
                    Message = $"相机:{model.SerialNumber},绑定到{model.BoundType}",
                    CameraSerialNumber = model.SerialNumber
                });
            };
            _deviceService.CameraEnumerationRefreshed += delegate (object? sender, IReadOnlyList<CameraFinderItemInfoModel> list)
            {
                EventAggregator.Instance.Publish(new CameraLogInfoModel()
                {
                    Type = LogType.Information,
                    Message = $"枚举相机",
                });
            };
            _deviceService.CameraDisconnected += delegate (object? sender, IReadOnlyList<ICamera> list)
            {
                EventAggregator.Instance.Publish(new CameraLogInfoModel()
                {
                    Type = LogType.Warning,
                    Message = $"相机断开连接",
                });
            };
            _deviceService.CameraFault += delegate (object? sender, IReadOnlyList<ICamera> list)
            {
                EventAggregator.Instance.Publish(new CameraLogInfoModel()
                {
                    Type = LogType.Exception,
                    Message = $"相机故障",
                });
            };
            _deviceService.CameraException += delegate (object? sender, DeviceExceptionEventArgs args)
            {
                EventAggregator.Instance.Publish(new CameraLogInfoModel()
                {
                    Type = LogType.Exception,
                    Message = args.Exception?.Message ?? string.Empty,
                });
            };
            _deviceService.CameraUnbound += delegate (object? sender, CameraFinderItemInfoModel model)
            {
                EventAggregator.Instance.Publish(new CameraLogInfoModel()
                {
                    Type = LogType.Information,
                    Message = $"相机已解绑",
                    CameraSerialNumber = model.SerialNumber
                });
            };
            _deviceService.BarcodeMissed += delegate (object? sender, BarcodeReadEventArgs args)
            {
                EventAggregator.Instance.Publish(new CameraLogInfoModel()
                {
                    Type = LogType.Warning,
                    Message = $"相机:{args.CameraSerialNumber}光电触发但未识别到条码",
                    CameraSerialNumber = args.CameraSerialNumber
                });
            };
            _deviceService.PanoramaCaptured += delegate (object? sender, PanoramaCaptureEventArgs args)
            {
                EventAggregator.Instance.Publish(new CameraLogInfoModel()
                {
                    Type = LogType.Information,
                    Message = $"相机截取到全景图",
                    CameraSerialNumber = args.CameraSerialNumber
                });
            };
            //Ocr
            _deviceService.OcrContentRecognized += delegate (object? sender, OcrResult args)
            {
                EventAggregator.Instance.Publish(new OcrLogInfoModel()
                {
                    Type = LogType.Information,
                    Message = $"Ocr获取到条码[{args.BarCode}]",
                    CameraSerialNumber = args.CameraSerialNumber,
                    BarCode = args.BarCode,
                    ElapsedTime = args.ElapsedTime,
                    RecipientAddress = args.RecipientAddress,
                    RecipientName = args.RecipientName,
                    RecipientPhone = args.RecipientPhone,
                    RecognitionTimestamp = args.RecognitionTimestamp,
                    RecognitionTime = args.RecognitionTime,
                    SubmitTimestamp = args.SubmitTimestamp,
                    SenderAddress = args.SenderAddress,
                    SenderName = args.SenderName,
                    SenderPhone = args.SenderPhone,
                    ThreeSegmentCode = args.ThreeSegmentCode,
                    VirtualNumber = args.VirtualNumber,
                    VirtualNumberLast4 = args.VirtualNumberLast4,
                });
            };
            //磅秤
            _deviceService.ScaleConnected += delegate (object? sender, ScaleConnectedEventArgs args)
            {
                EventAggregator.Instance.Publish(new WeighingLogInfoModel()
                {
                    Type = LogType.Information,
                    Message = $"磅秤已连接",
                    DataSourceType = DataSourceType.DeviceInput
                });
            };
            _deviceService.ScaleDisconnected += delegate (object? sender, ScaleDisconnectedEventArgs args)
            {

                EventAggregator.Instance.Publish(new WeighingLogInfoModel()
                {
                    Type = LogType.Warning,
                    Message = $"磅秤已断开",
                    DataSourceType = DataSourceType.DeviceInput
                });
            };
            _deviceService.WeightStabilized += delegate (object? sender, WeightChangedEventArgs args)
            {
                /*EventAggregator.Instance.Publish(new WeighingLogInfoModel() {
                    Type = LogType.Information,
                    FormatWeight = args.FormattedWeight,
                    Source = args.OriginalContent,
                    Message = $"获取到重量,原内容[{args.OriginalContent}],格式化后重量:{args.FormattedWeight:F3}",
                    DataSourceType = DataSourceType.DeviceInput,
                    CommunicationType = CommunicationType.Receive,
                });*/
            };
            _externalDataService.ContentInputReceived += (sender, args) =>
            {
                //外部输入输入
                EventAggregator.Instance.Publish(new InputLogInfoModel()
                {
                    Type = LogType.Information,
                    DataSourceType = DataSourceType.ExternalInput,
                    InputContent = args.SourceContent,
                    Message = $"获取到外部TCP输入:{args.SourceContent}",
                });
            };
            /*_exitMonitor.LockExitEvent += (sender, model) => {
                NLog.LogManager.GetCurrentClassLogger().Info($"{model.CreateTime:yyyy-MM-dd HH:mm:ss.fff}--[锁格]-[格口号:{model.ExitName}]锁定");
            };
            _exitMonitor.UnLockExitEvent += (sender, model) => {
                NLog.LogManager.GetCurrentClassLogger().Info($"{model.CreateTime:yyyy-MM-dd HH:mm:ss.fff}--[锁格]-[格口号:{model.ExitName}]解锁");
            };*/
            EventAggregator.Instance.Subscribe<InstructionReceived>(item =>
            {
                if (item is { } model && model.InstructionInfos?.Any() == true
                    )
                {
                    var instructionInfoModel = model.InstructionInfos.FirstOrDefault();
                    switch (instructionInfoModel?.InstructionType)
                    {
                        case InstructionType.CreatePackage:
                            _diagnosticLogItems.Enqueue($"{instructionInfoModel?.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[格口号:{model.ExitName}]-[序号:{model.SortingCode}]-[创建指令]{instructionInfoModel?.InstructionContent}");
                            break;

                        case InstructionType.SendSorting:
                            _diagnosticLogItems.Enqueue($"{instructionInfoModel?.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[格口号:{model.ExitName}]-[序号:{model.SortingCode}]-[发送格口]{instructionInfoModel?.InstructionContent}");
                            break;

                        case InstructionType.SignalCallback:
                            _diagnosticLogItems.Enqueue($"{instructionInfoModel?.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[格口号:{model.ExitName}]-[序号:{model.SortingCode}]-[分拣完成]{instructionInfoModel?.InstructionContent}");
                            break;

                        case InstructionType.PackageException:
                            _diagnosticLogItems.Enqueue($"{instructionInfoModel?.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[格口号:{model.ExitName}]-[序号:{model.SortingCode}]-[包裹异常]{instructionInfoModel?.InstructionContent}");
                            break;

                        case InstructionType.SendPreSignal:
                            _diagnosticLogItems.Enqueue($"{instructionInfoModel?.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[格口号:{model.ExitName}]-[序号:{model.SortingCode}]-[前置信号发送]{instructionInfoModel?.InstructionContent}");
                            break;

                        case InstructionType.ReceivePreSignalReply:
                            _diagnosticLogItems.Enqueue($"{instructionInfoModel?.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[格口号:{model.ExitName}]-[序号:{model.SortingCode}]-[前置信号回复]{instructionInfoModel?.InstructionContent}");
                            break;

                        case InstructionType.PackageInfoCompletedSignal:
                            _diagnosticLogItems.Enqueue($"{instructionInfoModel?.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[格口号:{model.ExitName}]-[序号:{model.SortingCode}]-[包裹信息赋值完成]{instructionInfoModel?.InstructionContent}");
                            break;

                        case InstructionType.SequenceBindingReply:
                            _diagnosticLogItems.Enqueue($"{instructionInfoModel?.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[格口号:{model.ExitName}]-[序号:{model.SortingCode}]-[序号绑定回复]{instructionInfoModel?.InstructionContent}");
                            break;

                        case InstructionType.ResetButtonTrigger:
                            _diagnosticLogItems.Enqueue($"{instructionInfoModel?.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[格口号:{model.ExitName}]-[序号:{model.SortingCode}]-[按下复位]{instructionInfoModel?.InstructionContent}");
                            break;

                        case InstructionType.PackageExceptionEx:
                            _diagnosticLogItems.Enqueue($"{instructionInfoModel?.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[格口号:{model.ExitName}]-[序号:{model.SortingCode}]-[包裹异常(需要判断)]{instructionInfoModel?.InstructionContent}");
                            break;
                    }
                }
            });
            EventAggregator.Instance.Subscribe<TriggerPositionEvent>(position =>
            {
                //创建包裹
                if (position is { } trigger)
                {
                    if (trigger is { TriggerPosition: TriggerPositionEnum.CreateTimePackageAfter, PackageInfo: not null })
                    {
                        _diagnosticLogItems.Enqueue($"{trigger.PackageInfo.CreateTime:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[序号:{trigger.PackageInfo.Guid}]-[创建包裹成功]");
                    }
                    else if (trigger is { TriggerPosition: TriggerPositionEnum.RemovePackageAfter, PackageInfo: not null })
                    {
                        _diagnosticLogItems.Enqueue($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[序号:{trigger.PackageInfo.Guid}]-[移除包裹成功] {trigger.PackageInfo.BarCodeInfo?.Barcode} -[{trigger.Description}]");
                    }
                    else if (trigger is { TriggerPosition: TriggerPositionEnum.BarCodeSetValueAfter, PackageInfo.BarCodeInfo: not null })
                    {
                        _diagnosticLogItems.Enqueue($"{trigger.PackageInfo.BarCodeInfo.BindTime:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[序号:{trigger.PackageInfo.Guid}]-[条码赋值] {trigger.PackageInfo.BarCodeInfo?.Barcode}");
                    }
                    else if (trigger is { TriggerPosition: TriggerPositionEnum.WeightSetValueAfter, PackageInfo.WeightInfo: not null })
                    {
                        _diagnosticLogItems.Enqueue($"{trigger.PackageInfo.WeightInfo.CreateTime:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[序号:{trigger.PackageInfo.Guid}]-[重量赋值] {trigger.PackageInfo.WeightInfo.FormattedWeight}");
                    }
                    else if (trigger is { TriggerPosition: TriggerPositionEnum.VolumeSetValueAfter, PackageInfo.VolumeInfo: not null })
                    {
                        _diagnosticLogItems.Enqueue($"{trigger.PackageInfo.VolumeInfo.CreateTime:yyyy-MM-dd HH:mm:ss.fff}--[分拣]-[序号:{trigger.PackageInfo.Guid}]-[体积赋值] {trigger.PackageInfo.VolumeInfo.OriginalText}");
                    }
                }
            });

            //http
            EventAggregator.Instance.Subscribe<ApiResponseReceived>(item =>
            {
                if (item is { } model)
                {
                    EventAggregator.Instance.Publish(new ApiLogInfoModel()
                    {
                        Type = model.UploadResponse?.IsSuccess == true ? LogType.Information : LogType.Exception,
                        ApiParameters = model.UploadResponse?.ApiParameters ?? string.Empty,
                        CreateTime = model.UploadResponse?.RequestTime ?? DateTime.Now,
                        Duration = model.UploadResponse?.DurationSeconds ?? 0,
                        ExceptionMsg = model.UploadResponse?.ExceptionMsg ?? string.Empty,
                        RequestContent = model.UploadResponse?.RequestContent ?? string.Empty,
                        RequestTime = model.UploadResponse?.RequestTime ?? DateTime.Now,
                        ResponseContent = model.UploadResponse?.ResponseContent ?? string.Empty,
                        ResponseTime = model.UploadResponse?.ResponseTime ?? DateTime.Now,
                        Url = model.UploadResponse?.RequestUrl ?? string.Empty,
                    });
                }
            });
            _stackedPackageService.StackedPackageReturned += (sender, args) =>
            {
                _diagnosticLogItems.Enqueue(
                    $"{args.ReceivedTime:yyyy-MM-dd HH:mm:ss.fff}--[叠包判断]-[判断结果:{(args.IsStacked ? "叠包" : "不叠包")}]-[序号:{args.PackageInfo?.Guid}]-{args.StackedContent}");
            };

            //写出log字符串的信息
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var flushTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
            try
            {
                while (await flushTimer.WaitForNextTickAsync(stoppingToken) &&
                       Volatile.Read(ref _isWindowsClose) == 0)
                {
                    try
                    {
                        await FlushAllQueuesAsync(stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error($"日志管理异常:{e}");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // 进入 finally 后尽力刷新已经接收的日志。
            }
            finally
            {
                await FlushPendingLogsOnShutdownAsync().ConfigureAwait(false);
            }
        }

        /// <summary>按固定小批次刷新全部数据库日志和诊断文本队列。</summary>
        private async Task FlushAllQueuesAsync(CancellationToken token)
        {
            await FlushBatchAsync(_exceptionItems, _exceptionLogRepository, token);
            await FlushBatchAsync(_appLogItems, _appLogRepository, token);
            await FlushBatchAsync(_cameraLogItems, _cameraLogRepository, token);
            await FlushBatchAsync(_sortingLogItems, _sortingLogRepository, token);
            await FlushBatchAsync(_weighingLogItems, _weighingLogRepository, token);
            await FlushBatchAsync(_volumeLogItems, _volumeLogRepository, token);
            await FlushBatchAsync(_apiLogInfoItems, _apiLogRepository, token);
            await FlushBatchAsync(_outputLogItems, _outputLogRepository, token);
            await FlushBatchAsync(_inputLogItems, _inputLogRepository, token);
            await FlushBatchAsync(_ocrLogItems, _ocrLogRepository, token);
            await FlushBatchAsync(_ftpLogItems, _ftpLogRepository, token);
            await FlushBatchAsync(_logCleaningLogItems, _cleanupLogRepository, token);
            FlushDiagnosticMessages();
            ReportDroppedLogs();
        }

        /// <summary>停机时在三秒边界内尽量写出已经接收的日志，防止正常维护造成整批丢失。</summary>
        private async Task FlushPendingLogsOnShutdownAsync()
        {
            using var flushCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try
            {
                while (HasPendingLogs())
                {
                    await FlushAllQueuesAsync(flushCancellation.Token).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromMilliseconds(10), flushCancellation.Token)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (flushCancellation.IsCancellationRequested)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn("停机日志刷新超过三秒，剩余日志将不再等待");
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(exception, "停机日志刷新失败");
            }
        }

        /// <summary>判断任意日志缓冲中是否还有未写出的项目。</summary>
        private bool HasPendingLogs() =>
            !_exceptionItems.IsEmpty ||
            !_appLogItems.IsEmpty ||
            !_cameraLogItems.IsEmpty ||
            !_sortingLogItems.IsEmpty ||
            !_weighingLogItems.IsEmpty ||
            !_volumeLogItems.IsEmpty ||
            !_apiLogInfoItems.IsEmpty ||
            !_outputLogItems.IsEmpty ||
            !_inputLogItems.IsEmpty ||
            !_ocrLogItems.IsEmpty ||
            !_ftpLogItems.IsEmpty ||
            !_logCleaningLogItems.IsEmpty ||
            !_diagnosticLogItems.IsEmpty;

        /// <summary>
        /// 在后台工作循环中批量写出诊断日志，避免事件热路径直接触发文件输出。
        /// </summary>
        private void FlushDiagnosticMessages()
        {
            const int maximumBatchSize = 256;
            var logger = NLog.LogManager.GetCurrentClassLogger();
            for (var index = 0;
                 index < maximumBatchSize && _diagnosticLogItems.TryDequeue(out var message);
                 index++)
            {
                logger.Info(message);
            }
        }

        /// <summary>
        /// 将队列中的日志按小批次写入数据库，减少上下文创建和事务提交次数。
        /// </summary>
        /// <typeparam name="T">日志实体类型。</typeparam>
        /// <param name="queue">待写入的日志队列。</param>
        /// <param name="repository">日志仓储。</param>
        /// <param name="token">取消令牌。</param>
        private static async Task FlushBatchAsync<T>(
            BoundedLogQueue<T> queue,
            IRepository<T> repository,
            CancellationToken token) where T : class
        {
            if (queue.IsEmpty)
            {
                return;
            }

            const int maximumBatchSize = 64;
            var batch = new List<T>(maximumBatchSize);
            while (batch.Count < maximumBatchSize && queue.TryDequeue(out var item))
            {
                batch.Add(item);
            }

            var saved = batch.Count == 1
                ? await repository.Insert(batch[0], token)
                : await repository.InsertRange(batch, token);
            if (!saved)
            {
                foreach (var item in batch)
                {
                    queue.Enqueue(item);
                }
            }
        }

        /// <summary>按固定时间窗口汇总报告有界日志队列的丢弃数量。</summary>
        private void ReportDroppedLogs()
        {
            var now = System.Diagnostics.Stopwatch.GetTimestamp();
            var previous = Interlocked.Read(ref _lastDroppedLogReportTimestamp);
            if (previous != 0 &&
                System.Diagnostics.Stopwatch.GetElapsedTime(previous, now) < DroppedLogReportInterval)
            {
                return;
            }

            Interlocked.Exchange(ref _lastDroppedLogReportTimestamp, now);
            var droppedCount = _exceptionItems.ConsumeDroppedCount() +
                               _appLogItems.ConsumeDroppedCount() +
                               _cameraLogItems.ConsumeDroppedCount() +
                               _sortingLogItems.ConsumeDroppedCount() +
                               _weighingLogItems.ConsumeDroppedCount() +
                               _volumeLogItems.ConsumeDroppedCount() +
                               _apiLogInfoItems.ConsumeDroppedCount() +
                               _outputLogItems.ConsumeDroppedCount() +
                               _inputLogItems.ConsumeDroppedCount() +
                               _ocrLogItems.ConsumeDroppedCount() +
                               _ftpLogItems.ConsumeDroppedCount() +
                               _logCleaningLogItems.ConsumeDroppedCount() +
                               _diagnosticLogItems.ConsumeDroppedCount();
            if (droppedCount > 0)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(
                    $"日志写入积压超过内存队列上限，已丢弃 {droppedCount} 条日志");
            }
        }

    }
}
