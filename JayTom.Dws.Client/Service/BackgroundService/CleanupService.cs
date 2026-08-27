using JayTom.Dws.Application.Configuration;
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Legacy.Contracts.Services.CacheCleanup;
using WindowsAction = JayTom.Dws.Client.Events.WindowsAction;
using WindowsActionType = JayTom.Dws.Client.Events.WindowsActionType;
using SettingsChangedEvent = JayTom.Dws.Application.Events.SettingsChangedEvent;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    public class CleanupService : Microsoft.Extensions.Hosting.BackgroundService
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;
        /// <summary>磁盘及保留期清理的执行间隔。</summary>
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
        /// <summary>单次低磁盘清理允许删除的最大图片日期批次数。</summary>
        private const int MaxImageReclamationPasses = 10;
        /// <summary>后台清理日志记录器。</summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly ICacheCleanupService _cacheCleanupService;
        private readonly ISettingsStore _settingsStore;
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
            ISettingsStore settingsStore, IComputer computer,
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            _cacheCleanupService = cacheCleanupService;
            _settingsStore = settingsStore;
            _computer = computer;
            _eventBus.Subscribe<SettingsChangedEvent>(settings =>
            {
                if (settings.SettingsName is "SaveImageSettings" or "CacheClearSettings")
                {
                    ReloadSettingsAsync(settings.SettingsName, _stoppingToken)
                        .Forget("重新加载清理设置");
                }
            });
            _eventBus.Subscribe<WindowsAction>(item =>
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
            while (!stoppingToken.IsCancellationRequested &&
                   Volatile.Read(ref _isWindowsClose) == 0)
            {
                try
                {
                    await RunCleanupCycleAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "定时清理异常");
                }

                try
                {
                    await Task.Delay(CleanupInterval, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 执行一次磁盘水位保护和按保留期清理。
        /// </summary>
        /// <param name="stoppingToken">服务停止令牌。</param>
        private async Task RunCleanupCycleAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var cacheSettings = Volatile.Read(ref _cacheClearSettingsDto);
            if (cacheSettings is null)
            {
                CleanupExpiredLogFiles();
                return;
            }

            if (cacheSettings.MinimumSpaceRetention > 0)
            {
                var minimumSpaceBytes = cacheSettings.MinimumSpaceRetention >
                                        long.MaxValue / (1024L * 1024L)
                    ? long.MaxValue
                    : cacheSettings.MinimumSpaceRetention * 1024L * 1024L;
                var dataDriveName = NormalizeDriveName(
                    Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory));
                if (await IsDiskSpaceLowAsync(dataDriveName, minimumSpaceBytes).ConfigureAwait(false))
                {
                    Logger.Warn($"数据盘剩余空间低于保留水位，开始清理最早数据。盘符:{dataDriveName}");
                    await ExecuteCleanupOperationAsync(
                            "最早条码数据",
                            _cacheCleanupService.DeleteEarliestBarcodeData)
                        .ConfigureAwait(false);
                    await ExecuteCleanupOperationAsync(
                            "最早数据库日志",
                            _cacheCleanupService.DeleteEarliestLogData)
                        .ConfigureAwait(false);
                }

                var imageDriveName = NormalizeDriveName(Volatile.Read(ref _imagePathRoot));
                await ReclaimImageDiskSpaceAsync(
                        imageDriveName,
                        minimumSpaceBytes,
                        stoppingToken)
                    .ConfigureAwait(false);
            }

            if (cacheSettings.BarcodeDataAgoDays > 0)
            {
                await ExecuteCleanupOperationAsync(
                        "过期条码数据",
                        () => _cacheCleanupService.DeleteBarcodeDataOlderThanDays(
                            cacheSettings.BarcodeDataAgoDays))
                    .ConfigureAwait(false);
            }
            if (cacheSettings.ScanImageAgoDays > 0)
            {
                await ExecuteCleanupOperationAsync(
                        "过期扫码图片",
                        () => _cacheCleanupService.DeleteScanImagesOlderThanDays(
                            cacheSettings.ScanImageAgoDays))
                    .ConfigureAwait(false);
            }
            if (cacheSettings.PanoramaImageAgoDays > 0)
            {
                await ExecuteCleanupOperationAsync(
                        "过期全景图片",
                        () => _cacheCleanupService.DeletePanoramaImagesOlderThanDays(
                            cacheSettings.PanoramaImageAgoDays))
                    .ConfigureAwait(false);
            }
            if (cacheSettings.FtpImageAgoDays > 0)
            {
                await ExecuteCleanupOperationAsync(
                        "过期 FTP 图片",
                        () => _cacheCleanupService.DeleteFtpImagesOlderThanDays(
                            cacheSettings.FtpImageAgoDays))
                    .ConfigureAwait(false);
            }
            if (cacheSettings.LogDataAgoDays > 0)
            {
                await ExecuteCleanupOperationAsync(
                        "过期数据库日志",
                        () => _cacheCleanupService.DeleteLogDataOlderThanDays(
                            cacheSettings.LogDataAgoDays))
                    .ConfigureAwait(false);
            }

            CleanupExpiredLogFiles();
        }

        /// <summary>
        /// 在图片盘空间不足时按最早日期分批删除，达到水位或没有可删文件后停止。
        /// </summary>
        /// <param name="driveName">图片盘盘符。</param>
        /// <param name="minimumSpaceBytes">最低保留空间。</param>
        /// <param name="stoppingToken">服务停止令牌。</param>
        private async Task ReclaimImageDiskSpaceAsync(
            string driveName,
            long minimumSpaceBytes,
            CancellationToken stoppingToken)
        {
            if (string.IsNullOrWhiteSpace(driveName))
            {
                return;
            }

            for (var pass = 1; pass <= MaxImageReclamationPasses; pass++)
            {
                stoppingToken.ThrowIfCancellationRequested();
                if (!await IsDiskSpaceLowAsync(driveName, minimumSpaceBytes).ConfigureAwait(false))
                {
                    return;
                }

                Logger.Warn($"图片盘剩余空间低于保留水位，执行第 {pass} 批清理。盘符:{driveName}");
                var panoramaRemoved = await ExecuteCleanupOperationAsync(
                        "最早全景图片",
                        _cacheCleanupService.DeleteEarliestPanoramaImages)
                    .ConfigureAwait(false);
                var scanRemoved = await ExecuteCleanupOperationAsync(
                        "最早扫码图片",
                        _cacheCleanupService.DeleteEarliestScanImages)
                    .ConfigureAwait(false);
                if (!panoramaRemoved && !scanRemoved)
                {
                    Logger.Error($"图片盘空间不足且已没有可清理图片。盘符:{driveName}");
                    return;
                }
            }

            if (await IsDiskSpaceLowAsync(driveName, minimumSpaceBytes).ConfigureAwait(false))
            {
                Logger.Error($"图片盘完成最大批次清理后仍低于保留水位。盘符:{driveName}");
            }
        }

        /// <summary>
        /// 判断指定固定磁盘的剩余空间是否不高于安全水位。
        /// </summary>
        /// <param name="driveName">不含分隔符的盘符。</param>
        /// <param name="minimumSpaceBytes">最低保留空间。</param>
        /// <returns>磁盘存在且剩余空间不足时为 <see langword="true"/>。</returns>
        private async Task<bool> IsDiskSpaceLowAsync(string driveName, long minimumSpaceBytes)
        {
            if (string.IsNullOrWhiteSpace(driveName))
            {
                return false;
            }

            var diskInfos = await _computer.GetDiskInfoAsync().ConfigureAwait(false);
            var diskInfo = diskInfos.FirstOrDefault(item =>
                string.Equals(item.Name, driveName, StringComparison.OrdinalIgnoreCase));
            return diskInfo is { UsedDiskSpace: > 0 } &&
                   diskInfo.AvailableDiskSpace <= minimumSpaceBytes;
        }

        /// <summary>
        /// 执行清理操作并统一记录失败原因。
        /// </summary>
        /// <param name="operationName">便于诊断的操作名称。</param>
        /// <param name="operation">清理操作。</param>
        /// <returns>清理操作是否成功。</returns>
        private static async Task<bool> ExecuteCleanupOperationAsync(
            string operationName,
            Func<Task<KeyValuePair<bool, string>>> operation)
        {
            var result = await operation().ConfigureAwait(false);
            if (!result.Key)
            {
                Logger.Warn($"{operationName}清理未完成:{result.Value}");
            }

            return result.Key;
        }

        /// <summary>
        /// 将路径根目录转换成硬盘监控使用的不含冒号和分隔符的盘符。
        /// </summary>
        /// <param name="pathRoot">路径根目录。</param>
        /// <returns>标准化盘符。</returns>
        private static string NormalizeDriveName(string? pathRoot) =>
            pathRoot?.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .TrimEnd(':') ?? string.Empty;

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
                    var imageSettings = await _settingsStore
                        .GetAsync<ImageSettingsDto>(settingsName, cancellationToken)
                        .ConfigureAwait(false) ?? new ImageSettingsDto();
                    var imagePathRoot = string.IsNullOrWhiteSpace(imageSettings.ImageRootDirectory)
                        ? string.Empty
                        : Path.GetPathRoot(imageSettings.ImageRootDirectory) ?? string.Empty;
                    Volatile.Write(ref _imagePathRoot, imagePathRoot);
                }
                else
                {
                    var cacheSettings = await _settingsStore
                        .GetAsync<CacheClearSettingsDto>(settingsName, cancellationToken)
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
