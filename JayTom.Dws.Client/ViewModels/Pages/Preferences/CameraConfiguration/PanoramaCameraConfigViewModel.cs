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
using JayTom.Dws.Application.CameraConfigurations;
using JayTom.Dws.Models.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration
{

    public class PanoramaCameraConfigViewModel : BindableBase
    {
        private readonly IDeviceService _deviceService;
        private readonly ICameraConfigurationCatalog<PanoramaCameraConfigInfoModel> _panoramaCameraConfigRepository;

        private ObservableCollection<PanoramaCameraItemInfoModel> _panoramaCameraItems = new();

        private SnackbarMessageQueue _panoramaCameraMessageQueue = new(TimeSpan.FromSeconds(2));
        private int _captureDelayTime;
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

        public PanoramaCameraConfigViewModel(IDeviceService deviceService,
            ICameraConfigurationCatalog<PanoramaCameraConfigInfoModel> panoramaCameraConfigRepository)
        {
            _deviceService = deviceService;
            _panoramaCameraConfigRepository = panoramaCameraConfigRepository;
            _cameraBoundHandler = async delegate (object? sender, CameraFinderItemInfoModel model)
            {
                if (model.BoundType == CameraBindingType.PanoramaCamera)
                {
                    try
                    {
                        //增加到集合,从数据库获取
                        var infoModel = await _panoramaCameraConfigRepository.FirstOrDefault(f =>
                            f.SerialNumber.Equals(model.SerialNumber));
                        if (infoModel is not null)
                        {
                            await UiThread.Dispatcher.InvokeAsync(() =>
                                PanoramaCameraItems.Add(new PanoramaCameraItemInfoModel
                                {
                                    ConnectionType = (CameraConnectionType)infoModel.ConnectionType,
                                    CameraType = (CameraType)infoModel.CameraType,
                                    IpAddress = infoModel.IpAddress,
                                    CaptureDelayTime = infoModel.CaptureDelayTime,
                                    Name = infoModel.Name,
                                    SerialNumber = infoModel.SerialNumber,
                                    Version = infoModel.Version,
                                    Model = infoModel.Model,
                                    Num = PanoramaCameraItems.Count + 1
                                }));
                        }
                    }
                    catch (Exception exception)
                    {
                        await UiThread.Dispatcher.InvokeAsync(() =>
                            PanoramaCameraMessageQueue.Enqueue(
                                $"加载新绑定全景相机失败:{exception.Message}"));
                    }
                }
            };
            _cameraUnboundHandler = async delegate (object? sender, CameraFinderItemInfoModel model)
            {
                //解绑相机,更新列表
                await UiThread.Dispatcher.InvokeAsync(() =>
                {
                    var infoModel =
                        PanoramaCameraItems.FirstOrDefault(f => f.SerialNumber.Equals(model.SerialNumber));
                    if (infoModel is not null)
                    {
                        PanoramaCameraItems.Remove(infoModel);
                        //重新排列
                        for (var i = 0; i < PanoramaCameraItems.Count; i++)
                        {
                            PanoramaCameraItems[i].Num = i + 1;
                        }
                    }
                });
            };
        }

        public SnackbarMessageQueue PanoramaCameraMessageQueue
        {
            get => _panoramaCameraMessageQueue;
            set => SetProperty(ref _panoramaCameraMessageQueue, value);
        }

        public ObservableCollection<PanoramaCameraItemInfoModel> PanoramaCameraItems
        {
            get => _panoramaCameraItems;
            set => SetProperty(ref _panoramaCameraItems, value);
        }

        /// <summary>
        /// 延迟时间拍照时间（单位：秒）
        /// </summary>
        public int CaptureDelayTime
        {
            get => _captureDelayTime;
            set => SetProperty(ref _captureDelayTime, value);
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

            LoadCameraItemsAsync().Forget("加载全景相机列表");
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
            get => new DelegateCommand<PanoramaCameraItemInfoModel>(UnbindCameraDelegate);
        }

        private async void UnbindCameraDelegate(PanoramaCameraItemInfoModel obj)
        {
            try
            {
                var isSuccess = false;
                var model = await _panoramaCameraConfigRepository.
                    FirstOrDefault(s =>
                        s.SerialNumber.Equals(obj.SerialNumber));
                if (model is not null)
                {
                    var delete = await _panoramaCameraConfigRepository.Delete(model);
                    if (delete)
                    {
                        var (key, value) = await _deviceService.UnbindCameraAsync(new CameraFinderItemInfoModel()
                        {
                            BoundType = CameraBindingType.PanoramaCamera,
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
                PanoramaCameraMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Camera")}:{obj.Name},{Languages.Language.ResourceManager.GetString("Unbind")}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
            }
            catch (Exception exception)
            {
                PanoramaCameraMessageQueue.Enqueue($"解绑全景相机失败:{exception.Message}");
            }
        }

        /// <summary>
        /// 保存选择项修改
        /// </summary>
        public ICommand ApplyChangesCommand
        {
            get => new DelegateCommand<PanoramaCameraItemInfoModel>(ApplyChangesDelegate);
        }

        private async void ApplyChangesDelegate(PanoramaCameraItemInfoModel obj)
        {
            try
            {
                var isSuccess = false;
                var infoModel = await _panoramaCameraConfigRepository.
                    FirstOrDefault(f =>
                        f.SerialNumber.Equals(obj.SerialNumber));
                if (infoModel is not null)
                {
                    infoModel.CaptureDelayTime = obj.CaptureDelayTime;
                    var update = await _panoramaCameraConfigRepository.Update(infoModel);
                    if (update)
                    {
                        var (key, value) = await _deviceService.ModifyCameraParametersAsync(
                            [new PanoramaCameraParametersModifiedEventArgs(infoModel)]);
                        isSuccess = key;
                    }
                }
                PanoramaCameraMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Camera")}:{obj.Name},{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
            }
            catch (Exception exception)
            {
                PanoramaCameraMessageQueue.Enqueue($"保存全景相机失败:{exception.Message}");
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
                var infoModels = await _panoramaCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                if (infoModels?.Any() == true)
                {
                    foreach (var model in infoModels)
                    {
                        model.CaptureDelayTime = CaptureDelayTime;
                    }

                    var updateRange = await _panoramaCameraConfigRepository.UpdateRange(infoModels);
                    if (updateRange)
                    {
                        var list = infoModels.Select(s =>
                            (CameraParametersModifiedEventArgs)new PanoramaCameraParametersModifiedEventArgs(s)).ToList();

                        var (key, value) = await _deviceService.ModifyCameraParametersAsync(list ?? new List<CameraParametersModifiedEventArgs>());
                        isSuccess = key;
                        if (isSuccess)
                        {
                            await LoadCameraItemsAsync();
                        }
                    }
                }

                //从数据库修改
                //触发修改事件
                PanoramaCameraMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
            }
            catch (Exception exception)
            {
                PanoramaCameraMessageQueue.Enqueue($"批量保存全景相机失败:{exception.Message}");
            }
        }

        /// <summary>
        /// 加载全景相机配置列表。
        /// </summary>
        private async Task LoadCameraItemsAsync()
        {
            try
            {
                var infoModels = await _panoramaCameraConfigRepository.Select(
                    static camera => camera.Id > 0,
                    static camera => camera.Id);
                var itemInfoModels = infoModels.Select((s, i) => new PanoramaCameraItemInfoModel
                {
                    ConnectionType = (CameraConnectionType)s.ConnectionType,
                    CameraType = (CameraType)s.CameraType,
                    IpAddress = s.IpAddress,
                    CaptureDelayTime = s.CaptureDelayTime,
                    Model = s.Model,
                    Name = s.Name,
                    Num = i + 1,
                    SerialNumber = s.SerialNumber,
                    Version = s.Version
                }).ToList();
                PanoramaCameraItems.Clear();
                PanoramaCameraItems.AddRange(itemInfoModels);
            }
            catch (Exception exception)
            {
                PanoramaCameraMessageQueue.Enqueue($"加载全景相机失败:{exception.Message}");
            }
        }
    }
}
