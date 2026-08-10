using System;
using System.Linq;
using JayTom.Dws.Application.Configuration;
using System.Text;
using Newtonsoft.Json;
using TouchSocket.Core;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Ftp;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Repository.LocalLog;
using JayTom.Dws.Domain.Service.CacheCleanup;
using JayTom.Dws.Domain.Repository.LocalData;

namespace JayTom.Dws.Infrastructure.Service {

    public class CacheCleanupService : ICacheCleanupService {
        private readonly IBarCodeRepository _barCodeRepository;
        private readonly ISettingsReader _settingsReader;
        private readonly IFtp _ftp;
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
        private static readonly SemaphoreSlim _deleteBarcodeSlim = new(1, 1);
        private static readonly SemaphoreSlim _deleteScanImagesSlim = new(1, 1);
        private static readonly SemaphoreSlim _deletePanoramaImagesSlim = new(1, 1);
        private static readonly SemaphoreSlim _deleteFtpImagesSlim = new(1, 1);

        //DeleteFtpImages
        public CacheCleanupService(IBarCodeRepository barCodeRepository,
            ISettingsReader settingsReader, IFtp ftp,
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
            IWeighingLogRepository weighingLogRepository) {
            _barCodeRepository = barCodeRepository;
            _settingsReader = settingsReader;
            _ftp = ftp;
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
        }

        public async Task<KeyValuePair<bool, string>> DeleteBarcodeDataOlderThanDays(int days) {
            //从数据库删除
            try {
                await _deleteBarcodeSlim.WaitAsync();
                const int count = 5000;
                var deleteCount = 0;
                do {
                    deleteCount = await _barCodeRepository.DeleteCount(count,
                        w => w.ScanTime.CompareTo(DateTime.Today.AddDays(0 - days)) < 0);
                } while (deleteCount == count);
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _deleteBarcodeSlim.Release();
            }
            return new KeyValuePair<bool, string>(true, string.Empty);
        }

        public async Task<KeyValuePair<bool, string>> DeleteScanImagesOlderThanDays(int days) {
            //查找根文件目录，遍历所有文件路径，找出包含扫码路径的文件，再按时间正序删除
            try {
                await _deleteScanImagesSlim.WaitAsync();
                var configInfoModel = await _settingsReader.GetRawAsync("SaveImageSettings");
                if (configInfoModel is not null) {
                    var imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel);
                    if (imageSettingsDto is not null) {
                        if (Directory.Exists(imageSettingsDto.ImageRootDirectory)) {
                            return DeleteImagesOlderThan(
                                imageSettingsDto.ImageRootDirectory,
                                "BarcodeImage",
                                DateTime.Now.AddDays(0 - days));
                        }
                        else {
                            return new KeyValuePair<bool, string>(false, "存图目录不存在");
                        }
                    }
                    else {
                        return new KeyValuePair<bool, string>(false, "存图配置为空");
                    }
                }
                else {
                    return new KeyValuePair<bool, string>(false, "找不到存图配置");
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _deleteScanImagesSlim.Release();
            }
        }

        public async Task<KeyValuePair<bool, string>> DeletePanoramaImagesOlderThanDays(int days) {
            //查找根文件目录，遍历所有文件路径，找出包含全景路径的文件，再按时间正序删除
            try {
                await _deletePanoramaImagesSlim.WaitAsync();
                var configInfoModel = await _settingsReader.GetRawAsync("SaveImageSettings");
                if (configInfoModel is not null) {
                    var imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel);
                    if (imageSettingsDto is not null) {
                        if (Directory.Exists(imageSettingsDto.ImageRootDirectory)) {
                            return DeleteImagesOlderThan(
                                imageSettingsDto.ImageRootDirectory,
                                "PanoramaImage",
                                DateTime.Now.AddDays(0 - days));
                        }
                        else {
                            return new KeyValuePair<bool, string>(false, "存图目录不存在");
                        }
                    }
                    else {
                        return new KeyValuePair<bool, string>(false, "存图配置为空");
                    }
                }
                else {
                    return new KeyValuePair<bool, string>(false, "找不到存图配置");
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _deletePanoramaImagesSlim.Release();
            }
        }

