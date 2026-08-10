using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration
{

    public class BarcodeScannerCameraConfigViewModel : BindableBase
    {
        private readonly IDeviceService _deviceService;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;

        private ObservableCollection<BarcodeScannerCameraItemInfoModel> _barcodeScannerCameraItems = new();

        private SnackbarMessageQueue _barcodeScannerCameraMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isShowRealTimeImage;
        /// <summary>
        /// 相机绑定事件处理器，保存实例以便页面卸载时退订。
        /// </summary>
        private readonly EventHandler<CameraFinderItemInfoModel> _cameraBoundHandler;
        /// <summary>
        /// 相机解绑事件处理器，保存实例以便页面卸载时退订。
        /// </summary>
        private readonly EventHandler<CameraFinderItemInfoModel> _cameraUnboundHandler;
        /// <summary>
        /// 页面事件是否已订阅。
        /// </summary>
        private bool _eventsSubscribed;

        public BarcodeScannerCameraConfigViewModel(IDeviceService deviceService,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository)
        {
            _deviceService = deviceService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _cameraBoundHandler = async delegate (object? sender, CameraFinderItemInfoModel model)
            {
                if (model.BoundType is CameraBindingType.ScannerCamera or CameraBindingType.OcrCamera)
                {
                    try
                    {
                        //增加到集合,从数据库获取
                        var infoModel = await _barcodeScannerCameraConfigRepository.FirstOrDefault(f =>
                            f.SerialNumber.Equals(model.SerialNumber));
                        if (infoModel is not null)
                        {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                                BarcodeScannerCameraItems.Add(new BarcodeScannerCameraItemInfoModel
                                {
                                    ConnectionType = (CameraConnectionType)infoModel.ConnectionType,
                                    CameraType = (CameraType)infoModel.CameraType,
                                    IpAddress = infoModel.IpAddress,
                                    IsShowRealTimeImage = infoModel.IsShowRealTimeImage,
                                    Name = infoModel.Name,
                                    SerialNumber = infoModel.SerialNumber,
                                    Version = infoModel.Version,
                                    Model = infoModel.Model,
                                    Num = BarcodeScannerCameraItems.Count + 1
                                }));
                        }
                    }
                    catch (Exception exception)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            BarcodeScannerCameraMessageQueue.Enqueue(
                                $"加载新绑定扫码相机失败:{exception.Message}"));
                    }
                }
            };
            _cameraUnboundHandler = async delegate (object? sender, CameraFinderItemInfoModel model)
            {
                //解绑相机,更新列表
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var infoModel =
                        BarcodeScannerCameraItems.FirstOrDefault(f => f.SerialNumber.Equals(model.SerialNumber));
                    if (infoModel is not null)
                    {
                        BarcodeScannerCameraItems.Remove(infoModel);
                        //重新排列
                        for (var i = 0; i < BarcodeScannerCameraItems.Count; i++)
                        {
                            BarcodeScannerCameraItems[i].Num = i + 1;
                        }
                    }
                });
            };
        }

        public SnackbarMessageQueue BarcodeScannerCameraMessageQueue
        {
            get => _barcodeScannerCameraMessageQueue;
            set => SetProperty(ref _barcodeScannerCameraMessageQueue, value);
        }

        public ObservableCollection<BarcodeScannerCameraItemInfoModel> BarcodeScannerCameraItems
        {
            get => _barcodeScannerCameraItems;
            set => SetProperty(ref _barcodeScannerCameraItems, value);
        }

        /// <summary>
        /// 是否显示实时图像
        /// </summary>
        public bool IsShowRealTimeImage
        {
            get => _isShowRealTimeImage;
            set => SetProperty(ref _isShowRealTimeImage, value);
        }

        public ICommand LoadedCommand
        {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        /// <summary>
        /// 页面卸载命令。
        /// </summary>
        public ICommand UnloadedCommand => new DelegateCommand<object>(UnloadedDelegate);

        private void LoadedDelegate(object obj)
        {
            if (!_eventsSubscribed)
            {
                _deviceService.CameraBound += _cameraBoundHandler;
                _deviceService.CameraUnbound += _cameraUnboundHandler;
                _eventsSubscribed = true;
            }

            _ = LoadCameraItemsAsync();
        }

        /// <summary>
        /// 页面卸载时解除设备事件订阅。
        /// </summary>
        private void UnloadedDelegate(object obj)
        {
            if (!_eventsSubscribed)
            {
                return;
            }

            _deviceService.CameraBound -= _cameraBoundHandler;
            _deviceService.CameraUnbound -= _cameraUnboundHandler;
            _eventsSubscribed = false;
        }

        /// <summary>
        /// 解绑
        /// </summary>
        public ICommand UnbindCameraCommand
        {
            get => new DelegateCommand<BarcodeScannerCameraItemInfoModel>(UnbindCameraDelegate);
        }

        private async void UnbindCameraDelegate(BarcodeScannerCameraItemInfoModel obj)
        {
            try
            {
                var isSuccess = false;
                var model = await _barcodeScannerCameraConfigRepository.
                    FirstOrDefault(s =>
                        s.SerialNumber.Equals(obj.SerialNumber));
                if (model is not null)
                {
                    var delete = await _barcodeScannerCameraConfigRepository.Delete(model);
                    if (delete)
                    {
                        var (key, value) = await _deviceService.OnCameraUnbound(new CameraFinderItemInfoModel()
                        {
                            BoundType = CameraBindingType.ScannerCamera,
                            ConnectionType = obj.ConnectionType,
                            HasBinding = false,
                            IpAddress = obj.IpAddress,
                            CameraType = obj.CameraType,
                            Model = obj.Model,
                            Name = obj.Name,
                            SerialNumber = obj.SerialNumber,
                            Version = obj.Version,
                            Num = obj.Num,
                        });
                        isSuccess = key;
                    }
                }
                BarcodeScannerCameraMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Camera")}:{obj.Name},{Languages.Language.ResourceManager.GetString("Unbind")}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
            }
            catch (Exception exception)
            {
                BarcodeScannerCameraMessageQueue.Enqueue($"解绑扫码相机失败:{exception.Message}");
            }
        }

        /// <summary>
        /// 保存选择项修改
        /// </summary>
        public ICommand ApplyChangesCommand
        {
            get => new DelegateCommand<BarcodeScannerCameraItemInfoModel>(ApplyChangesDelegate);
        }

        private async void ApplyChangesDelegate(BarcodeScannerCameraItemInfoModel obj)
        {
            try
            {
                var isSuccess = false;
                var infoModel = await _barcodeScannerCameraConfigRepository.
                    FirstOrDefault(f =>
                        f.SerialNumber.Equals(obj.SerialNumber));
                if (infoModel is not null)
                {
                    infoModel.IsShowRealTimeImage = obj.IsShowRealTimeImage;
                    var update = await _barcodeScannerCameraConfigRepository.Update(infoModel);
                    if (update)
                    {
                        var (key, value) = await _deviceService.OnCameraParametersModified(new List<CameraParametersModifiedEventArgs>()
                        {
                            new()
                            {
                                Type = CameraBindingType.ScannerCamera,
                                Parameters = infoModel
                            }
                        });
                        isSuccess = key;
                    }
                }
                BarcodeScannerCameraMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Camera")}:{obj.Name},{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
            }
            catch (Exception exception)
            {
                BarcodeScannerCameraMessageQueue.Enqueue($"保存扫码相机失败:{exception.Message}");
            }
        }

        /// <summary>
        /// 应用全部更改
        /// </summary>
        public ICommand ApplyAllChangesCommand
        {
            get => new DelegateCommand<object>(ApplyAllChangesDelegate);
        }

        private async void ApplyAllChangesDelegate(object obj)
        {
            try
            {
                var isSuccess = false;
                var infoModels = await _barcodeScannerCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                if (infoModels?.Any() == true)
                {
                    foreach (var model in infoModels)
                    {
                        model.IsShowRealTimeImage = IsShowRealTimeImage;
                    }

                    var updateRange = await _barcodeScannerCameraConfigRepository.UpdateRange(infoModels);
                    if (updateRange)
                    {
                        var list = infoModels?.Select(s => new CameraParametersModifiedEventArgs
                        {
                            Type = CameraBindingType.ScannerCamera,
                            Parameters = s
                        })?.ToList();

                        var (key, value) = await _deviceService.OnCameraParametersModified(list ?? new List<CameraParametersModifiedEventArgs>());
                        isSuccess = key;
                        if (isSuccess)
                        {
                            await LoadCameraItemsAsync();
                        }
                    }
                }

                //从数据库修改
                //触发修改事件
                BarcodeScannerCameraMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
            }
            catch (Exception exception)
            {
                BarcodeScannerCameraMessageQueue.Enqueue($"批量保存扫码相机失败:{exception.Message}");
            }
        }

        /// <summary>
        /// 加载扫码相机配置列表。
        /// </summary>
        private async Task LoadCameraItemsAsync()
        {
            try
            {
                var infoModels = await _barcodeScannerCameraConfigRepository.Select(
                    static camera => camera.Id > 0,
                    static camera => camera.Id);
                var itemInfoModels = infoModels.Select((s, i) => new BarcodeScannerCameraItemInfoModel
                {
                    ConnectionType = (CameraConnectionType)s.ConnectionType,
                    CameraType = (CameraType)s.CameraType,
                    IpAddress = s.IpAddress,
                    IsShowRealTimeImage = s.IsShowRealTimeImage,
                    Model = s.Model,
                    Name = s.Name,
                    Num = i + 1,
                    SerialNumber = s.SerialNumber,
                    Version = s.Version
                }).ToList();
                BarcodeScannerCameraItems.Clear();
                BarcodeScannerCameraItems.AddRange(itemInfoModels);
            }
            catch (Exception exception)
            {
                BarcodeScannerCameraMessageQueue.Enqueue($"加载扫码相机失败:{exception.Message}");
            }
        }
    }
}
