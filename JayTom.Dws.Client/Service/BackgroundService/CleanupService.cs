using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.CacheCleanup;

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
                if (settings is SettingsChangedEvent { SettingsName: "SaveImageSettings" }) {
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
                if (item is WindowsAction { Type: WindowsActionType.Close }) {
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
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
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
                        await _cacheCleanupService.DeleteFtpImagesOlderThanDays(_cacheClearSettingsDto.FtpImageAgoDays);
                    }

                    if (_cacheClearSettingsDto?.LogDataAgoDays > 0) {
                        await _cacheCleanupService.DeleteLogDataOlderThanDays(_cacheClearSettingsDto.LogDataAgoDays);
                    }
                    _lastCleanupTime = DateTime.Now;
                }
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}