        public async Task<KeyValuePair<bool, string>> DeleteFtpImagesOlderThanDays(int days) {
            //ftp遍历所有文件，再按时间正序删除
            try {
                await _deleteFtpImagesSlim.WaitAsync();
                var configInfoModel = await _settingsReader.GetRawAsync("SaveImageSettings");
                if (configInfoModel is not null) {
                    var imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel);
                    if (imageSettingsDto is not null) {
                        if (!string.IsNullOrEmpty(imageSettingsDto.FtpInfo.IpAddress) &&
                            imageSettingsDto.FtpInfo.Port > 0 &&
                            !string.IsNullOrEmpty(imageSettingsDto.FtpInfo.Username) &&
                            !string.IsNullOrEmpty(imageSettingsDto.FtpInfo.Password) &&
                            imageSettingsDto.IsFtpUploadEnabled) {
                            if (!_ftp.IsConnected) {
                                var (key, value) = await _ftp.Connect(imageSettingsDto.FtpInfo.IpAddress,
                                    imageSettingsDto.FtpInfo.Port, imageSettingsDto.FtpInfo.Username,
                                    imageSettingsDto.FtpInfo.Password);
                                if (!key) {
                                    return new KeyValuePair<bool, string>(false, "FTP登录失败!");
                                }
                            }

                            //删除
                            var fileInfoList = await _ftp.GetFileInfoList("");
                            if (fileInfoList?.Any() == true) {
                                var list = fileInfoList.Where(w => IsManagedImagePath(w.FullPath))
                                    .Where(w => w.CreatedTime.CompareTo(DateTime.Now.AddDays(0 - days)) < 0).
                                    Select(s => s.FullPath)?.ToList();
                                if (list?.Any() == true) {
                                    for (var i = list.Count - 1; i >= 0; i--) {
                                        var (isDeleted, error) = await _ftp.DeleteFile(list[i]);
                                        if (!isDeleted) {
                                            return new KeyValuePair<bool, string>(false,
                                                $"FTP文件删除失败:{list[i]};{error}");
                                        }
                                    }
                                }
                            }
                        }
                        else {
                            return new KeyValuePair<bool, string>(false, "FTP配置为空");
                        }
                    }
                    else {
                        return new KeyValuePair<bool, string>(false, "存图配置为空");
                    }
                }
                else {
                    return new KeyValuePair<bool, string>(false, "找不到存图配置");
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _deleteFtpImagesSlim.Release();
            }

            return new KeyValuePair<bool, string>(true, string.Empty);
        }

        public async Task<KeyValuePair<bool, string>> DeleteLogDataOlderThanDays(int days) {
            //暂时没有日志写
            await _apiLogRepository.DeleteDataThanDays(days);
            await _appLogRepository.DeleteDataThanDays(days);
            await _cameraLogRepository.DeleteDataThanDays(days);
            await _exceptionLogRepository.DeleteDataThanDays(days);
            await _ftpLogRepository.DeleteDataThanDays(days);
            await _inputLogRepository.DeleteDataThanDays(days);
            await _ocrLogRepository.DeleteDataThanDays(days);

            await _outputLogRepository.DeleteDataThanDays(days);
            await _sortingLogRepository.DeleteDataThanDays(days);
            await _volumeLogRepository.DeleteDataThanDays(days);
            await _weighingLogRepository.DeleteDataThanDays(days);
            return new KeyValuePair<bool, string>(true, string.Empty);
        }

