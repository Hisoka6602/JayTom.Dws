using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
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
        private bool _isMaximized;
        private bool _isRegularDownload;
        private double _playbackSpeed;
        private bool _isReversed;
        private bool _isConvertingFormat;
        private ICommand? _videoScreenShotCommand;
        private ICommand? _downloadCommand;
        private ICommand? _maximizeCommand;
        private bool _isBuffering;

        public VideoPlayerModel() {
            RealtimePreviewCallback = async info => {
                if (info.RgbData is not null && info is { Width: > 0, Height: > 0 }) {
                    if (Application.Current is not null) {
                        await Application.Current.Dispatcher.InvokeAsync(() => {
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

        /// <summary>
        /// 获取或设置一个值，该值指示窗口是否最大化。
        /// </summary>
        public bool IsMaximized {
            get => _isMaximized;
            set => SetProperty(ref _isMaximized, value);
        }

        /// <summary>
        /// 获取或设置一个值，该值指示是否为常规下载。
        /// </summary>
        public bool IsRegularDownload {
            get => _isRegularDownload;
            set => SetProperty(ref _isRegularDownload, value);
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
        /// 获取或设置一个值，该值指示是否正在转换格式。
        /// </summary>
        public bool IsConvertingFormat {
            get => _isConvertingFormat;
            set => SetProperty(ref _isConvertingFormat, value);
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
        public ICommand? MaximizeCommand {
            get => _maximizeCommand;
            set => SetProperty(ref _maximizeCommand, value);
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
    }
}