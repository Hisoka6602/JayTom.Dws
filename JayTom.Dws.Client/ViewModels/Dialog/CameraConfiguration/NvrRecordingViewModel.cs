using System;
using NetSDKCS;
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
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Attributes.WinClientAttributes;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Client.Models.Cameras.CameraConfiguration;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.NVR;

namespace JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration {

    public class NvrRecordingViewModel : BindableBase {
        private ObservableCollection<VideoPlayerModel> _videoPlayerItems = new();

        //private BaseDaHuatech? _baseDaHuatech;
        private DaHuatechNVR? _daHuatechNvr;

        private string _identifier = string.Empty;
        private DateTime _startTime = DateTime.Now.AddSeconds(-20);
        private DateTime _endTime = DateTime.Now.AddHours(1);
        private DateTime _currentTime = DateTime.Now;
        private DateTime _selectionStartTime = DateTime.Now.AddSeconds(10);
        private DateTime _selectionEndTime = DateTime.Now.AddSeconds(400);
        private ObservableCollection<PlaybackStream> _playbackStreamItems = new(Enum.GetValues(typeof(PlaybackStream)).Cast<PlaybackStream>());
        private PlaybackStream? _selectPlaybackStream;
        private int? _fastForwardSpeed;
        private int? _rewindSpeed;
        private int? _slowMotionSpeed;
        private PlaybackState _playbackState = PlaybackState.Ready;

        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public ObservableCollection<VideoPlayerModel> VideoPlayerItems {
            get => _videoPlayerItems;
            set => SetProperty(ref _videoPlayerItems, value);
        }

        public ObservableCollection<PlaybackStream> PlaybackStreamItems {
            get => _playbackStreamItems;
            set => SetProperty(ref _playbackStreamItems, value);
        }

        public PlaybackStream? SelectPlaybackStream {
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
        public int? FastForwardSpeed {
            get => _fastForwardSpeed;
            set => SetProperty(ref _fastForwardSpeed, value);
        }

        /// <summary>
        /// 快退倍数
        /// </summary>
        public int? RewindSpeed {
            get => _rewindSpeed;
            set => SetProperty(ref _rewindSpeed, value);
        }

        /// <summary>
        /// 慢放倍数
        /// </summary>
        public int? SlowMotionSpeed {
            get => _slowMotionSpeed;
            set => SetProperty(ref _slowMotionSpeed, value);
        }

        public PlaybackState PlaybackState {
            get => _playbackState;
            set => SetProperty(ref _playbackState, value);
        }

        public ICommand ToggleImageSizeCommand => new DelegateCommand<VideoPlayerModel>(ToggleImageSizeDelegate);

        private void ToggleImageSizeDelegate(VideoPlayerModel obj) {
            if (obj.ScreenState == ScreenState.Normal) {
                foreach (var videoPlayerModel in VideoPlayerItems) {
                    videoPlayerModel.ScreenState = !videoPlayerModel.Equals(obj) ? ScreenState.Hidden : ScreenState.Maximized;
                }
            }
            else {
                foreach (var videoPlayerModel in VideoPlayerItems) {
                    videoPlayerModel.ScreenState = ScreenState.Normal;
                }
            }
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            _daHuatechNvr ??= DaHuatechNVR.Instance;
            await BaseDaHuatech.EnumDevices();
            VideoPlayerItems.AddRange(new List<VideoPlayerModel>()
            {
                new() { IsBuffering = true,
                    ToggleImageSizeCommand = ToggleImageSizeCommand,
                    IpAddress = "192.168.31.111",
                    Port = 37777,
                    Username = "admin",
                    Password = "a12345678",
                    Channel = 1,
                    VideoScreenShotCommand = CaptureScreenShotCommand,
                    DownloadCommand = DownloadVideoCommand
                },
                /*new() {IsBuffering = true, ToggleImageSizeCommand = ToggleImageSizeCommand , },
                new() { IsBuffering = true,ToggleImageSizeCommand = ToggleImageSizeCommand , },*/
                /*new() { ToggleImageSizeCommand = ToggleImageSizeCommand },
                new() { ToggleImageSizeCommand = ToggleImageSizeCommand },
                new() { ToggleImageSizeCommand = ToggleImageSizeCommand },*/
            });

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
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                if (_daHuatechNvr is not null) {
                    if (PlaybackState == PlaybackState.Ready) {
                        PlaybackState = PlaybackState.Playing;
                        var (key, value) = await _daHuatechNvr.QueryVideoFile("192.168.31.111",
                            1, StartTime, EndTime, 0);

                        if (key && value is NET_RECORDFILE_INFO[] recordFileInfos) {
                            VideoPlayerItems[0].VideoFrame = new(449, 253, 96, 96, PixelFormats.Bgr24, null);

                            var (b, o) = await _daHuatechNvr.PlayBackVideo("192.168.31.111", 1,
                                recordFileInfos[0].endtime.ToDateTime().AddMinutes(-5),
                                recordFileInfos[0].endtime.ToDateTime(), VideoPlayerItems[0].RealtimePreviewCallback
                                , async info => {
                                    await Application.Current.Dispatcher.InvokeAsync(() => {
                                        var addSeconds = StartTime.AddSeconds(info.LoadSize);
                                        if (!CurrentTime.Equals(addSeconds)) {
                                            CurrentTime = addSeconds;
                                        }
                                    });
                                });
                            if (b) {
                                VideoPlayerItems[0].IsBuffering = false;
                            }

                            Console.WriteLine(o);
                        }
                        else {
                            //失败
                        }
                    }
                    else if (PlaybackState == PlaybackState.Paused) {
                        var (key, value) = await _daHuatechNvr.ResumePlayback("192.168.31.111", 1);
                        if (key) {
                            PlaybackState = PlaybackState.Playing;
                        }
                    }
                    else {
                        var (key, value) = await _daHuatechNvr.PausePlayback("192.168.31.111", 1);
                        if (key) {
                            PlaybackState = PlaybackState.Paused;
                        }
                    }
                }
            });
        }

        public ICommand FastForwardCommand => new DelegateCommand<object>(FastForwardDelegate);

        /// <summary>
        /// 快进
        /// </summary>
        /// <param name="obj"></param>
        private void FastForwardDelegate(object obj) {
            PlaybackState = PlaybackState.FastForwarding;
            Debug.WriteLine($"快进");
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
            if (obj.ChangedButton == MouseButton.Left) {
                Debug.WriteLine($"{CurrentTime}");
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
                    await _daHuatechNvr.CaptureAsync(obj.IpAddress, obj.Channel,
                        DateTimeOffset.Now.ToUnixTimeMilliseconds(), async info => {
                            Console.WriteLine("截图完成");
                        });
                }
            }
        }

        /// <summary>
        /// 视频下载
        /// </summary>
        public ICommand DownloadVideoCommand => new DelegateCommand<VideoPlayerModel>(DownloadVideoDelegate);

        private async void DownloadVideoDelegate(VideoPlayerModel obj) {
            if (_daHuatechNvr is not null) {
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
                                 await Application.Current.Dispatcher.InvokeAsync(() => {
                                     obj.DownloadState = DownloadState.Downloading;
                                     obj.DownloadProgress = ((double)info.LoadSize / info.TotalSize) * 100;
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