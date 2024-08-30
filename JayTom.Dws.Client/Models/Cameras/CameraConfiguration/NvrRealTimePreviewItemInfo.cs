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

    public class NvrRealTimePreviewItemInfo : BindableBase {
        private bool _isStopRead = false;
        private WriteableBitmap? _videoFrame;
        private int _num;
        private string _ipAddress = string.Empty;
        private int _port;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private int _channel;
        private PlaybackError _playbackError = PlaybackError.None;
        private bool _isBuffering = true;
        private ScreenState _screenState = ScreenState.Normal;
        private Size _maxSize = new(1800, 1012);
        private ICommand? _increaseZoomCommand;
        private ICommand? _decreaseZoomCommand;
        private ICommand? _increaseFocusCommand;
        private ICommand? _decreaseFocusCommand;
        private ICommand? _autoFocusCommand;
        private ICommand? _toggleImageSizeCommand;

        public NvrRealTimePreviewItemInfo() {
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
        /// 序号
        /// </summary>
        public int Num {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        /// <summary>
        /// ip
        /// </summary>
        public string IpAddress {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>
        /// 通道
        /// </summary>
        public int Channel {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        /// <summary>
        /// 播放异常
        /// </summary>
        public PlaybackError PlaybackError {
            get => _playbackError;
            set => SetProperty(ref _playbackError, value);
        }

        /// <summary>
        /// 是否缓冲中
        /// </summary>
        public bool IsBuffering {
            get => _isBuffering;
            set => SetProperty(ref _isBuffering, value);
        }

        /// <summary>
        /// 窗口状态
        /// </summary>
        public ScreenState ScreenState {
            get => _screenState;
            set => SetProperty(ref _screenState, value);
        }

        /// <summary>
        /// 最大尺寸
        /// </summary>
        public Size MaxSize {
            get => _maxSize;
            set => SetProperty(ref _maxSize, value);
        }

        /// <summary>
        /// 增加缩放倍数
        /// </summary>
        public ICommand? IncreaseZoomCommand {
            get => _increaseZoomCommand;
            set => SetProperty(ref _increaseZoomCommand, value);
        }

        /// <summary>
        /// 减少缩放倍数
        /// </summary>
        public ICommand? DecreaseZoomCommand {
            get => _decreaseZoomCommand;
            set => SetProperty(ref _decreaseZoomCommand, value);
        }

        /// <summary>
        /// 增加焦距倍数
        /// </summary>
        public ICommand? IncreaseFocusCommand {
            get => _increaseFocusCommand;
            set => SetProperty(ref _increaseFocusCommand, value);
        }

        /// <summary>
        /// 减少焦距倍数
        /// </summary>
        public ICommand? DecreaseFocusCommand {
            get => _decreaseFocusCommand;
            set => SetProperty(ref _decreaseFocusCommand, value);
        }

        /// <summary>
        /// 自动焦距
        /// </summary>
        public ICommand? AutoFocusCommand {
            get => _autoFocusCommand;
            set => SetProperty(ref _autoFocusCommand, value);
        }

        /// <summary>
        /// 切换尺寸
        /// </summary>
        public ICommand? ToggleImageSizeCommand {
            get => _toggleImageSizeCommand;
            set => SetProperty(ref _toggleImageSizeCommand, value);
        }

        /// <summary>
        /// 回调事件
        /// </summary>
        public Func<RealtimePreviewInfo, Task> RealtimePreviewCallback { get; private set; }

        public WriteableBitmap? VideoFrame {
            get => _videoFrame;
            set => SetProperty(ref _videoFrame, value);
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
}