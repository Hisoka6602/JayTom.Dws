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
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.SubHomeViewModels {

    public class CameraHomeViewModel : BindableBase {
        private readonly IDeviceService _deviceService;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private ObservableCollection<CameraItemInfoModel> _cameraItems = new();

        public ObservableCollection<CameraItemInfoModel> CameraItems {
            get => _cameraItems;
            set => SetProperty(ref _cameraItems, value);
        }

        public CameraHomeViewModel(IDeviceService deviceService,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository) {
            _deviceService = deviceService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
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
                        BindingType = s?.BindingType ?? new CameraBindingType()
                    })?.ToList();
                    CameraItems.AddRange(infoModels);
                });
            };
            _deviceService.CameraReleased += async delegate (object? sender, string s) {
                await Application.Current.Dispatcher.BeginInvoke(() => {
                    var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(s));
                    if (model != null) {
                        CameraItems.Remove(model);
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
            var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(args.CameraSerialNumber) && f.BindingType is CameraBindingType.PanoramaCamera);
            if (model is not null &&
                model.Image is not null) {
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