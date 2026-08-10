using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;

namespace JayTom.Dws.Client.Models.Cameras.CameraConfiguration
{

    public class NvrPreviewViewItemInfo : BindableBase, IDisposable
    {
        private int _channelId;
        private string _displayName = string.Empty;
        private WriteableBitmap? _videoFrame = new(768, 432, 96, 96, PixelFormats.Bgr24, null);
        private bool _isShow;
        private string _serialNumber = string.Empty;
        private ICommand? _increaseZoomCommand;
        private ICommand? _decreaseZoomCommand;
        private ICommand? _increaseFocusCommand;
        private ICommand? _decreaseFocusCommand;
        private ICommand? _autoFocusCommand;
        private ScreenState _screenState = ScreenState.Normal;
        private Size _maxSize = new(768, 432);
        private ICommand? _toggleImageSizeCommand;
        private bool _isStopRead = false;

        public NvrPreviewViewItemInfo()
        {
            RealtimePreviewCallback = async info =>
            {
                if (info.RgbData is not null && info is { Width: > 0, Height: > 0 } && !_isStopRead)
                {
                    if (System.Windows.Application.Current is not null)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (VideoFrame is not null)
                            {
                                VideoFrame.Lock();
                                var rect = new Int32Rect(0, 0, info.Width, info.Height);

                                // 检查数据缓冲区大小
                                if (info.RgbData.Length >= info.Width * info.Height * 3)
                                {
                                    VideoFrame.WritePixels(rect, info.RgbData, info.Width * 3, 0);
                                    VideoFrame.AddDirtyRect(rect);
                                }

                                VideoFrame.Unlock();
                            }
                        }, System.Windows.Threading.DispatcherPriority.Background);
                    }
                }
            };
        }

        public string SerialNumber
        {
            get => _serialNumber;
            set => SetProperty(ref _serialNumber, value);
        }

        public int ChannelId
        {
            get => _channelId;
            set => SetProperty(ref _channelId, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public WriteableBitmap? VideoFrame
        {
            get => _videoFrame;
            set => SetProperty(ref _videoFrame, value);
        }

        public bool IsShow
        {
            get => _isShow;
            set => SetProperty(ref _isShow, value);
        }

        public ScreenState ScreenState
        {
            get => _screenState;
            set => SetProperty(ref _screenState, value);
        }

        public Size MaxSize
        {
            get => _maxSize;
            set => SetProperty(ref _maxSize, value);
        }

        public void Dispose()
        {
            Action releaseResources = () =>
            {
                _isStopRead = true;
                IsShow = false;
                VideoFrame?.Freeze();
                VideoFrame = null;
                RealtimePreviewCallback = null;
            };
            if (System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                releaseResources();
            }
            else
            {
                System.Windows.Application.Current.Dispatcher.Invoke(releaseResources);
            }
        }

        /// <summary>
        /// 增加缩放
        /// </summary>
        public ICommand? IncreaseZoomCommand
        {
            get => _increaseZoomCommand;
            set => SetProperty(ref _increaseZoomCommand, value);
        }

        /// <summary>
        /// 减少缩放
        /// </summary>
        public ICommand? DecreaseZoomCommand
        {
            get => _decreaseZoomCommand;
            set => SetProperty(ref _decreaseZoomCommand, value);
        }

        /// <summary>
        /// 增加焦距
        /// </summary>
        public ICommand? IncreaseFocusCommand
        {
            get => _increaseFocusCommand;
            set => SetProperty(ref _increaseFocusCommand, value);
        }

        /// <summary>
        /// 减少焦距
        /// </summary>
        public ICommand? DecreaseFocusCommand
        {
            get => _decreaseFocusCommand;
            set => SetProperty(ref _decreaseFocusCommand, value);
        }

        /// <summary>
        /// 自动焦距
        /// </summary>
        public ICommand? AutoFocusCommand
        {
            get => _autoFocusCommand;
            set => SetProperty(ref _autoFocusCommand, value);
        }

        public ICommand? ToggleImageSizeCommand
        {
            get => _toggleImageSizeCommand;
            set => SetProperty(ref _toggleImageSizeCommand, value);
        }

        public Func<RealtimePreviewInfo, Task> RealtimePreviewCallback { get; private set; }
    }
}
