using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalConf;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    public class CameraFinderViewModel : BindableBase {
        private readonly IDeviceService _deviceService;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly IPanoramaCameraConfigRepository _panoramaCameraConfigRepository;
        private readonly IVolumeCameraConfigRepository _volumeCameraConfigRepository;
        private bool _isExecuting;
        private static bool _isLoaded;

        private ObservableCollection<CameraFinderItemInfoModel> _cameraFinderItems = new()
        {
            new CameraFinderItemInfoModel() {
                Num = 1,
                Name = "增加一个转换、如果是工业相机、智能相机则不显示体积绑定",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.IndustrialCamera,
                SerialNumber = "测试序列号1",
                IpAddress = "192.168.888.888",
                Model = "在WPF中我需要新建一个绑定相机类型显示的的转换器，请给我代码",
            },
            new CameraFinderItemInfoModel() {
                Num = 2,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.SmartCamera,
                SerialNumber = "测试序列号2",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
            },
            new CameraFinderItemInfoModel() {
                Num = 3,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.IndustrialCamera,
                SerialNumber = "测试序列号3",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
                BoundType = BoundCameraType.BarcodeScannerCamera
            },
            new CameraFinderItemInfoModel() {
                Num = 4,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.ThreeDCamera,
                SerialNumber = "测试序列号4",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
                BoundType = BoundCameraType.BarcodeScannerCamera
            },
        };

        private SnackbarMessageQueue _cameraFinderMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isRefreshing;

        public CameraFinderViewModel(IDeviceService deviceService,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IPanoramaCameraConfigRepository panoramaCameraConfigRepository,
            IVolumeCameraConfigRepository volumeCameraConfigRepository) {
            _deviceService = deviceService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _panoramaCameraConfigRepository = panoramaCameraConfigRepository;
            _volumeCameraConfigRepository = volumeCameraConfigRepository;
            _deviceService.CameraUnbound += async delegate (object? sender, CameraFinderItemInfoModel model) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    var infoModel = CameraFinderItems.FirstOrDefault(f => f.SerialNumber.Equals(model.SerialNumber));
                    if (infoModel is not null) {
                        infoModel.HasBinding = false;
                    }
                });
            };
            _deviceService.CameraEnumerationRefreshed += async delegate (object? sender, List<CameraFinderItemInfoModel> list) {
                await Task.Run(async () => {
                    var infoModels = new List<CameraFinderItemInfoModel>();
                    var scannerCameraConfigInfoModels = await _barcodeScannerCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                    var panoramaCameraConfigInfoModels = await _panoramaCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                    var volumeCameraConfigInfoModels = await _volumeCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                    infoModels.AddRange(scannerCameraConfigInfoModels.Select(s => new CameraFinderItemInfoModel {
                        BoundType = BoundCameraType.BarcodeScannerCamera,
                        ConnectionType = (ConnectionType)s.ConnectionType,
                        CameraType = (CameraType)s.CameraType,
                        HasBinding = true,
                        IpAddress = s.IpAddress,
                        Model = s.Model,
                        Name = s.Name,
                        SerialNumber = s.SerialNumber,
                        Version = s.Version,
                    })?.ToList() ?? new List<CameraFinderItemInfoModel>());
                    infoModels.AddRange(panoramaCameraConfigInfoModels.Select(s => new CameraFinderItemInfoModel {
                        BoundType = BoundCameraType.PanoramicCamera,
                        ConnectionType = (ConnectionType)s.ConnectionType,
                        CameraType = (CameraType)s.CameraType,
                        HasBinding = true,
                        IpAddress = s.IpAddress,
                        Model = s.Model,
                        Name = s.Name,
                        SerialNumber = s.SerialNumber,
                        Version = s.Version,
                    })?.ToList() ?? new List<CameraFinderItemInfoModel>());
                    infoModels.AddRange(volumeCameraConfigInfoModels.Select(s => new CameraFinderItemInfoModel {
                        BoundType = BoundCameraType.VolumeCamera,
                        ConnectionType = (ConnectionType)s.ConnectionType,
                        CameraType = (CameraType)s.CameraType,
                        HasBinding = true,
                        IpAddress = s.IpAddress,
                        Model = s.Model,
                        Name = s.Name,
                        SerialNumber = s.SerialNumber,
                        Version = s.Version,
                    })?.ToList() ?? new List<CameraFinderItemInfoModel>());
                    await Application.Current.Dispatcher.BeginInvoke(async () => {
                        CameraFinderItems.Clear();
                        await Task.Delay(300);
                        list = list.OrderBy(f => f.SerialNumber).ToList();
                        for (var i = 0; i < list.Count; i++) {
                            list[i].Num = i + 1;
                            list[i].HasBinding = infoModels?.Any(a => a.SerialNumber.Equals(list[i].SerialNumber)) ?? false;
                            list[i].BoundType = infoModels?.FirstOrDefault(f => f.SerialNumber.Equals(list[i].SerialNumber))?.BoundType ??
                                                BoundCameraType.BarcodeScannerCamera;
                        }
                        CameraFinderItems.AddRange(list);
                    });
                });
            };
        }

        public SnackbarMessageQueue CameraFinderMessageQueue {
            get => _cameraFinderMessageQueue;
            set => SetProperty(ref _cameraFinderMessageQueue, value);
        }

        public ObservableCollection<CameraFinderItemInfoModel> CameraFinderItems {
            get => _cameraFinderItems;
            set => SetProperty(ref _cameraFinderItems, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadeDelegate);
        }

        /// <summary>
        /// 刷新中
        /// </summary>
        public bool IsRefreshing {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        private void LoadeDelegate(object obj) {
            //加载相机对比绑定状态
            if (!_isLoaded) {
                _isLoaded = true;
                RefreshDelegate(obj);
            }
        }

        /// <summary>
        /// 刷新
        /// </summary>
        public ICommand RefreshCommand {
            get => new DelegateCommand<object>(RefreshDelegate);
        }

        private async void RefreshDelegate(object obj) {
            if (IsRefreshing) {
                return;
            }
            IsRefreshing = true;
            await Task.Delay(100);
            Task.Run(async () => {
                await Application.Current.Dispatcher.InvokeAsync(async () => {
                    var (key, value) = await _deviceService.OnCameraEnumerationRefreshed();
                    CameraFinderMessageQueue.Enqueue(key ? $"已重新枚举连接相机" : value);

                    IsRefreshing = false;
                });
            }).ConfigureAwait(false).GetAwaiter();
        }

        /// <summary>
        ///  绑定全景相机
        /// </summary>
        public ICommand BindPanoramaCameraCommand {
            get => new DelegateCommand<CameraFinderItemInfoModel>(BindPanoramaCameraDelegate);
        }

        private async void BindPanoramaCameraDelegate(CameraFinderItemInfoModel obj) {
            if (_isExecuting) {
                return;
            }
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                _isExecuting = true;
                var isSuccess = false;
                var insertOrUpdate = await _panoramaCameraConfigRepository.InsertOrUpdate(new PanoramaCameraConfigInfoModel() {
                    ConnectionType = (int)obj.ConnectionType,
                    CameraType = (int)obj.CameraType,
                    IpAddress = obj.IpAddress,
                    CaptureDelayTime = 2000,
                    Model = obj.Model,
                    Name = obj.Name,
                    SerialNumber = obj.SerialNumber,
                    Version = obj.Version
                });
                if (insertOrUpdate) {
                    //从数据库修改或增加
                    //触发修改事件
                    obj.HasBinding = true;
                    obj.BoundType = BoundCameraType.PanoramicCamera;
                    var (key, value) = await _deviceService.OnCameraBound(obj);
                    if (!key) {
                        obj.HasBinding = false;
                    }
                    isSuccess = key;
                }

                CameraFinderMessageQueue.Enqueue($"相机:{obj.Name},绑定{(isSuccess ? "成功" : "失败")}");
                _isExecuting = false;
            });
        }

        /// <summary>
        /// 绑定扫码相机
        /// </summary>
        public ICommand BindBarcodeScannerCameraCommand {
            get => new DelegateCommand<CameraFinderItemInfoModel>(BindBarcodeScannerCameraDelegate);
        }

        private async void BindBarcodeScannerCameraDelegate(CameraFinderItemInfoModel obj) {
            if (_isExecuting) {
                return;
            }
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                _isExecuting = true;
                var isSuccess = false;
                var insertOrUpdate = await _barcodeScannerCameraConfigRepository.InsertOrUpdate(new BarcodeScannerCameraConfigInfoModel() {
                    ConnectionType = (int)obj.ConnectionType,
                    CameraType = (int)obj.CameraType,
                    IpAddress = obj.IpAddress,
                    Model = obj.Model,
                    Name = obj.Name,
                    SerialNumber = obj.SerialNumber,
                    Version = obj.Version,
                    IsShowRealTimeImage = true
                });
                if (insertOrUpdate) {
                    //从数据库修改或增加
                    //触发修改事件
                    obj.HasBinding = true;
                    obj.BoundType = BoundCameraType.BarcodeScannerCamera;
                    var (key, value) = await _deviceService.OnCameraBound(obj);
                    if (!key) {
                        obj.HasBinding = false;
                    }
                    isSuccess = key;
                }

                CameraFinderMessageQueue.Enqueue($"相机:{obj.Name},绑定{(isSuccess ? "成功" : "失败")}");
                _isExecuting = false;
            });
        }

        /// <summary>
        /// 绑定体积相机
        /// </summary>
        public ICommand BindVolumeCameraCommand {
            get => new DelegateCommand<CameraFinderItemInfoModel>(BindVolumeCameraDelegate);
        }

        private async void BindVolumeCameraDelegate(CameraFinderItemInfoModel obj) {
            if (_isExecuting) {
                return;
            }
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                _isExecuting = true;
                var isSuccess = false;
                var insertOrUpdate = await _volumeCameraConfigRepository.InsertOrUpdate(new VolumeCameraConfigInfoModel() {
                    ConnectionType = (int)obj.ConnectionType,
                    CameraType = (int)obj.CameraType,
                    IpAddress = obj.IpAddress,
                    Model = obj.Model,
                    Name = obj.Name,
                    SerialNumber = obj.SerialNumber,
                    Version = obj.Version,
                    MaxLength = 2000,
                    MinLength = 1000,
                    MaxSyncTime = 1000,
                    MinSyncTime = 500,
                    VolumeMeasurementMode = 0,
                    TriggerMode = 0
                });
                if (insertOrUpdate) {
                    //从数据库修改或增加
                    //触发修改事件
                    obj.HasBinding = true;
                    obj.BoundType = BoundCameraType.VolumeCamera;
                    var (key, value) = await _deviceService.OnCameraBound(obj);
                    if (!key) {
                        obj.HasBinding = false;
                    }
                    isSuccess = key;
                }

                CameraFinderMessageQueue.Enqueue($"相机:{obj.Name},绑定{(isSuccess ? "成功" : "失败")}");
                _isExecuting = false;
            });
        }

        /// <summary>
        /// 解绑
        /// </summary>
        public ICommand UnbindCommand {
            get => new DelegateCommand<CameraFinderItemInfoModel>(UnbindDelegate);
        }

        private async void UnbindDelegate(CameraFinderItemInfoModel obj) {
            if (_isExecuting) {
                return;
            }
            var isSuccess = false;
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                _isExecuting = true;
                if (obj.BoundType == BoundCameraType.BarcodeScannerCamera) {
                    //从扫码相机删除
                    var model = await _barcodeScannerCameraConfigRepository.
                        FirstOrDefault(s =>
                            s.SerialNumber.Equals(obj.SerialNumber));
                    if (model is not null) {
                        isSuccess = await _barcodeScannerCameraConfigRepository.Delete(model);
                    }
                }
                else if (obj.BoundType == BoundCameraType.PanoramicCamera) {
                    //从全景相机删除
                    var model = await _panoramaCameraConfigRepository.
                        FirstOrDefault(f => f.SerialNumber.Equals(obj.SerialNumber));
                    if (model is not null) {
                        isSuccess = await _panoramaCameraConfigRepository.Delete(model);
                    }
                }
                else if (obj.BoundType == BoundCameraType.VolumeCamera) {
                    //从体积相机删除
                    var model = await _volumeCameraConfigRepository.
                        FirstOrDefault(f => f.SerialNumber.Equals(obj.SerialNumber));
                    if (model is not null) {
                        isSuccess = await _volumeCameraConfigRepository.Delete(model);
                    }
                }
                if (isSuccess) {
                    var (key, value) = await _deviceService.OnCameraUnbound(obj);
                    isSuccess = key;
                }

                CameraFinderMessageQueue.Enqueue($"相机:{obj.Name},解绑{(isSuccess ? "成功" : "失败")}");
                _isExecuting = false;
            });
        }
    }
}