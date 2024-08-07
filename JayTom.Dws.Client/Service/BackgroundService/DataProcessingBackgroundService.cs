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
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using JayTom.Dws.Interface.Cloud;
using System.Collections.Concurrent;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Domain.Interface.Cloud;
using JayTom.Dws.Domain.Interface.Attributes;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using PackageInfo = JayTom.Dws.Domain.Manager.PackageInfo;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;

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
        private ConcurrentQueue<PackageInfoModel> _insertItems = new();
        private ConcurrentQueue<SavedImageInfo> _savedImageItems = new();

        private ConcurrentQueue<InstructionReceived> _instructionItems = new();
        private ConcurrentQueue<ExceptionSortingReceived> _exceptionSortingItems = new();
        private ConcurrentQueue<PackageExitUpdateEvent> _packageExitUpdateItems = new();

        private static bool _isWindowsClose;

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

                _savedImageItems.Enqueue(new SavedImageInfo() {
                    PackageTimestamp = args.PackageTimestamp,
                    BarCode = args.BarCode,
                    FilePath = args.FilePath,
                    ImageType = args.ImageType,
                    CameraSerialNumber = args.CameraSerialNumber ?? string.Empty,
                    ScanTime = args.ScanTime,
                });
            };
            EventAggregator.Instance.Subscribe<PackageInfo>(async item => {
                await Task.Yield();
                if (item is { } model) {
                    //添加
                    _insertItems.Enqueue(new PackageInfoModel() {
                        BarCodeInfo = model.BarCodeInfo,
                        WeightInfo = model.WeightInfo,
                        VolumeInfo = model.VolumeInfo,
                        PackageCreateTime = model.CreateTime,
                        PackageTimestamped = new DateTimeOffset(model.CreateTime).ToUnixTimeMilliseconds(),
                    });
                }
            });
            SubmitApiInfoManager.ApiResponseEvent += async (sender, info) => {
                //后续需要存所有的上传信息(暂时只存获取格口的)
                if (info is { UploadResponse.ExecutionType: ExecutionType.UploadInformation, PackageInfo: not null }) {
                    var updateResponseItems = await _packageRepository.FillNavigationPropertyAsync(info.PackageInfo.Timestamp, new UploadInfoModel() {
                        RequestStatus = info.UploadResponse.IsSuccess
                            ? UploadStatus.Succeeded
                            : UploadStatus.Failed,
                        RequestContent = info.UploadResponse.RequestContent,
                        ResponseContent = info.UploadResponse.ResponseContent,
                        RequestTime = info.UploadResponse.RequestTime,
                        ResponseTime = info.UploadResponse.ResponseTime,
                        DurationInSeconds = info.UploadResponse.Duration,
                        InterfaceParameters = info.UploadResponse.ApiParameters,
                        RequestUrl = info.UploadResponse.RequestUrl,
                        ExceptionMessage = info.UploadResponse.ExceptionMsg,
                        ApiExceptionType = (ApiExceptionType)info.UploadResponse.ApiExceptionType,
                    });
                    if (!updateResponseItems) {
                        NLog.LogManager.GetCurrentClassLogger().Error($"上传信息存储失败:{JsonConvert.SerializeObject(info.UploadResponse)}");
                    }
                }
            };

            EventAggregator.Instance.Subscribe<InstructionReceived>(async item => {
                await Task.Yield();
                if (item is { } model) {
                    _instructionItems.Enqueue(model);
                }
            });
            EventAggregator.Instance.Subscribe<ExceptionSortingReceived>(item => {
                if (item is { } model) {
                    _exceptionSortingItems.Enqueue(model);
                }
            });
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                await Task.Yield();
                if (item is { Type: WindowsActionType.Close }) {
                    _isWindowsClose = true;
                }
            });

            EventAggregator.Instance.Subscribe<PackageExitUpdateEvent>(async item => {
                await Task.Yield();
                if (item is { } info) {
                    _packageExitUpdateItems.Enqueue(info);
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                try {
                    await Task.Delay(TimeSpan.FromMilliseconds(80), stoppingToken)
                        .ContinueWith(async a => {
                            if (a.IsCompletedSuccessfully) {
                                var tryDequeue = _insertItems.TryDequeue(out var insertModel);
                                if (tryDequeue && insertModel is not null) {
                                    var insert = await _packageRepository.Insert(insertModel, stoppingToken);
                                    if (!insert) {
                                        LogManager.GetCurrentClassLogger().Error($"数据保存失败,正在重试...");
                                        _insertItems.Enqueue(insertModel);
                                    }
                                }

                                var isSaved = _savedImageItems.TryDequeue(out var savedImageInfo);
                                if (isSaved && savedImageInfo is not null) {
                                    if (savedImageInfo.ImageType == SaveImageType.BarcodeImage) {
                                        //扫码图
                                        var (key, value) = await _packageRepository.FirstOrDefaultInfo(f =>
                                                f.PackageTimestamped.Equals(savedImageInfo.PackageTimestamp),
                                            stoppingToken);

                                        if (key && value is { } packageInfoModel) {
                                            //获取相机信息
                                            var cameraConfigInfoModel =
                                                await _barcodeScannerCameraConfigRepository.FirstOrDefault(
                                                    f =>
                                                        f.SerialNumber.Equals(savedImageInfo.CameraSerialNumber),
                                                    stoppingToken);

                                            var insert = await _imageRepository.Insert(new ImageInfoModel() {
                                                PackageId = packageInfoModel.Id,
                                                CameraName = cameraConfigInfoModel?.Name ?? string.Empty,
                                                CameraSerialNumber = savedImageInfo.CameraSerialNumber,
                                                CustomCameraName = cameraConfigInfoModel?.CustomName ?? string.Empty,
                                                LocalPath = savedImageInfo.FilePath ?? string.Empty,
                                                Type = 0
                                            }, stoppingToken);

                                            if (!insert) {
                                                _savedImageItems.Enqueue(savedImageInfo);
                                            }
                                        }
                                        else {
                                            _savedImageItems.Enqueue(savedImageInfo);
                                        }
                                    }
                                    else if (savedImageInfo.ImageType == SaveImageType.PanoramaImage) {
                                        //全景图
                                        var (key, value) = await _packageRepository.FirstOrDefaultInfo(f =>
                                            f.PackageTimestamped.Equals(savedImageInfo.PackageTimestamp),
                                            stoppingToken);
                                        if (key && value is { } packageInfoModel) {
                                            var cameraConfigInfoModel =
                                                await _barcodeScannerCameraConfigRepository.FirstOrDefault(
                                                    f =>
                                                        f.SerialNumber.Equals(savedImageInfo.CameraSerialNumber),
                                                    stoppingToken);

                                            var insert = await _imageRepository.Insert(new ImageInfoModel() {
                                                PackageId = packageInfoModel.Id,
                                                CameraName = cameraConfigInfoModel?.Name ?? string.Empty,
                                                CameraSerialNumber = savedImageInfo.CameraSerialNumber,
                                                CustomCameraName = cameraConfigInfoModel?.CustomName ?? string.Empty,
                                                LocalPath = savedImageInfo.FilePath ?? string.Empty,
                                                Type = 1
                                            }, stoppingToken);

                                            if (!insert) {
                                                _savedImageItems.Enqueue(savedImageInfo);
                                            }
                                        }
                                        else {
                                            _savedImageItems.Enqueue(savedImageInfo);
                                        }
                                    }
                                }

                                //指令更新
                                var isSorting = _instructionItems.TryDequeue(out var sortingModel);
                                if (isSorting && sortingModel is not null) {
                                    var (key, value) = await _packageRepository.FirstOrDefaultInfo(
                                        f => f.PackageTimestamped.Equals(sortingModel.Timestamp),
                                        stoppingToken);
                                    if (key && value is { } packageInfoModel) {
                                        //判断是否已存在记录
                                        var sortingInfoModel = await _sortingRepository.FirstOrDefault(f =>
                                            f.PackageId.Equals(packageInfoModel.Id), stoppingToken);
                                        bool isSortingUpdateInfo;
                                        if (sortingInfoModel is null) {
                                            //存到表
                                            isSortingUpdateInfo = await _sortingRepository.Insert(new SortingInfoModel() {
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
                                            }, stoppingToken);
                                        }
                                        else {
                                            //更新
                                            sortingInfoModel.InstructionInfos ??= new List<InstructionInfoModel>();
                                            sortingInfoModel.IsCreatedByLowerMachine = sortingModel.IsCreatedByLowerMachine;
                                            sortingInfoModel.IsSortingUsed = true;
                                            if (!string.IsNullOrEmpty(sortingModel.ChecksumProtocolName)) {
                                                sortingInfoModel.ChecksumProtocolName = sortingModel.ChecksumProtocolName;
                                            }

                                            if (sortingModel.CommunicationMethod != CommunicationsType.None) {
                                                sortingInfoModel.CommunicationMethod = sortingModel.CommunicationMethod;
                                            }

                                            if (sortingModel.SortingMode != SortMode.None) {
                                                sortingInfoModel.SortingMode = sortingModel.SortingMode;
                                            }

                                            var instructionInfoModels = sortingModel.InstructionInfos?.Select(s =>
                                                new InstructionInfoModel {
                                                    InstructionType = s.InstructionType,
                                                    InstructionContent = s.InstructionContent,
                                                    InstructionGeneratedTime = s.InstructionGeneratedTime,
                                                })?.ToList() ?? new List<InstructionInfoModel>();
                                            foreach (var instructionInfoModel in instructionInfoModels) {
                                                sortingInfoModel.InstructionInfos.Add(instructionInfoModel);
                                            }

                                            isSortingUpdateInfo =
                                                await _sortingRepository.Update(sortingInfoModel, stoppingToken);
                                        }

                                        if (!isSortingUpdateInfo) {
                                            _instructionItems.Enqueue(sortingModel);
                                        }
                                    }
                                    else {
                                        _instructionItems.Enqueue(sortingModel);
                                    }
                                }
                                //异常更新

                                var isExceptionSorting = _exceptionSortingItems.TryDequeue(out var exceptionSortingModel);
                                if (isExceptionSorting && exceptionSortingModel is not null) {
                                    var (key, value) = await _packageRepository.FirstOrDefaultInfo(f => f.BarCodeInfo != null &&
                                            f.BarCodeInfo.Barcode.Equals(exceptionSortingModel.BarCode) &&
                                            f.BarCodeInfo.ScanTime.Equals(exceptionSortingModel.ScanTime),
                                        stoppingToken);
                                    var sortingInfoModel =
                                        await _sortingRepository.FirstOrDefault(f => f.PackageId.Equals(value.Id),
                                            stoppingToken);
                                    if (sortingInfoModel is not null) {
                                        sortingInfoModel.AbnormalSortingType =
                                            (AbnormalSortingType)exceptionSortingModel.PackageCloudAbnormalSortingType;
                                        sortingInfoModel.IsAbnormalSorting =
                                            exceptionSortingModel.PackageCloudAbnormalSortingType !=
                                            PackageCloudAbnormalSortingType.None;
                                        var update = await _sortingRepository.Update(sortingInfoModel, stoppingToken);
                                        if (!update) {
                                            _exceptionSortingItems.Enqueue(exceptionSortingModel);
                                        }
                                    }
                                    else {
                                        _exceptionSortingItems.Enqueue(exceptionSortingModel);
                                    }
                                }

                                //格口更新
                                var packageExitUpdate = _packageExitUpdateItems.TryDequeue(out var packageExitUpdateModel);
                                if (packageExitUpdate && packageExitUpdateModel is not null) {
                                    //找到对应的包裹
                                    var (key, value) = await _packageRepository.FirstOrDefaultInfo(
                                        f => f.PackageTimestamped.Equals(packageExitUpdateModel.Timestamp),
                                        stoppingToken);
                                    if (key && value is not null) {
                                        //更新格口

                                        //如果出现异常则不再更新

                                        var model = await _exitInfoRepository.FirstOrDefault(f => f.PackageId.Equals(value.Id),
                                            stoppingToken);

                                        if (model is not null) {
                                            //更新
                                            switch (packageExitUpdateModel.ExitType) {
                                                case SortingExitType.PhysicalExit:
                                                    model.PhysicalExit = packageExitUpdateModel.ExitName;
                                                    model.PhysicalExitId = packageExitUpdateModel.ExitId;
                                                    break;

                                                case SortingExitType.TheoreticalExit:
                                                    model.TheoreticalExit = packageExitUpdateModel.ExitName;
                                                    break;
                                            }

                                            var update = await _exitInfoRepository.Update(model, stoppingToken);
                                            if (!update) {
                                                _packageExitUpdateItems.Enqueue(packageExitUpdateModel);
                                                return;
                                            }
                                        }
                                        else {
                                            //添加
                                            var exitInfoModel = packageExitUpdateModel.ExitType switch {
                                                SortingExitType.PhysicalExit => new ExitInfoModel() {
                                                    PackageId = value.Id,
                                                    PhysicalExit = packageExitUpdateModel.ExitName,
                                                    PhysicalExitId = packageExitUpdateModel.ExitId,
                                                },
                                                SortingExitType.TheoreticalExit => new ExitInfoModel() {
                                                    PackageId = value.Id,
                                                    TheoreticalExit = packageExitUpdateModel.ExitName,
                                                    PhysicalExit = packageExitUpdateModel.ExitName,
                                                    PhysicalExitId = packageExitUpdateModel.ExitId,
                                                },
                                                _ => new()
                                            };

                                            var insert = await _exitInfoRepository.Insert(exitInfoModel, stoppingToken);
                                            if (!insert) {
                                                _packageExitUpdateItems.Enqueue(packageExitUpdateModel);
                                                return;
                                            }
                                        }

                                        if (packageExitUpdateModel.PackageAbnormalSortingType !=
                                            PackageAbnormalSortingType.None &&
                                            model?.PackageId > 0) {
                                            //更新异常
                                            var sortingInfoModel =

                                                await _sortingRepository.FirstOrDefault(
                                                    f => f.PackageId.Equals(model.PackageId), stoppingToken);
                                            if (sortingInfoModel is not null) {
                                                sortingInfoModel.AbnormalSortingType =
                                                    (AbnormalSortingType)packageExitUpdateModel.PackageAbnormalSortingType;
                                                sortingInfoModel.IsAbnormalSorting =
                                                    packageExitUpdateModel.PackageAbnormalSortingType !=
                                                    PackageAbnormalSortingType.None;
                                                await _sortingRepository.Update(sortingInfoModel, stoppingToken);
                                            }
                                        }
                                    }
                                    else {
                                        _packageExitUpdateItems.Enqueue(packageExitUpdateModel);
                                    }
                                }
                            }
                        }, stoppingToken)
                        .Unwrap();
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"数据存储异常,正在重试:{e}");
                }
            }
        }
    }
}