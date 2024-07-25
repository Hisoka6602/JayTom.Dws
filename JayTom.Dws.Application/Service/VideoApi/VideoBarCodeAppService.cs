using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using JayTom.Dws.Application.Dto;
using JayTom.Dws.Data.VideoApiData;
using JayTom.Dws.Domain.Dto.CloudDto;
using JayTom.Dws.Domain.Dto.VideoApi;
using JayTom.Dws.Domain.Dto.CloudApiDto;
using JayTom.Dws.Domain.Service.VideoApi;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JayTom.Dws.Application.Service.VideoApi {

    public class VideoBarCodeAppService : IVideoBarCodeAppService {
        private readonly IVideoBarCodeService _videoBarCodeService;

        public VideoBarCodeAppService(IVideoBarCodeService videoBarCodeService) {
            _videoBarCodeService = videoBarCodeService;
        }

        public Task<KeyValuePair<bool, object>> AddOrUpdateBarcodeInfo(BarcodeImageDto barcodeImageInfo, List<BarcodeImageDto> panoramaImageInfos, PackageDto scanNodeInfo,
            string rootImagePath) {
            return _videoBarCodeService.AddOrUpdateBarcodeInfo(barcodeImageInfo,
                 panoramaImageInfos, scanNodeInfo, rootImagePath);
        }

        public Task<KeyValuePair<bool, object>> GroupedNodeNames(CancellationToken token = default) {
            return _videoBarCodeService.GroupedNodeNames(token);
        }

        public async Task<KeyValuePair<bool, object>> GetBarcodeInfos(string barCode, DateTime? nodeStartDateTime, DateTime? nodeEndDateTime, string? nodeName,
            string? cameraSerialNumber, string? cameraName, int pageIndex = 0, int pageSize = 1000,
            CancellationToken token = default) {
            var (key, value) = await _videoBarCodeService.GetBarcodeTotal(barCode,
                nodeStartDateTime,
                nodeEndDateTime,
                nodeName,
                cameraSerialNumber,
                cameraName,
                token);
            if (key && value is int total) {
                if (total > 0) {
                    var (b, o) = await _videoBarCodeService.GetBarcodeInfos(barCode,
                        nodeStartDateTime,
                        nodeEndDateTime,
                        nodeName,
                        cameraSerialNumber,
                        cameraName,
                        pageIndex, pageSize, token);
                    if (b && o is List<PackageInfoModel> models) {
                        return new KeyValuePair<bool, object>(true, new VideoPackageDto {
                            Packages = models,
                            Total = total
                        });
                    }
                    return new KeyValuePair<bool, object>(false, o);
                }
                else {
                    return new KeyValuePair<bool, object>(true, new VideoPackageDto {
                        Packages = new List<PackageInfoModel>(),
                        Total = total
                    });
                }
            }
            return new KeyValuePair<bool, object>(false, value);
        }

        public Task<KeyValuePair<bool, object>> GetBarcodeTotal(string barCode, DateTime? nodeStartDateTime, DateTime? nodeEndDateTime, string? nodeName,
            string? cameraSerialNumber, string? cameraName, CancellationToken token = default) {
            return _videoBarCodeService.GetBarcodeTotal(barCode,
                nodeStartDateTime, nodeEndDateTime, nodeName,
                cameraSerialNumber, cameraName, token);
        }

        public Task<KeyValuePair<bool, object>> BarcodeTotalForDate(DateTime date) {
            return _videoBarCodeService.BarcodeTotalForDate(date);
        }

        public Task<KeyValuePair<bool, object>> BarcodeTotalForDateBetween(DateTime startDate, DateTime endDate) {
            return _videoBarCodeService.BarcodeTotalForDateBetween(startDate, endDate);
        }

        public Task<KeyValuePair<bool, object>> CleanupDataDaysAgo(int days, CancellationToken token = default) {
            return _videoBarCodeService.CleanupDataDaysAgo(days, token);
        }
    }
}