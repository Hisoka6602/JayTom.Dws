using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows;
using Prism.Commands;
using JayTom.Dws.Ocr;
using JayTom.Dws.Data;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.SubHomeViewModels {

    public class CameraHomeViewModel : BindableBase {
        private readonly IDeviceService _deviceService;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly IPanoramaCameraConfigRepository _panoramaCameraConfigRepository;
        private readonly IUsbCameraConfigRepository _usbCameraConfigRepository;
        private readonly IVolumeCameraConfigRepository _volumeCameraConfigRepository;

        private ObservableCollection<CameraItemInfoModel> _cameraItems = new();

        private ObservableCollection<CameraItemInfoModel> _hiddenCameraItems = new();

        public ObservableCollection<CameraItemInfoModel> CameraItems {
            get => _cameraItems;
            set => SetProperty(ref _cameraItems, value);
        }

        public ObservableCollection<CameraItemInfoModel> HiddenCameraItems {
            get => _hiddenCameraItems;
            set => SetProperty(ref _hiddenCameraItems, value);
        }

        public CameraHomeViewModel(IDeviceService deviceService,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IPanoramaCameraConfigRepository panoramaCameraConfigRepository,
            IUsbCameraConfigRepository usbCameraConfigRepository,
            IVolumeCameraConfigRepository volumeCameraConfigRepository) {
            _deviceService = deviceService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _panoramaCameraConfigRepository = panoramaCameraConfigRepository;
            _usbCameraConfigRepository = usbCameraConfigRepository;
            _volumeCameraConfigRepository = volumeCameraConfigRepository;
            //判断启停
            _deviceService.CameraStarted += async (sender, args) => {
                await Task.Delay(500);
                var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(args.CameraInfo?.SerialNumber ?? string.Empty));
                if (model is not null) {
                    await Application.Current.Dispatcher.BeginInvoke(() => {
                        model.IsRealtimeImageEnabled = args.Camera?.IsRealtimeImageEnabled ?? false;
                    });
                }
            };
            _deviceService.CameraInitialized += async delegate (object? sender, List<ICamera> list) {
                await Application.Current.Dispatcher.BeginInvoke(() => {
                    CameraItems.Clear();
                    Task.Delay(100);
                    var infoModels = list.Select(s => new CameraItemInfoModel {
                        ConnectionType = (s?.Info?.ConnectionType ?? CameraConnectionType.Ethernet),
                        CameraName = $"{s?.Info?.Brand}:{s?.Info?.SerialNumber}" ?? string.Empty,
                        Type = s?.Info?.Type ?? JayTom.Dws.Camera.CameraType.IndustrialCamera,
                        Status = CameraStatus.Running,
                        CameraId = (s?.Info?.Id)?.ToString() ?? string.Empty,
                        SerialNumber = s?.Info?.SerialNumber ?? string.Empty,
                        Camera = s,
                        StatusClickCommand = StatusClickCommand,
                        TakePhotoCommand = TakePhotoCommand,
                        SwitchRealtimeImageCommand = SwitchRealtimeImageCommand,
                        IsRealtimeImageEnabled = s?.IsRealtimeImageEnabled ?? false,
                        BindingType = s?.BindingType ?? new CameraBindingType(),

                        CameraDisplayStatus = GetCameraDisplayStatus(s?.Info?.SerialNumber ?? string.Empty),
                        HideCommand = HideCommand,
                        ShowCommand = ShowCommand
                    })?.ToList();
                    CameraItems.AddRange(infoModels?.Where(w => w.CameraDisplayStatus == CameraDisplayStatus.Visible));

                    HiddenCameraItems.AddRange(infoModels?.Where(w => w.CameraDisplayStatus == CameraDisplayStatus.Hidden));
                });
            };
            _deviceService.CameraReleased += async delegate (object? sender, string s) {
                await Application.Current.Dispatcher.BeginInvoke(() => {
                    var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(s));
                    if (model != null) {
                        CameraItems.Remove(model);
                    }
                    else {
                        var cameraItemInfoModel = HiddenCameraItems.FirstOrDefault(f => f.SerialNumber.Equals(s));
                        if (cameraItemInfoModel != null) {
                            HiddenCameraItems.Remove(cameraItemInfoModel);
                        }
                    }
                });
            };
            _deviceService.NotBarcodeHitEvent += async delegate (object? sender, BarcodeReadEventArgs args) {
                await Application.Current.Dispatcher.BeginInvoke(async () => {
                    var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(args.CameraSerialNumber));

                    if (model?.Image != null) {
                        //图片转换
                        if (args?.ThumbImage is not null) {
                            if (args.Timestamp != model.ImageTimestamp) {
                                model.ImageTimestamp = args.Timestamp;
                                model.BitmapQueue.Enqueue(args.ThumbImage);
                            }
                        }
                    }
                });
            };
            _deviceService.BarcodeScanned += DeviceServiceOnBarcodeScanned;
            _deviceService.RealTimeImage += DeviceServiceOnRealTimeImage;
            _deviceService.PanoramaCaptured += DeviceServiceOnPanoramaCaptured;
            _deviceService.CameraDisconnected += delegate (object? sender, List<ICamera> list) {
                //更新现有列表,例如删除相机成员
            };
            _deviceService.VolumeCaptured += DeviceServiceOnVolumeCaptured;
            _deviceService.OcrContentRecognized += DeviceServiceOnOcrContentRecognized;
        }

        /// <summary>
        /// 状态点击事件
        /// </summary>
        public ICommand? StatusClickCommand => new DelegateCommand<CameraItemInfoModel>(StatusClickDelegate);

        private async void StatusClickDelegate(CameraItemInfoModel obj) {
            //先加载进度条
            //临时截图
            if (obj.Camera is IIndustrialCamera industrialCamera) {
                await industrialCamera.TakePhotoAsync(string.Empty, 0);
            }
            else if (obj.Camera is ISecurityCamera securityCamera) {
                await securityCamera.TakePhotoAsync(string.Empty, 0);
            }
        }

        /// <summary>
        /// 拍照
        /// </summary>
        public ICommand? TakePhotoCommand => new DelegateCommand<CameraItemInfoModel>(TakePhotoDelegate);

        private async void TakePhotoDelegate(CameraItemInfoModel obj) {
            if (obj.Camera is { } camera) {
                camera.StopRealTimeImage();
                obj.IsRealtimeImageEnabled = camera.IsRealtimeImageEnabled;
                await camera.TakePhotoAsync(string.Empty, 0);
            }
        }

        /// <summary>
        /// 开关实时图像
        /// </summary>
        public ICommand? SwitchRealtimeImageCommand => new DelegateCommand<CameraItemInfoModel>(SwitchRealtimeImageDelegate);

        private async void SwitchRealtimeImageDelegate(CameraItemInfoModel obj) {
            if (obj.Camera is { } camera) {
                if (camera.IsRealtimeImageEnabled) {
                    camera.StopRealTimeImage();
                }
                else {
                    camera.StartRealTimeImage();
                }
                obj.IsRealtimeImageEnabled = camera.IsRealtimeImageEnabled;

                //保存到数据库
                if (camera.BindingType == CameraBindingType.ScannerCamera) {
                    var configInfoModel = await _barcodeScannerCameraConfigRepository.FirstOrDefault(f =>
                        camera.Info != null && f.SerialNumber.Equals(camera.Info.SerialNumber));
                    if (configInfoModel != null) {
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

        private async void ImageClickDelegate(CameraItemInfoModel obj) {
            //放大图片(用另一个图像框显示、并重新绑定接收图像来源、过渡动画)
            /*await Application.Current.Dispatcher.BeginInvoke(() => {
                AddNewRow(new BarCodeItemModel() {
                    Barcode = new Random().Next(100000000, 999999999).ToString()
                });
            });*/
        }

        public ICommand LoadedCommand => new DelegateCommand<UserControl>(LoadedDelegate);

        private async void LoadedDelegate(UserControl obj) {
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

        private async void HideDelegate(CameraItemInfoModel obj) {
            //加载到隐藏中
            await Application.Current.Dispatcher.BeginInvoke(async () => {
                var remove = CameraItems.Remove(obj);
                if (remove) {
                    HiddenCameraItems.Add(obj);
                    var barcodeScannerCameraConfigInfoModel = (await _barcodeScannerCameraConfigRepository.MemoryCacheData())
                        .FirstOrDefault(f => f.SerialNumber.Equals(obj.SerialNumber));
                    if (barcodeScannerCameraConfigInfoModel is not null) {
                        barcodeScannerCameraConfigInfoModel.CameraDisplayStatus = CameraDisplayStatus.Hidden;
                        await _barcodeScannerCameraConfigRepository.InsertOrUpdate(barcodeScannerCameraConfigInfoModel);
                        return;
                    }

                    var panoramaCameraConfigInfoModel = (await _panoramaCameraConfigRepository.MemoryCacheData())
                        .FirstOrDefault(f => f.SerialNumber.Equals(obj.SerialNumber));
                    if (panoramaCameraConfigInfoModel is not null) {
                        panoramaCameraConfigInfoModel.CameraDisplayStatus = CameraDisplayStatus.Hidden;
                        await _panoramaCameraConfigRepository.InsertOrUpdate(panoramaCameraConfigInfoModel);
                        return;
                    }

                    var usbCameraConfigInfoModel = await _usbCameraConfigRepository
                        .FirstOrDefault(f => f.SerialNumber.Equals(obj.SerialNumber));
                    if (usbCameraConfigInfoModel is not null) {
                        usbCameraConfigInfoModel.CameraDisplayStatus = CameraDisplayStatus.Hidden;
                        await _usbCameraConfigRepository.InsertOrUpdate(usbCameraConfigInfoModel);
                        return;
                    }

                    var volumeCameraConfigInfoModel = (await _volumeCameraConfigRepository.MemoryCacheData())
                        .FirstOrDefault(f => f.SerialNumber.Equals(obj.SerialNumber));
                    if (volumeCameraConfigInfoModel is not null) {
                        volumeCameraConfigInfoModel.CameraDisplayStatus = CameraDisplayStatus.Hidden;
                        await _volumeCameraConfigRepository.InsertOrUpdate(volumeCameraConfigInfoModel);
                        return;
                    }
                }
            });
        }

        /// <summary>
        /// 点击显示事件
        /// </summary>
        public ICommand? ShowCommand => new DelegateCommand<CameraItemInfoModel>(ShowDelegate);

        private async void ShowDelegate(CameraItemInfoModel obj) {
            //加载到显示中
            await Application.Current.Dispatcher.BeginInvoke(async () => {
                var remove = HiddenCameraItems.Remove(obj);
                if (remove) {
                    CameraItems.Add(obj);
                    var barcodeScannerCameraConfigInfoModel = (await _barcodeScannerCameraConfigRepository.MemoryCacheData())
                        .FirstOrDefault(f => f.SerialNumber.Equals(obj.SerialNumber));
                    if (barcodeScannerCameraConfigInfoModel is not null) {
                        barcodeScannerCameraConfigInfoModel.CameraDisplayStatus = CameraDisplayStatus.Visible;
                        await _barcodeScannerCameraConfigRepository.InsertOrUpdate(barcodeScannerCameraConfigInfoModel);
                        return;
                    }

                    var panoramaCameraConfigInfoModel = (await _panoramaCameraConfigRepository.MemoryCacheData())
                        .FirstOrDefault(f => f.SerialNumber.Equals(obj.SerialNumber));
                    if (panoramaCameraConfigInfoModel is not null) {
                        panoramaCameraConfigInfoModel.CameraDisplayStatus = CameraDisplayStatus.Visible;
                        await _panoramaCameraConfigRepository.InsertOrUpdate(panoramaCameraConfigInfoModel);
                        return;
                    }

                    var usbCameraConfigInfoModel = await _usbCameraConfigRepository
                        .FirstOrDefault(f => f.SerialNumber.Equals(obj.SerialNumber));
                    if (usbCameraConfigInfoModel is not null) {
                        usbCameraConfigInfoModel.CameraDisplayStatus = CameraDisplayStatus.Visible;
                        await _usbCameraConfigRepository.InsertOrUpdate(usbCameraConfigInfoModel);
                        return;
                    }

                    var volumeCameraConfigInfoModel = (await _volumeCameraConfigRepository.MemoryCacheData())
                        .FirstOrDefault(f => f.SerialNumber.Equals(obj.SerialNumber));
                    if (volumeCameraConfigInfoModel is not null) {
                        volumeCameraConfigInfoModel.CameraDisplayStatus = CameraDisplayStatus.Visible;
                        await _volumeCameraConfigRepository.InsertOrUpdate(volumeCameraConfigInfoModel);
                        return;
                    }
                }
            });
        }

        private CameraDisplayStatus GetCameraDisplayStatus(string serialNumber) {
            try {
                var barcodeScannerCameraConfigInfoModel = _barcodeScannerCameraConfigRepository.MemoryCacheData().Result
                    .FirstOrDefault(f => f.SerialNumber.Equals(serialNumber));
                if (barcodeScannerCameraConfigInfoModel is not null) {
                    return barcodeScannerCameraConfigInfoModel.CameraDisplayStatus;
                }

                var panoramaCameraConfigInfoModel = _panoramaCameraConfigRepository.MemoryCacheData().Result
                    .FirstOrDefault(f => f.SerialNumber.Equals(serialNumber));

                if (panoramaCameraConfigInfoModel is not null) {
                    return panoramaCameraConfigInfoModel.CameraDisplayStatus;
                }

                var volumeCameraConfigInfoModel = _volumeCameraConfigRepository.MemoryCacheData().Result
                    .FirstOrDefault(f => f.SerialNumber.Equals(serialNumber));
                if (volumeCameraConfigInfoModel is not null) {
                    return volumeCameraConfigInfoModel.CameraDisplayStatus;
                }

                var usbCameraConfigInfoModel = _usbCameraConfigRepository.FirstOrDefault(f =>
                        f.SerialNumber.Equals(serialNumber))
                    .Result;

                if (usbCameraConfigInfoModel is not null) {
                    return usbCameraConfigInfoModel.CameraDisplayStatus;
                }
            }
            catch (Exception e) {
                return CameraDisplayStatus.Visible;
            }
            return CameraDisplayStatus.Visible;
        }

        /// <summary>
        /// Ocr识别到内容触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void DeviceServiceOnOcrContentRecognized(object? sender, OcrResult args) {
            //更新图片

            var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(args.CameraSerialNumber) &&
                                                        f.Type is CameraType.IndustrialCamera or CameraType.SmartCamera);
            if (model is not null) {
                //图片转换
                if (args?.Thumbnail is not null &&
                    model.Image is not null) {
                    if (args.RecognitionTimestamp != model.ImageTimestamp) {
                        model.ImageTimestamp = args.RecognitionTimestamp;
                        if (!model.IsRealtimeImageEnabled) {
                            model.BitmapQueue.Enqueue(args.Thumbnail);
                        }
                    }
                }
            }
        }

        private async void DeviceServiceOnVolumeCaptured(object? sender, VolumeCapturedEventArgs args) {
            var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(args.CameraSerialNumber));
            if (model is not null && args?.Thumbnail is not null) {
                if (!model.IsRealtimeImageEnabled) {
                    model.BitmapQueue.Enqueue(args.Thumbnail);
                }
            }
        }

        private async void DeviceServiceOnPanoramaCaptured(object? sender, PanoramaCaptureEventArgs args) {
            //全景相机
            await Task.Yield();
            var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(args.CameraSerialNumber) && (f.BindingType is CameraBindingType.PanoramaCamera ||
                    f.Type == CameraType.VideoCamera));
            if (model?.Image != null) {
                //图片转换
                if (args?.ThumbImage is not null) {
                    if (args.Timestamp != model.ImageTimestamp) {
                        //model.Image = null;

                        model.ImageTimestamp = args.Timestamp;
                        if (!model.IsRealtimeImageEnabled) {
                            model.BitmapQueue.Enqueue(args.ThumbImage);
                        }
                    }
                }
            }
        }

        private void DeviceServiceOnRealTimeImage(object? sender, RealTimeImageEventArgs args) {
            //实时画面
            //await Task.Yield();
            var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(args.Camera?.Info?.SerialNumber));
            if (model is not null && args.Image is not null &&
                model.Image is not null) {
                if (model.IsRealtimeImageEnabled) {
                    //先清除累积的
                    if (model.BitmapQueue.Count > 2) {
                        model.BitmapQueue.Clear();
                    }
                    //NLog.LogManager.GetCurrentClassLogger().Error($"-推送到实时画面图片");
                    model.BitmapQueue.Enqueue(args.Image);
                }
            }
        }

        private async void DeviceServiceOnBarcodeScanned(object? sender, BarcodeReadEventArgs args) {
            //更新图片

            var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(args.CameraSerialNumber) &&
                                                        f.BindingType is CameraBindingType.ScannerCamera or CameraBindingType.OcrCamera);
            if (model is not null) {
                //图片转换
                if (args?.ThumbImage is not null &&
                    model.Image is not null) {
                    if (args.Timestamp != model.ImageTimestamp) {
                        model.ImageTimestamp = args.Timestamp;
                        if (!model.IsRealtimeImageEnabled) {
                            //先清除累积的
                            if (model.BitmapQueue.Count > 2) {
                                model.BitmapQueue.Clear();
                            }
                            model.BitmapQueue.Enqueue(args.ThumbImage);
                        }
                    }
                }
            }
        }
    }
}