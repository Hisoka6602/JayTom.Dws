using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows;
using System.Diagnostics;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;

namespace JayTom.Dws.Client.Models.Cameras.CameraConfiguration {

    public class NvrPreviewViewItemInfo : BindableBase, IDisposable {
        private int _channelId;
        private string _displayName = string.Empty;
        private WriteableBitmap? _videoFrame = new(449, 253, 96, 96, PixelFormats.Bgr24, null);
        private bool _isShow;
        private string _serialNumber = string.Empty;

        public NvrPreviewViewItemInfo() {
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

        public string SerialNumber {
            get => _serialNumber;
            set => SetProperty(ref _serialNumber, value);
        }

        public int ChannelId {
            get => _channelId;
            set => SetProperty(ref _channelId, value);
        }

        public string DisplayName {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public WriteableBitmap? VideoFrame {
            get => _videoFrame;
            set => SetProperty(ref _videoFrame, value);
        }

        public bool IsShow {
            get => _isShow;
            set => SetProperty(ref _isShow, value);
        }

        public void Dispose() {
            IsShow = false;
            VideoFrame?.Freeze();
            VideoFrame = null;
            RealtimePreviewCallback = null;
        }

        public Func<RealtimePreviewInfo, Task> RealtimePreviewCallback { get; private set; }
    }
}