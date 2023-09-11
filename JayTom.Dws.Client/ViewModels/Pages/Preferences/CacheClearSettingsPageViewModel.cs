using System;
using System.IO;
using NPOI.HPSF;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Ftp;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using NetTopologySuite.Algorithm;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.CacheClearSettings;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class CacheClearSettingsPageViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;
        private readonly IComputer _computer;
        private readonly IFtp _ftp;
        private bool _isSameDiskStorage;
        private FtpUsageInfo _ftpUsageInfo = new();
        private LocalDiskUsageInfo _localDiskUsageInfo = new();
        private CacheClearSettingsInfoModel _autoCleanupParams = new();
        private CacheClearSettingsInfoModel _manualCleanupParams = new();
        private long _minimumSpaceRetention;
        private bool _isDeletingInProgress;
        private bool _isSavingInProgress;
        private SnackbarMessageQueue _cacheClearSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private ImageSettingsDto? _imageSettingsDto;
        private bool _isShowingFtpSpaceInfo;

        public CacheClearSettingsPageViewModel(IConfigRepository configRepository,
            IComputer computer, IFtp ftp) {
            _configRepository = configRepository;
            _computer = computer;
            _ftp = ftp;
        }

        public SnackbarMessageQueue CacheClearSettingsMessageQueue {
            get => _cacheClearSettingsMessageQueue;
            set => SetProperty(ref _cacheClearSettingsMessageQueue, value);
        }

        /// <summary>
        /// 是否删除中
        /// </summary>
        public bool IsDeletingInProgress {
            get => _isDeletingInProgress;
            set => SetProperty(ref _isDeletingInProgress, value);
        }

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        /// <summary>
        /// 是否同一磁盘存储
        /// </summary>
        public bool IsSameDiskStorage {
            get => _isSameDiskStorage;
            set => SetProperty(ref _isSameDiskStorage, value);
        }

        /// <summary>
        /// 是否显示Ftp空间占用
        /// </summary>
        public bool IsShowingFtpSpaceInfo {
            get => _isShowingFtpSpaceInfo;
            set => SetProperty(ref _isShowingFtpSpaceInfo, value);
        }

        /// <summary>
        /// Ftp占用信息
        /// </summary>
        public FtpUsageInfo FtpUsageInfo {
            get => _ftpUsageInfo;
            set => SetProperty(ref _ftpUsageInfo, value);
        }

        /// <summary>
        /// 本地磁盘占用信息
        /// </summary>
        public LocalDiskUsageInfo LocalDiskUsageInfo {
            get => _localDiskUsageInfo;
            set => SetProperty(ref _localDiskUsageInfo, value);
        }

        /// <summary>
        /// 最小空间保留（以MB为单位）
        /// </summary>
        public long MinimumSpaceRetention {
            get => _minimumSpaceRetention;
            set => SetProperty(ref _minimumSpaceRetention, value);
        }

        /// <summary>
        /// 自动清理参数
        /// </summary>
        public CacheClearSettingsInfoModel AutoCleanupParams {
            get => _autoCleanupParams;
            set => SetProperty(ref _autoCleanupParams, value);
        }

        /// <summary>
        /// 手动清理参数
        /// </summary>
        public CacheClearSettingsInfoModel ManualCleanupParams {
            get => _manualCleanupParams;
            set => SetProperty(ref _manualCleanupParams, value);
        }

        /// <summary>
        /// 手动清理方法
        /// </summary>
        public ICommand ManualCleanupCommand {
            get => new DelegateCommand<string>(ManualCleanupDelegate);
        }

        private void ManualCleanupDelegate(string obj) {
            Debug.WriteLine(obj);

            if (obj.Equals("BarcodeData")) {
                //删除指定天数之前的条码数据
            }
            else if (obj.Equals("ScanImage")) {
                //删除指定天数之前的扫码图片
            }
            else if (obj.Equals("PanoramaImage")) {
                //删除指定天数之前的全景图片
            }
            else if (obj.Equals("FtpImage")) {
                //删除指定天数之前的FTP
            }
            else if (obj.Equals("LogData")) {
                //删除指定天数之前的日志
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public ICommand SaveSettingsCommand {
            get => new DelegateCommand<object>(SaveSettingDelegate);
        }

        private async void SaveSettingDelegate(object obj) {
            if (!IsSavingInProgress) {
                IsSavingInProgress = true;

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                        ConfigName = "CacheClearSettings",
                        Value = JsonConvert.SerializeObject(new CacheClearSettingsDto() {
                            BarcodeDataAgoDays = AutoCleanupParams.BarcodeDataAgoDays,
                            FtpImageAgoDays = AutoCleanupParams.FtpImageAgoDays,
                            LogDataAgoDays = AutoCleanupParams.LogDataAgoDays,
                            MinimumSpaceRetention = MinimumSpaceRetention,
                            PanoramaImageAgoDays = AutoCleanupParams.PanoramaImageAgoDays,
                            ScanImageAgoDays = AutoCleanupParams.ScanImageAgoDays
                        })
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "CacheClearSettings"
                        });
                    }
                    IsSavingInProgress = false;
                    CacheClearSettingsMessageQueue.Enqueue($"保存{(insertOrUpdate ? "成功" : "失败")}");
                });
            }
        }

        /// <summary>
        /// 页面加载完成
        /// </summary>
        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private void LoadedDelegate(object obj) {
            Task.Run(async () => {
                //判断是否在同一个
                LocalDiskUsageInfo localDiskUsageInfo = new();
                FtpUsageInfo ftpUsageInfo = new();
                var orDefault = await _configRepository.
                    FirstOrDefault(f => f.ConfigName.Equals("SaveImageSettings"));
                if (orDefault is not null) {
                    try {
                        _imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(orDefault.Value);
                        if (_imageSettingsDto is not null) {
                            if (!string.IsNullOrEmpty(_imageSettingsDto?.ImageRootDirectory)) {
                                var pathRoot = Path.GetPathRoot(_imageSettingsDto.ImageRootDirectory);
                                IsSameDiskStorage = string.Equals(pathRoot, Path.GetPathRoot(Directory.GetCurrentDirectory()), StringComparison.OrdinalIgnoreCase) &&
                                                    Directory.Exists($"{_imageSettingsDto?.ImageRootDirectory}\\BarcodeImage") &&
                                                    Directory.Exists($"{_imageSettingsDto?.ImageRootDirectory}\\PanoramaImage");
                            }

                            if (!_ftp.IsConnected) {
                                var (key, value) = await _ftp.Connect(_imageSettingsDto.FtpInfo.IpAddress, _imageSettingsDto.FtpInfo.Username,
                                    _imageSettingsDto.FtpInfo.Password);
                                IsShowingFtpSpaceInfo = (key && await _ftp.DirectoryExists("BarcodeImage") &&
                                                         await _ftp.DirectoryExists("PanoramaImage"));
                            }
                        }
                    }
                    catch {
                        //
                    }
                }

                if (IsSameDiskStorage) {
                    localDiskUsageInfo = GetDiskUsageInfo();
                }

                //判断FTP是否连接
                if (IsShowingFtpSpaceInfo) {
                    ftpUsageInfo = await GetFtpUsageInfo();
                }
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    if (IsSameDiskStorage) {
                        double progress = 0;
                        LocalDiskUsageInfo = new LocalDiskUsageInfo() {
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
                    if (IsShowingFtpSpaceInfo) {
                        double progress = 0;

                        FtpUsageInfo = new FtpUsageInfo() {
                            DiskUsagePercentage = ftpUsageInfo.DiskUsagePercentage,
                            DataUsagePercentage = progress += ftpUsageInfo.DataUsagePercentage,
                            ScanImageUsagePercentage = progress += ftpUsageInfo.ScanImageUsagePercentage,
                            PanoramaImageUsagePercentage = progress += ftpUsageInfo.PanoramaImageUsagePercentage,
                            OtherUsagePercentage = progress += ftpUsageInfo.OtherUsagePercentage,
                        };
                        NLog.LogManager.GetCurrentClassLogger().Error(JsonConvert.SerializeObject(FtpUsageInfo));
                    }
                });
                var configInfoModel = await _configRepository.
                    FirstOrDefault(f =>
                        f.ConfigName.Equals("CacheClearSettings"));
                if (configInfoModel != null) {
                    try {
                        var cacheClearSettingsDto = JsonConvert.DeserializeObject<CacheClearSettingsDto>(configInfoModel.Value);

                        if (cacheClearSettingsDto is not null) {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                                AutoCleanupParams = new CacheClearSettingsInfoModel() {
                                    BarcodeDataAgoDays = cacheClearSettingsDto.BarcodeDataAgoDays,
                                    FtpImageAgoDays = cacheClearSettingsDto.FtpImageAgoDays,
                                    LogDataAgoDays = cacheClearSettingsDto.LogDataAgoDays,
                                    PanoramaImageAgoDays = cacheClearSettingsDto.PanoramaImageAgoDays,
                                    ScanImageAgoDays = cacheClearSettingsDto.ScanImageAgoDays
                                };
                            });
                        }
                    }
                    catch (Exception e) {
                    }
                }
            });
        }

        private LocalDiskUsageInfo GetDiskUsageInfo() {
            var localDiskUsageInfo = new LocalDiskUsageInfo();
            var firstOrDefault = _computer.GetDiskInfo()?.FirstOrDefault(w =>
                w.Name.Equals(Path.GetPathRoot(Directory.GetCurrentDirectory())?.Replace(":\\", string.Empty)));
            if (firstOrDefault is not null) {
                localDiskUsageInfo.DiskUsagePercentage = (double)firstOrDefault.UsedDiskSpacePercentage;
                localDiskUsageInfo.UsedBytes = firstOrDefault.UsedDiskSpace;

                //获取本地磁盘信息
                //获取已用空间百分比
                //获取已用空间字节数
                //获取数据(data.db文件大小)
                var dbFileName = $"{AppDomain.CurrentDomain.BaseDirectory}\\data.db";
                if (File.Exists(dbFileName)) {
                    var length = new FileInfo(dbFileName).Length;
                    var space = (double)length / firstOrDefault.UsedDiskSpace;
                    localDiskUsageInfo.DataUsagePercentage = Math.Round(space, 2);
                }
                //获取扫码文件夹数据总大小

                var barcodeImageDirectory = $"{_imageSettingsDto?.ImageRootDirectory}\\BarcodeImage";
                if (Directory.Exists(barcodeImageDirectory)) {
                    var totalSizeInBytes = Directory.EnumerateFiles(barcodeImageDirectory)
                        .AsParallel()
                        .Select(filePath => new FileInfo(filePath).Length)
                        .Sum();
                    var space = (double)totalSizeInBytes / firstOrDefault.UsedDiskSpace;
                    localDiskUsageInfo.ScanImageUsagePercentage = Math.Round(space, 2);
                }
                //获取全景图片文件夹数据总大小
                var panoramaImageDirectory = $"{_imageSettingsDto?.ImageRootDirectory}\\PanoramaImage";
                if (Directory.Exists(barcodeImageDirectory)) {
                    var totalSizeInBytes = Directory.EnumerateFiles(panoramaImageDirectory)
                        .AsParallel()
                        .Select(filePath => new FileInfo(filePath).Length)
                        .Sum();
                    var space = (double)totalSizeInBytes / firstOrDefault.UsedDiskSpace;
                    localDiskUsageInfo.PanoramaImageUsagePercentage = Math.Round(space, 2);
                }
                //获取日志文件(log.db文件大小,目前没有填0)
                var logFileName = $"{AppDomain.CurrentDomain.BaseDirectory}\\log.db";
                if (File.Exists(dbFileName)) {
                    var length = new FileInfo(dbFileName).Length;
                    var space = (double)length / firstOrDefault.UsedDiskSpace;
                    localDiskUsageInfo.LogFileUsagePercentage = Math.Round(space, 2);
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

        private async Task<FtpUsageInfo> GetFtpUsageInfo() {
            var info = new FtpUsageInfo();

            var ftpDiskInfo = await _ftp.GetDiskUsage();

            if (ftpDiskInfo is not null) {
                info.UsedBytes = ftpDiskInfo.UsedSize;
                var directorySize = await _ftp.GetDirectorySize("PanoramaImage");
                info.PanoramaImageUsagePercentage = Math.Round((double)directorySize / info.UsedBytes, 2);
                var size = await _ftp.GetDirectorySize("BarcodeImage");
                info.ScanImageUsagePercentage = Math.Round((double)size / info.UsedBytes, 2);
                info.DataUsagePercentage = Math.Round((double)(ftpDiskInfo.UsedSize / (ftpDiskInfo.TotalSize > 0 ? ftpDiskInfo.TotalSize : 1)), 2);
            }

            return info;
        }
    }
}