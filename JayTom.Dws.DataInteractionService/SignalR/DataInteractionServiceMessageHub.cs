using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using JayTom.Dws.CrossCutting.SignalR;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Entities.PackageEntities;
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
            ICloudVideoUploadRepository cloudVideoUploadRepository) : base(hubContext, logger) {
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
            var insertOrUpdate = await _imageRepository.InsertOrUpdate(imageData);

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

        public Task<KeyValuePair<PackageInfoModel, string>> SaveImageDataAsync(PackageInfoModel packageInfo, string rootPath, byte[] imageData) {
            throw new NotImplementedException();
        }

        public Task<PackageInfoEntities> GetPackageDataAsync(long? packageId = null, DateTime? startTime = null, DateTime? endTime = null,
            string? compartment = null, string? barcode = null, double? minWeight = null, double? maxWeight = null,
            bool? uploadStatus = null, string? deviceName = null, string? nodeName = null, string? logisticsName = null,
            string? aggregatedPackageCode = null) {
            throw new NotImplementedException();
        }

        public Task<bool> DeletePackageDataAsync(string packageId) {
            throw new NotImplementedException();
        }

        public Task<bool> DeletePackagesOlderThanAsync(int days) {
            throw new NotImplementedException();
        }

        public Task<object> GetConfigAsync(string configKey) {
            throw new NotImplementedException();
        }

        public Task<bool> AddOrUpdateConfigAsync(string configKey, object configValue) {
            throw new NotImplementedException();
        }

        public Task<object> GetLogAsync(string logId) {
            throw new NotImplementedException();
        }

        public void AddLogAsync(object log) {
            throw new NotImplementedException();
        }

        public Task<bool> ClearLogsAsync() {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteLogsOlderThanAsync(int days) {
            throw new NotImplementedException();
        }
    }
}