        public async Task<KeyValuePair<bool, string>> DeleteEarliestBarcodeData() {
            //从数据库删除
            try {
                await _deleteBarcodeSlim.WaitAsync();
                var barCodeInfoModels = await _barCodeRepository.Select(w => w.Id > 0,
                    o => o.ScanTime, 0, 1);
                var barCodeInfoModel = barCodeInfoModels?.FirstOrDefault();
                if (barCodeInfoModel is not null) {
                    const int count = 5000;
                    var deleteCount = 0;
                    do {
                        deleteCount = await _barCodeRepository.DeleteCount(count,
                            w => w.ScanTime.CompareTo(barCodeInfoModel.ScanTime.Date.AddDays(1)) < 0);
                    } while (deleteCount == count);
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _deleteBarcodeSlim.Release();
            }
            return new KeyValuePair<bool, string>(true, string.Empty);
        }

        public async Task<KeyValuePair<bool, string>> DeleteEarliestScanImages() {
            //获取最早一天的扫码图
            try {
                await _deleteScanImagesSlim.WaitAsync();
                var configInfoModel = await _settingsReader.GetRawAsync("SaveImageSettings");
                if (configInfoModel is not null) {
                    var imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel);
                    if (imageSettingsDto is not null) {
                        if (Directory.Exists(imageSettingsDto.ImageRootDirectory)) {
                            return DeleteEarliestImageDay(
                                imageSettingsDto.ImageRootDirectory,
                                "BarcodeImage");
                        }
                        else {
                            return new KeyValuePair<bool, string>(false, "存图目录不存在");
                        }
                    }
                    else {
                        return new KeyValuePair<bool, string>(false, "存图配置为空");
                    }
                }
                else {
                    return new KeyValuePair<bool, string>(false, "找不到存图配置");
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _deleteScanImagesSlim.Release();
            }
        }

        public async Task<KeyValuePair<bool, string>> DeleteEarliestPanoramaImages() {
            try {
                await _deletePanoramaImagesSlim.WaitAsync();
                var configInfoModel = await _settingsReader.GetRawAsync("SaveImageSettings");
                if (configInfoModel is not null) {
                    var imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel);
                    if (imageSettingsDto is not null) {
                        if (Directory.Exists(imageSettingsDto.ImageRootDirectory)) {
                            return DeleteEarliestImageDay(
                                imageSettingsDto.ImageRootDirectory,
                                "PanoramaImage");
                        }
                        else {
                            return new KeyValuePair<bool, string>(false, "存图目录不存在");
                        }
                    }
                    else {
                        return new KeyValuePair<bool, string>(false, "存图配置为空");
                    }
                }
                else {
                    return new KeyValuePair<bool, string>(false, "找不到存图配置");
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _deletePanoramaImagesSlim.Release();
            }
        }

        /// <summary>以流式枚举方式删除早于阈值的图片，避免为海量文件一次性分配路径数组。</summary>
        private static KeyValuePair<bool, string> DeleteImagesOlderThan(
            string imageRootDirectory,
            string imageCategory,
            DateTime threshold) =>
            DeleteMatchingImages(
                imageRootDirectory,
                imageCategory,
                creationTime => creationTime < threshold);

        /// <summary>查找并删除指定图片类别中最早自然日的全部文件。</summary>
        private static KeyValuePair<bool, string> DeleteEarliestImageDay(
            string imageRootDirectory,
            string imageCategory) {
            DateTime? earliestCreationTime = null;
            var inspectionFailures = 0;
            foreach (var file in EnumerateManagedImageFiles(imageRootDirectory, imageCategory)) {
                try {
                    var creationTime = File.GetCreationTime(file);
                    if (earliestCreationTime is null || creationTime < earliestCreationTime.Value) {
                        earliestCreationTime = creationTime;
                    }
                }
                catch (Exception) {
                    inspectionFailures++;
                }
            }

            if (earliestCreationTime is null) {
                return new KeyValuePair<bool, string>(
                    false,
                    inspectionFailures == 0
                        ? "不存在任何文件"
                        : $"没有可读取的图片文件，读取失败数量:{inspectionFailures}");
            }

            return DeleteMatchingImages(
                imageRootDirectory,
                imageCategory,
                creationTime => creationTime < earliestCreationTime.Value.Date.AddDays(1));
        }

        /// <summary>逐个删除满足创建时间条件的托管图片，并在单文件失败时继续处理其余文件。</summary>
        private static KeyValuePair<bool, string> DeleteMatchingImages(
            string imageRootDirectory,
            string imageCategory,
            Func<DateTime, bool> shouldDelete) {
            var removedCount = 0;
            var failedCount = 0;
            foreach (var file in EnumerateManagedImageFiles(imageRootDirectory, imageCategory)) {
                try {
                    if (!shouldDelete(File.GetCreationTime(file))) {
                        continue;
                    }

                    File.Delete(file);
                    removedCount++;
                }
                catch (Exception) {
                    failedCount++;
                }
            }

            return failedCount == 0
                ? new KeyValuePair<bool, string>(true, string.Empty)
                : new KeyValuePair<bool, string>(
                    false,
                    $"已删除:{removedCount}，失败:{failedCount}");
        }

        /// <summary>流式枚举指定根目录下属于目标图片类别的文件，并跳过连接点和无权限目录。</summary>
        private static IEnumerable<string> EnumerateManagedImageFiles(
            string imageRootDirectory,
            string imageCategory) {
            var enumerationOptions = new EnumerationOptions {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };
            var windowsMarker = $"\\{imageCategory}\\";
            var alternateMarker = $"/{imageCategory}/";
            return Directory.EnumerateFiles(imageRootDirectory, "*", enumerationOptions)
                .Where(path =>
                    path.Contains(windowsMarker, StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(alternateMarker, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>判断远程路径是否属于应用管理的图片目录。</summary>
        private static bool IsManagedImagePath(string path) {
            var normalizedPath = path.Replace('\\', '/');
            return normalizedPath.Contains("/PanoramaImage/", StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.Contains("/BarcodeImage/", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<KeyValuePair<bool, string>> DeleteEarliestLogData() {
            //暂时没有日志写
            await _apiLogRepository.DeleteEarliestData();
            await _appLogRepository.DeleteEarliestData();
            await _cameraLogRepository.DeleteEarliestData();
            await _exceptionLogRepository.DeleteEarliestData();
            await _ftpLogRepository.DeleteEarliestData();
            await _inputLogRepository.DeleteEarliestData();
            await _ocrLogRepository.DeleteEarliestData();
            await _outputLogRepository.DeleteEarliestData();
            await _sortingLogRepository.DeleteEarliestData();
            await _volumeLogRepository.DeleteEarliestData();
            await _weighingLogRepository.DeleteEarliestData();

            return new KeyValuePair<bool, string>(true, string.Empty);
        }
    }
}
