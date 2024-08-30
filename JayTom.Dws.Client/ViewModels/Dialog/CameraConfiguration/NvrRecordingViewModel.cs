using System;
using NetSDKCS;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Plugin.Speech;
using System.Windows.Threading;
using NPOI.SS.Formula.Functions;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using System.Windows.Controls.Primitives;
using JayTom.Dws.Client.Models.DataModels;
using JayTom.Dws.Domain.Dto.LocalVideoDto;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.VideoSettingModel;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Client.Attributes.WinClientAttributes;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Client.Models.Cameras.CameraConfiguration;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.NVR;
using static JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.NVR.DaHuatechNVR;

namespace JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration {

    public class NvrRecordingViewModel : BindableBase {
        private readonly INvrCameraBindingRepository _nvrCameraBindingRepository;
        private readonly IPackageRepository _packageRepository;
        private readonly IConfigRepository _configRepository;
        private ObservableCollection<VideoPlayerModel> _videoPlayerItems = new();

        //private BaseDaHuatech? _baseDaHuatech;
        private DaHuatechNVR? _daHuatechNvr;

        private string _identifier = string.Empty;
        private DateTime _startTime = DateTime.Now.AddMinutes(-10);
        private DateTime _endTime = DateTime.Now.AddMinutes(-5);
        private DateTime _currentTime;
        private DateTime? _selectionStartTime;
        private DateTime? _selectionEndTime;
        private ObservableCollection<PlaybackStream> _playbackStreamItems = new(Enum.GetValues(typeof(PlaybackStream)).Cast<PlaybackStream>());
        private PlaybackStream _selectPlaybackStream = PlaybackStream.MainStream;
        private string? _fastForwardSpeed;
        private string? _slowMotionSpeed;
        private PlaybackState _playbackState = PlaybackState.Ready;
        private DateTime _playDateTime;
        private int _loadedSize = 0;

        private readonly DaHuatechNVR.FastForwardSpeed[] _fastForwardSpeeds =
            { DaHuatechNVR.FastForwardSpeed.X2,
                DaHuatechNVR.FastForwardSpeed.X4,
                DaHuatechNVR.FastForwardSpeed.X8,
                DaHuatechNVR.FastForwardSpeed.X16,
                DaHuatechNVR.FastForwardSpeed.Normal
            };

        private int _currentSpeedIndex = 0;

        // 定义慢放速度数组
        private readonly DaHuatechNVR.SlowSpeed[] _slowMotionSpeeds = {
            DaHuatechNVR.SlowSpeed.X2,
            DaHuatechNVR.SlowSpeed.X4,
            DaHuatechNVR.SlowSpeed.X8,
            DaHuatechNVR.SlowSpeed.X16,
            DaHuatechNVR.SlowSpeed.Normal,
        };

        // 当前慢放速度索引
        private int _currentSlowMotionSpeedIndex = 0;

        private double _speed = 1;
        private PackageItemModel _packageItemModel = new();
        private VideoPlaybackSettingsInfoModel _videoPlaybackSettingsInfo = new();

        public NvrRecordingViewModel(INvrCameraBindingRepository nvrCameraBindingRepository,
            IPackageRepository packageRepository,
            IConfigRepository configRepository) {
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
            _packageRepository = packageRepository;
            _configRepository = configRepository;
        }

        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public VideoPlaybackSettingsInfoModel VideoPlaybackSettingsInfo {
            get => _videoPlaybackSettingsInfo;
            set => SetProperty(ref _videoPlaybackSettingsInfo, value);
        }

        public PackageItemModel PackageItemModel {
            get => _packageItemModel;
            set => SetProperty(ref _packageItemModel, value);
        }

        public bool ProgressRelease { get; private set; }

        public ObservableCollection<VideoPlayerModel> VideoPlayerItems {
            get => _videoPlayerItems;
            set => SetProperty(ref _videoPlayerItems, value);
        }

        public ObservableCollection<PlaybackStream> PlaybackStreamItems {
            get => _playbackStreamItems;
            set => SetProperty(ref _playbackStreamItems, value);
        }

        public PlaybackStream SelectPlaybackStream {
            get => _selectPlaybackStream;
            set => SetProperty(ref _selectPlaybackStream, value);
        }

