using NLog;
using System;
using JayTom.Dws.Application.Workflows;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using static ImTools.ImMap;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Model;
using System.Windows.Documents;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Interface.Cloud;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using PackageInfo = JayTom.Dws.Domain.Manager.PackageInfo;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;
using WindowsAction = JayTom.Dws.Domain.EventMediators.WindowsAction;
using SortingExitType = JayTom.Dws.Domain.EventMediators.SortingExitType;
using WindowsActionType = JayTom.Dws.Domain.EventMediators.WindowsActionType;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;
using PackageExitUpdateEvent = JayTom.Dws.Domain.EventMediators.PackageExitUpdateEvent;
using PackageAbnormalSortingType = JayTom.Dws.Domain.EventMediators.PackageAbnormalSortingType;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    /// <summary>
    /// 数据处理器
    /// </summary>
    public class DataProcessingBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {

        //private readonly IPanoramaImageRepository _panoramaImageRepository;
        private readonly IPackageRepository _packageRepository;

        private readonly IImageStorageService _imageStorageService;

        private readonly ISortingRepository _sortingRepository;
        private readonly IUploadRepository _uploadRepository;
        private readonly IImageRepository _imageRepository;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly IExitInfoRepository _exitInfoRepository;
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
        private int _retryRequested;

        private CameraMetadata[] _cameraMetadata = [];
        private int _isWindowsClose;

        public DataProcessingBackgroundService(IPackageRepository packageRepository,
            IImageStorageService imageStorageService, ISortingRepository sortingRepository,
            IUploadRepository uploadRepository,
            IImageRepository imageRepository,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IExitInfoRepository exitInfoRepository)
        {
            _packageRepository = packageRepository;
            _imageStorageService = imageStorageService;
            _sortingRepository = sortingRepository;
            _uploadRepository = uploadRepository;
            _imageRepository = imageRepository;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _exitInfoRepository = exitInfoRepository;
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
            EventAggregator.Instance.SubscribePackage<PackageInfo>(item =>
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
            EventAggregator.Instance.SubscribePackage<ApiResponseReceived>(item =>
            {
                if (item is { } model)
                {
                    EnqueueWork(_updateResponseItems, model);
                }
            });
            EventAggregator.Instance.Subscribe<InstructionReceived>(item =>
            {
                if (item is { } model)
                {
                    EnqueueWork(_instructionItems, model);
                }
            });
            EventAggregator.Instance.Subscribe<ExceptionSortingReceived>(item =>
            {
                if (item is { } model)
                {
                    EnqueueWork(_exceptionSortingItems, model);
                }
            });
            EventAggregator.Instance.Subscribe<WindowsAction>(item =>
            {
                if (item is { Type: WindowsActionType.Close })
                {
                    Interlocked.Exchange(ref _isWindowsClose, 1);
                    SignalWork();
                }
            });

            EventAggregator.Instance.Subscribe<PackageExitUpdateEvent>(item =>
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
            Volatile.Write(ref _cameraMetadata, [.. cameraConfigs.Select(camera => new CameraMetadata(camera.SerialNumber, camera.Name, camera.CustomName))]);

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
                    var insertBatch = new List<PackageInfoModel>(PackageInsertBatchSize);
                    while (insertBatch.Count < PackageInsertBatchSize &&
                           _insertItems.TryDequeue(out var insertModel))
                    {
                        insertBatch.Add(insertModel);
                    }
                    if (insertBatch.Count > 0)
                    {
                        inFlightInsertBatch = insertBatch;
                        var inserted = await _packageRepository.InsertPackageRange(
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
                        var packageInfo = await _packageRepository.GetMemoryCachePackageInfo(responseModel.Timestamp, stoppingToken);

                        //更新
                        /*var (key, value) = await _packageRepository.FirstOrDefaultInfo(f => f.BarCodeInfo != null &&
                                f.BarCodeInfo.Barcode.Equals(responseModel.Barcode) &&
                                f.BarCodeInfo.ScanTime.Equals(responseModel.ScanTime),
                            stoppingToken);*/

                        if (packageInfo is { Id: > 0 } packageInfoModel && responseModel.UploadResponse is not null)
                        {
                            var insert = await _uploadRepository.Insert(new UploadInfoModel()
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
                                ApiExceptionType = (ApiExceptionType)responseModel.UploadResponse.ApiExceptionType,
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
                            var packageInfo = await _packageRepository
                                .GetMemoryCachePackageInfo(savedImageInfo.PackageTimestamp, stoppingToken);
                            if (packageInfo is { Id: > 0 } packageInfoModel)
                            {
                                var cameraConfigInfoModel = Volatile.Read(ref _cameraMetadata)
                                    .FirstOrDefault(camera =>
                                        camera.SerialNumber.Equals(
                                            savedImageInfo.CameraSerialNumber,
                                            StringComparison.Ordinal));

                                var insert = await _imageRepository.Insert(new ImageInfoModel()
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

                        /*var (key, value) = await _packageRepository.FirstOrDefaultInfo(
                            f => f.PackageTimestamped.Equals(sortingModel.Timestamp),
                            stoppingToken);*/

                        var packageInfo = await _packageRepository.GetMemoryCachePackageInfo(sortingModel.Timestamp, stoppingToken);

                        if (packageInfo is { Id: > 0 } packageInfoModel)
                        {
                            /*//判断是否已存在记录
                            var sortingInfoModel = await _sortingRepository.FirstOrDefault(f =>
                                f.PackageId.Equals(packageInfoModel.Id), stoppingToken);*/
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
                                isSortingUpdateInfo = await _sortingRepository.Insert(packageInfo.SortingInfo, stoppingToken);
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
                                    await _sortingRepository.Update(packageInfo.SortingInfo, stoppingToken);
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
                                _packageRepository.UpDateMemoryCachePackageInfo(packageInfo, stoppingToken);
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
                        /*var (key, value) = await _packageRepository.FirstOrDefaultInfo(f => f.BarCodeInfo != null &&
                                f.BarCodeInfo.Barcode.Equals(exceptionSortingModel.BarCode) &&
                                f.BarCodeInfo.ScanTime.Equals(exceptionSortingModel.ScanTime),
                            stoppingToken);*/

                        var packageInfo = await _packageRepository.GetMemoryCachePackageInfo(exceptionSortingModel.Timestamp, stoppingToken);
                        /*var sortingInfoModel =
                            await _sortingRepository.FirstOrDefault(f => f.PackageId.Equals(value.Id),
                                stoppingToken);*/
                        if (packageInfo?.SortingInfo is not null)
                        {
                            packageInfo.SortingInfo.AbnormalSortingType =
                                (AbnormalSortingType)exceptionSortingModel.PackageCloudAbnormalSortingType;
                            packageInfo.SortingInfo.IsAbnormalSorting =
                                exceptionSortingModel.PackageCloudAbnormalSortingType !=
                                PackageCloudAbnormalSortingType.None;
                            var update = await _sortingRepository.Update(packageInfo.SortingInfo, stoppingToken);
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
                        /*var (key, value) = await _packageRepository.FirstOrDefaultInfo(
                            f => f.PackageTimestamped.Equals(packageExitUpdateModel.Timestamp),
                            stoppingToken);*/
                        var packageInfo = await _packageRepository.GetMemoryCachePackageInfo(packageExitUpdateModel.Timestamp, stoppingToken);
                        if (packageInfo is not null)
                        {
                            //更新格口

                            //如果出现异常则不再更新

                            /*var model = await _exitInfoRepository.FirstOrDefault(f => f.PackageId.Equals(value.Id),
                                stoppingToken);*/

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

                                var update = await _exitInfoRepository.Update(packageInfo.ExitInfo, stoppingToken);
                                if (!update)
                                {
                                    RequeueWork(_packageExitUpdateItems, packageExitUpdateModel);
                                    await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
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

                                var insert = await _exitInfoRepository.Insert(packageInfo.ExitInfo, stoppingToken);
                                if (!insert)
                                {
                                    packageInfo.ExitInfo = null;
                                    RequeueWork(_packageExitUpdateItems, packageExitUpdateModel);
                                    await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
                                    continue;
                                }
                            }

                            if (packageExitUpdateModel.PackageAbnormalSortingType !=
                                PackageAbnormalSortingType.None &&
                                packageInfo.ExitInfo?.PackageId > 0)
                            {
                                //更新异常
                                /*var sortingInfoModel =

                                    await _sortingRepository.FirstOrDefault(
                                        f => f.PackageId.Equals(packageInfo.ExitInfo.PackageId), stoppingToken);*/
                                if (packageInfo.SortingInfo is not null)
                                {
                                    packageInfo.SortingInfo.AbnormalSortingType =
                                        (AbnormalSortingType)packageExitUpdateModel.PackageAbnormalSortingType;
                                    packageInfo.SortingInfo.IsAbnormalSorting =
                                        packageExitUpdateModel.PackageAbnormalSortingType !=
                                        PackageAbnormalSortingType.None;
                                    var sortingUpdated = await _sortingRepository.Update(
                                        packageInfo.SortingInfo,
                                        stoppingToken);
                                    if (!sortingUpdated)
                                    {
                                        RequeueWork(_packageExitUpdateItems, packageExitUpdateModel);
                                        await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
                                        continue;
                                    }
                                }
                            }
                            _packageRepository.UpDateMemoryCachePackageInfo(packageInfo, stoppingToken);
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
                    if (Interlocked.Exchange(ref _retryRequested, 0) != 0)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
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
                    await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
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
            Interlocked.Exchange(ref _retryRequested, 1);
            EnqueueWork(queue, item);
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
            foreach (var pair in _pendingInstructions.ToArray())
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
                    _pendingInstructions.Remove(pair.Key);
                }
            }
        }

        /// <summary>
        /// 尝试发送一次无计数累积的工作通知。
        /// </summary>
        private void SignalWork()
        {
            if (_workSignal.CurrentCount != 0)
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
            }
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
