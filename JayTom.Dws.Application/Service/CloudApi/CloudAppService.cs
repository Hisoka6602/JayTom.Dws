using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.CloudApiDto;
using JayTom.Dws.Domain.Service.CloudApi;

namespace JayTom.Dws.Application.Service.CloudApi {

    public class CloudAppService : ICloudAppService {
        private readonly ICloudService _cloudService;

        public CloudAppService(ICloudService cloudService) {
            _cloudService = cloudService;
        }

        public Task<KeyValuePair<bool, object>> SavePackageInfo(PackageDto packageInfo, string rootImagePath, string webImagePath,
            CancellationToken cancellationToken = default) {
            if (string.IsNullOrEmpty(rootImagePath)) {
                rootImagePath = $"{System.AppDomain.CurrentDomain.BaseDirectory}Images";
            }

            var barcodeImageInfo = packageInfo.ImageInfos?.FirstOrDefault(f => f.Type == 0);

            //扫码图
            if (barcodeImageInfo?.Image is not null) {
                var barcodeImageRootPath = $"{rootImagePath}\\barcodeImages\\{DateTime.Now:yyyy}\\{DateTime.Now:MM}\\{DateTime.Now:dd}\\{DateTime.Now:HH}";

                if (!Directory.Exists(barcodeImageRootPath)) {
                    Directory.CreateDirectory(barcodeImageRootPath);
                }
                var barcodeImagePath = $"{barcodeImageRootPath}\\{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.jpg";
                barcodeImageInfo.Image.Save(barcodeImagePath, ImageFormat.Jpeg);
                barcodeImageInfo.Image.Dispose();
                barcodeImageInfo.LocalPath = barcodeImagePath;
                barcodeImageInfo.ImageUrl = barcodeImagePath.Replace(rootImagePath, webImagePath).Replace("\\", "/");
                barcodeImageInfo.Image = null;
            }
            //全景图
            var panoramaImageInfos = packageInfo.ImageInfos?.Where(w => w is { Type: 1, Image: not null })?.ToList();
            if (panoramaImageInfos?.Any() == true) {
                var panoramaRootImage = $"{rootImagePath}\\panoramaImages\\{DateTime.Now:yyyy}\\{DateTime.Now:MM}\\{DateTime.Now:dd}\\{DateTime.Now:HH}";

                if (!Directory.Exists(panoramaRootImage)) {
                    Directory.CreateDirectory(panoramaRootImage);
                }
                var num = 0;
                foreach (var panoramaImageInfo in panoramaImageInfos) {
                    var panoramaImagePath = $"{panoramaRootImage}\\{DateTimeOffset.Now.ToUnixTimeMilliseconds()}-{num}.jpg";
                    panoramaImageInfo.Image.Save(panoramaImagePath, ImageFormat.Jpeg);
                    panoramaImageInfo.Image.Dispose();
                    panoramaImageInfo.LocalPath = panoramaImagePath;
                    panoramaImageInfo.ImageUrl = panoramaImagePath.Replace(rootImagePath, webImagePath).Replace("\\", "/");
                    panoramaImageInfo.Image = null;
                }
            }
            return _cloudService.SavePackageInfo(packageInfo, cancellationToken);
        }

        public Task<KeyValuePair<bool, object>> GetPackages(string? barcode, DateTime? startScanTime, DateTime? endScanTime, string? cameraSerialNumber,
            double? minWeight, double? maxWeight, int? requestStatus, string? physicalExit, string? sentInstruction,
            string? logisticsName, string? threeSegmentCode, string? nodeName, string? deviceName, int pageIndex, int pageSize,
            CancellationToken cancellationToken) {
            return _cloudService.GetPackages(barcode, startScanTime, endScanTime, cameraSerialNumber, minWeight, maxWeight,
                 requestStatus, physicalExit, sentInstruction, logisticsName, threeSegmentCode, nodeName, deviceName,
                 pageIndex, pageSize, cancellationToken);
        }

        public Task<KeyValuePair<bool, object>> GetStatistics(DateTime? startDateTime, DateTime? endDateTime, string? deviceName,
            CancellationToken cancellationToken) {
            return _cloudService.GetStatistics(startDateTime, endDateTime, deviceName, cancellationToken);
        }
    }
}