using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Xml.Linq;
using System.Collections;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Linq.Expressions;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalConf;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using NPOI.XSSF.Streaming.Values;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.CrossCutting.SignalR;
using JayTom.Dws.Data.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalLog;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Domain.Entities.LogsEntities;
using JayTom.Dws.Domain.Entities.PackageEntities;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.DataInteractionService.SignalR {

    public class DataInteractionServiceMessageHub : BaseServerMessageHub, IDataInteractionServiceMessageHub {
        private readonly IPackageRepository _packageRepository;
        private readonly IConfigRepository _configRepository;
        private readonly IBarCodeRepository _barCodeRepository;
        private readonly IWeightRepository _weightRepository;
        private readonly IVolumeRepository _volumeRepository;
        private readonly IUploadRepository _uploadRepository;
        private readonly IExitInfoRepository _exitInfoRepository;
        private readonly ISortingRepository _sortingRepository;
        private readonly IOcrRepository _ocrRepository;
        private readonly IImageRepository _imageRepository;
        private readonly ICloudVideoUploadRepository _cloudVideoUploadRepository;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly IPanoramaCameraConfigRepository _panoramaCameraConfigRepository;
        private readonly IUsbCameraConfigRepository _usbCameraConfigRepository;
        private readonly IVolumeCameraConfigRepository _volumeCameraConfigRepository;
        private readonly INvrCameraBindingRepository _nvrCameraBindingRepository;
        private readonly ICommunicationConnectionConfigRepository _communicationConnectionConfigRepository;
        private readonly IApiSortingRepository _apiSortingRepository;
        private readonly IBarCodeSortingRepository _barCodeSortingRepository;
        private readonly ILogisticsCodeRecognitionRepository _logisticsCodeRecognitionRepository;
        private readonly ILogisticsSortingRepository _logisticsSortingRepository;
        private readonly IOcrSortingRepository _ocrSortingRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly IPackageExitLockBindingRepository _packageExitLockBindingRepository;
        private readonly ISortingInstructionBindingRepository _sortingInstructionBindingRepository;
        private readonly ISortingInstructionRepository _sortingInstructionRepository;
        private readonly IVolumeSortingRepository _volumeSortingRepository;
        private readonly IWeightSortingRepository _weightSortingRepository;
        private readonly IApiLogRepository _apiLogRepository;
        private readonly IAppLogRepository _appLogRepository;
        private readonly ICameraLogRepository _cameraLogRepository;
        private readonly IExceptionLogRepository _exceptionLogRepository;
        private readonly IFtpLogRepository _ftpLogRepository;
        private readonly IInputLogRepository _inputLogRepository;
        private readonly IOcrLogRepository _ocrLogRepository;
        private readonly IOutputLogRepository _outputLogRepository;
        private readonly ISortingLogRepository _sortingLogRepository;
        private readonly IVolumeLogRepository _volumeLogRepository;
        private readonly IWeighingLogRepository _weighingLogRepository;
        private readonly IImageStorageService _imageStorageService;

        public DataInteractionServiceMessageHub(IHubContext<BaseServerMessageHub> hubContext,
            ILogger<BaseServerMessageHub> logger, IPackageRepository packageRepository,
            IConfigRepository configRepository, IBarCodeRepository barCodeRepository,
            IWeightRepository weightRepository,
            IVolumeRepository volumeRepository,
            IUploadRepository uploadRepository,
            IExitInfoRepository exitInfoRepository,
            ISortingRepository sortingRepository,
            IOcrRepository ocrRepository,
            IImageRepository imageRepository,
            ICloudVideoUploadRepository cloudVideoUploadRepository,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IPanoramaCameraConfigRepository panoramaCameraConfigRepository,
            IUsbCameraConfigRepository usbCameraConfigRepository,
            IVolumeCameraConfigRepository volumeCameraConfigRepository,
            INvrCameraBindingRepository nvrCameraBindingRepository,
            ICommunicationConnectionConfigRepository communicationConnectionConfigRepository,
            IApiSortingRepository apiSortingRepository,
            IBarCodeSortingRepository barCodeSortingRepository,
            ILogisticsCodeRecognitionRepository logisticsCodeRecognitionRepository,
            ILogisticsSortingRepository logisticsSortingRepository,
            IOcrSortingRepository ocrSortingRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IPackageExitLockBindingRepository packageExitLockBindingRepository,
            ISortingInstructionBindingRepository sortingInstructionBindingRepository,
            ISortingInstructionRepository sortingInstructionRepository,
            IVolumeSortingRepository volumeSortingRepository,
            IWeightSortingRepository weightSortingRepository,
            IApiLogRepository apiLogRepository,
            IAppLogRepository appLogRepository,
            ICameraLogRepository cameraLogRepository,
            IExceptionLogRepository exceptionLogRepository,
            IFtpLogRepository ftpLogRepository,
            IInputLogRepository inputLogRepository,
            IOcrLogRepository ocrLogRepository,
            IOutputLogRepository outputLogRepository,
            ISortingLogRepository sortingLogRepository,
            IVolumeLogRepository volumeLogRepository,
            IWeighingLogRepository weighingLogRepository,
            IImageStorageService imageStorageService) : base(hubContext, logger) {
            _packageRepository = packageRepository;
            _configRepository = configRepository;
            _barCodeRepository = barCodeRepository;
            _weightRepository = weightRepository;
            _volumeRepository = volumeRepository;
            _uploadRepository = uploadRepository;
            _exitInfoRepository = exitInfoRepository;
            _sortingRepository = sortingRepository;
            _ocrRepository = ocrRepository;
            _imageRepository = imageRepository;
            _cloudVideoUploadRepository = cloudVideoUploadRepository;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _panoramaCameraConfigRepository = panoramaCameraConfigRepository;
            _usbCameraConfigRepository = usbCameraConfigRepository;
            _volumeCameraConfigRepository = volumeCameraConfigRepository;
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
            _communicationConnectionConfigRepository = communicationConnectionConfigRepository;
            _apiSortingRepository = apiSortingRepository;
            _barCodeSortingRepository = barCodeSortingRepository;
            _logisticsCodeRecognitionRepository = logisticsCodeRecognitionRepository;
            _logisticsSortingRepository = logisticsSortingRepository;
            _ocrSortingRepository = ocrSortingRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _packageExitLockBindingRepository = packageExitLockBindingRepository;
            _sortingInstructionBindingRepository = sortingInstructionBindingRepository;
            _sortingInstructionRepository = sortingInstructionRepository;
            _volumeSortingRepository = volumeSortingRepository;
            _weightSortingRepository = weightSortingRepository;
            _apiLogRepository = apiLogRepository;
            _appLogRepository = appLogRepository;
            _cameraLogRepository = cameraLogRepository;
            _exceptionLogRepository = exceptionLogRepository;
            _ftpLogRepository = ftpLogRepository;
            _inputLogRepository = inputLogRepository;
            _ocrLogRepository = ocrLogRepository;
            _outputLogRepository = outputLogRepository;
            _sortingLogRepository = sortingLogRepository;
            _volumeLogRepository = volumeLogRepository;
            _weighingLogRepository = weighingLogRepository;
            _imageStorageService = imageStorageService;

            _imageStorageService.ImageSaved += async (sender, args) => {
                //更新存图信息
                ImageInfoModel imageInfo = new();
                switch (args.ImageType) {
                    case SaveImageType.BarcodeImage: {
                            var scannerCameraConfigInfoModels = await _barcodeScannerCameraConfigRepository.MemoryCacheData();
                            var model = scannerCameraConfigInfoModels.FirstOrDefault(f => f.SerialNumber.Equals(args.CameraSerialNumber))
                                        ?? new BarcodeScannerCameraConfigInfoModel();

                            imageInfo = new ImageInfoModel() {
                                CameraSerialNumber = args.CameraSerialNumber ?? string.Empty,
                                CameraName = model.Name,
                                CustomCameraName = model.CustomName,
                                LocalPath = args.FilePath ?? string.Empty,
                                PackageId = args.PackageId ?? 0,
                                Type = (int)(args.ImageType ?? SaveImageType.BarcodeImage)
                            };
                            break;
                        }
                    case SaveImageType.PanoramaImage: {
                            var panoramaCameraConfigInfoModels = await _panoramaCameraConfigRepository.MemoryCacheData();
                            var model = panoramaCameraConfigInfoModels.FirstOrDefault(f => f.SerialNumber.Equals(args.CameraSerialNumber))
                                        ?? new PanoramaCameraConfigInfoModel();

                            imageInfo = new ImageInfoModel() {
                                CameraSerialNumber = args.CameraSerialNumber ?? string.Empty,
                                CameraName = model.Name,
                                CustomCameraName = model.CustomName,
                                LocalPath = args.FilePath ?? string.Empty,
                                PackageId = args.PackageId ?? 0,
                                Type = (int)(args.ImageType ?? SaveImageType.BarcodeImage)
                            };
                            break;
                        }
                    case SaveImageType.VolumeImage: {
                            var volumeCameraConfigInfoModels = await _volumeCameraConfigRepository.MemoryCacheData();
                            var model = volumeCameraConfigInfoModels.FirstOrDefault(f =>
                                            f.SerialNumber.Equals(args.CameraSerialNumber))
                                        ?? new VolumeCameraConfigInfoModel();

                            imageInfo = new ImageInfoModel() {
                                CameraSerialNumber = args.CameraSerialNumber ?? string.Empty,
                                CameraName = model.Name,
                                CustomCameraName = model.CustomName,
                                LocalPath = args.FilePath ?? string.Empty,
                                PackageId = args.PackageId ?? 0,
                                Type = (int)(args.ImageType ?? SaveImageType.BarcodeImage)
                            };
                            break;
                        }
                }
                UpdateImageDataAsync(args.PackageId ?? 0, imageInfo);
            };

            _imageStorageService.ImageSaveFailed += (sender, exception) => {
                logger.LogError($"存图失败:{exception.Message}");
            };
        }

        public ConcurrentQueue<PackageInfoModel> FallInsertPackageInfoModels { get; private set; } = new();
        public ConcurrentQueue<PackageInfoModel> FallUpdatePackageInfoModels { get; private set; } = new();
        public ConcurrentQueue<BarCodeInfoModel> FallUpdateBarcodeInfoModels { get; private set; } = new();
        public ConcurrentQueue<WeightInfoModel> FallUpdateWeightInfoModels { get; private set; } = new();
        public ConcurrentQueue<VolumeInfoModel> FallUpdateVolumeInfoModels { get; private set; } = new();
        public ConcurrentQueue<UploadInfoModel> FallUpdateUploadInfoModels { get; private set; } = new();
        public ConcurrentQueue<ExitInfoModel> FallUpdateCompartmentInfoModels { get; private set; } = new();
        public ConcurrentQueue<SortingInfoModel> FallUpdateSortingInfoModels { get; private set; } = new();
        public ConcurrentQueue<LogisticsInfoModel> FallUpdateLogisticsInfoModels { get; private set; } = new();
        public ConcurrentQueue<OcrInfoModel> FallUpdateOcrInfoModels { get; private set; } = new();
        public ConcurrentQueue<ImageInfoModel> FallUpdateImageInfoModels { get; private set; } = new();
        public ConcurrentQueue<CloudVideoUploadInfoModel> FallUpdateVideoCloudInfoModels { get; private set; } = new();
        public ConcurrentQueue<InstructionInfoModel> FallUpdateInstructionInfoModels { get; private set; } = new();
        public ConcurrentQueue<PackageInfoModel> FallUpdateDeviceInfoModels { get; private set; } = new();
        public ConcurrentQueue<PackageInfoModel> FallUpdateAggregatePackageInfoModels { get; private set; } = new();

        public async void AddOrUpdatePackageDataAsync(PackageInfoModel packageData) {
            var insertOrUpdate = await _packageRepository.InsertOrUpdate(packageData);
            if (!insertOrUpdate) {
                FallInsertPackageInfoModels.Enqueue(packageData);
            }
        }

        public async void UpdatePackageDataAsync(PackageInfoModel packageData) {
            var update = await _packageRepository.Update(packageData);
            if (!update) {
                FallUpdatePackageInfoModels.Enqueue(packageData);
            }
        }

        public async void UpdateBarcodeDataAsync(long packageId, BarCodeInfoModel barcodeData) {
            barcodeData.PackageId = packageId;
            var insertOrUpdate = await _barCodeRepository.InsertOrUpdate(barcodeData);
            if (!insertOrUpdate) {
                FallUpdateBarcodeInfoModels.Enqueue(barcodeData);
            }
        }

        public async void UpdateWeightDataAsync(long packageId, WeightInfoModel weightData) {
            weightData.PackageId = packageId;
            var insertOrUpdate = await _weightRepository.InsertOrUpdate(weightData);
            if (!insertOrUpdate) {
                FallUpdateWeightInfoModels.Enqueue(weightData);
            }
        }

        public async void UpdateVolumeDataAsync(long packageId, VolumeInfoModel volumeData) {
            volumeData.PackageId = packageId;
            var insertOrUpdate = await _volumeRepository.InsertOrUpdate(volumeData);
            if (!insertOrUpdate) {
                FallUpdateVolumeInfoModels.Enqueue(volumeData);
            }
        }

        public async void UpdateUploadDataAsync(long packageId, UploadInfoModel uploadData) {
            uploadData.PackageId = packageId;
            var insertOrUpdate = await _uploadRepository.InsertOrUpdate(uploadData);

            if (!insertOrUpdate) {
                FallUpdateUploadInfoModels.Enqueue(uploadData);
            }
        }

        public async void UpdateExitDataAsync(long packageId, ExitInfoModel compartmentData) {
            compartmentData.PackageId = packageId;

            var insertOrUpdate = await _exitInfoRepository.InsertOrUpdate(compartmentData);
            if (!insertOrUpdate) {
                FallUpdateCompartmentInfoModels.Enqueue(compartmentData);
            }
        }

        public async void UpdateSortingDataAsync(long packageId, SortingInfoModel sortingData) {
            sortingData.PackageId = packageId;
            var insertOrUpdate = await _sortingRepository.InsertOrUpdate(sortingData);
            if (!insertOrUpdate) {
                FallUpdateSortingInfoModels.Enqueue(sortingData);
            }
        }

        public async void UpdateOcrDataAsync(long packageId, OcrInfoModel ocrData) {
            ocrData.PackageId = packageId;
            var insertOrUpdate = await _ocrRepository.InsertOrUpdate(ocrData);

            if (!insertOrUpdate) {
                FallUpdateOcrInfoModels.Enqueue(ocrData);
            }
        }

        public async void UpdateImageDataAsync(long packageId, ImageInfoModel imageData) {
            imageData.PackageId = packageId;
            var insertOrUpdate = await _imageRepository.Insert(imageData);
            if (!insertOrUpdate) {
                FallUpdateImageInfoModels.Enqueue(imageData);
            }
        }

        public async void UpdateVideoCloudDataAsync(long packageId, CloudVideoUploadInfoModel videoCloudData) {
            videoCloudData.PackageId = packageId;
            var insertOrUpdate = await _cloudVideoUploadRepository.InsertOrUpdate(videoCloudData);
            if (!insertOrUpdate) {
                FallUpdateVideoCloudInfoModels.Enqueue(videoCloudData);
            }
        }

        public async void AddInstructionDataAsync(long packageId, InstructionInfoModel instructionData) {
            var sortingInfoModel = await _sortingRepository.FirstOrDefault(f => f.PackageId.Equals(packageId));
            if (sortingInfoModel is null) {
                //添加到回流
                instructionData.SortingInfoId = packageId;
                FallUpdateInstructionInfoModels.Enqueue(instructionData);
            }
            else {
                //存在
                sortingInfoModel.InstructionInfos ??= new List<InstructionInfoModel>();
                var instructionInfoModels = sortingInfoModel.InstructionInfos?.Select(s =>
                    new InstructionInfoModel {
                        InstructionType = s.InstructionType,
                        InstructionContent = s.InstructionContent,
                        InstructionGeneratedTime = s.InstructionGeneratedTime,
                    })?.ToList() ?? new List<InstructionInfoModel>();
                sortingInfoModel.InstructionInfos?.Clear();
                foreach (var instructionInfoModel in instructionInfoModels) {
                    sortingInfoModel.InstructionInfos?.Add(instructionInfoModel);
                }
                sortingInfoModel.InstructionInfos?.Add(instructionData);
                var update = await _sortingRepository.Update(sortingInfoModel);
                if (!update) {
                    //添加到回流
                }
            }
        }

        public async Task SaveImageDataAsync(PackageInfoModel packageInfo, SaveImageType type, byte[] imageData) {
            //获取设置

            using var ms = new MemoryStream(imageData);
            var image = Image.FromStream(ms);
            await _imageStorageService.SaveImage(packageInfo.Id, image,
                type, packageInfo.BarCodeInfo?.Barcode ?? string.Empty, MathF.Round((float)(packageInfo.WeightInfo?.FormattedWeight ?? 0), 3),
                packageInfo.BarCodeInfo?.ScanTime ?? DateTime.MinValue, MathF.Round((float)(packageInfo.VolumeInfo?.FormattedLength ?? 0), 3),
                MathF.Round((float)(packageInfo.VolumeInfo?.FormattedWidth ?? 0), 3),
                MathF.Round((float)(packageInfo.VolumeInfo?.FormattedHeight ?? 0), 3),
                MathF.Round((float)(packageInfo.VolumeInfo?.FormattedVolume ?? 0), 3), packageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty);

            //释放imageData
        }

        public async Task<PackageInfoEntities> GetPackageDataAsync(int pageIndex, int pageSize, long? packageId = null, DateTime? startTime = null, DateTime? endTime = null,
            string? exitName = null, string? barcode = null, double? minWeight = null, double? maxWeight = null,
            int? uploadStatus = null, string? deviceName = null, string? nodeName = null, string? logisticsName = null,
            string? aggregatedPackageCode = null) {
            var total = await _packageRepository.Total(w =>
                w.BarCodeInfo != null && w.WeightInfo != null &&
                (packageId == null || w.Id.Equals(packageId)) &&
                (startTime == null || w.BarCodeInfo.ScanTime >= startTime) &&
                (endTime == null || w.BarCodeInfo.ScanTime <= endTime) &&
                (endTime == null || w.BarCodeInfo.ScanTime <= endTime) &&
                (string.IsNullOrWhiteSpace(barcode) ||
                 EF.Functions.Like(w.BarCodeInfo.Barcode, "%" + barcode + "%")) &&
                (exitName == null || (w.ExitInfo != null && w.ExitInfo.PhysicalExit.Equals(exitName))) &&
                (minWeight == null || w.WeightInfo.FormattedWeight >= minWeight) &&
                (maxWeight == null || w.WeightInfo.FormattedWeight <= maxWeight) &&
                (uploadStatus == null ||
                 (w.UploadInfo != null && w.UploadInfo.RequestStatus.Equals(uploadStatus))) &&
                (deviceName == null || (w.DeviceInfo != null && w.DeviceInfo.DeviceName.Contains(deviceName))) &&
                (nodeName == null || (w.DeviceInfo != null && w.DeviceInfo.NodeName.Contains(nodeName))) &&
                (logisticsName == null ||
                 (w.LogisticsInfo != null && w.LogisticsInfo.LogisticsName.Contains(logisticsName)))
            );
            if (total > 0) {
                var (key, value) = await _packageRepository.SelectPackageOrderByDescending(w =>
                        w.BarCodeInfo != null && w.WeightInfo != null &&
                        (packageId == null || w.Id.Equals(packageId)) &&
                        (startTime == null || w.BarCodeInfo.ScanTime >= startTime) &&
                        (endTime == null || w.BarCodeInfo.ScanTime <= endTime) &&
                        (endTime == null || w.BarCodeInfo.ScanTime <= endTime) &&
                        (string.IsNullOrWhiteSpace(barcode) ||
                         EF.Functions.Like(w.BarCodeInfo.Barcode, "%" + barcode + "%")) &&
                        (exitName == null || (w.ExitInfo != null && w.ExitInfo.PhysicalExit.Equals(exitName))) &&
                        (minWeight == null || w.WeightInfo.FormattedWeight >= minWeight) &&
                        (maxWeight == null || w.WeightInfo.FormattedWeight <= maxWeight) &&
                        (uploadStatus == null ||
                         (w.UploadInfo != null && w.UploadInfo.RequestStatus.Equals(uploadStatus))) &&
                        (deviceName == null || (w.DeviceInfo != null && w.DeviceInfo.DeviceName.Contains(deviceName))) &&
                        (nodeName == null || (w.DeviceInfo != null && w.DeviceInfo.NodeName.Contains(nodeName))) &&
                        (logisticsName == null ||
                         (w.LogisticsInfo != null && w.LogisticsInfo.LogisticsName.Contains(logisticsName)))
                    , o => o.PackageCreateTime, pageIndex, pageSize);
                if (key) {
                    return new PackageInfoEntities() {
                        TotalCount = total,
                        Description = "查询成功",
                        Infos = value
                    };
                }
                else {
                    return new PackageInfoEntities() {
                        TotalCount = total,
                        Description = "获取数据失败"
                    };
                }
            }
            else {
                return new PackageInfoEntities() {
                    TotalCount = total,
                    Description = "未查询到数据"
                };
            }
        }

        public async Task<string> GetConfigAsync(string configKey) {
            return await _configRepository.FirstOrDefaultJsonEntity(configKey);
        }

        public async Task<bool> AddOrUpdateConfigAsync(ConfigInfoModel config) {
            return await _configRepository.InsertOrUpdate(config);
        }

        public async Task<object?> GetCameraConfigAsync(string configName) {
            if (configName.Equals("BarcodeScannerCameraConfig", StringComparison.InvariantCultureIgnoreCase)) {
                return await _barcodeScannerCameraConfigRepository.Select(s => s.Id > 0,
                    o => o.Id);
            }
            else if (configName.Equals("PanoramaCameraConfig", StringComparison.InvariantCultureIgnoreCase)) {
                return await _panoramaCameraConfigRepository.Select(s => s.Id > 0,
                    o => o.Id);
            }
            else if (configName.Equals("UsbCameraConfig", StringComparison.InvariantCultureIgnoreCase)) {
                return await _usbCameraConfigRepository.Select(s => s.Id > 0,
                    o => o.Id);
            }
            else if (configName.Equals("VolumeCameraConfig", StringComparison.InvariantCultureIgnoreCase)) {
                return await _volumeCameraConfigRepository.Select(s => s.Id > 0,
                    o => o.Id);
            }

            return null;
        }

        public async Task<bool> AddOrUpdateCameraConfigAsync(string configName, object value) {
            if (configName.Equals("BarcodeScannerCameraConfig", StringComparison.InvariantCultureIgnoreCase) &&
                value is BarcodeScannerCameraConfigInfoModel info) {
                return await _barcodeScannerCameraConfigRepository.InsertOrUpdate(info);
            }
            else if (configName.Equals("PanoramaCameraConfig", StringComparison.InvariantCultureIgnoreCase) &&
                     value is PanoramaCameraConfigInfoModel panoramaInfo) {
                return await _panoramaCameraConfigRepository.InsertOrUpdate(panoramaInfo);
            }
            else if (configName.Equals("UsbCameraConfig", StringComparison.InvariantCultureIgnoreCase) &&
                     value is UsbCameraConfigInfoModel usbCameraInfo) {
                return await _usbCameraConfigRepository.InsertOrUpdate(usbCameraInfo);
            }
            else if (configName.Equals("VolumeCameraConfig", StringComparison.InvariantCultureIgnoreCase) &&
                    value is VolumeCameraConfigInfoModel volumeCameraInfo) {
                return await _volumeCameraConfigRepository.InsertOrUpdate(volumeCameraInfo);
            }

            return false;
        }

        public async Task<object?> GetNvrConfigAsync() {
            return await _nvrCameraBindingRepository.FirstOrDefault(f => f.Id > 0);
        }

        public async Task<bool> AddOrUpdateNvrConfigAsync(object value) {
            if (value is NvrCameraBindingInfoModel info) {
                return await _nvrCameraBindingRepository.InsertOrUpdate(info);
            }
            return false;
        }

        public async Task<object?> GetSortingConfigAsync(string configName) {
            if (configName.Equals("CommunicationConnectionConfig", StringComparison.CurrentCultureIgnoreCase)) {
                return await _communicationConnectionConfigRepository.CommunicationConnectionConfigItems(f => f.Id > 0);
            }
            else if (configName.Equals("ApiSortingConfig", StringComparison.CurrentCultureIgnoreCase)) {
                return await _apiSortingRepository.ApiSortingItems(w => w.Id > 0);
            }
            else if (configName.Equals("BarCodeSortingConfig", StringComparison.CurrentCultureIgnoreCase)) {
                return await _barCodeSortingRepository.BarCodeSortingItems(w => w.Id > 0);
            }
            else if (configName.Equals("LogisticsCodeRecognitionConfig", StringComparison.CurrentCultureIgnoreCase)) {
                return await _logisticsCodeRecognitionRepository.LogisticsCodes(w => w.Id > 0);
            }
            else if (configName.Equals("LogisticsSortingConfig", StringComparison.CurrentCultureIgnoreCase)) {
                return await _logisticsSortingRepository.LogisticsSortingItems(w => w.Id > 0);
            }
            else if (configName.Equals("OcrSortingConfig", StringComparison.CurrentCultureIgnoreCase)) {
                return await _ocrSortingRepository.OcrSortingItems(w => w.Id > 0);
            }
            else if (configName.Equals("PackageExitDefinitionConfig", StringComparison.CurrentCultureIgnoreCase)) {
                return await _packageExitDefinitionRepository.Select(w => w.Id > 0, o => o.Id);
            }
            else if (configName.Equals("PackageExitLockBindingConfig", StringComparison.CurrentCultureIgnoreCase)) {
                return await _packageExitLockBindingRepository.Select(w => w.Id > 0, o => o.Id);
            }
            else if (configName.Equals("SortingInstructionBindingConfig", StringComparison.CurrentCultureIgnoreCase)) {
                return await _sortingInstructionBindingRepository.InstructionBindings(w => w.Id > 0);
            }
            else if (configName.Equals("SortingInstructionConfig", StringComparison.CurrentCultureIgnoreCase)) {
                return await _sortingInstructionRepository.Select(w => w.Id > 0, o => o.Id);
            }
            else if (configName.Equals("VolumeSortingConfig", StringComparison.CurrentCultureIgnoreCase)) {
                return await _volumeSortingRepository.VolumeSortingItems(w => w.Id > 0);
            }
            else if (configName.Equals("WeightSortingConfig", StringComparison.CurrentCultureIgnoreCase)) {
                return await _weightSortingRepository.WeightSortingItems(w => w.Id > 0);
            }

            return null;
        }

        public async Task<bool> AddOrUpdateSortingConfigAsync(string configName, object value) {
            if (configName.Equals("CommunicationConnectionConfig", StringComparison.CurrentCultureIgnoreCase) &&
                value is List<CommunicationConnectionConfigInfoModel> communicationInfos) {
                return await _communicationConnectionConfigRepository.InsertRangeDetailAsync(communicationInfos);
            }
            else if (configName.Equals("CommunicationConnectionConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is CommunicationConnectionConfigInfoModel communicationInfo) {
                return await _communicationConnectionConfigRepository.InsertDetailAsync(communicationInfo);
            }
            else if (configName.Equals("ApiSortingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is List<ApiSortingInfoModel> apiSortingInfos) {
                return await _apiSortingRepository.InsertRangeDetailAsync(apiSortingInfos);
            }
            else if (configName.Equals("ApiSortingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is ApiSortingInfoModel apiSortingInfo) {
                return await _apiSortingRepository.InsertDetailAsync(apiSortingInfo);
            }
            else if (configName.Equals("BarCodeSortingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is List<BarCodeSortingInfoModel> barCodeSortingInfos) {
                return await _barCodeSortingRepository.InsertRangeDetailAsync(barCodeSortingInfos);
            }
            else if (configName.Equals("BarCodeSortingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is BarCodeSortingInfoModel barCodeSortingInfo) {
                return await _barCodeSortingRepository.InsertDetailAsync(barCodeSortingInfo);
            }
            else if (configName.Equals("LogisticsCodeRecognitionConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is List<LogisticsCodeRecognitionInfoModel> logisticsCodeRecognitionInfos) {
                return await _logisticsCodeRecognitionRepository.InsertRangeDetailAsync(logisticsCodeRecognitionInfos);
            }
            else if (configName.Equals("LogisticsCodeRecognitionConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is LogisticsCodeRecognitionInfoModel logisticsCodeRecognitionInfo) {
                return await _logisticsCodeRecognitionRepository.InsertDetailAsync(logisticsCodeRecognitionInfo);
            }
            else if (configName.Equals("LogisticsSortingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is List<LogisticsSortingInfoModel> logisticsSortingInfos) {
                return await _logisticsSortingRepository.InsertRangeDetailAsync(logisticsSortingInfos);
            }
            else if (configName.Equals("LogisticsSortingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is LogisticsSortingInfoModel logisticsSortingInfo) {
                return await _logisticsSortingRepository.InsertDetailAsync(logisticsSortingInfo);
            }
            else if (configName.Equals("OcrSortingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is List<OcrSortingInfoModel> ocrSortingInfos) {
                return await _ocrSortingRepository.InsertRangeDetailAsync(ocrSortingInfos);
            }
            else if (configName.Equals("OcrSortingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is OcrSortingInfoModel ocrSortingInfo) {
                return await _ocrSortingRepository.InsertDetailAsync(ocrSortingInfo);
            }
            else if (configName.Equals("PackageExitDefinitionConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is List<PackageExitDefinitionInfoModel> packageExitDefinitionInfos) {
                return await _packageExitDefinitionRepository.InsertOrUpdateRange(packageExitDefinitionInfos);
            }
            else if (configName.Equals("PackageExitDefinitionConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is PackageExitDefinitionInfoModel packageExitDefinitionInfo) {
                return await _packageExitDefinitionRepository.InsertOrUpdate(packageExitDefinitionInfo);
            }
            else if (configName.Equals("PackageExitLockBindingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is List<PackageExitLockBindingInfoModel> packageExitLockBindingInfos) {
                return await _packageExitLockBindingRepository.InsertOrUpdateRange(packageExitLockBindingInfos);
            }
            else if (configName.Equals("PackageExitLockBindingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is PackageExitLockBindingInfoModel packageExitLockBindingInfo) {
                return await _packageExitLockBindingRepository.InsertOrUpdate(packageExitLockBindingInfo);
            }
            else if (configName.Equals("SortingInstructionBindingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is List<SortingInstructionBindingInfoModel> sortingInstructionBindingInfos) {
                return await _sortingInstructionBindingRepository.InsertRangeDetailAsync(sortingInstructionBindingInfos);
            }
            else if (configName.Equals("SortingInstructionBindingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is SortingInstructionBindingInfoModel sortingInstructionBindingInfo) {
                return await _sortingInstructionBindingRepository.InsertDetailAsync(sortingInstructionBindingInfo);
            }
            else if (configName.Equals("SortingInstructionConfig", StringComparison.CurrentCultureIgnoreCase) &&
                    value is List<SortingInstructionInfoModel> sortingInstructionInfos) {
                return await _sortingInstructionRepository.InsertOrUpdateRange(sortingInstructionInfos);
            }
            else if (configName.Equals("SortingInstructionConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is SortingInstructionInfoModel sortingInstructionInfo) {
                return await _sortingInstructionRepository.InsertOrUpdate(sortingInstructionInfo);
            }
            else if (configName.Equals("VolumeSortingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is List<VolumeSortingInfoModel> volumeSortingInfos) {
                return await _volumeSortingRepository.InsertRangeDetailAsync(volumeSortingInfos);
            }
            else if (configName.Equals("VolumeSortingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is VolumeSortingInfoModel volumeSortingInfo) {
                return await _volumeSortingRepository.InsertDetailAsync(volumeSortingInfo);
            }
            else if (configName.Equals("WeightSortingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is List<WeightSortingInfoModel> weightSortingInfos) {
                return await _weightSortingRepository.InsertRangeDetailAsync(weightSortingInfos);
            }
            else if (configName.Equals("WeightSortingConfig", StringComparison.CurrentCultureIgnoreCase) &&
                     value is WeightSortingInfoModel weightSortingInfo) {
                return await _weightSortingRepository.InsertDetailAsync(weightSortingInfo);
            }
            return false;
        }

        public async Task<LogsInfoEntities> GetLogAsync(string configName, int pageIndex, int pageSize, DateTime? startTime = null, DateTime? endTime = null, string? keyword = null) {
            if (configName.Equals("ApiLog", StringComparison.CurrentCultureIgnoreCase)) {
                //Api日志
                Expression<Func<ApiLogInfoModel, bool>> predicate = w =>
                    (startTime == null || w.RequestTime >= startTime) &&
                    (endTime == null || w.RequestTime <= endTime) &&
                    (keyword == null || w.ResponseContent.Contains(keyword) ||
                     w.ExceptionMsg.Contains(keyword) ||
                     w.RequestContent.Contains(keyword));
                var total = await _apiLogRepository.Total(predicate);
                if (total > 0) {
                    var selectOrderByDescending = await _apiLogRepository.SelectOrderByDescending(predicate, o => o.RequestTime, pageIndex, pageSize);
                    return new LogsInfoEntities() {
                        TotalCount = total,
                        Description = "查询成功",
                        Infos = selectOrderByDescending
                    };
                }
                return new LogsInfoEntities() {
                    TotalCount = total,
                    Description = "未查询到相关数据",
                };
            }
            else if (configName.Equals("AppLog", StringComparison.CurrentCultureIgnoreCase)) {
                //App日志
                Expression<Func<AppLogInfoModel, bool>> predicate = w =>
                    (startTime == null || w.CreateTime >= startTime) &&
                    (endTime == null || w.CreateTime <= endTime) &&
                    (keyword == null || w.Message.Contains(keyword));

                var total = await _appLogRepository.Total(predicate);
                if (total > 0) {
                    var selectOrderByDescending = await _appLogRepository.SelectOrderByDescending(predicate, o => o.CreateTime, pageIndex, pageSize);
                    return new LogsInfoEntities() {
                        TotalCount = total,
                        Description = "查询成功",
                        Infos = selectOrderByDescending
                    };
                }
                return new LogsInfoEntities() {
                    TotalCount = total,
                    Description = "未查询到相关数据",
                };
            }
            else if (configName.Equals("CameraLog", StringComparison.CurrentCultureIgnoreCase)) {
                //相机日志
                Expression<Func<CameraLogInfoModel, bool>> predicate = w =>
                    (startTime == null || w.CreateTime >= startTime) &&
                    (endTime == null || w.CreateTime <= endTime) &&
                    (keyword == null || w.Message.Contains(keyword) ||
                     w.CameraSerialNumber.Contains(keyword));
                var total = await _cameraLogRepository.Total(predicate);
                if (total > 0) {
                    var selectOrderByDescending = await _cameraLogRepository.SelectOrderByDescending(predicate,
                        o => o.CreateTime, pageIndex, pageSize);
                    return new LogsInfoEntities() {
                        TotalCount = total,
                        Description = "查询成功",
                        Infos = selectOrderByDescending
                    };
                }
                return new LogsInfoEntities() {
                    TotalCount = total,
                    Description = "未查询到相关数据",
                };
            }
            else if (configName.Equals("ExceptionLog", StringComparison.CurrentCultureIgnoreCase)) {
                Expression<Func<ExceptionLogInfoModel, bool>> predicate = w =>
                    (startTime == null || w.CreateTime >= startTime) &&
                    (endTime == null || w.CreateTime <= endTime) &&
                    (keyword == null || w.Message.Contains(keyword) ||
                     w.Message.Contains(keyword));
                var total = await _exceptionLogRepository.Total(predicate);
                if (total > 0) {
                    var selectOrderByDescending = await _exceptionLogRepository.SelectOrderByDescending(predicate,
                        o => o.CreateTime, pageIndex, pageSize);
                    return new LogsInfoEntities() {
                        TotalCount = total,
                        Description = "查询成功",
                        Infos = selectOrderByDescending
                    };
                }
                return new LogsInfoEntities() {
                    TotalCount = total,
                    Description = "未查询到相关数据",
                };
            }
            else if (configName.Equals("FtpLog", StringComparison.CurrentCultureIgnoreCase)) {
                Expression<Func<FtpLogInfoModel, bool>> predicate = w =>
                    (startTime == null || w.CreateTime >= startTime) &&
                    (endTime == null || w.CreateTime <= endTime) &&
                    (keyword == null || w.Message.Contains(keyword) ||
                     w.Message.Contains(keyword));
                var total = await _ftpLogRepository.Total(predicate);
                if (total > 0) {
                    var selectOrderByDescending = await _ftpLogRepository.SelectOrderByDescending(predicate,
                        o => o.CreateTime, pageIndex, pageSize);
                    return new LogsInfoEntities() {
                        TotalCount = total,
                        Description = "查询成功",
                        Infos = selectOrderByDescending
                    };
                }
                return new LogsInfoEntities() {
                    TotalCount = total,
                    Description = "未查询到相关数据",
                };
            }
            else if (configName.Equals("InputLog", StringComparison.CurrentCultureIgnoreCase)) {
                Expression<Func<InputLogInfoModel, bool>> predicate = w =>
                    (startTime == null || w.CreateTime >= startTime) &&
                    (endTime == null || w.CreateTime <= endTime) &&
                    (keyword == null || w.Message.Contains(keyword) ||
                     w.Message.Contains(keyword));
                var total = await _inputLogRepository.Total(predicate);
                if (total > 0) {
                    var selectOrderByDescending = await _inputLogRepository.SelectOrderByDescending(predicate,
                        o => o.CreateTime, pageIndex, pageSize);
                    return new LogsInfoEntities() {
                        TotalCount = total,
                        Description = "查询成功",
                        Infos = selectOrderByDescending
                    };
                }
                return new LogsInfoEntities() {
                    TotalCount = total,
                    Description = "未查询到相关数据",
                };
            }
            else if (configName.Equals("OcrLog", StringComparison.CurrentCultureIgnoreCase)) {
                Expression<Func<OcrLogInfoModel, bool>> predicate = w =>
                    (startTime == null || w.CreateTime >= startTime) &&
                    (endTime == null || w.CreateTime <= endTime) &&
                    (keyword == null || w.Message.Contains(keyword) ||
                     w.Message.Contains(keyword));
                var total = await _ocrLogRepository.Total(predicate);
                if (total > 0) {
                    var selectOrderByDescending = await _ocrLogRepository.SelectOrderByDescending(predicate,
                        o => o.CreateTime, pageIndex, pageSize);
                    return new LogsInfoEntities() {
                        TotalCount = total,
                        Description = "查询成功",
                        Infos = selectOrderByDescending
                    };
                }
                return new LogsInfoEntities() {
                    TotalCount = total,
                    Description = "未查询到相关数据",
                };
            }
            else if (configName.Equals("OutputLog", StringComparison.CurrentCultureIgnoreCase)) {
                Expression<Func<OutputLogInfoModel, bool>> predicate = w =>
                    (startTime == null || w.CreateTime >= startTime) &&
                    (endTime == null || w.CreateTime <= endTime) &&
                    (keyword == null || w.Message.Contains(keyword) ||
                     w.Message.Contains(keyword));
                var total = await _outputLogRepository.Total(predicate);
                if (total > 0) {
                    var selectOrderByDescending = await _outputLogRepository.SelectOrderByDescending(predicate,
                        o => o.CreateTime, pageIndex, pageSize);
                    return new LogsInfoEntities() {
                        TotalCount = total,
                        Description = "查询成功",
                        Infos = selectOrderByDescending
                    };
                }
                return new LogsInfoEntities() {
                    TotalCount = total,
                    Description = "未查询到相关数据",
                };
            }
            else if (configName.Equals("SortingLog", StringComparison.CurrentCultureIgnoreCase)) {
                Expression<Func<SortingLogInfoModel, bool>> predicate = w =>
                    (startTime == null || w.CreateTime >= startTime) &&
                    (endTime == null || w.CreateTime <= endTime) &&
                    (keyword == null || w.Message.Contains(keyword) ||
                     w.Message.Contains(keyword));
                var total = await _sortingLogRepository.Total(predicate);
                if (total > 0) {
                    var selectOrderByDescending = await _sortingLogRepository.SelectOrderByDescending(predicate,
                        o => o.CreateTime, pageIndex, pageSize);
                    return new LogsInfoEntities() {
                        TotalCount = total,
                        Description = "查询成功",
                        Infos = selectOrderByDescending
                    };
                }
                return new LogsInfoEntities() {
                    TotalCount = total,
                    Description = "未查询到相关数据",
                };
            }
            else if (configName.Equals("VolumeLog", StringComparison.CurrentCultureIgnoreCase)) {
                Expression<Func<VolumeLogInfoModel, bool>> predicate = w =>
                    (startTime == null || w.CreateTime >= startTime) &&
                    (endTime == null || w.CreateTime <= endTime) &&
                    (keyword == null || w.Message.Contains(keyword) ||
                     w.Message.Contains(keyword));
                var total = await _volumeLogRepository.Total(predicate);
                if (total > 0) {
                    var selectOrderByDescending = await _volumeLogRepository.SelectOrderByDescending(predicate,
                        o => o.CreateTime, pageIndex, pageSize);
                    return new LogsInfoEntities() {
                        TotalCount = total,
                        Description = "查询成功",
                        Infos = selectOrderByDescending
                    };
                }
                return new LogsInfoEntities() {
                    TotalCount = total,
                    Description = "未查询到相关数据",
                };
            }
            else if (configName.Equals("WeighingLog", StringComparison.CurrentCultureIgnoreCase)) {
                Expression<Func<WeighingLogInfoModel, bool>> predicate = w =>
                    (startTime == null || w.CreateTime >= startTime) &&
                    (endTime == null || w.CreateTime <= endTime) &&
                    (keyword == null || w.Message.Contains(keyword) ||
                     w.Message.Contains(keyword));
                var total = await _weighingLogRepository.Total(predicate);
                if (total > 0) {
                    var selectOrderByDescending = await _weighingLogRepository.SelectOrderByDescending(predicate,
                        o => o.CreateTime, pageIndex, pageSize);
                    return new LogsInfoEntities() {
                        TotalCount = total,
                        Description = "查询成功",
                        Infos = selectOrderByDescending
                    };
                }
                return new LogsInfoEntities() {
                    TotalCount = total,
                    Description = "未查询到相关数据",
                };
            }

            return new LogsInfoEntities() {
                Description = "日志类别不存在"
            };
        }

        public void AddLogAsync(string configName, object value) {
            if (configName.Equals("ApiLog", StringComparison.CurrentCultureIgnoreCase) &&
                value is ApiLogInfoModel apiLogInfo) {
                _apiLogRepository.InsertAsync(apiLogInfo);
            }
            else if (configName.Equals("AppLog", StringComparison.CurrentCultureIgnoreCase) &&
                     value is AppLogInfoModel appLogInfo) {
                _appLogRepository.InsertAsync(appLogInfo);
            }
            else if (configName.Equals("CameraLog", StringComparison.CurrentCultureIgnoreCase) &&
                     value is CameraLogInfoModel cameraLogInfo) {
                _cameraLogRepository.InsertAsync(cameraLogInfo);
            }
            else if (configName.Equals("ExceptionLog", StringComparison.CurrentCultureIgnoreCase) &&
                     value is ExceptionLogInfoModel exceptionLogInfo) {
                _exceptionLogRepository.InsertAsync(exceptionLogInfo);
            }
            else if (configName.Equals("FtpLog", StringComparison.CurrentCultureIgnoreCase) &&
                     value is FtpLogInfoModel ftpLogInfo) {
                _ftpLogRepository.InsertAsync(ftpLogInfo);
            }
            else if (configName.Equals("InputLog", StringComparison.CurrentCultureIgnoreCase) &&
                     value is InputLogInfoModel inputLogInfo) {
                _inputLogRepository.InsertAsync(inputLogInfo);
            }
            else if (configName.Equals("OcrLog", StringComparison.CurrentCultureIgnoreCase) &&
                     value is OcrLogInfoModel ocrLogInfo) {
                _ocrLogRepository.InsertAsync(ocrLogInfo);
            }
            else if (configName.Equals("OutputLog", StringComparison.CurrentCultureIgnoreCase) &&
                     value is OutputLogInfoModel outputLogInfo) {
                _outputLogRepository.InsertAsync(outputLogInfo);
            }
            else if (configName.Equals("SortingLog", StringComparison.CurrentCultureIgnoreCase) &&
                     value is SortingLogInfoModel sortingInfo) {
                _sortingLogRepository.InsertAsync(sortingInfo);
            }
            else if (configName.Equals("VolumeLog", StringComparison.CurrentCultureIgnoreCase) &&
                     value is VolumeLogInfoModel volumeLogInfo) {
                _volumeLogRepository.InsertAsync(volumeLogInfo);
            }
            else if (configName.Equals("WeighingLog", StringComparison.CurrentCultureIgnoreCase) &&
                     value is WeighingLogInfoModel weighingLogInfo) {
                _weighingLogRepository.InsertAsync(weighingLogInfo);
            }
        }

        public async Task<bool> ClearLogsAsync(string configName) {
            if (configName.Equals("ApiLog", StringComparison.CurrentCultureIgnoreCase)) {
                var total = await _apiLogRepository.Total(t => t.Id > 0);
                await _apiLogRepository.DeleteCount(total);
            }
            else if (configName.Equals("AppLog", StringComparison.CurrentCultureIgnoreCase)) {
                var total = await _appLogRepository.Total(t => t.Id > 0);
                await _appLogRepository.DeleteCount(total);
            }
            else if (configName.Equals("CameraLog", StringComparison.CurrentCultureIgnoreCase)) {
                var total = await _cameraLogRepository.Total(t => t.Id > 0);
                await _cameraLogRepository.DeleteCount(total);
            }
            else if (configName.Equals("ExceptionLog", StringComparison.CurrentCultureIgnoreCase)) {
                var total = await _exceptionLogRepository.Total(t => t.Id > 0);
                await _exceptionLogRepository.DeleteCount(total);
            }
            else if (configName.Equals("FtpLog", StringComparison.CurrentCultureIgnoreCase)) {
                var total = await _ftpLogRepository.Total(t => t.Id > 0);
                await _ftpLogRepository.DeleteCount(total);
            }
            else if (configName.Equals("InputLog", StringComparison.CurrentCultureIgnoreCase)) {
                var total = await _inputLogRepository.Total(t => t.Id > 0);
                await _inputLogRepository.DeleteCount(total);
            }
            else if (configName.Equals("OcrLog", StringComparison.CurrentCultureIgnoreCase)) {
                var total = await _ocrLogRepository.Total(t => t.Id > 0);
                await _ocrLogRepository.DeleteCount(total);
            }
            else if (configName.Equals("OutputLog", StringComparison.CurrentCultureIgnoreCase)) {
                var total = await _outputLogRepository.Total(t => t.Id > 0);
                await _outputLogRepository.DeleteCount(total);
            }
            else if (configName.Equals("SortingLog", StringComparison.CurrentCultureIgnoreCase)) {
                var total = await _sortingLogRepository.Total(t => t.Id > 0);
                await _sortingLogRepository.DeleteCount(total);
            }
            else if (configName.Equals("VolumeLog", StringComparison.CurrentCultureIgnoreCase)) {
                var total = await _volumeLogRepository.Total(t => t.Id > 0);
                await _volumeLogRepository.DeleteCount(total);
            }
            else if (configName.Equals("WeighingLog", StringComparison.CurrentCultureIgnoreCase)) {
                var total = await _weighingLogRepository.Total(t => t.Id > 0);
                await _weighingLogRepository.DeleteCount(total);
            }
            return true;
        }
    }
}