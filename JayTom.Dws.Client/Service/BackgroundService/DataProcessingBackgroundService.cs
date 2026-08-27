using NLog;
using JayTom.Dws.Models.LocalConf.CloudConfig;
using JayTom.Dws.Models.LocalConf.IpcNvrConfig;
using JayTom.Dws.Models.LocalConf.CameraConfig;
using JayTom.Dws.Application.CameraConfigurations;
using JayTom.Dws.Application.PackageHistory;
using System;
using JayTom.Dws.Application.Workflows;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using static ImTools.ImMap;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Legacy.Contracts.Model;
using System.Windows.Documents;
using JayTom.Dws.Models.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Integrations.Cloud;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;
using JayTom.Dws.Legacy.Contracts.Services.ImageService;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.CameraConfig;
using PackageInfo = JayTom.Dws.Legacy.Contracts.Packages.PackageInfo;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;
using WindowsAction = JayTom.Dws.Client.Events.WindowsAction;
using SortingExitType = JayTom.Dws.Application.Events.SortingExitType;
using WindowsActionType = JayTom.Dws.Client.Events.WindowsActionType;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;
using PackageExitUpdateEvent = JayTom.Dws.Application.Events.PackageExitUpdateEvent;
using PackageAbnormalSortingType = JayTom.Dws.Application.Events.PackageAbnormalSortingType;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    /// <summary>
    /// 数据处理器
    /// </summary>
    public class DataProcessingBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;

        private readonly IPackageProcessingPersistence _packagePersistence;

        private readonly IImageStorageService _imageStorageService;

        private readonly ICameraConfigurationCatalog<BarcodeScannerCameraConfigInfoModel> _barcodeScannerCameraConfigRepository;
        private readonly LosslessWorkQueue<PackageInfoModel> _insertItems = new();
        private readonly LosslessWorkQueue<ApiResponseReceived> _updateResponseItems = new();
        private readonly LosslessWorkQueue<SavedImageInfo> _savedImageItems = new();
        private readonly LosslessWorkQueue<InstructionReceived> _instructionItems = new();
        private readonly LosslessWorkQueue<ExceptionSortingReceived> _exceptionSortingItems = new();
        private readonly LosslessWorkQueue<PackageExitUpdateEvent> _packageExitUpdateItems = new();
        /// <summary>包裹主记录每次 SQLite 提交允许合并的最大数量。</summary>
        private const int PackageInsertBatchSize = 64;
        /// <summary>
        /// 指令可能早于包裹主记录到达；在该窗口内暂存，避免密集流量下丢失创建指令。
        /// </summary>
        private static readonly TimeSpan InstructionAssociationTimeout = TimeSpan.FromMinutes(10);
        /// <summary>按包裹时间戳保存尚未能关联主记录的下位机指令。</summary>
        private readonly Dictionary<long, Queue<(InstructionReceived Instruction, long EnqueuedAt)>>
            _pendingInstructions = [];
        /// <summary>当前暂存且尚未关联到包裹主记录的指令总数。</summary>
        private int _pendingInstructionCount;
        /// <summary>记录持久化工作项的重试次数，用于诊断长期故障但不丢弃数据。</summary>
        private readonly RetryAttemptTracker _retryTracker = new(5);

        /// <summary>
        /// 在任意持久化队列产生数据时唤醒后台消费者。
        /// </summary>
        private readonly SemaphoreSlim _workSignal = new(0, 1);
        /// <summary>
        /// 标记当前是否包含需要退避后重试的工作。
        /// </summary>
        /// <summary>在独立单调时钟线程上安排失败持久化任务的重试。</summary>
        private readonly MonotonicDeadlineScheduler _retryScheduler =
            new("DataPersistenceRetry", ThreadPriority.BelowNormal);
        /// <summary>合并密集生产者发出的重复唤醒信号。</summary>
        private int _workSignalArmed;
        /// <summary>下一次清理孤立指令的单调时钟时间戳。</summary>
        private long _nextPendingInstructionCleanupTimestamp;

        private IReadOnlyDictionary<string, CameraMetadata> _cameraMetadata =
            new Dictionary<string, CameraMetadata>(StringComparer.Ordinal);
        private int _isWindowsClose;
        /// <summary>标记持久化服务已经释放，阻止晚到事件访问已释放的唤醒句柄。</summary>
        private int _isDisposed;

        public DataProcessingBackgroundService(
            IPackageProcessingPersistence packagePersistence,
            IImageStorageService imageStorageService,
            ICameraConfigurationCatalog<BarcodeScannerCameraConfigInfoModel> barcodeScannerCameraConfigRepository,
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            _packagePersistence = packagePersistence;
            _imageStorageService = imageStorageService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _imageStorageService.ImageSaved += delegate (object? sender, ImageSavedEventArgs args)
            {
                //保存后触发

                EnqueueWork(_savedImageItems, new SavedImageInfo()
                {
                    PackageTimestamp = args.PackageTimestamp,
                    BarCode = args.BarCode,
                    FilePath = args.FilePath,
                    ImageType = args.ImageType,
                    CameraSerialNumber = args.CameraSerialNumber ?? string.Empty,
                    ScanTime = args.ScanTime,
                });
            };
            _eventBus.SubscribePackage<PackageInfo>(item =>
            {
                if (item is { } model)
                {
                    //添加
                    EnqueueWork(_insertItems, new PackageInfoModel()
                    {
                        BarCodeInfo = model.BarCodeInfo,
                        WeightInfo = model.WeightInfo,
                        VolumeInfo = model.VolumeInfo,
                        PackageCreateTime = model.CreateTime,
                        PackageTimestamped = new DateTimeOffset(model.CreateTime).ToUnixTimeMilliseconds(),
                    });
                }
            });
            _eventBus.SubscribePackage<ApiResponseReceived>(item =>
            {
                if (item is { } model)
                {
                    EnqueueWork(_updateResponseItems, model);
                }
            });
            _eventBus.Subscribe<InstructionReceived>(item =>
            {
                if (item is { } model)
                {
                    EnqueueWork(_instructionItems, model);
                }
            });
            _eventBus.Subscribe<ExceptionSortingReceived>(item =>
            {
                if (item is { } model)
                {
                    EnqueueWork(_exceptionSortingItems, model);
                }
            });
            _eventBus.Subscribe<WindowsAction>(item =>
            {
                if (item is { Type: WindowsActionType.Close })
                {
                    Interlocked.Exchange(ref _isWindowsClose, 1);
                    SignalWork();
                }
            });

            _eventBus.Subscribe<PackageExitUpdateEvent>(item =>
            {
                if (item is { } info)
                {
                    EnqueueWork(_packageExitUpdateItems, info);
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 相机名称是配置数据，只在进入持久化循环前加载一次。热循环只查内存快照。
            var cameraConfigs = await _barcodeScannerCameraConfigRepository.MemoryCacheData();
            Volatile.Write(
                ref _cameraMetadata,
                cameraConfigs
                    .GroupBy(camera => camera.SerialNumber, StringComparer.Ordinal)
                    .ToDictionary(
                        group => group.Key,
                        group => new CameraMetadata(
                            group.Key,
                            group.Last().Name,
                            group.Last().CustomName),
                        StringComparer.Ordinal));

            while (!stoppingToken.IsCancellationRequested && Volatile.Read(ref _isWindowsClose) == 0)
            {
                List<PackageInfoModel>? inFlightInsertBatch = null;
                ApiResponseReceived? inFlightResponse = null;
                SavedImageInfo? inFlightSavedImage = null;
                InstructionReceived? inFlightInstruction = null;
                ExceptionSortingReceived? inFlightExceptionSorting = null;
                PackageExitUpdateEvent? inFlightExitUpdate = null;
                try
                {
                    await _workSignal.WaitAsync(stoppingToken);
                    Volatile.Write(ref _workSignalArmed, 0);
                    var insertBatch = new List<PackageInfoModel>(PackageInsertBatchSize);
                    while (insertBatch.Count < PackageInsertBatchSize &&
                           _insertItems.TryDequeue(out var insertModel))
                    {
                        insertBatch.Add(insertModel);
                    }
                    if (insertBatch.Count > 0)
                    {
                        inFlightInsertBatch = insertBatch;
                        var inserted = await _packagePersistence.AddPackagesAsync(
                            insertBatch,
                            stoppingToken);
                        if (!inserted)
                        {
                            LogManager.GetCurrentClassLogger().Error($"数据批量保存失败,正在重试...");
                            foreach (var package in insertBatch)
                            {
                                RequeueWork(_insertItems, package);
                            }
                        }
                        else
                        {
                            foreach (var package in insertBatch)
                            {
                                _retryTracker.Forget(package);
                                ReleasePendingInstructions(package.PackageTimestamped);
                            }
                        }
                    }
                    inFlightInsertBatch = null;

                    var dequeue = _updateResponseItems.TryDequeue(out var responseModel);
                    inFlightResponse = responseModel;
                    if (dequeue && responseModel is not null)
                    {
                        var packageInfo = await _packagePersistence.FindCachedPackageAsync(responseModel.Timestamp, stoppingToken);

                        //更新
                        if (packageInfo is { Id: > 0 } packageInfoModel && responseModel.UploadResponse is not null)
                        {
                            var insert = await _packagePersistence.AddUploadAttemptAsync(new UploadInfoModel()
                            {
                                PackageId = packageInfoModel.Id,
                                RequestStatus = responseModel.UploadResponse.IsSuccess
                                    ? UploadStatus.Succeeded
                                    : UploadStatus.Failed,
                                RequestContent = responseModel.UploadResponse.RequestContent,
                                ResponseContent = responseModel.UploadResponse.ResponseContent,
                                RequestTime = responseModel.UploadResponse.RequestTime,
                                ResponseTime = responseModel.UploadResponse.ResponseTime,
                                DurationInSeconds = responseModel.UploadResponse.DurationSeconds,
                                InterfaceParameters = responseModel.UploadResponse.ApiParameters,
                                RequestUrl = responseModel.UploadResponse.RequestUrl,
                                ExceptionMessage = responseModel.UploadResponse.ExceptionMsg,
                                ApiExceptionType = (JayTom.Dws.Models.Package.ApiExceptionType)
                                    responseModel.UploadResponse.ApiExceptionType,
                            }, stoppingToken);

                            if (!insert)
                            {
                                RequeueWork(_updateResponseItems, responseModel);
                            }
                        }
                        else
                        {
                            RequeueWork(_updateResponseItems, responseModel);
                        }
                    }
                    inFlightResponse = null;
                    //更新图片路径

                    var isSaved = _savedImageItems.TryDequeue(out var savedImageInfo);
                    inFlightSavedImage = savedImageInfo;
                    if (isSaved && savedImageInfo is not null)
                    {
                        var imageType = savedImageInfo.ImageType switch
                        {
                            SaveImageType.BarcodeImage => 0,
                            SaveImageType.PanoramaImage => 1,
                            SaveImageType.VolumeImage => 2,
                            _ => -1
                        };
                        if (imageType >= 0)
                        {
                            var packageInfo = await _packagePersistence
                                .FindCachedPackageAsync(savedImageInfo.PackageTimestamp, stoppingToken);
                            if (packageInfo is { Id: > 0 } packageInfoModel)
                            {
                                Volatile.Read(ref _cameraMetadata).TryGetValue(
                                    savedImageInfo.CameraSerialNumber,
                                    out var cameraConfigInfoModel);

                                var insert = await _packagePersistence.AddImageMetadataAsync(new ImageInfoModel()
                                {
                                    PackageId = packageInfoModel.Id,
                                    CameraName = cameraConfigInfoModel?.Name ?? string.Empty,
                                    CameraSerialNumber = savedImageInfo.CameraSerialNumber,
                                    CustomCameraName = cameraConfigInfoModel?.CustomName ?? string.Empty,
                                    LocalPath = savedImageInfo.FilePath ?? string.Empty,
                                    Type = imageType
                                }, stoppingToken);

                                if (!insert)
                                {
                                    RequeueWork(_savedImageItems, savedImageInfo);
                                }
                            }
                            else
                            {
                                RequeueWork(_savedImageItems, savedImageInfo);
                            }
                        }
                    }
                    inFlightSavedImage = null;

                    //指令更新
                    var isSorting = _instructionItems.TryDequeue(out var sortingModel);
                    inFlightInstruction = sortingModel;
                    if (isSorting && sortingModel is not null)
                    {
                        //取出对应条码id(根据条码、扫码时间)

                        var packageInfo = await _packagePersistence.FindCachedPackageAsync(sortingModel.Timestamp, stoppingToken);

                        if (packageInfo is { Id: > 0 } packageInfoModel)
                        {
                            bool isSortingUpdateInfo;
                            var createdSortingInfo = packageInfo.SortingInfo is null;
                            if (packageInfo.SortingInfo is null)
                            {
                                //存到表
                                packageInfo.SortingInfo = new SortingInfoModel()
                                {
                                    PackageId = packageInfoModel.Id,
                                    ChecksumProtocolName = sortingModel.ChecksumProtocolName,
                                    CommunicationMethod = sortingModel.CommunicationMethod,
                                    IsCreatedByLowerMachine = sortingModel.IsCreatedByLowerMachine,
                                    IsSortingUsed = true,
                                    SortingCode = sortingModel.SortingCode,
                                    SortingMode = sortingModel.SortingMode,
                                    InstructionInfos = sortingModel.InstructionInfos?.Select(s =>
                                        new InstructionInfoModel
                                        {
                                            InstructionType = s.InstructionType,
                                            InstructionContent = s.InstructionContent,
                                            InstructionGeneratedTime = s.InstructionGeneratedTime,
                                        })?.ToList() ?? new List<InstructionInfoModel>()
                                };
                                isSortingUpdateInfo = await _packagePersistence.AddSortingAsync(packageInfo.SortingInfo, stoppingToken);
                            }
                            else
                            {
                                //更新
                                packageInfo.SortingInfo.InstructionInfos ??= new List<InstructionInfoModel>();
                                packageInfo.SortingInfo.IsCreatedByLowerMachine = sortingModel.IsCreatedByLowerMachine;
                                packageInfo.SortingInfo.IsSortingUsed = true;
                                if (!string.IsNullOrEmpty(sortingModel.ChecksumProtocolName))
                                {
                                    packageInfo.SortingInfo.ChecksumProtocolName = sortingModel.ChecksumProtocolName;
                                }

                                if (sortingModel.CommunicationMethod != CommunicationsType.None)
                                {
                                    packageInfo.SortingInfo.CommunicationMethod = sortingModel.CommunicationMethod;
                                }

                                if (sortingModel.SortingMode != SortMode.None)
                                {
                                    packageInfo.SortingInfo.SortingMode = sortingModel.SortingMode;
                                }

                                var instructionInfoModels = sortingModel.InstructionInfos?.Select(s =>
                                    new InstructionInfoModel
                                    {
                                        InstructionType = s.InstructionType,
                                        InstructionContent = s.InstructionContent,
                                        InstructionGeneratedTime = s.InstructionGeneratedTime,
                                    })?.ToList() ?? new List<InstructionInfoModel>();
                                foreach (var instructionInfoModel in instructionInfoModels)
                                {
                                    if (!packageInfo.SortingInfo.InstructionInfos.Any(existing =>
                                            existing.InstructionType == instructionInfoModel.InstructionType &&
                                            existing.InstructionGeneratedTime == instructionInfoModel.InstructionGeneratedTime &&
                                            string.Equals(
                                                existing.InstructionContent,
                                                instructionInfoModel.InstructionContent,
                                                StringComparison.Ordinal)))
                                    {
                                        packageInfo.SortingInfo.InstructionInfos.Add(instructionInfoModel);
                                    }
                                }

                                isSortingUpdateInfo =
                                    await _packagePersistence.UpdateSortingAsync(packageInfo.SortingInfo, stoppingToken);
                            }

                            if (!isSortingUpdateInfo)
                            {
                                if (createdSortingInfo)
                                {
                                    packageInfo.SortingInfo = null;
                                }
                                RequeueWork(_instructionItems, sortingModel);
                            }
                            else
                            {
                                _packagePersistence.RefreshCachedPackage(packageInfo, stoppingToken);
                            }
                        }
                        else
                        {
                            StoreInstructionWaitingForPackage(sortingModel);
                        }
                    }
                    inFlightInstruction = null;
                    //异常更新

                    var isExceptionSorting = _exceptionSortingItems.TryDequeue(out var exceptionSortingModel);
                    inFlightExceptionSorting = exceptionSortingModel;
                    if (isExceptionSorting && exceptionSortingModel is not null)
                    {
                        var packageInfo = await _packagePersistence.FindCachedPackageAsync(exceptionSortingModel.Timestamp, stoppingToken);
                        if (packageInfo?.SortingInfo is not null)
                        {
                            packageInfo.SortingInfo.AbnormalSortingType =
                                (AbnormalSortingType)exceptionSortingModel.PackageCloudAbnormalSortingType;
                            packageInfo.SortingInfo.IsAbnormalSorting =
                                exceptionSortingModel.PackageCloudAbnormalSortingType !=
                                PackageCloudAbnormalSortingType.None;
                            var update = await _packagePersistence.UpdateSortingAsync(packageInfo.SortingInfo, stoppingToken);
                            if (!update)
                            {
                                RequeueWork(_exceptionSortingItems, exceptionSortingModel);
                            }
                        }
                        else
                        {
                            RequeueWork(_exceptionSortingItems, exceptionSortingModel);
                        }
                    }
                    inFlightExceptionSorting = null;

                    //格口更新
                    var packageExitUpdate = _packageExitUpdateItems.TryDequeue(out var packageExitUpdateModel);
                    inFlightExitUpdate = packageExitUpdateModel;
                    if (packageExitUpdate && packageExitUpdateModel is not null)
                    {
                        //找到对应的包裹
                        var packageInfo = await _packagePersistence.FindCachedPackageAsync(packageExitUpdateModel.Timestamp, stoppingToken);
                        if (packageInfo is not null)
                        {
                            //更新格口

                            //如果出现异常则不再更新

                            if (packageInfo.ExitInfo is not null)
                            {
                                //更新
                                switch (packageExitUpdateModel.ExitType)
                                {
                                    case SortingExitType.PhysicalExit:
                                        if (!string.IsNullOrWhiteSpace(packageExitUpdateModel.ExitName))
                                        {
                                            packageInfo.ExitInfo.PhysicalExit = packageExitUpdateModel.ExitName;
                                            packageInfo.ExitInfo.PhysicalExitId = packageExitUpdateModel.ExitId;
                                        }
                                        break;

                                    case SortingExitType.TheoreticalExit:
                                        packageInfo.ExitInfo.TheoreticalExit = packageExitUpdateModel.ExitName;
                                        break;
                                }

                                var update = await _packagePersistence.UpdateExitAsync(packageInfo.ExitInfo, stoppingToken);
                                if (!update)
                                {
                                    RequeueWork(_packageExitUpdateItems, packageExitUpdateModel);
                                    continue;
                                }
                            }
                            else if (!string.IsNullOrWhiteSpace(packageExitUpdateModel.ExitName))
                            {
                                //添加
                                packageInfo.ExitInfo = packageExitUpdateModel.ExitType switch
                                {
                                    SortingExitType.PhysicalExit => new ExitInfoModel()
                                    {
                                        PackageId = packageInfo.Id,
                                        PhysicalExit = packageExitUpdateModel.ExitName,
                                        PhysicalExitId = packageExitUpdateModel.ExitId,
                                    },
                                    SortingExitType.TheoreticalExit => new ExitInfoModel()
                                    {
                                        PackageId = packageInfo.Id,
                                        TheoreticalExit = packageExitUpdateModel.ExitName,
                                    },
                                    _ => new()
                                };

                                var insert = await _packagePersistence.AddExitAsync(packageInfo.ExitInfo, stoppingToken);
                                if (!insert)
                                {
                                    packageInfo.ExitInfo = null;
                                    RequeueWork(_packageExitUpdateItems, packageExitUpdateModel);
                                    continue;
                                }
                            }

                            if (packageExitUpdateModel.PackageAbnormalSortingType !=
                                PackageAbnormalSortingType.None &&
                                packageInfo.ExitInfo?.PackageId > 0)
                            {
                                //更新异常
                                if (packageInfo.SortingInfo is not null)
                                {
                                    packageInfo.SortingInfo.AbnormalSortingType =
                                        (AbnormalSortingType)packageExitUpdateModel.PackageAbnormalSortingType;
                                    packageInfo.SortingInfo.IsAbnormalSorting =
                                        packageExitUpdateModel.PackageAbnormalSortingType !=
                                        PackageAbnormalSortingType.None;
                                    var sortingUpdated = await _packagePersistence.UpdateSortingAsync(
                                        packageInfo.SortingInfo,
                                        stoppingToken);
                                    if (!sortingUpdated)
                                    {
                                        RequeueWork(_packageExitUpdateItems, packageExitUpdateModel);
                                        continue;
                                    }
                                }
                            }
                            _packagePersistence.RefreshCachedPackage(packageInfo, stoppingToken);
                        }
                        else
                        {
                            RequeueWork(_packageExitUpdateItems, packageExitUpdateModel);
                        }
                    }
                    inFlightExitUpdate = null;
                    RemoveExpiredPendingInstructions();
                    if (HasPendingWork())
                    {
                        SignalWork();
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    if (inFlightInsertBatch is not null)
                    {
                        foreach (var package in inFlightInsertBatch)
                        {
                            RequeueWork(_insertItems, package);
                        }
                    }
                    if (inFlightResponse is not null)
                    {
                        RequeueWork(_updateResponseItems, inFlightResponse);
                    }
                    if (inFlightSavedImage is not null)
                    {
                        RequeueWork(_savedImageItems, inFlightSavedImage);
                    }
                    if (inFlightInstruction is not null)
                    {
                        RequeueWork(_instructionItems, inFlightInstruction);
                    }
                    if (inFlightExceptionSorting is not null)
                    {
                        RequeueWork(_exceptionSortingItems, inFlightExceptionSorting);
                    }
                    if (inFlightExitUpdate is not null)
                    {
                        RequeueWork(_packageExitUpdateItems, inFlightExitUpdate);
                    }
                    NLog.LogManager.GetCurrentClassLogger().Error($"数据存储异常,正在重试:{e}");
                    if (HasPendingWork())
                    {
                        SignalWork();
                    }
                }
            }
        }

        /// <summary>
        /// 将数据加入指定队列并通知后台消费者。
        /// </summary>
        /// <typeparam name="T">队列元素类型。</typeparam>
        /// <param name="queue">目标并发队列。</param>
        /// <param name="item">需要持久化的数据。</param>
        private void EnqueueWork<T>(LosslessWorkQueue<T> queue, T item) where T : class
        {
            if (!queue.TryEnqueue(item))
            {
                LogManager.GetCurrentClassLogger().Error(
                    $"数据持久化队列已经停止，工作未入队:{typeof(T).Name}");
                return;
            }
            SignalWork();
        }

        /// <summary>
        /// 将失败工作重新入队并请求短暂退避。
        /// </summary>
        private void RequeueWork<T>(LosslessWorkQueue<T> queue, T item) where T : class
        {
            var isWithinInitialRetryWindow =
                _retryTracker.TryRegisterFailure(item, out var attempt);
            if (!isWithinInitialRetryWindow &&
                (attempt == _retryTracker.MaxAttempts + 1 || attempt % 100 == 0))
            {
                LogManager.GetCurrentClassLogger().Warn(
                    $"数据持久化任务持续失败但将保留重试:{typeof(T).Name},Attempt={attempt}");
            }
            _retryScheduler.Schedule(
                TimeSpan.FromMilliseconds(50),
                () => EnqueueWork(queue, item));
        }

        /// <summary>
        /// 包裹主记录的插入依赖条码、重量等数据，通常晚于 FC12 指令。暂存后在主记录插入成功时回灌，
        /// 避免用短间隔轮询占用持久化消费者。
        /// </summary>
        private void StoreInstructionWaitingForPackage(InstructionReceived item)
        {
            if (!_pendingInstructions.TryGetValue(item.Timestamp, out var instructions))
            {
                instructions = new Queue<(InstructionReceived Instruction, long EnqueuedAt)>();
                _pendingInstructions[item.Timestamp] = instructions;
            }
            instructions.Enqueue((item, Stopwatch.GetTimestamp()));
            _pendingInstructionCount++;
        }

        /// <summary>主记录插入成功后立即恢复此前暂存的同包裹指令。</summary>
        private void ReleasePendingInstructions(long packageTimestamp)
        {
            if (!_pendingInstructions.Remove(packageTimestamp, out var instructions))
            {
                return;
            }

            while (instructions.TryDequeue(out var pending))
            {
                _pendingInstructionCount--;
                EnqueueWork(_instructionItems, pending.Instruction);
            }
        }

        /// <summary>清理始终没有生成主记录的孤立指令，保持暂存集合有界。</summary>
        private void RemoveExpiredPendingInstructions()
        {
            var now = Stopwatch.GetTimestamp();
            if (now < Volatile.Read(ref _nextPendingInstructionCleanupTimestamp))
            {
                return;
            }
            Volatile.Write(
                ref _nextPendingInstructionCleanupTimestamp,
                now + Stopwatch.Frequency * 60L);

            List<long>? emptyKeys = null;
            foreach (var pair in _pendingInstructions)
            {
                while (pair.Value.TryPeek(out var pending) &&
                       Stopwatch.GetElapsedTime(pending.EnqueuedAt, now) > InstructionAssociationTimeout)
                {
                    pair.Value.Dequeue();
                    _pendingInstructionCount--;
                    LogManager.GetCurrentClassLogger().Error(
                        $"指令等待包裹主记录超时，停止关联:Timestamp={pending.Instruction.Timestamp},SortingCode={pending.Instruction.SortingCode}");
                }

                if (pair.Value.Count == 0)
                {
                    (emptyKeys ??= []).Add(pair.Key);
                }
            }
            if (emptyKeys is not null)
            {
                foreach (var key in emptyKeys)
                {
                    _pendingInstructions.Remove(key);
                }
            }
        }

        /// <summary>
        /// 尝试发送一次无计数累积的工作通知。
        /// </summary>
        private void SignalWork()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                return;
            }

            if (Interlocked.Exchange(ref _workSignalArmed, 1) != 0)
            {
                return;
            }

            try
            {
                _workSignal.Release();
            }
            catch (SemaphoreFullException)
            {
                // 其他生产者已经完成通知，无需重复累积。
                Volatile.Write(ref _workSignalArmed, 1);
            }
            catch (ObjectDisposedException) when (Volatile.Read(ref _isDisposed) != 0)
            {
                // 服务关闭期间允许已在途的事件安静退出。
            }
        }

        /// <summary>释放持久化服务拥有的调度器和唤醒句柄。</summary>
        public override void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            {
                return;
            }

            _retryScheduler.Dispose();
            _workSignal.Dispose();
            base.Dispose();
        }

        /// <summary>
        /// 判断任意持久化队列中是否仍有待处理数据。
        /// </summary>
        /// <returns>存在待处理数据时返回 true。</returns>
        private bool HasPendingWork()
        {
            return !_insertItems.IsEmpty ||
                   !_updateResponseItems.IsEmpty ||
                   !_savedImageItems.IsEmpty ||
                   !_instructionItems.IsEmpty ||
                   !_exceptionSortingItems.IsEmpty ||
                   !_packageExitUpdateItems.IsEmpty;
        }

        private sealed record CameraMetadata(string SerialNumber, string Name, string CustomName);
    }
}
