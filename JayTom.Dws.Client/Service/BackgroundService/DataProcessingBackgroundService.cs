using NLog;
using System;
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
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using PackageInfo = JayTom.Dws.Domain.Manager.PackageInfo;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;
using WindowsAction = JayTom.Dws.Client.EventMediators.WindowsAction;
using SortingExitType = JayTom.Dws.Client.EventMediators.SortingExitType;
using WindowsActionType = JayTom.Dws.Client.EventMediators.WindowsActionType;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;
using PackageExitUpdateEvent = JayTom.Dws.Client.EventMediators.PackageExitUpdateEvent;
using PackageAbnormalSortingType = JayTom.Dws.Client.EventMediators.PackageAbnormalSortingType;

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
        private readonly ConcurrentQueue<PackageInfoModel> _insertItems = new();
        private readonly ConcurrentQueue<ApiResponseReceived> _updateResponseItems = new();
        private readonly ConcurrentQueue<SavedImageInfo> _savedImageItems = new();
        private readonly ConcurrentQueue<InstructionReceived> _instructionItems = new();
        private readonly ConcurrentQueue<ExceptionSortingReceived> _exceptionSortingItems = new();
        private readonly ConcurrentQueue<PackageExitUpdateEvent> _packageExitUpdateItems = new();

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
            EventAggregator.Instance.Subscribe<PackageInfo>(item =>
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
            EventAggregator.Instance.Subscribe<ApiResponseReceived>(item =>
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
                PackageInfoModel? inFlightInsert = null;
                ApiResponseReceived? inFlightResponse = null;
                SavedImageInfo? inFlightSavedImage = null;
                InstructionReceived? inFlightInstruction = null;
                ExceptionSortingReceived? inFlightExceptionSorting = null;
                PackageExitUpdateEvent? inFlightExitUpdate = null;
                try
                {
                    await _workSignal.WaitAsync(stoppingToken);
                    var tryDequeue = _insertItems.TryDequeue(out var insertModel);
                    inFlightInsert = insertModel;
                    if (tryDequeue && insertModel is not null)
                    {
                        var insert = await _packageRepository.Insert(insertModel, stoppingToken);
                        if (!insert)
                        {
                            LogManager.GetCurrentClassLogger().Error($"数据保存失败,正在重试...");
                            RequeueWork(_insertItems, insertModel);
                        }
                    }
                    inFlightInsert = null;

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
                                DurationInSeconds = responseModel.UploadResponse.Duration,
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
                            RequeueWork(_instructionItems, sortingModel);
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
                    if (HasPendingWork())
                    {
                        SignalWork();
                    }
                    if (Interlocked.Exchange(ref _retryRequested, 0) != 0)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
                    }
                }
                catch (Exception e)
                {
                    if (inFlightInsert is not null)
                    {
                        RequeueWork(_insertItems, inFlightInsert);
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
        private void EnqueueWork<T>(ConcurrentQueue<T> queue, T item)
        {
            queue.Enqueue(item);
            SignalWork();
        }

        /// <summary>
        /// 将失败工作重新入队并请求短暂退避。
        /// </summary>
        private void RequeueWork<T>(ConcurrentQueue<T> queue, T item)
        {
            Interlocked.Exchange(ref _retryRequested, 1);
            EnqueueWork(queue, item);
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
