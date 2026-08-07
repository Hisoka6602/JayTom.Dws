using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.CacheCleanup;
using WindowsAction = JayTom.Dws.Client.EventMediators.WindowsAction;
using WindowsActionType = JayTom.Dws.Client.EventMediators.WindowsActionType;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    public class CleanupService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly ICacheCleanupService _cacheCleanupService;
        private readonly IConfigRepository _configRepository;
        private readonly IComputer _computer;
        private CacheClearSettingsDto? _cacheClearSettingsDto;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private string _imagePathRoot = string.Empty;
        private int _isWindowsClose;
        /// <summary>
        /// 当前后台服务停止令牌。
        /// </summary>
        private CancellationToken _stoppingToken;

        //获取设置
        public CleanupService(ICacheCleanupService cacheCleanupService,
            IConfigRepository configRepository, IComputer computer)
        {
            _cacheCleanupService = cacheCleanupService;
            _configRepository = configRepository;
            _computer = computer;
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(settings =>
            {
                if (settings.SettingsName is "SaveImageSettings" or "CacheClearSettings")
                {
                    _ = ReloadSettingsAsync(settings.SettingsName, _stoppingToken);
                }
            });
            EventAggregator.Instance.Subscribe<WindowsAction>(item =>
            {
                if (item is { Type: WindowsActionType.Close })
                {
                    Interlocked.Exchange(ref _isWindowsClose, 1);
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _stoppingToken = stoppingToken;
            await ReloadSettingsAsync("SaveImageSettings", stoppingToken).ConfigureAwait(false);
            await ReloadSettingsAsync("CacheClearSettings", stoppingToken).ConfigureAwait(false);
            CleanupExpiredLogFiles();
            while (!stoppingToken.IsCancellationRequested &&
                   Volatile.Read(ref _isWindowsClose) == 0)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken).ConfigureAwait(false);
                    if (Volatile.Read(ref _isWindowsClose) != 0)
                    {
                        break;
                    }

                    var cacheSettings = Volatile.Read(ref _cacheClearSettingsDto);
                    //数据盘
                    if (cacheSettings?.MinimumSpaceRetention > 0)
                    {
                        var diskInfos = await _computer.GetDiskInfoAsync().ConfigureAwait(false);
                        var minimumSpaceBytes = cacheSettings.MinimumSpaceRetention >
                                                long.MaxValue / (1024L * 1024L)
                            ? long.MaxValue
                            : cacheSettings.MinimumSpaceRetention * 1024L * 1024L;
                        var dataDriveName = Path.GetPathRoot(Directory.GetCurrentDirectory())
                            ?.Replace(":\\", string.Empty);
                        var diskInfo = diskInfos.FirstOrDefault(w =>
                            string.Equals(w.Name, dataDriveName, StringComparison.OrdinalIgnoreCase));

                        if (diskInfo is { UsedDiskSpace: > 0 } &&
                            diskInfo.AvailableDiskSpace <= minimumSpaceBytes)
                        {
                            //清除
                            await _cacheCleanupService.DeleteEarliestBarcodeData().ConfigureAwait(false);
                            await _cacheCleanupService.DeleteEarliestLogData().ConfigureAwait(false);
                        }

                        //图片盘
                        var imageDriveName = Volatile.Read(ref _imagePathRoot)
                            .Replace(":\\", string.Empty);
                        var imageDiskInfo = diskInfos.FirstOrDefault(w =>
                            string.Equals(w.Name, imageDriveName, StringComparison.OrdinalIgnoreCase));

                        if (imageDiskInfo is { UsedDiskSpace: > 0 } &&
                            imageDiskInfo.AvailableDiskSpace <= minimumSpaceBytes)
                        {
                            //清除
                            await _cacheCleanupService.DeleteEarliestPanoramaImages().ConfigureAwait(false);
                            await _cacheCleanupService.DeleteEarliestScanImages().ConfigureAwait(false);
                        }
                    }

                    //删除指定日期前数据(小于等于0则不删除)
                    if (cacheSettings?.BarcodeDataAgoDays > 0)
                    {
                        _ = await _cacheCleanupService
                            .DeleteBarcodeDataOlderThanDays(cacheSettings.BarcodeDataAgoDays)
                            .ConfigureAwait(false);
                    }
                    if (cacheSettings?.ScanImageAgoDays > 0)
                    {
                        await _cacheCleanupService
                            .DeleteScanImagesOlderThanDays(cacheSettings.ScanImageAgoDays)
                            .ConfigureAwait(false);
                    }
                    if (cacheSettings?.PanoramaImageAgoDays > 0)
                    {
                        await _cacheCleanupService
                            .DeletePanoramaImagesOlderThanDays(cacheSettings.PanoramaImageAgoDays)
                            .ConfigureAwait(false);
                    }
                    if (cacheSettings?.FtpImageAgoDays > 0)
                    {
                        //await _cacheCleanupService.DeleteFtpImagesOlderThanDays(cacheSettings.FtpImageAgoDays);
                    }

                    if (cacheSettings?.LogDataAgoDays > 0)
                    {
                        await _cacheCleanupService
                            .DeleteLogDataOlderThanDays(cacheSettings.LogDataAgoDays)
                            .ConfigureAwait(false);
                    }
                    CleanupExpiredLogFiles();
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error($"定时清理异常:{e}");
                }
            }
        }

        /// <summary>
        /// 按当前配置清理超过保留天数的物理日志文件。
        /// </summary>
        private void CleanupExpiredLogFiles()
        {
            var retentionDays = _cacheClearSettingsDto?.LogDataAgoDays ?? 0;
            if (retentionDays <= 0)
            {
                return;
            }

            try
            {
                var logsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                var threshold = DateTime.Now.AddDays(-retentionDays);
                var (removedCount, failedCount) = RemoveExpiredLogFiles(logsFolderPath, threshold);
                if (removedCount > 0 || failedCount > 0)
                {
                    NLog.LogManager.GetCurrentClassLogger().Info(
                        $"日志文件清理完成，删除数量:{removedCount}，失败数量:{failedCount}");
                }
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger().Error($"删除日志文件异常:{e}");
            }
        }

        /// <summary>
        /// 串行重载清理服务配置，确保异常时也能释放同步门。
        /// </summary>
        /// <param name="settingsName">配置名称。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        private async Task ReloadSettingsAsync(string settingsName, CancellationToken cancellationToken)
        {
            var lockTaken = false;
            try
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                lockTaken = true;
                if (settingsName == "SaveImageSettings")
                {
                    var imageSettings = await _configRepository
                        .FirstOrDefaultEntity<ImageSettingsDto>(settingsName, cancellationToken)
                        .ConfigureAwait(false) ?? new ImageSettingsDto();
                    var imagePathRoot = string.IsNullOrWhiteSpace(imageSettings.ImageRootDirectory)
                        ? string.Empty
                        : Path.GetPathRoot(imageSettings.ImageRootDirectory) ?? string.Empty;
                    Volatile.Write(ref _imagePathRoot, imagePathRoot);
                }
                else
                {
                    var cacheSettings = await _configRepository
                        .FirstOrDefaultEntity<CacheClearSettingsDto>(settingsName, cancellationToken)
                        .ConfigureAwait(false);
                    Volatile.Write(ref _cacheClearSettingsDto, cacheSettings);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 服务停止时无需继续加载配置。
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger()
                    .Error(exception, $"加载{settingsName}配置失败");
            }
            finally
            {
                if (lockTaken)
                {
                    _semaphore.Release();
                }
            }
        }

        /// <summary>
        /// 删除指定目录及其子目录中最后写入时间早于阈值的日志文件。
        /// </summary>
        /// <param name="logsFolderPath">日志根目录。</param>
        /// <param name="threshold">日志过期阈值。</param>
        /// <returns>成功删除数量和失败数量。</returns>
        private static (int removedCount, int failedCount) RemoveExpiredLogFiles(
            string logsFolderPath,
            DateTime threshold)
        {
            if (!Directory.Exists(logsFolderPath))
            {
                return (0, 0);
            }

            var removedCount = 0;
            var failedCount = 0;
            foreach (var file in EnumerateLogFiles(logsFolderPath))
            {
                try
                {
                    if (File.GetLastWriteTime(file) >= threshold)
                    {
                        continue;
                    }

                    File.Delete(file);
                    removedCount++;
                }
                catch (Exception e)
                {
                    failedCount++;
                    NLog.LogManager.GetCurrentClassLogger().Warn(
                        e,
                        $"删除过期日志文件失败，文件:{file}");
                }
            }

            return (removedCount, failedCount);
        }

        /// <summary>
        /// 递归枚举主日志文件及 NLog 数字日期归档日志。
        /// </summary>
        /// <param name="logsFolderPath">日志根目录。</param>
        /// <returns>日志文件集合。</returns>
        private static IEnumerable<string> EnumerateLogFiles(string logsFolderPath)
        {
            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            foreach (var file in Directory.EnumerateFiles(
                         logsFolderPath,
                         "*.log",
                         enumerationOptions))
            {
                yield return file;
            }

            foreach (var file in Directory.EnumerateFiles(
                         logsFolderPath,
                         "*.log.*",
                         enumerationOptions))
            {
                var fileName = Path.GetFileName(file);
                var markerIndex = fileName.IndexOf(".log.", StringComparison.OrdinalIgnoreCase);
                if (markerIndex < 0)
                {
                    continue;
                }

                var archiveSuffix = fileName[(markerIndex + 5)..];
                if (archiveSuffix.Length == 0 ||
                    archiveSuffix.Any(character =>
                        !char.IsDigit(character) &&
                        character != '.' &&
                        character != '-'))
                {
                    continue;
                }

                yield return file;
            }
        }
    }
}
