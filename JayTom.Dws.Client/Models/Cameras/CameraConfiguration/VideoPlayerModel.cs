using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using JayTom.Dws.Client.Attributes.WinClientAttributes;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;

namespace JayTom.Dws.Client.Models.Cameras.CameraConfiguration {

    public class VideoPlayerModel : BindableBase {
        private WriteableBitmap? _videoFrame;
        private int _num;
        private int _port;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private int _channel;
        private double _downloadProgress;
        private double _playbackSpeed;
        private bool _isReversed;
        private ICommand? _videoScreenShotCommand;
        private ICommand? _downloadCommand;
        private ICommand? _toggleImageSizeCommand;
        private bool _isBuffering = true;
        private ScreenState _screenState;
        private ScreenshotState _screenshotState = ScreenshotState.Ready;
        private DownloadState _downloadState = DownloadState.Ready;
        private Size _maxSize = new(1152, 648);
        private PlaybackError _playbackError = PlaybackError.None;
        private string _ipAddress = string.Empty;
        private bool _isStopRead = false;

        public VideoPlayerModel() {
            RealtimePreviewCallback = async info => {
                if (info.RgbData is not null && info is { Width: > 0, Height: > 0 }
                                             && !_isStopRead && ScreenState != ScreenState.Hidden) {
                    if (Application.Current is not null) {
                        await Application.Current.Dispatcher.InvokeAsync(() => {
                            try {
                                if (VideoFrame is not null) {
                                    VideoFrame.Lock();
                                    var rect = new Int32Rect(0, 0, info.Width, info.Height);
                                    // 检查数据缓冲区大小
                                    if (info.RgbData.Length >= info.Width * info.Height * 3) {
                                        VideoFrame.WritePixels(rect, info.RgbData, info.Width * 3, 0);
                                        VideoFrame.AddDirtyRect(rect);
                                    }

                                    VideoFrame.Unlock();
                                }
                            }
                            catch (Exception e) {
                                Console.WriteLine(e);
                            }
                        }, System.Windows.Threading.DispatcherPriority.Render);
                    }
                }
            };
        }

        /// <summary>
        /// 获取或设置编号。
        /// </summary>
        public int Num {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        public WriteableBitmap? VideoFrame {
            get => _videoFrame;
            set => SetProperty(ref _videoFrame, value);
        }

        /// <summary>
        /// Ip地址
        /// </summary>
        public string IpAddress {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        /// <summary>
        /// 获取或设置端口号。
        /// </summary>
        public int Port {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        /// <summary>
        /// 获取或设置用户名。
        /// </summary>
        public string Username {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        /// <summary>
        /// 获取或设置密码。
        /// </summary>
        public string Password {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>
        /// 获取或设置通道号。
        /// </summary>
        public int Channel {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        /// <summary>
        /// 获取或设置下载进度（百分比）。
        /// </summary>
        public double DownloadProgress {
            get => _downloadProgress;
            set => SetProperty(ref _downloadProgress, value);
        }

        public ScreenState ScreenState {
            get => _screenState;
            set => SetProperty(ref _screenState, value);
        }

        /// <summary>
        /// 播放异常状态
        /// </summary>
        public PlaybackError PlaybackError {
            get => _playbackError;
            set => SetProperty(ref _playbackError, value);
        }

        /// <summary>
        /// 下载状态
        /// </summary>
        public DownloadState DownloadState {
            get => _downloadState;
            set => SetProperty(ref _downloadState, value);
        }

        /// <summary>
        /// 获取或设置播放速度。
        /// </summary>
        public double PlaybackSpeed {
            get => _playbackSpeed;
            set => SetProperty(ref _playbackSpeed, value);
        }

        /// <summary>
        /// 获取或设置一个值，该值指示视频是否倒放。
        /// </summary>
        public bool IsReversed {
            get => _isReversed;
            set => SetProperty(ref _isReversed, value);
        }

        /// <summary>
        /// 截图状态
        /// </summary>
        public ScreenshotState ScreenshotState {
            get => _screenshotState;
            set => SetProperty(ref _screenshotState, value);
        }

        /// <summary>
        /// 获取或设置视频截图命令。
        /// </summary>
        public ICommand? VideoScreenShotCommand {
            get => _videoScreenShotCommand;
            set => SetProperty(ref _videoScreenShotCommand, value);
        }

        /// <summary>
        /// 获取或设置下载命令。
        /// </summary>
        public ICommand? DownloadCommand {
            get => _downloadCommand;
            set => SetProperty(ref _downloadCommand, value);
        }

        /// <summary>
        /// 获取或设置最大化窗口命令。
        /// </summary>
        public ICommand? ToggleImageSizeCommand {
            get => _toggleImageSizeCommand;
            set => SetProperty(ref _toggleImageSizeCommand, value);
        }

        /// <summary>
        /// 回调事件
        /// </summary>
        public Func<RealtimePreviewInfo, Task> RealtimePreviewCallback { get; private set; }

        /// <summary>
        /// 获取或设置一个值，该值指示是否缓冲中。
        /// </summary>
        public bool IsBuffering {
            get => _isBuffering;
            set => SetProperty(ref _isBuffering, value);
        }

        public Size MaxSize {
            get => _maxSize;
            set => SetProperty(ref _maxSize, value);
        }

        public async void Dispose() {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                _isStopRead = true;
                VideoFrame?.Freeze();
                VideoFrame = null;
                RealtimePreviewCallback = null;
            });
        }
    }

    public enum ScreenState {

        [Description("设置最大化画面"), FontIcon("\xea02")]
        Normal,

        [Description("还原画面"), FontIcon("\xea00")]
        Maximized,

        [Description("隐藏画面"), FontIcon("\xea00")]
        Hidden
    }

    public enum ScreenshotState {

        [Description("截图准备就绪"), FontIcon("\xea03")]
        Ready,

        [Description("截图中"), FontIcon("\xea1e")]
        Screenshotting
    }

    public enum DownloadState {

        [Description("视频下载准备就绪"), FontIcon("\xe9fc")]
        Ready,

        [Description("下载中"), FontIcon("\xea23")]
        Downloading,

        [Description("转码中"), FontIcon("\xea20")]
        Transcoding
    }

    public enum PlaybackError {

        [Description("正常播放"), FontIcon("\xea28")]
        None,

        /// <summary>
        /// 视频文件不存在
        /// </summary>
        [Description("视频文件不存在"), FontIcon("\xea2e")]
        VideoFileNotFound,

        /// <summary>
        /// 取流连接中断
        /// </summary>
        [Description("取流连接中断"), FontIcon("\xea33")]
        StreamConnectionInterrupted,

        /// <summary>
        /// 未知异常
        /// </summary>
        [Description("未知异常"), FontIcon("\xea31")]
        UnknownError,

        /// <summary>
        /// 无效通道
        /// </summary>
        [Description("无效通道"), FontIcon("\xea38")]
        InvalidChannel
    }
}