using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.DataInteractionService.SignalR;

namespace JayTom.Dws.DataInteractionService.Services {

    public class DataInteractionService : BackgroundService {
        private readonly IDataInteractionServiceMessageHub _dataInteractionServiceMessageHub;

        public DataInteractionService(IDataInteractionServiceMessageHub dataInteractionServiceMessageHub) {
            _dataInteractionServiceMessageHub = dataInteractionServiceMessageHub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //重新提交失败的保存
            while (!stoppingToken.IsCancellationRequested) {
                //包裹数据
                var tryDequeue = _dataInteractionServiceMessageHub.FallInsertPackageInfoModels.TryDequeue(out var insertPackageInfo);
                if (tryDequeue && insertPackageInfo is not null) {
                    _dataInteractionServiceMessageHub.AddOrUpdatePackageDataAsync(insertPackageInfo);
                }
                //更新的包裹数据
                tryDequeue = _dataInteractionServiceMessageHub.FallUpdatePackageInfoModels.TryDequeue(out var updatePackageInfo);
                if (tryDequeue && updatePackageInfo is not null) {
                    _dataInteractionServiceMessageHub.UpdatePackageDataAsync(updatePackageInfo);
                }
                //条码数据
                tryDequeue = _dataInteractionServiceMessageHub.FallUpdateBarcodeInfoModels.TryDequeue(out var updateBarcodeInfo);
                if (tryDequeue && updateBarcodeInfo is not null) {
                    _dataInteractionServiceMessageHub.UpdateBarcodeDataAsync(updateBarcodeInfo.PackageId, updateBarcodeInfo);
                }
                //重量数据
                tryDequeue = _dataInteractionServiceMessageHub.FallUpdateWeightInfoModels.TryDequeue(out var updateWeightInfo);
                if (tryDequeue && updateWeightInfo is not null) {
                    _dataInteractionServiceMessageHub.UpdateWeightDataAsync(updateWeightInfo.PackageId, updateWeightInfo);
                }
                //体积数据
                tryDequeue = _dataInteractionServiceMessageHub.FallUpdateVolumeInfoModels.TryDequeue(out var updateVolumeInfo);
                if (tryDequeue && updateVolumeInfo is not null) {
                    _dataInteractionServiceMessageHub.UpdateVolumeDataAsync(updateVolumeInfo.PackageId, updateVolumeInfo);
                }
                //上传数据
                tryDequeue = _dataInteractionServiceMessageHub.FallUpdateUploadInfoModels.TryDequeue(out var uploadInfo);
                if (tryDequeue && uploadInfo is not null) {
                    _dataInteractionServiceMessageHub.UpdateUploadDataAsync(uploadInfo.PackageId, uploadInfo);
                }
                //格口数据
                tryDequeue = _dataInteractionServiceMessageHub.FallUpdateCompartmentInfoModels.TryDequeue(out var exitInfo);
                if (tryDequeue && exitInfo is not null) {
                    _dataInteractionServiceMessageHub.UpdateExitDataAsync(exitInfo.PackageId, exitInfo);
                }
                //分拣数据
                tryDequeue = _dataInteractionServiceMessageHub.FallUpdateSortingInfoModels.TryDequeue(out var sortingInfo);
                if (tryDequeue && sortingInfo is not null) {
                    _dataInteractionServiceMessageHub.UpdateSortingDataAsync(sortingInfo.PackageId, sortingInfo);
                }
                //Ocr数据
                tryDequeue = _dataInteractionServiceMessageHub.FallUpdateOcrInfoModels.TryDequeue(out var ocrInfo);
                if (tryDequeue && ocrInfo is not null) {
                    _dataInteractionServiceMessageHub.UpdateOcrDataAsync(ocrInfo.PackageId, ocrInfo);
                }

                tryDequeue = _dataInteractionServiceMessageHub.FallUpdateImageInfoModels.TryDequeue(out var imageInfo);
                if (tryDequeue && imageInfo is not null) {
                    _dataInteractionServiceMessageHub.UpdateImageDataAsync(imageInfo.PackageId, imageInfo);
                }

                tryDequeue = _dataInteractionServiceMessageHub.FallUpdateVideoCloudInfoModels.TryDequeue(out var videoCloudInfo);
                if (tryDequeue && videoCloudInfo is not null) {
                    _dataInteractionServiceMessageHub.UpdateVideoCloudDataAsync(videoCloudInfo.PackageId, videoCloudInfo);
                }

                tryDequeue = _dataInteractionServiceMessageHub.FallUpdateInstructionInfoModels.TryDequeue(out var instructionInfo);
                if (tryDequeue && instructionInfo is not null) {
                    _dataInteractionServiceMessageHub.AddInstructionDataAsync(instructionInfo.SortingInfoId, instructionInfo);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
            }
        }
    }
}