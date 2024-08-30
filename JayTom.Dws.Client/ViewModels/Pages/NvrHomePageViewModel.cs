using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Models.Cameras.CameraConfiguration;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.NVR;

namespace JayTom.Dws.Client.ViewModels.Pages {

    public class NvrHomePageViewModel : BindableBase {
        private ObservableCollection<NvrRealTimePreviewItemInfo> _nvrRealTimePreviewItems = new();

        private DaHuatechNVR? _daHuatechNvr;
        private HorizontalAlignment _playerHorizontalAlignment = HorizontalAlignment.Left;

        public NvrHomePageViewModel() {
            _daHuatechNvr ??= DaHuatechNVR.Instance;
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                if (item is { Type: WindowsActionType.Close }) {
                    if (_daHuatechNvr is not null) {
                        Parallel.ForEach(NvrRealTimePreviewItems, async item => {
                            if (!string.IsNullOrEmpty(item.IpAddress) &&
                                item.Channel > 0) {
                                await _daHuatechNvr.StopRealtimePreview(item.IpAddress, item.Channel);
                                item.Dispose();
                            }
                        });
                        await _daHuatechNvr.LogOut(NvrRealTimePreviewItems.FirstOrDefault().IpAddress);
                    }
                }
                else if (item is { Type: WindowsActionType.EnterSettings }) {
                    if (_daHuatechNvr is not null) {
                        Parallel.ForEach(NvrRealTimePreviewItems, async item => {
                            if (!string.IsNullOrEmpty(item.IpAddress) &&
                                item.Channel > 0) {
                                await _daHuatechNvr.StopRealtimePreview(item.IpAddress, item.Channel);
                                item.Dispose();
                            }
                        });
                        await _daHuatechNvr.LogOut(NvrRealTimePreviewItems.FirstOrDefault().IpAddress);
                    }
                }
                else if (item is { Type: WindowsActionType.ReturnToHome }) {
                    LoadedDelegate(null);
                }
            });
        }

        public ObservableCollection<NvrRealTimePreviewItemInfo> NvrRealTimePreviewItems {
            get => _nvrRealTimePreviewItems;
            set => SetProperty(ref _nvrRealTimePreviewItems, value);
        }

        public HorizontalAlignment PlayerHorizontalAlignment {
            get => _playerHorizontalAlignment;
            set => SetProperty(ref _playerHorizontalAlignment, value);
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            //验证授权
            //加载相机
            //加载读码

            NvrRealTimePreviewItems.Clear();
            NvrRealTimePreviewItems.AddRange(new List<NvrRealTimePreviewItemInfo>()
            {
                new NvrRealTimePreviewItemInfo()
                {
                    IpAddress = "192.168.31.111",
                    Password = "a12345678",
                    Port = 37777,
                    Username = "admin",
                    Channel = 1
                },
                /*new NvrRealTimePreviewItemInfo()
                {
                    IpAddress = "192.168.31.111",
                    Password = "a12345678",
                    Port = 37777,
                    Username = "admin",
                    Channel = 3
                },
                new NvrRealTimePreviewItemInfo(),
                new NvrRealTimePreviewItemInfo()*/
            });

            _daHuatechNvr ??= DaHuatechNVR.Instance;
            var (key, value) = await _daHuatechNvr.LogIn("192.168.31.111", 37777, "admin", "a12345678");
            if (key) {
                var videoPlayerSize = GetVideoPlayerSize();
                Parallel.ForEach(NvrRealTimePreviewItems, async item => {
                    await Application.Current.Dispatcher.InvokeAsync(async () => {
                        if (!string.IsNullOrEmpty(item.IpAddress) &&
                            item is { Channel: > 0, Port: > 0 }) {
                            var (b, s) = await _daHuatechNvr.StartRealTimePreview(item.IpAddress, item.Channel, item.RealtimePreviewCallback);
                            if (b) {
                                _daHuatechNvr.SetResolution(item.IpAddress, item.Channel, (int)videoPlayerSize.Width, (int)videoPlayerSize.Height);
                                item.VideoFrame = new((int)videoPlayerSize.Width, (int)videoPlayerSize.Height, 96, 96, PixelFormats.Bgr24, null);
                                item.ToggleImageSizeCommand = ToggleImageSizeCommand;
                                item.PlaybackError = PlaybackError.None;
                            }
                            else {
                                item.PlaybackError = PlaybackError.StreamConnectionInterrupted;
                            }
                        }
                        else {
                            item.PlaybackError = PlaybackError.InvalidChannel;
                        }

                        item.IsBuffering = false;
                    });
                });
            }
        }

        public ICommand ToggleImageSizeCommand => new DelegateCommand<NvrRealTimePreviewItemInfo>(ToggleImageSizeDelegate);

        private void ToggleImageSizeDelegate(NvrRealTimePreviewItemInfo obj) {
            if (obj.ScreenState == ScreenState.Normal) {
                foreach (var videoPlayerModel in NvrRealTimePreviewItems) {
                    videoPlayerModel.ScreenState = !videoPlayerModel.Equals(obj) ? ScreenState.Hidden : ScreenState.Maximized;
                }
                if (_daHuatechNvr is not null) {
                    obj.VideoFrame = new WriteableBitmap((int)(obj.MaxSize.Width * 0.8),
                        (int)(obj.MaxSize.Height * 0.8), 96, 96, PixelFormats.Bgr24, null);
                    _daHuatechNvr.SetResolution(obj.IpAddress, obj.Channel, (int)(obj.MaxSize.Width * 0.8),
                        (int)(obj.MaxSize.Height * 0.8));
                }
            }
            else {
                var size = GetVideoPlayerSize();

                foreach (var videoPlayerModel in NvrRealTimePreviewItems) {
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
            var size = new Size(1200, 675);
            switch (NvrRealTimePreviewItems.Count) {
                case 1:
                    size = new Size(1200, 675);
                    break;

                case > 1 and <= 4:
                    size = new Size(614, 346);
                    break;

                case > 4:
                    size = new Size(449, 253);
                    break;
            }
            size = new Size(size.Width * 0.8, size.Height * 0.8);
            return size;
        }

        public ICommand ExpanderExpandedCommand => new DelegateCommand<object>(ExpanderExpandedDelegate);

        private void ExpanderExpandedDelegate(object obj) {
            PlayerHorizontalAlignment = HorizontalAlignment.Left;
        }

        public ICommand ExpanderCollapsedCommand => new DelegateCommand<object>(ExpanderCollapsedDelegate);

        private void ExpanderCollapsedDelegate(object obj) {
            PlayerHorizontalAlignment = HorizontalAlignment.Center;
        }
    }
}