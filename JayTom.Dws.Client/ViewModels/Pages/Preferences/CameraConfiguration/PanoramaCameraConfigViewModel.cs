using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalConf;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    public class PanoramaCameraConfigViewModel : BindableBase {
        private readonly IDeviceService _deviceService;
        private readonly IPanoramaCameraConfigRepository _panoramaCameraConfigRepository;

        private ObservableCollection<PanoramaCameraItemInfoModel> _panoramaCameraItems = new();

        private SnackbarMessageQueue _panoramaCameraMessageQueue = new(TimeSpan.FromSeconds(2));
        private int _captureDelayTime;

        public PanoramaCameraConfigViewModel(IDeviceService deviceService,
            IPanoramaCameraConfigRepository panoramaCameraConfigRepository) {
            _deviceService = deviceService;
            _panoramaCameraConfigRepository = panoramaCameraConfigRepository;
            _deviceService.CameraBound += async delegate (object? sender, CameraFinderItemInfoModel model) {
                if (model.BoundType == BoundCameraType.PanoramicCamera) {
                    await Application.Current.Dispatcher.InvokeAsync(async () => {
                        //增加到集合,从数据库获取
                        var infoModel = await _panoramaCameraConfigRepository.FirstOrDefault(f =>
                            f.SerialNumber.Equals(model.SerialNumber));
                        if (infoModel is not null) {
                            PanoramaCameraItems.Add(new PanoramaCameraItemInfoModel() {
                                ConnectionType = (ConnectionType)infoModel.ConnectionType,
                                CameraType = (CameraType)infoModel.CameraType,
                                IpAddress = infoModel.IpAddress,
                                CaptureDelayTime = infoModel.CaptureDelayTime,
                                Name = infoModel.Name,
                                SerialNumber = infoModel.SerialNumber,
                                Version = infoModel.Version,
                                Model = infoModel.Model,
                                Num = PanoramaCameraItems.Count + 1,
                            });
                        }
                    });
                }
            };
            _deviceService.CameraUnbound += async delegate (object? sender, CameraFinderItemInfoModel model) {
                //解绑相机,更新列表
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    var infoModel =
                        PanoramaCameraItems.FirstOrDefault(f => f.SerialNumber.Equals(model.SerialNumber));
                    if (infoModel is not null) {
                        PanoramaCameraItems.Remove(infoModel);
                        //重新排列
                        for (var i = 0; i < PanoramaCameraItems.Count; i++) {
                            PanoramaCameraItems[i].Num = i + 1;
                        }
                    }
                });
            };
        }

        public SnackbarMessageQueue PanoramaCameraMessageQueue {
            get => _panoramaCameraMessageQueue;
            set => SetProperty(ref _panoramaCameraMessageQueue, value);
        }

        public ObservableCollection<PanoramaCameraItemInfoModel> PanoramaCameraItems {
            get => _panoramaCameraItems;
            set => SetProperty(ref _panoramaCameraItems, value);
        }

        /// <summary>
        /// 延迟时间拍照时间（单位：秒）
        /// </summary>
        public int CaptureDelayTime {
            get => _captureDelayTime;
            set => SetProperty(ref _captureDelayTime, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private void LoadedDelegate(object obj) {
            LoadCameraItems();
        }

        /// <summary>
        /// 解绑
        /// </summary>
        public ICommand UnbindCameraCommand {
            get => new DelegateCommand<PanoramaCameraItemInfoModel>(UnbindCameraDelegate);
        }

        private async void UnbindCameraDelegate(PanoramaCameraItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var isSuccess = false;
                var model = await _panoramaCameraConfigRepository.
                    FirstOrDefault(s =>
                        s.SerialNumber.Equals(obj.SerialNumber));
                if (model is not null) {
                    var delete = await _panoramaCameraConfigRepository.Delete(model);
                    if (delete) {
                        var (key, value) = await _deviceService.OnCameraUnbound(new CameraFinderItemInfoModel() {
                            BoundType = BoundCameraType.BarcodeScannerCamera,
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
            });
        }

        /// <summary>
        /// 保存选择项修改
        /// </summary>
        public ICommand ApplyChangesCommand {
            get => new DelegateCommand<PanoramaCameraItemInfoModel>(ApplyChangesDelegate);
        }

        private async void ApplyChangesDelegate(PanoramaCameraItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var isSuccess = false;
                var infoModel = await _panoramaCameraConfigRepository.
                    FirstOrDefault(f =>
                        f.SerialNumber.Equals(obj.SerialNumber));
                if (infoModel is not null) {
                    infoModel.CaptureDelayTime = obj.CaptureDelayTime;
                    var update = await _panoramaCameraConfigRepository.Update(infoModel);
                    if (update) {
                        var (key, value) = await _deviceService.OnCameraParametersModified(new List<CameraParametersModifiedEventArgs>()
                        {
                            new()
                            {
                                Type = BoundCameraType.PanoramicCamera,
                                Parameters = infoModel
                            }
                        });
                        isSuccess = key;
                    }
                }
                PanoramaCameraMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Camera")}:{obj.Name},{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
            });
        }

        /// <summary>
        /// 应用全部更改
        /// </summary>
        public ICommand ApplyAllChangesCommand {
            get => new DelegateCommand<object>(ApplyAllChangesDelegate);
        }

        private async void ApplyAllChangesDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var isSuccess = false;
                var infoModels = await _panoramaCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                if (infoModels?.Any() == true) {
                    foreach (var model in infoModels) {
                        model.CaptureDelayTime = CaptureDelayTime;
                    }

                    var updateRange = await _panoramaCameraConfigRepository.UpdateRange(infoModels);
                    if (updateRange) {
                        var list = infoModels?.Select(s => new CameraParametersModifiedEventArgs {
                            Type = BoundCameraType.PanoramicCamera,
                            Parameters = infoModels
                        })?.ToList();

                        var (key, value) = await _deviceService.OnCameraParametersModified(list ?? new List<CameraParametersModifiedEventArgs>());
                        isSuccess = key;
                        if (isSuccess) {
                            LoadCameraItems();
                        }
                    }
                }

                //从数据库修改
                //触发修改事件
                PanoramaCameraMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
            });
        }

        private async void LoadCameraItems() {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                PanoramaCameraItems.Clear();
                await Task.Delay(100);
                var infoModels = await _panoramaCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                var itemInfoModels = infoModels.Select((s, i) => new PanoramaCameraItemInfoModel() {
                    ConnectionType = (ConnectionType)s.ConnectionType,
                    CameraType = (CameraType)s.CameraType,
                    IpAddress = s.IpAddress,
                    CaptureDelayTime = s.CaptureDelayTime,
                    Model = s.Model,
                    Name = s.Name,
                    Num = i + 1,
                    SerialNumber = s.SerialNumber,
                    Version = s.Version
                })?.ToList();
                PanoramaCameraItems.AddRange(itemInfoModels);
            });
        }
    }
}