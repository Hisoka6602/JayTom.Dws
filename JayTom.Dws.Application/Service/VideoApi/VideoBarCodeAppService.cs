using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Application.Dto;
using JayTom.Dws.Data.VideoApiData;
using JayTom.Dws.Domain.Dto.CloudDto;
using JayTom.Dws.Domain.Dto.VideoApi;
using JayTom.Dws.Domain.Service.VideoApi;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JayTom.Dws.Application.Service.VideoApi {

    public class VideoBarCodeAppService : IVideoBarCodeAppService {
        private readonly IVideoBarCodeService _videoBarCodeService;

        public VideoBarCodeAppService(IVideoBarCodeService videoBarCodeService) {
            _videoBarCodeService = videoBarCodeService;
        }

        public Task<KeyValuePair<bool, string>> AddOrUpdateBarcodeInfo(BarcodeImageDto barcodeImageInfo, List<BarcodeImageDto> panoramaImageInfos, ScanNodeDto scanNodeInfo,
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
                    if (b && o is List<VideoBarCodeInfoModel> models) {
                        return new KeyValuePair<bool, object>(true, new BarcodesDto {
                            BarCodes = models.Select(s => new BarcodesInfoDto {
                                Barcode = s.Barcode,
                                Id = s.Id,
                                TimestampedGuid = s.TimestampedGuid,
                                ScanNodeInfos = s.VideoScanNodeInfos?
                                    .Select(s1 => new ScanNodeInfoDto {
                                        Description = s1.Description,
                                        Name = s1.Name,
                                        ScanTime = s1.ScanTime,
                                        NvrCameraBindingInfo = new NvrCameraBindingDto() {
                                            BarcodeScannerSerialNumber = s1.VideoNvrCameraBindingInfo?.BarcodeScannerSerialNumber ?? string.Empty,
                                            Channel = s1.VideoNvrCameraBindingInfo?.Channel ?? 0,
                                            IpAddress = s1.VideoNvrCameraBindingInfo?.IpAddress ?? string.Empty,
                                            Password = s1.VideoNvrCameraBindingInfo?.Password ?? string.Empty,
                                            Username = s1.VideoNvrCameraBindingInfo?.Username ?? string.Empty,
                                            Port = s1.VideoNvrCameraBindingInfo?.Port ?? 0,
                                        },
                                        BarcodeImageInfos = s1.VideoNodeImageInfos?
                                            .Select(s2 => new BarcodeImageInfoDto {
                                                CameraName = s2.CameraName,
                                                CameraSerialNumber = s2.CameraSerialNumber,
                                                ImageType = s2.ImageType,
                                                Name = s2.Name,
                                                Path = s2.Path,
                                            })?.ToList() ?? new List<BarcodeImageInfoDto>()
                                    })?.ToList() ?? new List<ScanNodeInfoDto>()
                            })?.ToList() ?? new List<BarcodesInfoDto>(),
                            Total = total
                        });
                    }
                    return new KeyValuePair<bool, object>(false, o);
                }
                else {
                    return new KeyValuePair<bool, object>(true, new BarcodesDto {
                        BarCodes = new List<BarcodesInfoDto>(),
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