        public DateTime StartTime {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DateTime EndTime {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public DateTime CurrentTime {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public DateTime PlayDateTime {
            get => _playDateTime;
            set => SetProperty(ref _playDateTime, value);
        }

        public DateTime? SelectionStartTime {
            get => _selectionStartTime;
            set => SetProperty(ref _selectionStartTime, value);
        }

        public DateTime? SelectionEndTime {
            get => _selectionEndTime;
            set => SetProperty(ref _selectionEndTime, value);
        }

        /// <summary>
        /// 快进倍数
        /// </summary>
        public string? FastForwardSpeed {
            get => _fastForwardSpeed;
            set => SetProperty(ref _fastForwardSpeed, value);
        }

        /// <summary>
        /// 慢放倍数
        /// </summary>
        public string? SlowMotionSpeed {
            get => _slowMotionSpeed;
            set => SetProperty(ref _slowMotionSpeed, value);
        }

        public double Speed {
            get => _speed;
            set => SetProperty(ref _speed, value);
        }

        public PlaybackState PlaybackState {
            get => _playbackState;
            set => SetProperty(ref _playbackState, value);
        }

        public ICommand ToggleImageSizeCommand => new DelegateCommand<VideoPlayerModel>(ToggleImageSizeDelegate);

        private async void ToggleImageSizeDelegate(VideoPlayerModel obj) {
            if (obj.ScreenState == ScreenState.Normal) {
                foreach (var videoPlayerModel in VideoPlayerItems) {
                    videoPlayerModel.ScreenState = !videoPlayerModel.Equals(obj) ? ScreenState.Hidden : ScreenState.Maximized;
                }
                if (_daHuatechNvr is not null) {
                    obj.VideoFrame = new WriteableBitmap((int)obj.MaxSize.Width,
                        (int)obj.MaxSize.Height, 96, 96, PixelFormats.Bgr24, null);
                    _daHuatechNvr.SetResolution(obj.IpAddress, obj.Channel, (int)obj.MaxSize.Width,
                        (int)obj.MaxSize.Height);
                }
            }
            else {
                var size = GetVideoPlayerSize();

                foreach (var videoPlayerModel in VideoPlayerItems) {
                    videoPlayerModel.ScreenState = ScreenState.Normal;
                    if (_daHuatechNvr is not null) {
                        obj.VideoFrame = new WriteableBitmap((int)size.Width,
                            (int)size.Height, 96, 96, PixelFormats.Bgr24, null);
                        _daHuatechNvr.SetResolution(obj.IpAddress, obj.Channel, (int)size.Width,
                            (int)size.Height);
                    }
                }
            }
        }

        private Size GetVideoPlayerSize() {
            var size = new Size(768, 432);
            switch (VideoPlayerItems.Count) {
                case 1:
                    size = new Size(768, 432);
                    break;

                case > 1 and <= 4:
                    size = new Size(614, 346);
                    break;

                case > 4:
                    size = new Size(449, 253);
                    break;
            }
            return size;
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            if (PackageItemModel.TimestampedGuid <= 0) {
                return;
            }

            var settingsDto = await _configRepository.FirstOrDefaultEntity<VideoPlaybackSettingsDto>("VideoPlaybackSettings") ?? new VideoPlaybackSettingsDto();
            VideoPlaybackSettingsInfo = new VideoPlaybackSettingsInfoModel() {
                IsWatermarkTimeMarked = settingsDto.IsWatermarkTimeMarked,
                SecondsToSubtract = settingsDto.SecondsToSubtract,
                VideoLengthInSeconds = settingsDto.VideoLengthInSeconds,
            };

            var (b, packageInfoModel) = await _packageRepository.FirstOrDefaultInfo(f => f.PackageTimestamped.Equals(PackageItemModel.TimestampedGuid));

            var serialNumber = packageInfoModel?.BarCodeInfo?.SerialNumber;
            if (serialNumber is null) {
                return;
            }

            StartTime = packageInfoModel?.BarCodeInfo?.ScanTime.AddSeconds(0 - VideoPlaybackSettingsInfo.SecondsToSubtract) ?? DateTime.Now.AddSeconds(-60);
            EndTime = StartTime.AddSeconds(VideoPlaybackSettingsInfo.VideoLengthInSeconds);
            CurrentTime = StartTime;
            if (VideoPlaybackSettingsInfo.IsWatermarkTimeMarked) {
                SelectionStartTime = packageInfoModel?.BarCodeInfo?.ScanTime;
                SelectionEndTime = SelectionStartTime?.AddSeconds(10);
            }
            //获取Nvr
            var nvrBindingInfoModels = await _nvrCameraBindingRepository.MemoryCacheData();
            var nvrCameraBindingInfoModels = nvrBindingInfoModels.Where(f => f.SerialNumber.Equals(serialNumber)).ToList();

            var videoPlayerModels = nvrCameraBindingInfoModels?.Select(s =>
                new VideoPlayerModel {
                    Channel = s.Channel,
                    IpAddress = s.IpAddress,
                    Password = s.Password,
                    Port = s.Port,
                    Username = s.Username,
                    VideoFrame = new(449, 253, 96, 96, PixelFormats.Bgr24, null),
                    VideoScreenShotCommand = CaptureScreenShotCommand,
                    DownloadCommand = DownloadVideoCommand,
                    ToggleImageSizeCommand = ToggleImageSizeCommand,
                })?.ToList();

            VideoPlayerItems.AddRange(videoPlayerModels);
            if (!VideoPlayerItems.Any()) {
                return;
            }
            _daHuatechNvr ??= DaHuatechNVR.Instance;

            //登录

            var any = _daHuatechNvr.GetDevLogInInfo(f => f.IpAddress.Equals(VideoPlayerItems.FirstOrDefault().IpAddress))?.ToList().Any();

            if (any != true) {
                //登录
                var (key, value) = await _daHuatechNvr.LogIn(VideoPlayerItems.FirstOrDefault().IpAddress, VideoPlayerItems.FirstOrDefault().Port,
                    VideoPlayerItems.FirstOrDefault().Username, VideoPlayerItems.FirstOrDefault().Password);
                if (key) {
                    PlaybackDelegate(obj);
                }
            }
        }

        public ICommand CloseDialogCommand => new DelegateCommand<object>(CloseDialogDelegate);

        private async void CloseDialogDelegate(object obj) {
            //退出播放
            //全部注销
            if (_daHuatechNvr is not null) {
                Parallel.ForEach(VideoPlayerItems, async item => {
                    await _daHuatechNvr.ClosePlayBackVideo(item.IpAddress, item.Channel);
                    item.Dispose();
                });
                _daHuatechNvr.LogOut(VideoPlayerItems.FirstOrDefault().IpAddress);
            }

            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand PlaybackCommand => new DelegateCommand<object>(PlaybackDelegate);

        /// <summary>
        /// 播放
        /// </summary>
        /// <param name="obj"></param>
        private async void PlaybackDelegate(object obj) {
            if (_daHuatechNvr is not null) {
                _loadedSize = 0;
                Speed = 1;
                _currentSpeedIndex = _currentSlowMotionSpeedIndex = 0;
                FastForwardSpeed = SlowMotionSpeed = null;
                if (PlaybackState == PlaybackState.Ready) {
                    PlaybackState = PlaybackState.Playing;
                    PlayDateTime = CurrentTime;
                    Parallel.ForEach(VideoPlayerItems, async item => {
                        item.IsBuffering = true;
                        await Application.Current.Dispatcher.InvokeAsync(async () => {
                            var (key, value) = await _daHuatechNvr.QueryVideoFile(item.IpAddress,
                                item.Channel, CurrentTime, EndTime, (int)SelectPlaybackStream);
                            if (key && value is NET_RECORDFILE_INFO[] recordFileInfos) {
                                //临时显示

                                var (b, o) = await _daHuatechNvr.PlayBackVideo(item.IpAddress, item.Channel,
                                    CurrentTime,
                                    EndTime, item.RealtimePreviewCallback
                                    , async info => {
                                        if (info.LoadSize > _loadedSize) {
                                            await Application.Current.Dispatcher.InvokeAsync(async () => {
                                                if (item.IsBuffering) {
                                                    item.IsBuffering = false;
                                                }

                                                var addSeconds = PlayDateTime.AddSeconds(info.LoadSize * Speed);
                                                if (CurrentTime.CompareTo(addSeconds) < 0 && !ProgressRelease) {
                                                    CurrentTime = addSeconds;
                                                }
                                            }, DispatcherPriority.Render);
                                        }
                                    });
                                if (b) {
                                    if (item.ScreenState == ScreenState.Maximized) {
                                        item.VideoFrame = new WriteableBitmap((int)item.MaxSize.Width,
                                            (int)item.MaxSize.Height, 96, 96, PixelFormats.Bgr24, null);
                                        _daHuatechNvr.SetResolution(item.IpAddress, item.Channel, (int)item.MaxSize.Width,
                                            (int)item.MaxSize.Height);
                                    }
                                    else {
                                        var size = GetVideoPlayerSize();
                                        item.VideoFrame = new WriteableBitmap((int)size.Width,
                                            (int)size.Height, 96, 96, PixelFormats.Bgr24, null);
                                        _daHuatechNvr.SetResolution(item.IpAddress, item.Channel, (int)size.Width,
                                            (int)size.Height);
                                    }
                                    item.IsBuffering = false;
                                }

                                item.PlaybackError = PlaybackError.None;
                            }
                            else {
                                item.VideoFrame = null;
                                if (value is string msg && msg.Contains("录像文件")) {
                                    item.PlaybackError = PlaybackError.VideoFileNotFound;
                                }
                                else {
                                    item.PlaybackError = PlaybackError.UnknownError;
                                }
                                item.IsBuffering = false;
                                PlaybackState = PlaybackState.Ready;
                            }
                        });
                    });
                }
                else if (PlaybackState == PlaybackState.Paused) {
                    Parallel.ForEach(VideoPlayerItems, async item => {
                        await _daHuatechNvr.ResumePlayback(item.IpAddress, item.Channel);
                    });
                    PlaybackState = PlaybackState.Playing;
                }
                else {
                    Parallel.ForEach(VideoPlayerItems, async item => {
                        await _daHuatechNvr.PausePlayback(item.IpAddress, item.Channel);
                    });
                    PlaybackState = PlaybackState.Paused;
                }
            }
        }

        public ICommand FastForwardCommand => new DelegateCommand<object>(FastForwardDelegate);

        /// <summary>
        /// 快进
        /// </summary>
        /// <param name="obj"></param>
        private void FastForwardDelegate(object obj) {
            if (_daHuatechNvr is not null) {
                if (PlaybackState is PlaybackState.Ready or PlaybackState.Paused) {
                    PlaybackDelegate(obj);
                }
                // 获取当前的快进倍速
                var currentSpeed = _fastForwardSpeeds[_currentSpeedIndex];
                Speed = currentSpeed switch {
                    DaHuatechNVR.FastForwardSpeed.X2 => 1,
                    DaHuatechNVR.FastForwardSpeed.X4 => 1,
                    DaHuatechNVR.FastForwardSpeed.X8 => 2.2,
                    DaHuatechNVR.FastForwardSpeed.X16 => 5,
                    DaHuatechNVR.FastForwardSpeed.Normal => 1,
                    _ => 1
                };
                Parallel.ForEach(VideoPlayerItems, async item => {
                    await _daHuatechNvr.FastForward(item.IpAddress, item.Channel, currentSpeed);
                });
                PlaybackState = PlaybackState.FastForwarding;
                // 更新索引，指向下一个倍速
                FastForwardSpeed = currentSpeed != DaHuatechNVR.FastForwardSpeed.Normal ? currentSpeed.ToString() : null;
                if (FastForwardSpeed == null) {
                    PlaybackState = PlaybackState.Playing;
                }
                _currentSpeedIndex = (_currentSpeedIndex + 1) % _fastForwardSpeeds.Length;
            }
        }

        public ICommand SlowMotionCommand => new DelegateCommand<object>(SlowMotionDelegate);

        /// <summary>
        /// 慢放
        /// </summary>
        /// <param name="obj"></param>
        private void SlowMotionDelegate(object obj) {
            if (_daHuatechNvr is not null) {
                if (PlaybackState is PlaybackState.Ready or PlaybackState.Paused) {
                    PlaybackDelegate(obj);
                }
                var currentSlowSpeed = _slowMotionSpeeds[_currentSlowMotionSpeedIndex];
                Parallel.ForEach(VideoPlayerItems, async item => {
                    await _daHuatechNvr.Slow(item.IpAddress, item.Channel, currentSlowSpeed);
                });
                PlaybackState = PlaybackState.SlowMotion;
                SlowMotionSpeed = currentSlowSpeed != DaHuatechNVR.SlowSpeed.Normal ? currentSlowSpeed.ToString() : null;
                // 更新索引，指向下一个慢放倍速
                if (SlowMotionSpeed == null) {
                    PlaybackState = PlaybackState.Playing;
                }
                _currentSlowMotionSpeedIndex = (_currentSlowMotionSpeedIndex + 1) % _slowMotionSpeeds.Length;
            }
        }

        public ICommand FastForwardBySecondsCommand => new DelegateCommand<object>(FastForwardBySecondsDelegate);

        /// <summary>
        /// 快进指定秒数
        /// </summary>
        /// <param name="obj"></param>
        private async void FastForwardBySecondsDelegate(object obj) {
            if (int.TryParse(obj as string, out var seconds)) {
                ProgressRelease = true;
                if (_daHuatechNvr is not null) {
                    Parallel.ForEach(VideoPlayerItems, async item => {
                        await _daHuatechNvr.StopPlayback(item.IpAddress, item.Channel);
                    });
                    PlaybackState = PlaybackState.Ready;
                }

                await Task.Delay(600);
                CurrentTime = CurrentTime.AddSeconds(seconds);
                PlaybackDelegate(obj);
                ProgressRelease = false;
            }
        }

        public ICommand RewindBySecondsCommand => new DelegateCommand<object>(RewindBySecondsDelegate);

        /// <summary>
        /// 快退指定秒数
        /// </summary>
        /// <param name="obj"></param>
        private async void RewindBySecondsDelegate(object obj) {
            if (int.TryParse(obj as string, out var seconds)) {
                ProgressRelease = true;
                if (_daHuatechNvr is not null) {
                    Parallel.ForEach(VideoPlayerItems, async item => {
                        await _daHuatechNvr.StopPlayback(item.IpAddress, item.Channel);
                    });
                    PlaybackState = PlaybackState.Ready;
                }

                await Task.Delay(600);
                CurrentTime = CurrentTime.AddSeconds(0 - seconds);
                PlaybackDelegate(obj);
                ProgressRelease = false;
            }
        }

        public ICommand ProgressChangedCommand => new DelegateCommand<MouseButtonEventArgs>(ProgressChangedDelegate);

        /// <summary>
        /// 改变进度后
        /// </summary>
        /// <param name="obj"></param>
        private void ProgressChangedDelegate(MouseButtonEventArgs obj) {
            if (obj.ChangedButton == MouseButton.Left && ProgressRelease) {
                PlaybackDelegate(obj);
                ProgressRelease = false;
            }
        }

        public ICommand ProgressReleaseCommand => new DelegateCommand<MouseButtonEventArgs>(ProgressReleaseDelegate);

        private void ProgressReleaseDelegate(MouseButtonEventArgs obj) {
            if (obj.ChangedButton == MouseButton.Left) {
                ProgressRelease = true;
                if (_daHuatechNvr is not null) {
                    Parallel.ForEach(VideoPlayerItems, async item => {
                        await _daHuatechNvr.StopPlayback(item.IpAddress, item.Channel);
                    });
                    PlaybackState = PlaybackState.Ready;
                }
            }
        }

        /// <summary>
        /// 截图
        /// </summary>
        public ICommand CaptureScreenShotCommand => new DelegateCommand<VideoPlayerModel>(CaptureScreenShotDelegate);

        private async void CaptureScreenShotDelegate(VideoPlayerModel obj) {
            if (_daHuatechNvr is not null) {
                var saveFileDialog = new SaveFileDialog() {
                    DefaultExt = ".bmp",
                    Filter = "Bitmap files (*.bmp)|*.bmp|All files (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop), // 初始路径
                    FileName = $"img_{PackageItemModel.Barcode}",
                    Title = "保存截图"
                };
                var showDialog = saveFileDialog.ShowDialog();
                if (showDialog == true) {
                    await _daHuatechNvr.CaptureAsync(obj.IpAddress, obj.Channel, saveFileDialog.FileName);
                }
            }
        }

        /// <summary>
        /// 视频下载
        /// </summary>
        public ICommand DownloadVideoCommand => new DelegateCommand<VideoPlayerModel>(DownloadVideoDelegate);

        private async void DownloadVideoDelegate(VideoPlayerModel obj) {
            if (_daHuatechNvr is not null && obj.DownloadState == DownloadState.Ready) {
                var saveFileDialog = new SaveFileDialog() {
                    DefaultExt = ".bmp",
                    Filter = "dav files (*.dav)|*.dav|All files (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop), // 初始路径
                    FileName = $"vid_{PackageItemModel.Barcode}",
                    Title = "保存视频"
                };
                var showDialog = saveFileDialog.ShowDialog();
                if (showDialog == true) {
                    var (key, value) = await _daHuatechNvr.QueryVideoFile(obj.IpAddress,
                        obj.Channel, StartTime, EndTime, 0);
                    if (key && value is NET_RECORDFILE_INFO[] recordFileInfos) {
                        obj.DownloadProgress = 0;
                        await _daHuatechNvr.DownloadRecording(obj.IpAddress,
                             obj.Channel,
                             StartTime.AddSeconds(-2), EndTime, (int)SelectPlaybackStream,
                             saveFileDialog.FileName, async info => {
                                 await Application.Current.Dispatcher.InvokeAsync(async () => {
                                     if (info.IsDownloadComplete) {
                                         obj.DownloadState = DownloadState.Transcoding;
                                         await _daHuatechNvr.ConvertDavToMp4(saveFileDialog.FileName,
                                              Path.ChangeExtension(saveFileDialog.FileName, ".mp4"),
                                              (i, i1) => {
                                                  if (obj.DownloadState != DownloadState.Transcoding) {
                                                      obj.DownloadState = DownloadState.Transcoding;
                                                      obj.DownloadProgress = 0;
                                                  }
                                                  var d = ((double)i / i1) * 100;
                                                  if (d - obj.DownloadProgress > 2) {
                                                      obj.DownloadProgress = d;
                                                  }

                                                  if (i == i1) {
                                                      obj.DownloadState = DownloadState.Ready;
                                                      obj.DownloadProgress = 100;
                                                  }
                                                  return true;
                                              });
                                         obj.DownloadState = DownloadState.Ready;
                                         obj.DownloadProgress = 100;
                                     }
                                     else if (info.IsDownloadError) {
                                         //下载错误
                                         Console.WriteLine("下载错误");
                                         obj.DownloadState = DownloadState.Ready;
                                     }
                                     else {
                                         if (obj.DownloadState != DownloadState.Downloading) {
                                             obj.DownloadState = DownloadState.Downloading;
                                         }
                                     }

                                     var infoTotalSize = ((double)info.LoadSize / info.TotalSize) * 100;
                                     if (infoTotalSize - obj.DownloadProgress > 2) {
                                         obj.DownloadProgress = infoTotalSize;
                                     }
                                 });
                             });
                    }
                }
            }
        }

        public ICommand ChangedTimeCommand => new DelegateCommand<TextBox>(ChangedTimeDelegate);

        private async void ChangedTimeDelegate(TextBox textBox) {
            ProgressRelease = true;
            if (_daHuatechNvr is not null) {
                Parallel.ForEach(VideoPlayerItems, async item => {
                    await _daHuatechNvr.StopPlayback(item.IpAddress, item.Channel);
                });
                PlaybackState = PlaybackState.Ready;
            }
            var binding = textBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();
            if (binding is not null) {
                //重新播放
                CurrentTime = StartTime;
                if (ProgressRelease) {
                    await Task.Delay(300);
                    PlaybackDelegate(null);
                }
            }
            ProgressRelease = false;
        }

        public ICommand SaveVideoPlaybackSettingsCommand => new DelegateCommand<Popup>(SaveVideoPlaybackSettingsDelegate);

        private async void SaveVideoPlaybackSettingsDelegate(Popup obj) {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = "VideoPlaybackSettings",
                Value = JsonConvert.SerializeObject(new VideoPlaybackSettingsDto {
                    IsWatermarkTimeMarked = VideoPlaybackSettingsInfo.IsWatermarkTimeMarked,
                    SecondsToSubtract = VideoPlaybackSettingsInfo.SecondsToSubtract,
                    VideoLengthInSeconds = VideoPlaybackSettingsInfo.VideoLengthInSeconds
                })
            });
            if (insertOrUpdate) {
                obj.IsOpen = false;
            }
        }
    }

    public enum PlaybackStream {

        [Description("主码流"), FontIcon("\xea07")]
        MainStream,

        [Description("辅码流"), FontIcon("\xea09")]
        SubStream
    }

    public enum PlaybackState {

        [Description("准备就绪"), AuxiliaryDescription("播放"),
         FontIcon("\xe9e9"), BackgroundColor("#4169E1"),
        LabelColor("#FFFFFF")]
        Ready,

        [Description("播放中"), AuxiliaryDescription("停止"),
         FontIcon("\xea2a"), BackgroundColor("#8B0000"),
         LabelColor("#FFFFFF")]
        Playing,

        [Description("快进中"), AuxiliaryDescription("停止"),
         FontIcon("\xea2a"), BackgroundColor("#8B0000"),
         LabelColor("#FFA500")]
        FastForwarding,

        [Description("慢放中"), AuxiliaryDescription("停止"),
         FontIcon("\xea2a"), BackgroundColor("#8B0000"),
         LabelColor("#00FA9A")]
        SlowMotion,

        [Description("暂停中"), AuxiliaryDescription("播放"),
         FontIcon("\xe9e9"), BackgroundColor("#4169E1"),
         LabelColor("#FFFFFF")]
        Paused
    }
}