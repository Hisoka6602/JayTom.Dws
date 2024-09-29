using System;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using System.Diagnostics;
using JayTom.Dws.License;
using System.Windows.Input;
using System.Windows.Media;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using JayTom.Dws.Interface.Cloud;
using Size = System.Drawing.Size;
using System.Windows.Media.Imaging;
using JayTom.Dws.Interface.License;
using JayTom.Dws.Client.Attributes;
using System.Collections.ObjectModel;
using JayTom.Dws.Domain.Dto.CloudDto;
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
using JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration;
using KeyboardDevice = JayTom.Dws.Plugin.Device.KeyboardDevice.KeyboardDevice;

namespace JayTom.Dws.Client.ViewModels.Pages {

    public class NvrHomePageViewModel : BindableBase {
        private readonly IDeviceService _deviceService;
        private readonly IKeyboardDeviceManager _keyboardDeviceManager;
        private readonly INvrCameraBindingRepository _nvrCameraBindingRepository;
        private readonly IConfigRepository _configRepository;
        private readonly IClientLicenseApi _clientLicenseApi;
        private readonly ICloud _cloud;
        private ObservableCollection<NvrRealTimePreviewItemInfo> _nvrRealTimePreviewItems = new();

        private DaHuatechNVR? _daHuatechNvr;
        private HorizontalAlignment _playerHorizontalAlignment = HorizontalAlignment.Left;
        private string _barCode = "包裹条码";
        private ObservableCollection<PackageItemModel> _packageItems = new();
        private SolidColorBrush _iconColor = (SolidColorBrush)(new BrushConverter().ConvertFromString("#4FFFFFFF"));
        private SnackbarMessageQueue _homeMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _itemIsExpanded = true;
        private bool? _isUnauthorized;
        private CloudVideoSettingsDto _cloudVideoSettingsDto = new();
        private bool _isRealTimeVideoEnabled = true;
        private ObservableCollection<PlaybackStream> _playbackStreamItems = new(Enum.GetValues(typeof(PlaybackStream)).Cast<PlaybackStream>());
        private PlaybackStream _selectPlaybackStream = PlaybackStream.MainStream;

