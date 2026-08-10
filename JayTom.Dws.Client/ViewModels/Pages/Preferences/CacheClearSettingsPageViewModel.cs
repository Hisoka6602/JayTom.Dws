using JayTom.Dws.Application.Configuration;
using System;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Ftp;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.CacheCleanup;
using JayTom.Dws.Client.Models.CacheClearSettings;
using JayTom.Dws.Infrastructure.Repository.LocalConf;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences
{

    public class CacheClearSettingsPageViewModel : SettingsPageTemplateViewModel
    {
        private readonly IComputer _computer;
        private readonly IFtp _ftp;
        private readonly ICacheCleanupService _cacheCleanupService;
        private bool _isSameDiskStorage;
        private FtpUsageInfo _ftpUsageInfo = new();
        private LocalDiskUsageInfo _localDiskUsageInfo = new();
        private CacheClearSettingsInfoModel _autoCleanupParams = new();
        private CacheClearSettingsInfoModel _manualCleanupParams = new();
        private long _minimumSpaceRetention;
        private bool _isDeletingInProgress;
        private ImageSettingsDto? _imageSettingsDto;
        private bool _isShowingFtpSpaceInfo;

        public CacheClearSettingsPageViewModel(ISettingsStore settingsStore,
            IComputer computer, IFtp ftp, ICacheCleanupService cacheCleanupService) : base(settingsStore)
        {
            _computer = computer;
            _ftp = ftp;
            _cacheCleanupService = cacheCleanupService;
        }

        /// <summary>
        /// 是否删除中
        /// </summary>
        public bool IsDeletingInProgress
        {
            get => _isDeletingInProgress;
            set => SetProperty(ref _isDeletingInProgress, value);
        }

        /// <summary>
        /// 是否同一磁盘存储
        /// </summary>
        public bool IsSameDiskStorage
        {
            get => _isSameDiskStorage;
            set => SetProperty(ref _isSameDiskStorage, value);
        }

        /// <summary>
        /// 是否显示Ftp空间占用
        /// </summary>
        public bool IsShowingFtpSpaceInfo
        {
            get => _isShowingFtpSpaceInfo;
            set => SetProperty(ref _isShowingFtpSpaceInfo, value);
        }

        /// <summary>
        /// Ftp占用信息
        /// </summary>
        public FtpUsageInfo FtpUsageInfo
        {
            get => _ftpUsageInfo;
            set => SetProperty(ref _ftpUsageInfo, value);
        }

        /// <summary>
        /// 本地磁盘占用信息
        /// </summary>
        public LocalDiskUsageInfo LocalDiskUsageInfo
        {
            get => _localDiskUsageInfo;
            set => SetProperty(ref _localDiskUsageInfo, value);
        }

        /// <summary>
        /// 最小空间保留（以MB为单位）
        /// </summary>
        public long MinimumSpaceRetention
        {
            get => _minimumSpaceRetention;
            set => SetProperty(ref _minimumSpaceRetention, value);
        }

        /// <summary>
        /// 自动清理参数
        /// </summary>
        public CacheClearSettingsInfoModel AutoCleanupParams
        {
            get => _autoCleanupParams;
            set => SetProperty(ref _autoCleanupParams, value);
        }

        /// <summary>
        /// 手动清理参数
        /// </summary>
        public CacheClearSettingsInfoModel ManualCleanupParams
        {
            get => _manualCleanupParams;
            set => SetProperty(ref _manualCleanupParams, value);
        }

        /// <summary>
        /// 手动清理方法
        /// </summary>
        public ICommand ManualCleanupCommand
        {
            get => new DelegateCommand<string>(ManualCleanupDelegate);
        }

        private void ManualCleanupDelegate(string obj)
        {
            switch (obj)
            {
                case "BarcodeData":
                    //删除指定天数之前的条码数据
                    Task.Run(async () =>
                    {
                        if (!IsDeletingInProgress && ManualCleanupParams.BarcodeDataAgoDays > 0)
                        {
                            IsDeletingInProgress = true;
                            var (key, value) =
                                await _cacheCleanupService.DeleteBarcodeDataOlderThanDays(
                                    ManualCleanupParams.BarcodeDataAgoDays);

                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                base.MessageQueue.Enqueue(key ? Languages.Language.ResourceManager.GetString("删除成功") ?? string.Empty
                                    : $"{Languages.Language.ResourceManager.GetString("删除失败") ?? string.Empty},{value}");
                            });
                            IsDeletingInProgress = false;
                        }
                    });
                    break;

                case "ScanImage":
                    //删除指定天数之前的扫码图片
                    Task.Run(async () =>
                    {
                        if (!IsDeletingInProgress && ManualCleanupParams.ScanImageAgoDays > 0)
                        {
                            IsDeletingInProgress = true;
                            var (key, value) =
                                await _cacheCleanupService.DeleteScanImagesOlderThanDays(ManualCleanupParams
                                    .ScanImageAgoDays);

                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                base.MessageQueue.Enqueue(key ? Languages.Language.ResourceManager.GetString("删除成功") ?? string.Empty
                                    : $"{Languages.Language.ResourceManager.GetString("删除失败") ?? string.Empty},{value}");
                            });
                            IsDeletingInProgress = false;
                        }
                    });
                    break;

                case "PanoramaImage":
                    //删除指定天数之前的全景图片
                    Task.Run(async () =>
                    {
                        if (!IsDeletingInProgress && ManualCleanupParams.PanoramaImageAgoDays > 0)
                        {
                            IsDeletingInProgress = true;
                            var (key, value) =
                                await _cacheCleanupService.DeletePanoramaImagesOlderThanDays(ManualCleanupParams.PanoramaImageAgoDays);
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                base.MessageQueue.Enqueue(key ? Languages.Language.ResourceManager.GetString("删除成功") ?? string.Empty
                                    : $"{Languages.Language.ResourceManager.GetString("删除失败") ?? string.Empty},{value}");
                            });
                            IsDeletingInProgress = false;
                        }
                    });
                    break;

                case "FtpImage":
                    //删除指定天数之前的FTP
                    Task.Run(async () =>
                    {
                        if (!IsDeletingInProgress && ManualCleanupParams.FtpImageAgoDays > 0)
                        {
                            IsDeletingInProgress = true;
                            var (key, value) =
                                await _cacheCleanupService.DeleteFtpImagesOlderThanDays(ManualCleanupParams.FtpImageAgoDays);
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                base.MessageQueue.Enqueue(key ? Languages.Language.ResourceManager.GetString("删除成功") ?? string.Empty
                                    : $"{Languages.Language.ResourceManager.GetString("删除失败") ?? string.Empty},{value}");
                            });
                            IsDeletingInProgress = false;
                        }
                    });
                    break;

                case "LogData":
                    //删除指定天数之前的日志
                    Task.Run(async () =>
                    {
                        if (!IsDeletingInProgress && ManualCleanupParams.LogDataAgoDays > 0)
                        {
                            IsDeletingInProgress = true;
                            var (key, value) =
                                await _cacheCleanupService.DeleteLogDataOlderThanDays(ManualCleanupParams.LogDataAgoDays);
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                base.MessageQueue.Enqueue(key ? Languages.Language.ResourceManager.GetString("删除成功") ?? string.Empty
                                    : $"{Languages.Language.ResourceManager.GetString("删除失败") ?? string.Empty},{value}");
                            });
                            IsDeletingInProgress = false;
                        }
                    });
                    break;
            }
        }

        public override string Identifier => "CacheClearSettingsDialogHost";
        public override string SettingsName => "CacheClearSettings";

        protected override async Task<bool> SaveSettingsProcess()
        {
            var insertOrUpdate = await _settingsStore.SaveAsync(SettingsName,new CacheClearSettingsDto()
                {
                    BarcodeDataAgoDays = AutoCleanupParams.BarcodeDataAgoDays,
                    FtpImageAgoDays = AutoCleanupParams.FtpImageAgoDays,
                    LogDataAgoDays = AutoCleanupParams.LogDataAgoDays,
                    MinimumSpaceRetention = MinimumSpaceRetention,
                    PanoramaImageAgoDays = AutoCleanupParams.PanoramaImageAgoDays,
                    ScanImageAgoDays = AutoCleanupParams.ScanImageAgoDays
                });

            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public override void LoadedDelegate(object obj)
        {
            Task.Run(async () =>
            {
                LocalDiskUsageInfo localDiskUsageInfo = new();
                FtpUsageInfo ftpUsageInfo = new();
                _imageSettingsDto = await _settingsStore.GetAsync<ImageSettingsDto>("SaveImageSettings");
                if (_imageSettingsDto is not null)
                {
                    if (!string.IsNullOrEmpty(_imageSettingsDto.ImageRootDirectory))
                    {
                        var pathRoot = Path.GetPathRoot(_imageSettingsDto.ImageRootDirectory);
                        IsSameDiskStorage = string.Equals(pathRoot, Path.GetPathRoot(System.Diagnostics.Process.GetCurrentProcess()?.MainModule?.FileName), StringComparison.OrdinalIgnoreCase) &&
                                            Directory.Exists($"{_imageSettingsDto.ImageRootDirectory}\\BarcodeImage") &&
                                            Directory.Exists($"{_imageSettingsDto.ImageRootDirectory}\\PanoramaImage");
                    }

                    if (!_ftp.IsConnected)
                    {
                        var (key, value) = await _ftp.Connect(_imageSettingsDto.FtpInfo.IpAddress, _imageSettingsDto.FtpInfo.Port, _imageSettingsDto.FtpInfo.Username,
                            _imageSettingsDto.FtpInfo.Password);
                        IsShowingFtpSpaceInfo = (key && await _ftp.DirectoryExists("BarcodeImage") &&
                                                 await _ftp.DirectoryExists("PanoramaImage"));
                    }
                }
                if (IsSameDiskStorage)
                {
                    localDiskUsageInfo = GetDiskUsageInfo();
                }

                //判断FTP是否连接
                if (IsShowingFtpSpaceInfo)
                {
                    ftpUsageInfo = await GetFtpUsageInfo();
                }
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (IsSameDiskStorage)
                    {
                        double progress = 0;
                        LocalDiskUsageInfo = new LocalDiskUsageInfo()
                        {
                            DiskUsagePercentage = localDiskUsageInfo.DiskUsagePercentage,
                            UsedBytes = localDiskUsageInfo.UsedBytes,
                            DataUsagePercentage = progress += localDiskUsageInfo.DataUsagePercentage,
                            ScanImageUsagePercentage = progress += localDiskUsageInfo.ScanImageUsagePercentage,
                            PanoramaImageUsagePercentage = progress += localDiskUsageInfo.PanoramaImageUsagePercentage,
                            LogFileUsagePercentage = progress += localDiskUsageInfo.LogFileUsagePercentage,
                            OtherUsagePercentage = progress += localDiskUsageInfo.OtherUsagePercentage,
                        };
                    }
                    //判断FTP是否连接
                    if (IsShowingFtpSpaceInfo)
                    {
                        double progress = 0;

                        FtpUsageInfo = new FtpUsageInfo()
                        {
                            DiskUsagePercentage = ftpUsageInfo.DiskUsagePercentage,
                            DataUsagePercentage = progress += ftpUsageInfo.DataUsagePercentage,
                            ScanImageUsagePercentage = progress += ftpUsageInfo.ScanImageUsagePercentage,
                            PanoramaImageUsagePercentage = progress += ftpUsageInfo.PanoramaImageUsagePercentage,
                            OtherUsagePercentage = progress += ftpUsageInfo.OtherUsagePercentage,
                        };
                        //NLog.LogManager.GetCurrentClassLogger().Error(JsonConvert.SerializeObject(FtpUsageInfo));
                    }
                });

                var cacheClearSettingsDto = await _settingsStore.GetAsync<CacheClearSettingsDto>(SettingsName);
                if (cacheClearSettingsDto is not null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        AutoCleanupParams = new CacheClearSettingsInfoModel()
                        {
                            BarcodeDataAgoDays = cacheClearSettingsDto.BarcodeDataAgoDays,
                            FtpImageAgoDays = cacheClearSettingsDto.FtpImageAgoDays,
                            LogDataAgoDays = cacheClearSettingsDto.LogDataAgoDays,
                            PanoramaImageAgoDays = cacheClearSettingsDto.PanoramaImageAgoDays,
                            ScanImageAgoDays = cacheClearSettingsDto.ScanImageAgoDays
                        };
                        MinimumSpaceRetention = cacheClearSettingsDto.MinimumSpaceRetention;
                    });
                }
            });
        }

        private LocalDiskUsageInfo GetDiskUsageInfo()
        {
            var localDiskUsageInfo = new LocalDiskUsageInfo();
            var firstOrDefault = _computer.GetDiskInfo()?.FirstOrDefault(w =>
                w.Name.Equals(Path.GetPathRoot(Directory.GetCurrentDirectory())?.Replace(":\\", string.Empty)));
            if (firstOrDefault is not null)
            {
                localDiskUsageInfo.DiskUsagePercentage = (double)firstOrDefault.UsedDiskSpacePercentage;
                localDiskUsageInfo.UsedBytes = firstOrDefault.UsedDiskSpace;

                //获取本地磁盘信息
                //获取已用空间百分比
                //获取已用空间字节数
                //获取数据(data.db文件大小)
                var dbFileName = $"{AppDomain.CurrentDomain.BaseDirectory}\\data.db";
                if (File.Exists(dbFileName))
                {
                    var length = new FileInfo(dbFileName).Length;
                    var space = (double)length / firstOrDefault.UsedDiskSpace;
                    localDiskUsageInfo.DataUsagePercentage = space;
                }
                //获取扫码文件夹数据总大小

                var barcodeImageDirectory = $"{_imageSettingsDto?.ImageRootDirectory}\\BarcodeImage";
                if (Directory.Exists(barcodeImageDirectory))
                {
                    var totalSizeInBytes = Directory.GetFiles(barcodeImageDirectory, "*", SearchOption.AllDirectories)
                        .AsParallel()
                        .Select(file => new FileInfo(file).Length)
                        .Sum();
                    var space = (double)totalSizeInBytes / firstOrDefault.UsedDiskSpace;
                    localDiskUsageInfo.ScanImageUsagePercentage = space;
                }
                //获取全景图片文件夹数据总大小
                var panoramaImageDirectory = $"{_imageSettingsDto?.ImageRootDirectory}\\PanoramaImage";
                if (Directory.Exists(panoramaImageDirectory))
                {
                    var totalSizeInBytes = Directory.GetFiles(panoramaImageDirectory, "*", SearchOption.AllDirectories)
                        .AsParallel()
                        .Select(file => new FileInfo(file).Length)
                        .Sum();
                    var space = (double)totalSizeInBytes / firstOrDefault.UsedDiskSpace;
                    localDiskUsageInfo.PanoramaImageUsagePercentage = space;
                }
                //获取日志文件(log.db文件大小,目前没有填0)
                var logFileName = $"{AppDomain.CurrentDomain.BaseDirectory}\\log.db";
                if (File.Exists(logFileName))
                {
                    var length = new FileInfo(logFileName).Length;
                    var space = (double)length / firstOrDefault.UsedDiskSpace;
                    localDiskUsageInfo.LogFileUsagePercentage = space;
                }

                var otherUsage = (double)firstOrDefault.UsedDiskSpacePercentage / 100 - (localDiskUsageInfo.LogFileUsagePercentage +
                                                                     localDiskUsageInfo.PanoramaImageUsagePercentage +
                                                                     localDiskUsageInfo.ScanImageUsagePercentage +
                                                                     localDiskUsageInfo.DataUsagePercentage);

                localDiskUsageInfo.OtherUsagePercentage = otherUsage;
                //剩下的就是其他占用
            }

            return localDiskUsageInfo;
        }

        private async Task<FtpUsageInfo> GetFtpUsageInfo()
        {
            var info = new FtpUsageInfo();

            var ftpDiskInfo = await _ftp.GetDiskUsage();

            if (ftpDiskInfo is not null)
            {
                info.UsedBytes = ftpDiskInfo.UsedSize;
                var directorySize = await _ftp.GetDirectorySize("PanoramaImage");
                info.PanoramaImageUsagePercentage = (double)directorySize / info.UsedBytes;
                var size = await _ftp.GetDirectorySize("BarcodeImage");
                info.ScanImageUsagePercentage = (double)size / info.UsedBytes;
                info.DataUsagePercentage = (double)(ftpDiskInfo.UsedSize / (ftpDiskInfo.TotalSize > 0 ? ftpDiskInfo.TotalSize : 1));
            }

            return info;
        }
    }
}