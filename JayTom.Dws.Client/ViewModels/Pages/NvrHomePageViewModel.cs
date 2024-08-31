using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Client.Models.DataModels;
using LibreHardwareMonitor.Hardware.Storage;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Plugin.Device.KeyboardDevice;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Client.Models.Cameras.CameraConfiguration;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.NVR;

namespace JayTom.Dws.Client.ViewModels.Pages {

    public class NvrHomePageViewModel : BindableBase {
        private readonly IDeviceService _deviceService;
        private readonly IKeyboardDeviceManager _keyboardDeviceManager;
        private readonly INvrCameraBindingRepository _nvrCameraBindingRepository;
        private readonly IConfigRepository _configRepository;
        private ObservableCollection<NvrRealTimePreviewItemInfo> _nvrRealTimePreviewItems = new();

        private DaHuatechNVR? _daHuatechNvr;
        private HorizontalAlignment _playerHorizontalAlignment = HorizontalAlignment.Left;
        private string _barCode = "包裹条码";
        private ObservableCollection<PackageItemModel> _packageItems = new();
        private SolidColorBrush _iconColor = (SolidColorBrush)(new BrushConverter().ConvertFromString("#4FFFFFFF"));
        private SnackbarMessageQueue _homeMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _itemIsExpanded = true;

