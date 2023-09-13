using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TouchSocket.Core;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Ftp;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.CacheCleanup;
using JayTom.Dws.Domain.Repository.LocalData;

namespace JayTom.Dws.Infrastructure.Service {

    public class CacheCleanupService : ICacheCleanupService {
        private readonly IBarCodeRepository _barCodeRepository;
        private readonly IConfigRepository _configRepository;
        private readonly IFtp _ftp;
        private static SemaphoreSlim _deleteBarcodeSlim = new(1, 1);
        private static SemaphoreSlim _deleteScanImagesSlim = new(1, 1);
        private static SemaphoreSlim _deletePanoramaImagesSlim = new(1, 1);
        private static SemaphoreSlim _deleteFtpImagesSlim = new(1, 1);

        //DeleteFtpImages
        public CacheCleanupService(IBarCodeRepository barCodeRepository,
            IConfigRepository configRepository, IFtp ftp) {
            _barCodeRepository = barCodeRepository;
            _configRepository = configRepository;
            _ftp = ftp;
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
                var configInfoModel = await _configRepository.FirstOrDefault(f =>
                    f.ConfigName.Equals("SaveImageSettings"));
                if (configInfoModel is not null) {
                    var imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel.Value);
                    if (imageSettingsDto is not null) {
                        if (Directory.Exists(imageSettingsDto.ImageRootDirectory)) {
                            var list = Directory.GetFiles(imageSettingsDto.ImageRootDirectory, "*", SearchOption.AllDirectories)
                                .AsParallel()
                                .Where(w => w.Contains("\\BarcodeImage\\"))
                                .Select(file => new FileInfo(file))
                                .Where(w => w.CreationTime.CompareTo(DateTime.Now.AddDays(0 - days)) < 0).
                                Select(s => s.FullName)?.ToList();
                            if (list?.Any() == true) {
                                for (var i = list.Count - 1; i >= 0; i--) {
                                    File.Delete(list[i]);
                                }
                            }
                            return new KeyValuePair<bool, string>(true, string.Empty);
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
                var configInfoModel = await _configRepository.FirstOrDefault(f =>
                    f.ConfigName.Equals("SaveImageSettings"));
                if (configInfoModel is not null) {
                    var imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel.Value);
                    if (imageSettingsDto is not null) {
                        if (Directory.Exists(imageSettingsDto.ImageRootDirectory)) {
                            var list = Directory.GetFiles(imageSettingsDto.ImageRootDirectory, "*", SearchOption.AllDirectories)
                                .AsParallel()
                                .Where(w => w.Contains("\\PanoramaImage\\"))
                                .Select(file => new FileInfo(file))
                                .Where(w => w.CreationTime.CompareTo(DateTime.Now.AddDays(0 - days)) < 0).
                                Select(s => s.FullName)?.ToList();
                            if (list?.Any() == true) {
                                for (var i = list.Count - 1; i >= 0; i--) {
                                    File.Delete(list[i]);
                                }
                            }
                            return new KeyValuePair<bool, string>(true, string.Empty);
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
                var configInfoModel = await _configRepository.FirstOrDefault(f =>
                    f.ConfigName.Equals("SaveImageSettings"));
                if (configInfoModel is not null) {
                    var imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel.Value);
                    if (imageSettingsDto is not null) {
                        if (!string.IsNullOrEmpty(imageSettingsDto.FtpInfo.IpAddress) &&
                            imageSettingsDto.FtpInfo.Port > 0 &&
                            !string.IsNullOrEmpty(imageSettingsDto.FtpInfo.Username) &&
                            !string.IsNullOrEmpty(imageSettingsDto.FtpInfo.Password)) {
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
                                var list = fileInfoList.Where(w => w.FullPath.Contains("\\PanoramaImage\\") ||
                                                                   w.FullPath.Contains("\\BarcodeImage\\"))
                                    .Where(w => w.CreatedTime.CompareTo(DateTime.Now.AddDays(0 - days)) < 0).
                                    Select(s => s.FileName)?.ToList();
                                if (list?.Any() == true) {
                                    for (var i = list.Count - 1; i >= 0; i--) {
                                        File.Delete(list[i]);
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

        public Task<KeyValuePair<bool, string>> DeleteLogDataOlderThanDays(int days) {
            //暂时没有日志写
            return Task.FromResult(new KeyValuePair<bool, string>(false, "未实现日志删除"));
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
                var configInfoModel = await _configRepository.FirstOrDefault(f =>
                    f.ConfigName.Equals("SaveImageSettings"));
                if (configInfoModel is not null) {
                    var imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel.Value);
                    if (imageSettingsDto is not null) {
                        if (Directory.Exists(imageSettingsDto.ImageRootDirectory)) {
                            var firstOrDefault = Directory.GetFiles(imageSettingsDto.ImageRootDirectory, "*", SearchOption.AllDirectories)
                                .AsParallel()
                                .Where(w => w.Contains("\\BarcodeImage\\"))
                                .Select(file => new FileInfo(file))
                                .OrderBy(o => o.CreationTime)
                                .FirstOrDefault();
                            if (firstOrDefault != null) {
                                var list = Directory.GetFiles(imageSettingsDto.ImageRootDirectory, "*", SearchOption.AllDirectories)
                                    .AsParallel()
                                    .Where(w => w.Contains("\\BarcodeImage\\"))
                                    .Select(file => new FileInfo(file))
                                    .Where(w => w.CreationTime.CompareTo(firstOrDefault.CreationTime.Date.AddDays(1)) < 0).
                                    OrderBy(o => o.CreationTime).
                                    Select(s => s.FullName)?.ToList();
                                if (list?.Any() == true) {
                                    foreach (var variable in list) {
                                        File.Delete(variable);
                                    }
                                }
                                return new KeyValuePair<bool, string>(true, string.Empty);
                            }
                            else {
                                return new KeyValuePair<bool, string>(false, "不存在任何文件");
                            }
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
                await _deleteScanImagesSlim.WaitAsync();
                var configInfoModel = await _configRepository.FirstOrDefault(f =>
                    f.ConfigName.Equals("SaveImageSettings"));
                if (configInfoModel is not null) {
                    var imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel.Value);
                    if (imageSettingsDto is not null) {
                        if (Directory.Exists(imageSettingsDto.ImageRootDirectory)) {
                            var firstOrDefault = Directory.GetFiles(imageSettingsDto.ImageRootDirectory, "*", SearchOption.AllDirectories)
                                .AsParallel()
                                .Where(w => w.Contains("\\PanoramaImage\\"))
                                .Select(file => new FileInfo(file))
                                .OrderBy(o => o.CreationTime)
                                .FirstOrDefault();
                            if (firstOrDefault != null) {
                                var list = Directory.GetFiles(imageSettingsDto.ImageRootDirectory, "*", SearchOption.AllDirectories)
                                    .AsParallel()
                                    .Where(w => w.Contains("\\PanoramaImage\\"))
                                    .Select(file => new FileInfo(file))
                                    .Where(w => w.CreationTime.CompareTo(firstOrDefault.CreationTime.Date.AddDays(1)) < 0).
                                    OrderBy(o => o.CreationTime).
                                    Select(s => s.FullName)?.ToList();
                                if (list?.Any() == true) {
                                    foreach (var variable in list) {
                                        File.Delete(variable);
                                    }
                                }
                                return new KeyValuePair<bool, string>(true, string.Empty);
                            }
                            else {
                                return new KeyValuePair<bool, string>(false, "不存在任何文件");
                            }
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

        public Task<KeyValuePair<bool, string>> DeleteEarliestLogData() {
            //暂时没有日志写
            return Task.FromResult(new KeyValuePair<bool, string>(false, "未实现日志删除"));
        }
    }
}