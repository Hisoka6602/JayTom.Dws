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

namespace JayTom.Dws.Client.Service.BackgroundService {

    /// <summary>
    /// 数据处理器
    /// </summary>
    public class DataProcessingBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {

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

        private CameraMetadata[] _cameraMetadata = Array.Empty<CameraMetadata>();
        private int _isWindowsClose;

        public DataProcessingBackgroundService(IPackageRepository packageRepository,
            IImageStorageService imageStorageService, ISortingRepository sortingRepository,
            IUploadRepository uploadRepository,
            IImageRepository imageRepository,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IExitInfoRepository exitInfoRepository) {
            _packageRepository = packageRepository;
            _imageStorageService = imageStorageService;
            _sortingRepository = sortingRepository;
            _uploadRepository = uploadRepository;
            _imageRepository = imageRepository;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _exitInfoRepository = exitInfoRepository;
            _imageStorageService.ImageSaved += delegate (object? sender, ImageSavedEventArgs args) {
                //保存后触发

                EnqueueWork(_savedImageItems, new SavedImageInfo() {
                    PackageTimestamp = args.PackageTimestamp,
                    BarCode = args.BarCode,
                    FilePath = args.FilePath,
                    ImageType = args.ImageType,
                    CameraSerialNumber = args.CameraSerialNumber ?? string.Empty,
                    ScanTime = args.ScanTime,
                });
            };
            EventAggregator.Instance.Subscribe<PackageInfo>(item => {
                if (item is { } model) {
                    //添加
                    EnqueueWork(_insertItems, new PackageInfoModel() {
                        BarCodeInfo = model.BarCodeInfo,
                        WeightInfo = model.WeightInfo,
                        VolumeInfo = model.VolumeInfo,
                        PackageCreateTime = model.CreateTime,
                        PackageTimestamped = new DateTimeOffset(model.CreateTime).ToUnixTimeMilliseconds(),
                    });
                }
            });
            EventAggregator.Instance.Subscribe<ApiResponseReceived>(item => {
                if (item is { } model) {
                    EnqueueWork(_updateResponseItems, model);
                }
            });
            EventAggregator.Instance.Subscribe<InstructionReceived>(item => {
                if (item is { } model) {
                    EnqueueWork(_instructionItems, model);
                }
            });
            EventAggregator.Instance.Subscribe<ExceptionSortingReceived>(item => {
                if (item is { } model) {
                    EnqueueWork(_exceptionSortingItems, model);
                }
            });
            EventAggregator.Instance.Subscribe<WindowsAction>(item => {
                if (item is { Type: WindowsActionType.Close }) {
                    Interlocked.Exchange(ref _isWindowsClose, 1);
                    SignalWork();
                }
            });

            EventAggregator.Instance.Subscribe<PackageExitUpdateEvent>(item => {
                if (item is { } info) {
                    EnqueueWork(_packageExitUpdateItems, info);
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            // 相机名称是配置数据，只在进入持久化循环前加载一次。热循环只查内存快照。
            var cameraConfigs = await _barcodeScannerCameraConfigRepository.MemoryCacheData();
            Volatile.Write(ref _cameraMetadata, cameraConfigs
                .Select(camera => new CameraMetadata(camera.SerialNumber, camera.Name, camera.CustomName))
                .ToArray());

            while (!stoppingToken.IsCancellationRequested && Volatile.Read(ref _isWindowsClose) == 0) {
                try {
                    await _workSignal.WaitAsync(stoppingToken);
                                var tryDequeue = _insertItems.TryDequeue(out var insertModel);
                                if (tryDequeue && insertModel is not null) {
                                    var insert = await _packageRepository.Insert(insertModel, stoppingToken);
                                    if (!insert) {
                                        LogManager.GetCurrentClassLogger().Error($"数据保存失败,正在重试...");
                                        EnqueueWork(_insertItems, insertModel);
                                    }
                                }

                                var dequeue = _updateResponseItems.TryDequeue(out var responseModel);
                                if (dequeue && responseModel is not null) {
                                    var packageInfo = await _packageRepository.GetMemoryCachePackageInfo(responseModel.Timestamp, stoppingToken);

                                    //更新
                                    /*var (key, value) = await _packageRepository.FirstOrDefaultInfo(f => f.BarCodeInfo != null &&
                                            f.BarCodeInfo.Barcode.Equals(responseModel.Barcode) &&
                                            f.BarCodeInfo.ScanTime.Equals(responseModel.ScanTime),
                                        stoppingToken);*/

                                    if (packageInfo is { Id: > 0 } packageInfoModel && responseModel.UploadResponse is not null) {
                                        var insert = await _uploadRepository.Insert(new UploadInfoModel() {
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

                                        if (!insert) {
                                            EnqueueWork(_updateResponseItems, responseModel);
                                        }
                                    }
                                    else {
                                        EnqueueWork(_updateResponseItems, responseModel);
                                    }
                                }
                                //更新图片路径

                                var isSaved = _savedImageItems.TryDequeue(out var savedImageInfo);
                                if (isSaved && savedImageInfo is not null) {
                                    if (savedImageInfo.ImageType == SaveImageType.BarcodeImage) {
                                        //扫码图
                                        /*var (key, value) = await _packageRepository.FirstOrDefaultInfo(f =>
                                                f.PackageTimestamped.Equals(savedImageInfo.PackageTimestamp),
                                            stoppingToken);*/
                                        var packageInfo = await _packageRepository.GetMemoryCachePackageInfo(savedImageInfo.PackageTimestamp, stoppingToken);

                                        if (packageInfo is { Id: > 0 } packageInfoModel) {
                                            //获取相机信息
                                            var cameraConfigInfoModel = Volatile.Read(ref _cameraMetadata)
                                                .FirstOrDefault(camera =>
                                                    camera.SerialNumber.Equals(
                                                        savedImageInfo.CameraSerialNumber,
                                                        StringComparison.Ordinal));

                                            var insert = await _imageRepository.Insert(new ImageInfoModel() {
                                                PackageId = packageInfoModel.Id,
                                                CameraName = cameraConfigInfoModel?.Name ?? string.Empty,
                                                CameraSerialNumber = savedImageInfo.CameraSerialNumber,
                                                CustomCameraName = cameraConfigInfoModel?.CustomName ?? string.Empty,
                                                LocalPath = savedImageInfo.FilePath ?? string.Empty,
                                                Type = 0
                                            }, stoppingToken);

                                            if (!insert) {
                                                EnqueueWork(_savedImageItems, savedImageInfo);
                                            }
                                        }
                                        else {
                                            EnqueueWork(_savedImageItems, savedImageInfo);
                                        }
                                    }
                                    else if (savedImageInfo.ImageType == SaveImageType.PanoramaImage) {
                                        //全景图
                                        /*var (key, value) = await _packageRepository.FirstOrDefaultInfo(f =>
                                            f.PackageTimestamped.Equals(savedImageInfo.PackageTimestamp),
                                            stoppingToken);*/
                                        var packageInfo = await _packageRepository.GetMemoryCachePackageInfo(savedImageInfo.PackageTimestamp, stoppingToken);

                                        if (packageInfo is { Id: > 0 } packageInfoModel) {
                                            var cameraConfigInfoModel = Volatile.Read(ref _cameraMetadata)
                                                .FirstOrDefault(camera =>
                                                    camera.SerialNumber.Equals(
                                                        savedImageInfo.CameraSerialNumber,
                                                        StringComparison.Ordinal));

                                            var insert = await _imageRepository.Insert(new ImageInfoModel() {
                                                PackageId = packageInfoModel.Id,
                                                CameraName = cameraConfigInfoModel?.Name ?? string.Empty,
                                                CameraSerialNumber = savedImageInfo.CameraSerialNumber,
                                                CustomCameraName = cameraConfigInfoModel?.CustomName ?? string.Empty,
                                                LocalPath = savedImageInfo.FilePath ?? string.Empty,
                                                Type = 1
                                            }, stoppingToken);

                                            if (!insert) {
                                                EnqueueWork(_savedImageItems, savedImageInfo);
                                            }
                                        }
                                        else {
                                            EnqueueWork(_savedImageItems, savedImageInfo);
                                        }
                                    }
                                }

                                //指令更新
                                var isSorting = _instructionItems.TryDequeue(out var sortingModel);
                                if (isSorting && sortingModel is not null) {
                                    //取出对应条码id(根据条码、扫码时间)

                                    /*var (key, value) = await _packageRepository.FirstOrDefaultInfo(
                                        f => f.PackageTimestamped.Equals(sortingModel.Timestamp),
                                        stoppingToken);*/

                                    var packageInfo = await _packageRepository.GetMemoryCachePackageInfo(sortingModel.Timestamp, stoppingToken);

                                    if (packageInfo is { Id: > 0 } packageInfoModel) {
                                        /*//判断是否已存在记录
                                        var sortingInfoModel = await _sortingRepository.FirstOrDefault(f =>
                                            f.PackageId.Equals(packageInfoModel.Id), stoppingToken);*/
                                        bool isSortingUpdateInfo;
                                        if (packageInfo.SortingInfo is null) {
                                            //存到表
                                            packageInfo.SortingInfo = new SortingInfoModel() {
                                                PackageId = packageInfoModel.Id,
                                                ChecksumProtocolName = sortingModel.ChecksumProtocolName,
                                                CommunicationMethod = sortingModel.CommunicationMethod,
                                                IsCreatedByLowerMachine = sortingModel.IsCreatedByLowerMachine,
                                                IsSortingUsed = true,
                                                SortingCode = sortingModel.SortingCode,
                                                SortingMode = sortingModel.SortingMode,
                                                InstructionInfos = sortingModel.InstructionInfos?.Select(s =>
                                                    new InstructionInfoModel {
                                                        InstructionType = s.InstructionType,
                                                        InstructionContent = s.InstructionContent,
                                                        InstructionGeneratedTime = s.InstructionGeneratedTime,
                                                    })?.ToList() ?? new List<InstructionInfoModel>()
                                            };
                                            isSortingUpdateInfo = await _sortingRepository.Insert(packageInfo.SortingInfo, stoppingToken);
                                        }
                                        else {
                                            //更新
                                            packageInfo.SortingInfo.InstructionInfos ??= new List<InstructionInfoModel>();
                                            packageInfo.SortingInfo.IsCreatedByLowerMachine = sortingModel.IsCreatedByLowerMachine;
                                            packageInfo.SortingInfo.IsSortingUsed = true;
                                            if (!string.IsNullOrEmpty(sortingModel.ChecksumProtocolName)) {
                                                packageInfo.SortingInfo.ChecksumProtocolName = sortingModel.ChecksumProtocolName;
                                            }

                                            if (sortingModel.CommunicationMethod != CommunicationsType.None) {
                                                packageInfo.SortingInfo.CommunicationMethod = sortingModel.CommunicationMethod;
                                            }

                                            if (sortingModel.SortingMode != SortMode.None) {
                                                packageInfo.SortingInfo.SortingMode = sortingModel.SortingMode;
                                            }

                                            var instructionInfoModels = sortingModel.InstructionInfos?.Select(s =>
                                                new InstructionInfoModel {
                                                    InstructionType = s.InstructionType,
                                                    InstructionContent = s.InstructionContent,
                                                    InstructionGeneratedTime = s.InstructionGeneratedTime,
                                                })?.ToList() ?? new List<InstructionInfoModel>();
                                            foreach (var instructionInfoModel in instructionInfoModels) {
                                                packageInfo.SortingInfo.InstructionInfos.Add(instructionInfoModel);
                                            }

                                            isSortingUpdateInfo =
                                                await _sortingRepository.Update(packageInfo.SortingInfo, stoppingToken);
                                        }

                                        if (!isSortingUpdateInfo) {
                                            EnqueueWork(_instructionItems, sortingModel);
                                        }
                                        _packageRepository.UpDateMemoryCachePackageInfo(packageInfo, stoppingToken);
                                    }
                                    else {
                                        EnqueueWork(_instructionItems, sortingModel);
                                    }
                                }
                                //异常更新

                                var isExceptionSorting = _exceptionSortingItems.TryDequeue(out var exceptionSortingModel);
                                if (isExceptionSorting && exceptionSortingModel is not null) {
                                    /*var (key, value) = await _packageRepository.FirstOrDefaultInfo(f => f.BarCodeInfo != null &&
                                            f.BarCodeInfo.Barcode.Equals(exceptionSortingModel.BarCode) &&
                                            f.BarCodeInfo.ScanTime.Equals(exceptionSortingModel.ScanTime),
                                        stoppingToken);*/

                                    var packageInfo = await _packageRepository.GetMemoryCachePackageInfo(exceptionSortingModel.Timestamp, stoppingToken);
                                    /*var sortingInfoModel =
                                        await _sortingRepository.FirstOrDefault(f => f.PackageId.Equals(value.Id),
                                            stoppingToken);*/
                                    if (packageInfo?.SortingInfo is not null) {
                                        packageInfo.SortingInfo.AbnormalSortingType =
                                            (AbnormalSortingType)exceptionSortingModel.PackageCloudAbnormalSortingType;
                                        packageInfo.SortingInfo.IsAbnormalSorting =
                                            exceptionSortingModel.PackageCloudAbnormalSortingType !=
                                            PackageCloudAbnormalSortingType.None;
                                        var update = await _sortingRepository.Update(packageInfo.SortingInfo, stoppingToken);
                                        if (!update) {
                                            EnqueueWork(_exceptionSortingItems, exceptionSortingModel);
                                        }
                                    }
                                    else {
                                        EnqueueWork(_exceptionSortingItems, exceptionSortingModel);
                                    }
                                }

                                //格口更新
                                var packageExitUpdate = _packageExitUpdateItems.TryDequeue(out var packageExitUpdateModel);
                                if (packageExitUpdate && packageExitUpdateModel is not null) {
                                    //找到对应的包裹
                                    /*var (key, value) = await _packageRepository.FirstOrDefaultInfo(
                                        f => f.PackageTimestamped.Equals(packageExitUpdateModel.Timestamp),
                                        stoppingToken);*/
                                    var packageInfo = await _packageRepository.GetMemoryCachePackageInfo(packageExitUpdateModel.Timestamp, stoppingToken);
                                    if (packageInfo is not null) {
                                        //更新格口

                                        //如果出现异常则不再更新

                                        /*var model = await _exitInfoRepository.FirstOrDefault(f => f.PackageId.Equals(value.Id),
                                            stoppingToken);*/

                                        if (packageInfo.ExitInfo is not null) {
                                            //更新
                                            switch (packageExitUpdateModel.ExitType) {
                                                case SortingExitType.PhysicalExit:
                                                    packageInfo.ExitInfo.PhysicalExit = packageExitUpdateModel.ExitName;
                                                    packageInfo.ExitInfo.PhysicalExitId = packageExitUpdateModel.ExitId;
                                                    break;

                                                case SortingExitType.TheoreticalExit:
                                                    packageInfo.ExitInfo.TheoreticalExit = packageExitUpdateModel.ExitName;
                                                    break;
                                            }

                                            var update = await _exitInfoRepository.Update(packageInfo.ExitInfo, stoppingToken);
                                            if (!update) {
                                                EnqueueWork(_packageExitUpdateItems, packageExitUpdateModel);
                                                return;
                                            }
                                        }
                                        else {
                                            //添加
                                            packageInfo.ExitInfo = packageExitUpdateModel.ExitType switch {
                                                SortingExitType.PhysicalExit => new ExitInfoModel() {
                                                    PackageId = packageInfo.Id,
                                                    PhysicalExit = packageExitUpdateModel.ExitName,
                                                    PhysicalExitId = packageExitUpdateModel.ExitId,
                                                },
                                                SortingExitType.TheoreticalExit => new ExitInfoModel() {
                                                    PackageId = packageInfo.Id,
                                                    TheoreticalExit = packageExitUpdateModel.ExitName,
                                                    PhysicalExit = packageExitUpdateModel.ExitName,
                                                    PhysicalExitId = packageExitUpdateModel.ExitId,
                                                },
                                                _ => new()
                                            };

                                            var insert = await _exitInfoRepository.Insert(packageInfo.ExitInfo, stoppingToken);
                                            if (!insert) {
                                                EnqueueWork(_packageExitUpdateItems, packageExitUpdateModel);
                                                return;
                                            }
                                        }

                                        if (packageExitUpdateModel.PackageAbnormalSortingType !=
                                            PackageAbnormalSortingType.None &&
                                            packageInfo.ExitInfo?.PackageId > 0) {
                                            //更新异常
                                            /*var sortingInfoModel =

                                                await _sortingRepository.FirstOrDefault(
                                                    f => f.PackageId.Equals(packageInfo.ExitInfo.PackageId), stoppingToken);*/
                                            if (packageInfo.SortingInfo is not null) {
                                                packageInfo.SortingInfo.AbnormalSortingType =
                                                    (AbnormalSortingType)packageExitUpdateModel.PackageAbnormalSortingType;
                                                packageInfo.SortingInfo.IsAbnormalSorting =
                                                    packageExitUpdateModel.PackageAbnormalSortingType !=
                                                    PackageAbnormalSortingType.None;
                                                await _sortingRepository.Update(packageInfo.SortingInfo, stoppingToken);
                                            }
                                        }
                                        _packageRepository.UpDateMemoryCachePackageInfo(packageInfo, stoppingToken);
                                    }
                                    else {
                                        EnqueueWork(_packageExitUpdateItems, packageExitUpdateModel);
                                    }
                                }
                                if (HasPendingWork()) {
                                    SignalWork();
                                }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"数据存储异常,正在重试:{e}");
                    if (HasPendingWork()) {
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
        private void EnqueueWork<T>(ConcurrentQueue<T> queue, T item) {
            queue.Enqueue(item);
            SignalWork();
        }

        /// <summary>
        /// 尝试发送一次无计数累积的工作通知。
        /// </summary>
        private void SignalWork() {
            if (_workSignal.CurrentCount != 0) {
                return;
            }

            try {
                _workSignal.Release();
            }
            catch (SemaphoreFullException) {
                // 其他生产者已经完成通知，无需重复累积。
            }
        }

        /// <summary>
        /// 判断任意持久化队列中是否仍有待处理数据。
        /// </summary>
        /// <returns>存在待处理数据时返回 true。</returns>
        private bool HasPendingWork() {
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
