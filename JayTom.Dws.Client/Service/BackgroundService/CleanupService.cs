using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.CacheCleanup;
using WindowsAction = JayTom.Dws.Client.EventMediators.WindowsAction;
using WindowsActionType = JayTom.Dws.Client.EventMediators.WindowsActionType;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class CleanupService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly ICacheCleanupService _cacheCleanupService;
        private readonly IConfigRepository _configRepository;
        private readonly IComputer _computer;
        private ImageSettingsDto? _imageSettingsDto;
        private CacheClearSettingsDto? _cacheClearSettingsDto;
        private SemaphoreSlim _semaphore = new(1);
        private string _imagePathRoot = string.Empty;
        private DateTime _lastCleanupTime = DateTime.Today;
        private static bool _isWindowsClose = false;

        //获取设置
        public CleanupService(ICacheCleanupService cacheCleanupService,
            IConfigRepository configRepository, IComputer computer) {
            _cacheCleanupService = cacheCleanupService;
            _configRepository = configRepository;
            _computer = computer;
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async settings => {
                if (settings is { SettingsName: "SaveImageSettings" }) {
                    await _semaphore.WaitAsync();
                    _imageSettingsDto = await _configRepository.FirstOrDefaultEntity<ImageSettingsDto>("SaveImageSettings") ?? new ImageSettingsDto();
                    if (_imageSettingsDto is not null) {
                        if (!string.IsNullOrEmpty(_imageSettingsDto?.ImageRootDirectory)) {
                            _imagePathRoot = Path.GetPathRoot(_imageSettingsDto.ImageRootDirectory) ?? string.Empty;
                        }
                    }
                    _semaphore.Release();
                }
                else if (settings is SettingsChangedEvent { SettingsName: "CacheClearSettings" }) {
                    await _semaphore.WaitAsync();
                    _cacheClearSettingsDto = await _configRepository.FirstOrDefaultEntity<CacheClearSettingsDto>("CacheClearSettings");
                    _semaphore.Release();
                    //CacheClearSettings
                }
            });
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                if (item is { Type: WindowsActionType.Close }) {
                    _isWindowsClose = true;
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _imageSettingsDto = await _configRepository.FirstOrDefaultEntity<ImageSettingsDto>("SaveImageSettings", stoppingToken) ?? new ImageSettingsDto();
            if (!string.IsNullOrEmpty(_imageSettingsDto?.ImageRootDirectory)) {
                _imagePathRoot = Path.GetPathRoot(_imageSettingsDto.ImageRootDirectory) ?? string.Empty;
            }

            _cacheClearSettingsDto = await _configRepository.FirstOrDefaultEntity<CacheClearSettingsDto>("CacheClearSettings", stoppingToken);
            //删除文件日志
            try {
                await Task.Yield();
                var logsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (Directory.Exists(logsFolderPath) && _cacheClearSettingsDto?.LogDataAgoDays > 0) {
                    // 匹配日期命名的.log文件
                    var regex = new Regex(@"^\d{4}-\d{2}-\d{2}\.log$");
                    // 调用递归方法来处理logs文件夹及其所有子文件夹
                    var logFiles = GetLogFiles(logsFolderPath, regex);
                    foreach (var file in from file in logFiles let creationTime = File.GetCreationTime(file) let difference = DateTime.Now - creationTime where difference.TotalDays > _cacheClearSettingsDto.LogDataAgoDays select file) {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"删除日志文件异常:{e}");
            }
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken).ContinueWith(async a => {
                    if (a.IsCompletedSuccessfully) {
                        //数据盘
                        if (_cacheClearSettingsDto?.MinimumSpaceRetention > 0) {
                            var diskInfo = (await _computer.GetDiskInfoAsync())
                                ?.FirstOrDefault(w =>
                                    w.Name.Equals(Path.GetPathRoot(Directory.GetCurrentDirectory())
                                        ?.Replace(":\\", string.Empty)));

                            if (diskInfo?.UsedDiskSpace > 0 && diskInfo?.AvailableDiskSpace <= (_cacheClearSettingsDto?.MinimumSpaceRetention * 1024 * 1024)) {
                                //清除
                                await _cacheCleanupService.DeleteEarliestBarcodeData();
                                await _cacheCleanupService.DeleteEarliestLogData();
                            }
                            //图片盘
                            var imagediskinfo = (await _computer.GetDiskInfoAsync())
                                ?.FirstOrDefault(w =>
                                    w.Name.Equals(_imagePathRoot
                                        ?.Replace(":\\", string.Empty)));

                            if (imagediskinfo?.UsedDiskSpace > 0 && imagediskinfo?.AvailableDiskSpace <= (_cacheClearSettingsDto?.MinimumSpaceRetention * 1024 * 1024)) {
                                //清除
                                await _cacheCleanupService.DeleteEarliestPanoramaImages();
                                await _cacheCleanupService.DeleteEarliestScanImages();
                            }
                        }

                        if (DateTime.Now.Subtract(_lastCleanupTime).TotalMinutes >= 10) {
                            //删除指定日期前数据(小于等于0则不删除)
                            if (_cacheClearSettingsDto?.BarcodeDataAgoDays > 0) {
                                var (key, value) = await _cacheCleanupService.DeleteBarcodeDataOlderThanDays(_cacheClearSettingsDto.BarcodeDataAgoDays);
                            }
                            if (_cacheClearSettingsDto?.ScanImageAgoDays > 0) {
                                await _cacheCleanupService.DeleteScanImagesOlderThanDays(_cacheClearSettingsDto.ScanImageAgoDays);
                            }
                            if (_cacheClearSettingsDto?.PanoramaImageAgoDays > 0) {
                                await _cacheCleanupService.DeletePanoramaImagesOlderThanDays(_cacheClearSettingsDto.PanoramaImageAgoDays);
                            }
                            if (_cacheClearSettingsDto?.FtpImageAgoDays > 0) {
                                //await _cacheCleanupService.DeleteFtpImagesOlderThanDays(_cacheClearSettingsDto.FtpImageAgoDays);
                            }

                            if (_cacheClearSettingsDto?.LogDataAgoDays > 0) {
                                await _cacheCleanupService.DeleteLogDataOlderThanDays(_cacheClearSettingsDto.LogDataAgoDays);
                            }
                            _lastCleanupTime = DateTime.Now;
                        }
                    }
                });
            }
        }

        private IEnumerable<string> GetLogFiles(string folderPath, Regex regex) {
            // 获取文件夹中的所有文件和子文件夹
            var files = Directory.GetFiles(folderPath);
            var subFolders = Directory.GetDirectories(folderPath);

            // 处理文件夹中的文件
            var logFiles = files?.Where(fileName => regex.IsMatch(Path.GetFileName(fileName)))?.ToList() ?? new List<string>();

            // 递归处理子文件夹
            foreach (var subFolder in subFolders) {
                var subFolderLogFiles = GetLogFiles(subFolder, regex);
                logFiles.AddRange(subFolderLogFiles);
            }

            // 返回完整路径的日志文件列表
            return logFiles;
        }
    }
}