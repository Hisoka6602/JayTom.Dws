using System;
using NetSDKCS;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Plugin.Speech;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Attributes.WinClientAttributes;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Client.Models.Cameras.CameraConfiguration;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.NVR;
using static JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.NVR.DaHuatechNVR;

namespace JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration {

    public class NvrRecordingViewModel : BindableBase {
        private ObservableCollection<VideoPlayerModel> _videoPlayerItems = new();

        //private BaseDaHuatech? _baseDaHuatech;
        private DaHuatechNVR? _daHuatechNvr;

        private string _identifier = string.Empty;
        private DateTime _startTime = DateTime.Now.AddMinutes(-10);
        private DateTime _endTime = DateTime.Now.AddMinutes(-5);
        private DateTime _currentTime;
        private DateTime _selectionStartTime;
        private DateTime _selectionEndTime;
        private ObservableCollection<PlaybackStream> _playbackStreamItems = new(Enum.GetValues(typeof(PlaybackStream)).Cast<PlaybackStream>());
        private PlaybackStream _selectPlaybackStream = PlaybackStream.MainStream;
        private string? _fastForwardSpeed;
        private string? _rewindSpeed;
        private string? _slowMotionSpeed;
        private PlaybackState _playbackState = PlaybackState.Ready;
        private DateTime _playDateTime;

        private readonly DaHuatechNVR.FastForwardSpeed[] _fastForwardSpeeds =
            { DaHuatechNVR.FastForwardSpeed.X2,
                DaHuatechNVR.FastForwardSpeed.X4,
                DaHuatechNVR.FastForwardSpeed.X8,
                DaHuatechNVR.FastForwardSpeed.X16,
                DaHuatechNVR.FastForwardSpeed.Normal
            };

        private int _currentSpeedIndex = 0;
        private double _speed = 1;

        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
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

        public DateTime SelectionStartTime {
            get => _selectionStartTime;
            set => SetProperty(ref _selectionStartTime, value);
        }

        public DateTime SelectionEndTime {
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
        /// 快退倍数
        /// </summary>
        public string? RewindSpeed {
            get => _rewindSpeed;
            set => SetProperty(ref _rewindSpeed, value);
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
            var size = new Size(449, 253);
            switch (VideoPlayerItems.Count) {
                case 1:
                    size = new Size(584, 329);
                    break;

                case > 1 and <= 4:
                    size = new Size(449, 253);
                    break;

                case > 4:
                    size = new Size(374, 211);
                    break;
            }
            return size;
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            CurrentTime = StartTime;
            SelectionStartTime = StartTime.AddSeconds(10);
            SelectionEndTime = StartTime.AddSeconds(400);
            VideoPlayerItems.AddRange(new List<VideoPlayerModel>()
            {
                new() { IsBuffering = true,
                    ToggleImageSizeCommand = ToggleImageSizeCommand,
                    IpAddress = "192.168.31.111",
                    Port = 37777,
                    Username = "admin",
                    Password = "a12345678",
                    Channel = 1,
                    VideoFrame =  new(449, 253, 96, 96, PixelFormats.Bgr24, null),
                    VideoScreenShotCommand = CaptureScreenShotCommand,
                    DownloadCommand = DownloadVideoCommand
                },
                new() { IsBuffering = true,
                    ToggleImageSizeCommand = ToggleImageSizeCommand,
                    IpAddress = "192.168.31.111",
                    Port = 37777,
                    Username = "admin",
                    Password = "a12345678",
                    Channel = 3,
                    VideoFrame =  new(449, 253, 96, 96, PixelFormats.Bgr24, null),
                    VideoScreenShotCommand = CaptureScreenShotCommand,
                    DownloadCommand = DownloadVideoCommand
                },
                /*new() {IsBuffering = true, ToggleImageSizeCommand = ToggleImageSizeCommand , },
                new() { IsBuffering = true,ToggleImageSizeCommand = ToggleImageSizeCommand , },*/
                /*new() { ToggleImageSizeCommand = ToggleImageSizeCommand },
                new() { ToggleImageSizeCommand = ToggleImageSizeCommand },
                new() { ToggleImageSizeCommand = ToggleImageSizeCommand },*/
            });
            _daHuatechNvr ??= DaHuatechNVR.Instance;
            await BaseDaHuatech.EnumDevices();
            //登录
            var any = _daHuatechNvr.GetDevLogInInfo(f => f.IpAddress.Equals("192.168.31.111"))?.ToList().Any();

            if (any != true) {
                //登录
                var (key, value) = await _daHuatechNvr.LogIn("192.168.31.111", 37777,
                    "admin", "a12345678");
                if (!key) {
                    Console.WriteLine("登录失败");
                }
            }
        }

        public ICommand CloseDialogCommand => new DelegateCommand<object>(CloseDialogDelegate);

        private void CloseDialogDelegate(object obj) {
            //退出播放
            //全部注销
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
                Speed = 1;
                _currentSpeedIndex = 0;
                FastForwardSpeed = null;
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
                                        await Application.Current.Dispatcher.InvokeAsync(() => {
                                            if (item.IsBuffering) {
                                                item.IsBuffering = false;
                                            }

                                            var addSeconds = PlayDateTime.AddSeconds(info.LoadSize * Speed);
                                            if (CurrentTime.CompareTo(addSeconds) < 0 && !ProgressRelease) {
                                                CurrentTime = addSeconds;
                                            }
                                        });
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
                            }
                            else {
                                if (value is string msg && msg.Contains("录像文件")) {
                                    item.PlaybackError = PlaybackError.VideoFileNotFound;
                                }
                                else {
                                    item.PlaybackError = PlaybackError.UnknownError;
                                }
                                item.IsBuffering = false;
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

                _currentSpeedIndex = (_currentSpeedIndex + 1) % _fastForwardSpeeds.Length;
            }
        }

