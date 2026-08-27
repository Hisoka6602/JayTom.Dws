using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows;
using Prism.Commands;
using JayTom.Dws.Ocr;
using JayTom.Dws.Models;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Models.LocalConf;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Models.LocalConf.CameraConfig;
using JayTom.Dws.Application.CameraConfigurations;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.SubHomeViewModels
{

    public class CameraHomeViewModel : BindableBase
    {
        private readonly IDeviceService _deviceService;
        private readonly ICameraConfigurationCatalog<BarcodeScannerCameraConfigInfoModel> _barcodeScannerCameraConfigRepository;
        private readonly ICameraConfigurationCatalog<PanoramaCameraConfigInfoModel> _panoramaCameraConfigRepository;
        private readonly ICameraConfigurationCatalog<UsbCameraConfigInfoModel> _usbCameraConfigRepository;
        private readonly ICameraConfigurationCatalog<VolumeCameraConfigInfoModel> _volumeCameraConfigRepository;

        /// <summary>
        /// 当前可见相机索引，供图像回调无锁查询。
        /// </summary>
        private readonly ConcurrentDictionary<string, CameraItemInfoModel> _visibleCameraItems =
            new(StringComparer.Ordinal);

        private ObservableCollection<CameraItemInfoModel> _cameraItems = new();

        private ObservableCollection<CameraItemInfoModel> _hiddenCameraItems = new();

        public ObservableCollection<CameraItemInfoModel> CameraItems
        {
            get => _cameraItems;
            set => SetProperty(ref _cameraItems, value);
        }

        public ObservableCollection<CameraItemInfoModel> HiddenCameraItems
        {
            get => _hiddenCameraItems;
            set => SetProperty(ref _hiddenCameraItems, value);
        }

        public CameraHomeViewModel(IDeviceService deviceService,
            ICameraConfigurationCatalog<BarcodeScannerCameraConfigInfoModel> barcodeScannerCameraConfigRepository,
            ICameraConfigurationCatalog<PanoramaCameraConfigInfoModel> panoramaCameraConfigRepository,
            ICameraConfigurationCatalog<UsbCameraConfigInfoModel> usbCameraConfigRepository,
            ICameraConfigurationCatalog<VolumeCameraConfigInfoModel> volumeCameraConfigRepository)
        {
            _deviceService = deviceService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _panoramaCameraConfigRepository = panoramaCameraConfigRepository;
            _usbCameraConfigRepository = usbCameraConfigRepository;
            _volumeCameraConfigRepository = volumeCameraConfigRepository;
            //判断启停
            _deviceService.CameraStarted += async (sender, args) =>
            {
                await Task.Delay(500).ConfigureAwait(false);
                if (_visibleCameraItems.TryGetValue(args.CameraInfo?.SerialNumber ?? string.Empty,
                        out var model))
                {
                    await UiThread.Dispatcher.BeginInvoke(() =>
                    {
                        model.IsRealtimeImageEnabled = args.Camera?.IsRealtimeImageEnabled ?? false;
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            };
            _deviceService.CameraInitialized += async delegate (object? sender, IReadOnlyList<ICamera> list)
            {
                var barcodeCameraConfigs = await _barcodeScannerCameraConfigRepository.MemoryCacheData()
                    .ConfigureAwait(false);
                var panoramaCameraConfigs = await _panoramaCameraConfigRepository.MemoryCacheData()
                    .ConfigureAwait(false);
                var volumeCameraConfigs = await _volumeCameraConfigRepository.MemoryCacheData()
                    .ConfigureAwait(false);
                var usbCameraConfigs = await _usbCameraConfigRepository
                    .Select(_ => true, 0, 1000)
                    .ConfigureAwait(false);
                var cameraDisplayStatuses = barcodeCameraConfigs
                    .Cast<BaseCameraConfigInfoModel>()
                    .Concat(panoramaCameraConfigs)
                    .Concat(volumeCameraConfigs)
                    .Concat(usbCameraConfigs)
                    .Where(item => !string.IsNullOrEmpty(item.SerialNumber))
                    .GroupBy(item => item.SerialNumber, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.First().CameraDisplayStatus,
                        StringComparer.Ordinal);

                await UiThread.Dispatcher.BeginInvoke(() =>
                {
                    foreach (var item in CameraItems.Concat(HiddenCameraItems).Distinct())
                    {
                        item.Dispose();
                    }
                    _visibleCameraItems.Clear();
                    CameraItems.Clear();
                    HiddenCameraItems.Clear();
                    var infoModels = list.Select(s => new CameraItemInfoModel
                    {
                        ConnectionType = (s?.Info?.ConnectionType ?? CameraConnectionType.Ethernet),
                        CameraName = $"{s?.Info?.Brand}:{s?.Info?.SerialNumber}" ?? string.Empty,
                        Type = s?.Info?.Type ?? JayTom.Dws.Camera.CameraType.IndustrialCamera,
                        Status = CameraStatus.Running,
                        CameraIdentifier = (s?.Info?.Id)?.ToString() ?? string.Empty,
                        SerialNumber = s?.Info?.SerialNumber ?? string.Empty,
                        Camera = s,
                        StatusClickCommand = StatusClickCommand,
                        TakePhotoCommand = TakePhotoCommand,
                        SwitchRealtimeImageCommand = SwitchRealtimeImageCommand,
                        IsRealtimeImageEnabled = s?.IsRealtimeImageEnabled ?? false,
                        BindingType = s?.BindingType ?? new CameraBindingType(),

                        CameraDisplayStatus = cameraDisplayStatuses.GetValueOrDefault(
                            s?.Info?.SerialNumber ?? string.Empty, CameraDisplayStatus.Visible),
                        HideCommand = HideCommand,
                        ShowCommand = ShowCommand
                    })?.ToList();
                    var visibleItems = infoModels?
                        .Where(item => item.CameraDisplayStatus == CameraDisplayStatus.Visible)
                        .ToList() ?? new List<CameraItemInfoModel>();
                    CameraItems.AddRange(visibleItems);
                    foreach (var visibleItem in visibleItems)
                    {
                        _visibleCameraItems[visibleItem.SerialNumber] = visibleItem;
                    }

                    HiddenCameraItems.AddRange(infoModels?.Where(w => w.CameraDisplayStatus == CameraDisplayStatus.Hidden));
                });
            };
            _deviceService.CameraReleased += async delegate (object? sender, string s)
            {
                _visibleCameraItems.TryRemove(s, out _);
                await UiThread.Dispatcher.BeginInvoke(() =>
                {
                    var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(s));
                    if (model != null)
                    {
                        CameraItems.Remove(model);
                        model.Dispose();
                    }
                    else
                    {
                        var cameraItemInfoModel = HiddenCameraItems.FirstOrDefault(f => f.SerialNumber.Equals(s));
                        if (cameraItemInfoModel != null)
                        {
                            HiddenCameraItems.Remove(cameraItemInfoModel);
                            cameraItemInfoModel.Dispose();
                        }
                    }
                });
            };
            _deviceService.BarcodeMissed += delegate (object? sender, BarcodeReadEventArgs args)
            {
                if (_visibleCameraItems.TryGetValue(args.CameraSerialNumber, out var model) &&
                    model.Image is not null &&
                    args.ThumbImage is not null)
                {
                    model.TryEnqueueImage(args.ThumbImage, args.Timestamp);
                }
            };
            _deviceService.BarcodeScanned += DeviceServiceOnBarcodeScanned;
            _deviceService.RealTimeImage += DeviceServiceOnRealTimeImage;
            _deviceService.PanoramaCaptured += DeviceServiceOnPanoramaCaptured;
            _deviceService.CameraDisconnected += delegate (object? sender, IReadOnlyList<ICamera> list)
            {
                //更新现有列表,例如删除相机成员
            };
            _deviceService.VolumeCaptured += DeviceServiceOnVolumeCaptured;
            _deviceService.OcrContentRecognized += DeviceServiceOnOcrContentRecognized;
        }

        /// <summary>
        /// 状态点击事件
        /// </summary>
        public ICommand? StatusClickCommand => new DelegateCommand<CameraItemInfoModel>(StatusClickDelegate);

        private async void StatusClickDelegate(CameraItemInfoModel obj)
        {
            //先加载进度条
            //临时截图
            if (obj.Camera is IIndustrialCamera industrialCamera)
            {
                await industrialCamera.TakePhotoAsync(string.Empty, 0);
            }
            else if (obj.Camera is ISecurityCamera securityCamera)
            {
                await securityCamera.TakePhotoAsync(string.Empty, 0);
            }
        }

        /// <summary>
        /// 拍照
        /// </summary>
        public ICommand? TakePhotoCommand => new DelegateCommand<CameraItemInfoModel>(TakePhotoDelegate);

        private async void TakePhotoDelegate(CameraItemInfoModel obj)
        {
            if (obj.Camera is { } camera)
            {
                camera.StopRealTimeImage();
                obj.IsRealtimeImageEnabled = camera.IsRealtimeImageEnabled;
                await camera.TakePhotoAsync(string.Empty, 0);
            }
        }

        /// <summary>
        /// 开关实时图像
        /// </summary>
        public ICommand? SwitchRealtimeImageCommand => new DelegateCommand<CameraItemInfoModel>(SwitchRealtimeImageDelegate);

        private async void SwitchRealtimeImageDelegate(CameraItemInfoModel obj)
        {
            if (obj.Camera is { } camera)
            {
                if (camera.IsRealtimeImageEnabled)
                {
                    camera.StopRealTimeImage();
                }
                else
                {
                    camera.StartRealTimeImage();
                }
                obj.IsRealtimeImageEnabled = camera.IsRealtimeImageEnabled;

                //保存到数据库
                if (camera.BindingType == CameraBindingType.ScannerCamera)
                {
                    var configInfoModel = await _barcodeScannerCameraConfigRepository.FirstOrDefault(f =>
                        camera.Info != null && f.SerialNumber.Equals(camera.Info.SerialNumber));
                    if (configInfoModel != null)
                    {
                        configInfoModel.IsShowRealTimeImage = camera.IsRealtimeImageEnabled;
                        await _barcodeScannerCameraConfigRepository.InsertOrUpdate(configInfoModel);
                    }
                }
            }
        }

        /// <summary>
        /// 图像点击事件
        /// </summary>
        public ICommand ImageClickCommand => new DelegateCommand<CameraItemInfoModel>(ImageClickDelegate);

        private async void ImageClickDelegate(CameraItemInfoModel obj)
        {
            //放大图片(用另一个图像框显示、并重新绑定接收图像来源、过渡动画)
            /*await UiThread.Dispatcher.BeginInvoke(() => {
                AddNewRow(new BarCodeItemModel() {
                    Barcode = new Random().Next(100000000, 999999999).ToString()
                });
            });*/
        }

        public ICommand LoadedCommand => new DelegateCommand<UserControl>(LoadedDelegate);

        private async void LoadedDelegate(UserControl obj)
        {
            /*CameraItems = new ObservableCollection<CameraItemInfoModel>()
            {
                new CameraItemInfoModel()
                {
                    Type = CameraType.SmartCamera,
                    HideCommand = HideCommand,
                    ShowCommand = ShowCommand
                },
                new CameraItemInfoModel()
                {
                    Type = CameraType.SmartCamera,
                    HideCommand = HideCommand,
                    ShowCommand = ShowCommand
                },
                new CameraItemInfoModel()
                {
                    Type = CameraType.IndustrialCamera,
                    HideCommand = HideCommand,
                    ShowCommand = ShowCommand
                },
                new CameraItemInfoModel()
                {
                    Type = CameraType.ThreeDCamera,
                    HideCommand = HideCommand,
                    ShowCommand = ShowCommand
                },
                new CameraItemInfoModel()
                {
                    Type = CameraType.ThreeDCamera,
                    HideCommand = HideCommand,
                    ShowCommand = ShowCommand
                },
            };
            HiddenCameraItems = new ObservableCollection<CameraItemInfoModel>()
            {
                new CameraItemInfoModel()
                {
                    Type = CameraType.SmartCamera,
                    HideCommand = HideCommand,
                    ShowCommand = ShowCommand
                },
                new CameraItemInfoModel() {
                    Type = CameraType.IndustrialCamera,
                    HideCommand = HideCommand,
                    ShowCommand = ShowCommand
                },
                new CameraItemInfoModel() {
                    Type = CameraType.ThreeDCamera,
                    HideCommand = HideCommand,
                    ShowCommand = ShowCommand
                },
            };*/
        }

        /// <summary>
        /// 隐藏画面
        /// </summary>
        public ICommand? HideCommand => new DelegateCommand<CameraItemInfoModel>(HideDelegate);

        private async void HideDelegate(CameraItemInfoModel obj)
        {
            //加载到隐藏中
            if (!CameraItems.Remove(obj))
            {
                return;
            }

            _visibleCameraItems.TryRemove(obj.SerialNumber, out _);
            HiddenCameraItems.Add(obj);
            await UpdateCameraDisplayStatusAsync(obj.SerialNumber, CameraDisplayStatus.Hidden);
        }

        /// <summary>
        /// 点击显示事件
        /// </summary>
        public ICommand? ShowCommand => new DelegateCommand<CameraItemInfoModel>(ShowDelegate);

        private async void ShowDelegate(CameraItemInfoModel obj)
        {
            //加载到显示中
            if (!HiddenCameraItems.Remove(obj))
            {
                return;
            }

            CameraItems.Add(obj);
            _visibleCameraItems[obj.SerialNumber] = obj;
            await UpdateCameraDisplayStatusAsync(obj.SerialNumber, CameraDisplayStatus.Visible);
        }

        /// <summary>
        /// 在后台更新指定相机的显示状态。
        /// </summary>
        /// <param name="serialNumber">相机序列号。</param>
        /// <param name="displayStatus">目标显示状态。</param>
        private async Task UpdateCameraDisplayStatusAsync(string serialNumber,
            CameraDisplayStatus displayStatus)
        {
            var barcodeCameraConfig = (await _barcodeScannerCameraConfigRepository.MemoryCacheData()
                    .ConfigureAwait(false))
                .FirstOrDefault(item => item.SerialNumber.Equals(serialNumber));
            if (barcodeCameraConfig is not null)
            {
                barcodeCameraConfig.CameraDisplayStatus = displayStatus;
                await _barcodeScannerCameraConfigRepository.InsertOrUpdate(barcodeCameraConfig)
                    .ConfigureAwait(false);
                return;
            }

            var panoramaCameraConfig = (await _panoramaCameraConfigRepository.MemoryCacheData()
                    .ConfigureAwait(false))
                .FirstOrDefault(item => item.SerialNumber.Equals(serialNumber));
            if (panoramaCameraConfig is not null)
            {
                panoramaCameraConfig.CameraDisplayStatus = displayStatus;
                await _panoramaCameraConfigRepository.InsertOrUpdate(panoramaCameraConfig)
                    .ConfigureAwait(false);
                return;
            }

            var usbCameraConfig = await _usbCameraConfigRepository
                .FirstOrDefault(item => item.SerialNumber.Equals(serialNumber))
                .ConfigureAwait(false);
            if (usbCameraConfig is not null)
            {
                usbCameraConfig.CameraDisplayStatus = displayStatus;
                await _usbCameraConfigRepository.InsertOrUpdate(usbCameraConfig)
                    .ConfigureAwait(false);
                return;
            }

            var volumeCameraConfig = (await _volumeCameraConfigRepository.MemoryCacheData()
                    .ConfigureAwait(false))
                .FirstOrDefault(item => item.SerialNumber.Equals(serialNumber));
            if (volumeCameraConfig is not null)
            {
                volumeCameraConfig.CameraDisplayStatus = displayStatus;
                await _volumeCameraConfigRepository.InsertOrUpdate(volumeCameraConfig)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Ocr识别到内容触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void DeviceServiceOnOcrContentRecognized(object? sender, OcrResult args)
        {
            //更新图片

            if (_visibleCameraItems.TryGetValue(args.CameraSerialNumber, out var model) &&
                model.Type is CameraType.IndustrialCamera or CameraType.SmartCamera)
            {
                //图片转换
                if (args.Thumbnail is not null &&
                    model.Image is not null)
                {
                    if (!model.IsRealtimeImageEnabled)
                    {
                        var thumbnail = OcrBitmapAdapter.Decode(args.Thumbnail);
                        if (!model.TryEnqueueImage(thumbnail, args.RecognitionTimestamp))
                        {
                            thumbnail.Dispose();
                        }
                    }
                }
            }
        }

        private void DeviceServiceOnVolumeCaptured(object? sender, VolumeCapturedEventArgs args)
        {
            if (_visibleCameraItems.TryGetValue(args.CameraSerialNumber, out var model) &&
                args.Thumbnail is not null)
            {
                if (!model.IsRealtimeImageEnabled)
                {
                    model.EnqueueImage(args.Thumbnail);
                }
            }
        }

        private void DeviceServiceOnPanoramaCaptured(object? sender, PanoramaCaptureEventArgs args)
        {
            //全景相机
            if (_visibleCameraItems.TryGetValue(args.CameraSerialNumber, out var model) &&
                model.BindingType is CameraBindingType.PanoramaCamera &&
                model.Image is not null)
            {
                //图片转换
                if (args.ThumbImage is not null)
                {
                    if (!model.IsRealtimeImageEnabled)
                    {
                        model.TryEnqueueImage(args.ThumbImage, args.Timestamp);
                    }
                }
            }
        }

        private void DeviceServiceOnRealTimeImage(object? sender, RealTimeImageEventArgs args)
        {
            //实时画面
            if (_visibleCameraItems.TryGetValue(args.Camera?.Info?.SerialNumber ?? string.Empty,
                    out var model) &&
                args.Image is not null &&
                model.Image is not null)
            {
                if (model.IsRealtimeImageEnabled)
                {
                    model.EnqueueImage(args.Image);
                }
            }
        }

        private void DeviceServiceOnBarcodeScanned(object? sender, BarcodeReadEventArgs args)
        {
            //更新图片

            if (_visibleCameraItems.TryGetValue(args.CameraSerialNumber, out var model) &&
                model.BindingType is CameraBindingType.ScannerCamera or CameraBindingType.OcrCamera)
            {
                //图片转换
                if (args.ThumbImage is not null &&
                    model.Image is not null)
                {
                    if (!model.IsRealtimeImageEnabled)
                    {
                        model.TryEnqueueImage(args.ThumbImage, args.Timestamp);
                    }
                }
            }
        }
    }
}