        public NvrHomePageViewModel(IDeviceService deviceService,
            IKeyboardDeviceManager keyboardDeviceManager,
                INvrCameraBindingRepository nvrCameraBindingRepository,
            IConfigRepository configRepository) {
            _deviceService = deviceService;
            _keyboardDeviceManager = keyboardDeviceManager;
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
            _configRepository = configRepository;
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
                        await _daHuatechNvr.LogOut(NvrRealTimePreviewItems.FirstOrDefault()?.IpAddress ?? string.Empty);

                        await _deviceService.Stop();
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
                        await _daHuatechNvr.LogOut(NvrRealTimePreviewItems.FirstOrDefault()?.IpAddress ?? string.Empty);
                    }
                }
                else if (item is { Type: WindowsActionType.ReturnToHome }) {
                    LoadedDelegate(null);
                }
            });
            EventAggregator.Instance.Subscribe<PackageInfo>(async info => {
                //填充数据到列表
                await Task.Yield();
                if (info is { } model) {
                    AddNewRow(new PackageItemModel() {
                        Barcode = model.BarCodeInfo?.Barcode ?? string.Empty,
                        ScanTime = model.BarCodeInfo?.ScanTime ?? DateTime.Now,
                        Weight = (float)(model.WeightInfo?.FormattedWeight ?? 0),
                        Length = (float)(model.VolumeInfo?.FormattedLength ?? 0),
                        Width = (float)(model.VolumeInfo?.FormattedWidth ?? 0),
                        Height = (float)(model.VolumeInfo?.FormattedHeight ?? 0),
                        Volume = (float)(model.VolumeInfo?.FormattedVolume ?? 0),
                        TimestampedGuid = model.Timestamp
                    });
                }
            });
            _deviceService.BarCodeKeyReceived += async (sender, args) => {
                //显示条码
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    BarCode = args.Barcode;
                });
            };
            _deviceService.DeviceException += async (sender, args) => {
                //提示异常
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    if (args.ExceptionMessage is not null) {
                        HomeMessageQueue.Enqueue(args.ExceptionMessage.Message);
                    }
                });
            };
        }

        public string BarCode {
            get => _barCode;
            set => SetProperty(ref _barCode, value);
        }

        public SolidColorBrush IconColor {
            get => _iconColor;
            set => SetProperty(ref _iconColor, value);
        }

        public bool ItemIsExpanded {
            get => _itemIsExpanded;
            set => SetProperty(ref _itemIsExpanded, value);
        }

        public SnackbarMessageQueue HomeMessageQueue {
            get => _homeMessageQueue;
            set => SetProperty(ref _homeMessageQueue, value);
        }

        public ObservableCollection<PackageItemModel> PackageItems {
            get => _packageItems;
            set => SetProperty(ref _packageItems, value);
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
            NvrRealTimePreviewItems.Clear();

            //验证授权
            //加载相机
            //加载读码

            //获取绑定的相机
            var settingsDto = await _configRepository.FirstOrDefaultEntity<ContentInputSettingsDto>("ContentInputSettings") ?? new ContentInputSettingsDto();
            if (settingsDto is { IsUseBarcodeScannerInput: true, KeyboardDevice: { ProductId: > 0, VendorId: > 0 } }) {
                var nvrCameraBindingInfoModels = await _nvrCameraBindingRepository.MemoryCacheData();
                var nvrRealTimePreviewItemInfos = nvrCameraBindingInfoModels.Where(w => w.SerialNumber.Equals(settingsDto.KeyboardDevice.DevicePath))
                    .Select(s => new NvrRealTimePreviewItemInfo {
                        IpAddress = s.IpAddress,
                        Password = s.Password,
                        Port = s.Port,
                        Username = s.Username,
                        Channel = s.Channel,
                        RealtimePreviewOperationCommand = RealtimePreviewOperationCommand,
                        IsBuffering = true,
                    }).OrderBy(o => o.Channel).ToList();
                NvrRealTimePreviewItems.AddRange(nvrRealTimePreviewItemInfos);
                if (NvrRealTimePreviewItems.Count is > 1 and < 4) {
                    var itemsToAdd = 4 - NvrRealTimePreviewItems.Count;

                    for (var i = 0; i < itemsToAdd; i++) {
                        NvrRealTimePreviewItems.Add(new NvrRealTimePreviewItemInfo());
                    }
                }

                var nvrRealTimePreviewItemInfo = nvrRealTimePreviewItemInfos.FirstOrDefault(f => !string.IsNullOrEmpty(f.IpAddress) &&
                    !string.IsNullOrEmpty(f.Username) &&
                    !string.IsNullOrWhiteSpace(f.Password) &&
                    f.Port > 0);
                if (nvrRealTimePreviewItemInfo is not null) {
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
            }

            if (!_keyboardDeviceManager.IsListening) {
                //启动设备
                var (key1, value1) = await _deviceService.Start();
                EventAggregator.Instance.Publish(new ApplicationStatusChanged {
                    Status = ApplicationStatus.Start
                });
                AppContext.SetData("IsRunning", true);
                if (_keyboardDeviceManager.IsListening) {
                    IconColor = (SolidColorBrush)(new BrushConverter().ConvertFromString("#2E8B57"));
                }
            }
        }

        public ICommand ToggleImageSizeCommand => new DelegateCommand<NvrRealTimePreviewItemInfo>(ToggleImageSizeDelegate);

        private void ToggleImageSizeDelegate(NvrRealTimePreviewItemInfo obj) {
            ItemIsExpanded = obj.ScreenState != ScreenState.Normal;
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

        public ICommand RealtimePreviewOperationCommand => new DelegateCommand<RealtimePreviewOperationParameters>(RealtimePreviewOperationDelegate);

        private async void RealtimePreviewOperationDelegate(RealtimePreviewOperationParameters obj) {
            if (obj.ItemInfo is not null && _daHuatechNvr is not null) {
                switch (obj.Action) {
                    case NvrPreviewAction.IncreaseZoom:
                        await _daHuatechNvr.AdjustZoomContinuouslyAsync(obj.ItemInfo.IpAddress,
                             obj.ItemInfo.Channel, true, obj.Type == NvrPreviewOperationType.Stop);
                        break;

                    case NvrPreviewAction.DecreaseZoom:
                        await _daHuatechNvr.AdjustZoomContinuouslyAsync(obj.ItemInfo.IpAddress,
                            obj.ItemInfo.Channel, false, obj.Type == NvrPreviewOperationType.Stop);
                        break;

                    case NvrPreviewAction.IncreaseFocus:
                        await _daHuatechNvr.AdjustPtzFocusContinuouslyAsync(obj.ItemInfo.IpAddress,
                            obj.ItemInfo.Channel,
                            true, obj.Type == NvrPreviewOperationType.Stop);
                        break;

                    case NvrPreviewAction.DecreaseFocus:
                        await _daHuatechNvr.AdjustPtzFocusContinuouslyAsync(obj.ItemInfo.IpAddress,
                            obj.ItemInfo.Channel,
                            false, obj.Type == NvrPreviewOperationType.Stop);
                        break;

                    case NvrPreviewAction.AutoFocus:
                        await _daHuatechNvr.AutoFocusAsync(obj.ItemInfo.IpAddress,
                            obj.ItemInfo.Channel);
                        break;
                }
            }
        }

        private async void AddNewRow(PackageItemModel item) {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                var num = PackageItems.FirstOrDefault()?.Num ?? 0;
                item.Num = num + 1;

                PackageItems.Insert(0, item);
                if (PackageItems.Count > 50) {
                    PackageItems.RemoveAt(PackageItems.Count - 1);
                }
                //item.IsInserting = true;
            }, DispatcherPriority.Background);
        }
    }
}