        public ICommand RewindCommand => new DelegateCommand<object>(RewindDelegate);

        /// <summary>
        /// 快退
        /// </summary>
        /// <param name="obj"></param>
        private void RewindDelegate(object obj) {
            PlaybackState = PlaybackState.Rewinding;
            Debug.WriteLine($"快退");
        }

        public ICommand SlowMotionCommand => new DelegateCommand<object>(SlowMotionDelegate);

        /// <summary>
        /// 慢放
        /// </summary>
        /// <param name="obj"></param>
        private void SlowMotionDelegate(object obj) {
            PlaybackState = PlaybackState.SlowMotion;
            Debug.WriteLine($"慢放");
        }

        public ICommand FastForwardBySecondsCommand => new DelegateCommand<object>(FastForwardBySecondsDelegate);

        /// <summary>
        /// 快进指定秒数
        /// </summary>
        /// <param name="obj"></param>
        private void FastForwardBySecondsDelegate(object obj) {
            Debug.WriteLine($"快进");
        }

        public ICommand RewindBySecondsCommand => new DelegateCommand<object>(RewindBySecondsDelegate);

        /// <summary>
        /// 快退指定秒数
        /// </summary>
        /// <param name="obj"></param>
        private void RewindBySecondsDelegate(object obj) {
            Debug.WriteLine($"快退");
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
                    FileName = "image",
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
                    FileName = "video",
                    Title = "保存视频"
                };
                var showDialog = saveFileDialog.ShowDialog();
                if (showDialog == true) {
                    var (key, value) = await _daHuatechNvr.QueryVideoFile(obj.IpAddress,
                        obj.Channel, StartTime, EndTime, 0);
                    if (key && value is NET_RECORDFILE_INFO[] recordFileInfos) {
                        await _daHuatechNvr.DownloadRecording(obj.IpAddress,
                             obj.Channel,
                             recordFileInfos[0].endtime.ToDateTime().AddMinutes(-5),
                             recordFileInfos[0].endtime.ToDateTime(), 0,
                             saveFileDialog.FileName, async info => {
                                 await Application.Current.Dispatcher.InvokeAsync(async () => {
                                     if (info.IsDownloadComplete) {
                                         obj.DownloadState = DownloadState.Transcoding;
                                         await _daHuatechNvr.StartAviConvert(saveFileDialog.FileName,
                                              Path.ChangeExtension(saveFileDialog.FileName, ".avi"),
                                              (i, i1) => {
                                                  return false;
                                              });
                                         obj.DownloadState = DownloadState.Ready;
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

        [Description("快退中"), AuxiliaryDescription("停止"),
         FontIcon("\xea2a"), BackgroundColor("#8B0000"),
         LabelColor("#00BFFF")]
        Rewinding,

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