        public NvrHomePageViewModel(IDeviceService deviceService,
            IKeyboardDeviceManager keyboardDeviceManager,
            INvrCameraBindingRepository nvrCameraBindingRepository,
            IConfigRepository configRepository,
            IClientLicenseApi clientLicenseApi,
            ICloud cloud) {
            _deviceService = deviceService;
            _keyboardDeviceManager = keyboardDeviceManager;
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
            _configRepository = configRepository;
            _clientLicenseApi = clientLicenseApi;
            _cloud = cloud;

            _daHuatechNvr ??= DaHuatechNVR.Instance;
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                if (item is { Type: WindowsActionType.Close }) {
                    await SetRealTimeVideo(false);

                    await _deviceService.Stop();
                }
                else if (item is { Type: WindowsActionType.EnterSettings }) {
                    await SetRealTimeVideo(false);
                    await _deviceService.Stop();
                    AppContext.SetData("IsRunning", false);
                    EventAggregator.Instance.Publish(new ApplicationStatusChanged {
                        Status = ApplicationStatus.Stop
                    });
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
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                if (item is { } model) {
                    switch (model.SettingsName) {
                        case "CreatePackageSettings":
                            var defaultEntity = await _configRepository.FirstOrDefaultEntity<CreatePackageSettingsDto>(model.SettingsName) ??
                                                           new CreatePackageSettingsDto();
                            await _cloud.SubmitCloudConfiguration(model.SettingsName, defaultEntity, "/api/Config/SaveConfig");

                            break;

                        case "ContentInputSettings":
                            var contentInputSettingsDto = await _configRepository.FirstOrDefaultEntity<ContentInputSettingsDto>(model.SettingsName) ??
                                                          new ContentInputSettingsDto();
                            await _cloud.SubmitCloudConfiguration(model.SettingsName, contentInputSettingsDto, "/api/Config/SaveConfig");
                            break;

                        case "BarcodeFilterSettings":
                            var barcodeFilterSettingsDto = await _configRepository.FirstOrDefaultEntity<BarcodeFilterSettingsDto>(model.SettingsName) ??
                                                           new BarcodeFilterSettingsDto();
                            await _cloud.SubmitCloudConfiguration(model.SettingsName, barcodeFilterSettingsDto, "/api/Config/SaveConfig");

                            break;

                        case "CloudVideoSettings":
                            _cloudVideoSettingsDto = await _configRepository.FirstOrDefaultEntity<CloudVideoSettingsDto>(model.SettingsName) ?? new CloudVideoSettingsDto();
                            await _cloud.SetParameters(new Dictionary<string, object>()
                              {
                                { "WebDoMain", _cloudVideoSettingsDto.WebDoMain },
                                { "Timeout", _cloudVideoSettingsDto.RequestTimeout },
                            });
                            break;
                    }
                }
            });
            //更新云视频上传状态
            EventAggregator.Instance.Subscribe<CloudVideoUploadMessage>(async item => {
                if (item is { } model) {
                    await Application.Current.Dispatcher.InvokeAsync(async () => {
                        var barCodeItemModel = PackageItems.FirstOrDefault(f => f.Barcode.Equals(item.Barcode) &&
                            f.ScanTime.Equals(item.ScanTime));
                        if (barCodeItemModel is not null) {
                            barCodeItemModel.IsUploadedToCloudVideo = item.IsSuccessful;
                        }
                    }, DispatcherPriority.Render);
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

        /// <summary>
        /// 是否使用实时视频
        /// </summary>
        public bool IsRealTimeVideoEnabled {
            get => _isRealTimeVideoEnabled;
            set => SetProperty(ref _isRealTimeVideoEnabled, value);
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

        public bool? IsUnauthorized {
            get => _isUnauthorized;
            set => SetProperty(ref _isUnauthorized, value);
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

        public ObservableCollection<PlaybackStream> PlaybackStreamItems {
            get => _playbackStreamItems;
            set => SetProperty(ref _playbackStreamItems, value);
        }

        public PlaybackStream SelectPlaybackStream {
            get => _selectPlaybackStream;
            set => SetProperty(ref _selectPlaybackStream, value);
        }

        public HorizontalAlignment PlayerHorizontalAlignment {
            get => _playerHorizontalAlignment;
            set => SetProperty(ref _playerHorizontalAlignment, value);
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            if (string.IsNullOrEmpty(_cloudVideoSettingsDto.WebDoMain)) {
                _cloudVideoSettingsDto = await _configRepository.FirstOrDefaultEntity<CloudVideoSettingsDto>("CloudVideoSettings")
                                         ?? new CloudVideoSettingsDto();
                await _cloud.SetParameters(new Dictionary<string, object>()
                {
                    { "WebDoMain", _cloudVideoSettingsDto.WebDoMain },
                    { "Timeout", _cloudVideoSettingsDto.RequestTimeout },
                });
            }

            //授权
#if !DEBUG
            if (IsUnauthorized != false) {
                var licenseDirectory = Path.Combine(AppContext.BaseDirectory, "License");
                if (!Directory.Exists(licenseDirectory)) {
                    Directory.CreateDirectory(licenseDirectory);
                }
                var firstOrDefault = Directory.GetFiles(licenseDirectory, "*.key").FirstOrDefault();
                if (firstOrDefault is not null) {
                    //解密授权
                    var (b, s) = LicenseManager.DecryptAuthorizationFile(firstOrDefault, out var data);

                    if (data is not null) {
                        //重新下载
                        Task.Run(async () => {
                            var (key1, o) = await _clientLicenseApi.CreateAuthorization(data.LicenseCode, data.MachineCode, data.Remarks);
                            if (o is ApiResult result &&
                                !string.IsNullOrEmpty(result.Data?.ToString() ?? string.Empty)) {
                                if (key1) {
                                    var licenseDirectory = Path.Combine(AppContext.BaseDirectory, "License");
                                    var files = Directory.GetFiles(licenseDirectory, "*.key");
                                    Parallel.ForEach(files, File.Delete);

                                    await _clientLicenseApi.DownloadFileAsync(result.Data?.ToString() ?? string.Empty,
                                        $"{licenseDirectory}\\License.key");
                                }
                            }
                        });
                    }
                    if (!b) {
                        IsUnauthorized = true;
                        EventAggregator.Instance.Publish(new AppLogInfoModel {
                            CreateTime = DateTime.Now,
                            Message = s,
                            Type = LogType.Exception
                        });
                        HomeMessageQueue.Enqueue(s);
                        return;
                    }
                    else {
                        IsUnauthorized = false;
                        //提交激活
                        if (data is not null) {
                            Task.Run(async () => {
                                await _clientLicenseApi.ActivateAuthorization(data.LicenseCode, data.MachineCode, data.Remarks);
                            });
                        }
                    }
                }
                else {
                    IsUnauthorized = true;
                    EventAggregator.Instance.Publish(new AppLogInfoModel {
                        CreateTime = DateTime.Now,
                        Message = "未检测到授权文件",
                        Type = LogType.Exception
                    });
                    HomeMessageQueue.Enqueue("未检测到授权文件");
                    return;
                }
            }

#endif
            if (IsRealTimeVideoEnabled) {
                await SetRealTimeVideo(true);
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

        public ICommand SwitchQualityCommand => new DelegateCommand<NvrRealTimePreviewItemInfo>(SwitchQualityDelegate);

        private void SwitchQualityDelegate(NvrRealTimePreviewItemInfo obj) {
            //获取分辨率尺寸
            //设置分辨率
            SetQuality(obj);
        }

        public ICommand ToggleImageSizeCommand => new DelegateCommand<NvrRealTimePreviewItemInfo>(ToggleImageSizeDelegate);

        private void ToggleImageSizeDelegate(NvrRealTimePreviewItemInfo obj) {
            ItemIsExpanded = obj.ScreenState != ScreenState.Normal;
            if (obj.ScreenState == ScreenState.Normal) {
                foreach (var videoPlayerModel in NvrRealTimePreviewItems) {
                    videoPlayerModel.ScreenState = !videoPlayerModel.Equals(obj) ? ScreenState.Hidden : ScreenState.Maximized;
                }
                if (_daHuatechNvr is not null) {
                    obj.VideoQuality = VideoQuality.Original;
                    SetQuality(obj);
                }
            }
            else {
                var (key, value) = GetVideoPlayerQuality();

                foreach (var videoPlayerModel in NvrRealTimePreviewItems) {
                    videoPlayerModel.ScreenState = ScreenState.Normal;
                    if (_daHuatechNvr is not null && videoPlayerModel.PlaybackError == PlaybackError.None) {
                        videoPlayerModel.VideoQuality = key;
                        SetQuality(videoPlayerModel);
                    }
                }
            }
        }

        private KeyValuePair<VideoQuality, System.Drawing.Size> GetVideoPlayerQuality() {
            var size = VideoQuality.FullHd.GetResolution();
            switch (NvrRealTimePreviewItems.Count) {
                case 1:
                    size = VideoQuality.FullHd.GetResolution();
                    return new KeyValuePair<VideoQuality, Size>(VideoQuality.FullHd, size);

                case > 1 and <= 4:
                    size = VideoQuality.Standard.GetResolution();
                    return new KeyValuePair<VideoQuality, Size>(VideoQuality.Standard, size);

                case > 4:
                    size = VideoQuality.Smooth.GetResolution();
                    return new KeyValuePair<VideoQuality, Size>(VideoQuality.Smooth, size);
            }
            return new KeyValuePair<VideoQuality, Size>(VideoQuality.FullHd, size);
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

        public ICommand ToggleRealTimeVideoCommand => new DelegateCommand<object>(ToggleRealTimeVideoDelegate);

        private async void ToggleRealTimeVideoDelegate(object obj) {
            await SetRealTimeVideo(IsRealTimeVideoEnabled);
        }

        private async Task SetRealTimeVideo(bool isEnabled) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                if (isEnabled) {
                    NvrRealTimePreviewItems.Clear();
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
                                DisplayName = $"通道:{s.Channel + 1}",
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
                            var (key, value) = await _daHuatechNvr.LogIn(nvrRealTimePreviewItemInfo.IpAddress, nvrRealTimePreviewItemInfo.Port, nvrRealTimePreviewItemInfo.Username, nvrRealTimePreviewItemInfo.Password);
                            if (key) {
                                var (videoQuality, size) = GetVideoPlayerQuality();
                                Parallel.ForEach(NvrRealTimePreviewItems, async item => {
                                    await Application.Current.Dispatcher.InvokeAsync(async () => {
                                        if (!string.IsNullOrEmpty(item.IpAddress) &&
                                            item is { Channel: >= 0, Port: > 0 }) {
                                            var (b, s) = await _daHuatechNvr.StartRealTimePreview(item.IpAddress, item.Channel, item.RealtimePreviewCallback);
                                            if (b) {
                                                item.VideoQuality = videoQuality;
                                                _daHuatechNvr.SetResolution(item.IpAddress, item.Channel, (int)size.Width, (int)size.Height);
                                                item.VideoFrame = new((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Bgr24, null);
                                                item.ToggleImageSizeCommand = ToggleImageSizeCommand;
                                                item.SwitchQualityCommand = SwitchQualityCommand;
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
                }
                else {
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
            });
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

        /// <summary>
        /// 设置清晰度
        /// </summary>
        /// <param name="obj"></param>
        private async void SetQuality(NvrRealTimePreviewItemInfo obj) {
            var resolution = obj.VideoQuality.GetResolution();
            if (_daHuatechNvr is not null) {
                obj.VideoFrame = new WriteableBitmap((int)(resolution.Width),
                    (int)(resolution.Height), 96, 96, PixelFormats.Bgr24, null);
                _daHuatechNvr.SetResolution(obj.IpAddress, obj.Channel, (int)(resolution.Width),
                    (int)(resolution.Height));
            }
        }
    }
}