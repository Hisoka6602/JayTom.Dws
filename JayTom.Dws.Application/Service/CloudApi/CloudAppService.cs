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
                barcodeImageInfo.Image?.Save(barcodeImagePath, ImageFormat.Jpeg);
                barcodeImageInfo.Image?.Dispose();
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
                    panoramaImageInfo.Image?.Save(panoramaImagePath, ImageFormat.Jpeg);
                    panoramaImageInfo.Image?.Dispose();
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

        public async Task<KeyValuePair<bool, object>> CleanupDataDaysAgo(int days, string rootImagePath, CancellationToken token = default) {
            var dateTime = DateTime.Now.AddDays(0 - days);
            var files = new List<string>();
            var barcodeImageRootPath = $"{rootImagePath}\\barcodeImages";

            if (Directory.Exists(barcodeImageRootPath)) {
                var filesBeforeDate = GetFilesBeforeDate(barcodeImageRootPath, dateTime);
                files.AddRange(filesBeforeDate);
            }

            var panoramaRootImage = $"{rootImagePath}\\panoramaImages";

            if (Directory.Exists(panoramaRootImage)) {
                var filesBeforeDate = GetFilesBeforeDate(panoramaRootImage, dateTime);
                files.AddRange(filesBeforeDate);
            }

            if (files.Any()) {
                Parallel.ForEach(files, File.Delete);
            }
            //删除数据

            return await _cloudService.CleanupDataDaysAgo(days, token);
        }

        public async Task CleanEarliestImageFiles(string folderPath, long minFreeSpaceInMb) {
            var driveInfo = new DriveInfo(Path.GetPathRoot(folderPath) ?? string.Empty);
            if (driveInfo is not null) {
                var convertBytesToMb = ConvertBytesToMb(driveInfo.TotalFreeSpace);
                if (convertBytesToMb < (double)minFreeSpaceInMb) {
                    var directoryInfo = new DirectoryInfo(folderPath);
                    var imageFiles = directoryInfo.GetFiles()
                        .OrderBy(f => f.LastWriteTime)
                        .ToArray();

                    long totalSpaceToFreeUp = 0;
                    foreach (var file in imageFiles) {
                        totalSpaceToFreeUp += file.Length;
                        if (totalSpaceToFreeUp >= minFreeSpaceInMb * 1024 * 1024 * 2) {
                            // 达到最低保障空间大小后停止删除文件
                            break;
                        }
                        file.Delete();
                    }

                    foreach (var directory in directoryInfo.GetDirectories()) {
                        await CleanEarliestImageFiles(directory.FullName, minFreeSpaceInMb);
                    }
                }
            }
        }

        public Task<KeyValuePair<bool, object>> AddExceptionType(string exceptionName, string exceptionColor, CancellationToken token = default) {
            return _cloudService.AddExceptionType(exceptionName, exceptionColor, token);
        }

        public Task<KeyValuePair<bool, object>> UpdateExceptionType(long exceptionCategoryId, string exceptionName, string exceptionColor,
            CancellationToken token = default) {
            return _cloudService.UpdateExceptionType(exceptionCategoryId, exceptionName, exceptionColor, token);
        }

        public Task<KeyValuePair<bool, object>> DeleteExceptionType(long exceptionCategoryId, CancellationToken token = default) {
            return _cloudService.DeleteExceptionType(exceptionCategoryId, token);
        }

        public Task<KeyValuePair<bool, object>> AddExceptionRule(string keywords, string customRegex, int dataSource, string exceptionTypeName,
            long exceptionTypeId, int priority, CancellationToken token = default) {
            return _cloudService.AddExceptionRule(keywords, customRegex, dataSource, exceptionTypeName, exceptionTypeId,
                 priority, token);
        }

        public Task<KeyValuePair<bool, object>> UpdateExceptionRule(long exceptionRuleId, string keywords, string customRegex, int dataSource,
            string exceptionTypeName, long exceptionTypeId, int priority, CancellationToken token = default) {
            return _cloudService.UpdateExceptionRule(exceptionRuleId, keywords, customRegex, dataSource,
                exceptionTypeName, exceptionTypeId, priority, token);
        }

        public Task<KeyValuePair<bool, object>> DeleteExceptionRule(long exceptionRuleId, CancellationToken token = default) {
            return _cloudService.DeleteExceptionRule(exceptionRuleId, token);
        }

        public Task<KeyValuePair<bool, object>> ExceptionTypes(CancellationToken token = default) {
            return _cloudService.ExceptionTypes(token);
        }

        public Task<KeyValuePair<bool, object>> ExceptionRule(CancellationToken token = default) {
            return _cloudService.ExceptionRule(token);
        }

        public Task<KeyValuePair<bool, object>> GetCloudConfig(string settingsName, CancellationToken token = default) {
            return _cloudService.GetCloudConfig(settingsName, token);
        }

        public double ConvertBytesToMb(long bytes) {
            return (bytes / 1024f) / 1024f;
        }

        public List<string> GetFilesBeforeDate(string path, DateTime targetDate) {
            var files = new List<string>();

            files.AddRange(Directory.GetFiles(path)
                .Where(f => File.GetCreationTime(f) < targetDate));

            var subDirectories = Directory.GetDirectories(path);

            foreach (var subDirectory in subDirectories) {
                files.AddRange(GetFilesBeforeDate(subDirectory, targetDate));
            }

            return files;
        }
    }
}