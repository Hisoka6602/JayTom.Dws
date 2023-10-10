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
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration
{

    public class BarcodeScannerCameraConfigViewModel : BindableBase {
        private readonly IDeviceService _deviceService;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;

        private ObservableCollection<BarcodeScannerCameraItemInfoModel> _barcodeScannerCameraItems = new();

        private SnackbarMessageQueue _barcodeScannerCameraMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isShowRealTimeImage;

        public BarcodeScannerCameraConfigViewModel(IDeviceService deviceService,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository) {
            _deviceService = deviceService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _deviceService.CameraBound += async delegate (object? sender, CameraFinderItemInfoModel model) {
                if (model.BoundType == BoundCameraType.BarcodeScannerCamera) {
                    await Application.Current.Dispatcher.InvokeAsync(async () => {
                        //增加到集合,从数据库获取
                        var infoModel = await _barcodeScannerCameraConfigRepository.FirstOrDefault(f =>
                            f.SerialNumber.Equals(model.SerialNumber));
                        if (infoModel is not null) {
                            BarcodeScannerCameraItems.Add(new BarcodeScannerCameraItemInfoModel() {
                                ConnectionType = (ConnectionType)infoModel.ConnectionType,
                                CameraType = (CameraType)infoModel.CameraType,
                                IpAddress = infoModel.IpAddress,
                                IsShowRealTimeImage = infoModel.IsShowRealTimeImage,
                                Name = infoModel.Name,
                                SerialNumber = infoModel.SerialNumber,
                                Version = infoModel.Version,
                                Model = infoModel.Model,
                                Num = BarcodeScannerCameraItems.Count + 1,
                            });
                        }
                    });
                }
            };
            _deviceService.CameraUnbound += async delegate (object? sender, CameraFinderItemInfoModel model) {
                //解绑相机,更新列表
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    var infoModel =
                        BarcodeScannerCameraItems.FirstOrDefault(f => f.SerialNumber.Equals(model.SerialNumber));
                    if (infoModel is not null) {
                        BarcodeScannerCameraItems.Remove(infoModel);
                        //重新排列
                        for (var i = 0; i < BarcodeScannerCameraItems.Count; i++) {
                            BarcodeScannerCameraItems[i].Num = i + 1;
                        }
                    }
                });
            };
        }

        public SnackbarMessageQueue BarcodeScannerCameraMessageQueue {
            get => _barcodeScannerCameraMessageQueue;
            set => SetProperty(ref _barcodeScannerCameraMessageQueue, value);
        }

        public ObservableCollection<BarcodeScannerCameraItemInfoModel> BarcodeScannerCameraItems {
            get => _barcodeScannerCameraItems;
            set => SetProperty(ref _barcodeScannerCameraItems, value);
        }

        /// <summary>
        /// 是否显示实时图像
        /// </summary>
        public bool IsShowRealTimeImage {
            get => _isShowRealTimeImage;
            set => SetProperty(ref _isShowRealTimeImage, value);
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
            get => new DelegateCommand<BarcodeScannerCameraItemInfoModel>(UnbindCameraDelegate);
        }

        private async void UnbindCameraDelegate(BarcodeScannerCameraItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var isSuccess = false;
                var model = await _barcodeScannerCameraConfigRepository.
                    FirstOrDefault(s =>
                        s.SerialNumber.Equals(obj.SerialNumber));
                if (model is not null) {
                    var delete = await _barcodeScannerCameraConfigRepository.Delete(model);
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
                BarcodeScannerCameraMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Camera")}:{obj.Name},{Languages.Language.ResourceManager.GetString("Unbind")}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
            });
        }

        /// <summary>
        /// 保存选择项修改
        /// </summary>
        public ICommand ApplyChangesCommand {
            get => new DelegateCommand<BarcodeScannerCameraItemInfoModel>(ApplyChangesDelegate);
        }

        private async void ApplyChangesDelegate(BarcodeScannerCameraItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var isSuccess = false;
                var infoModel = await _barcodeScannerCameraConfigRepository.
                    FirstOrDefault(f =>
                        f.SerialNumber.Equals(obj.SerialNumber));
                if (infoModel is not null) {
                    infoModel.IsShowRealTimeImage = obj.IsShowRealTimeImage;
                    var update = await _barcodeScannerCameraConfigRepository.Update(infoModel);
                    if (update) {
                        var (key, value) = await _deviceService.OnCameraParametersModified(new List<CameraParametersModifiedEventArgs>()
                        {
                            new()
                            {
                                Type = BoundCameraType.BarcodeScannerCamera,
                                Parameters = infoModel
                            }
                        });
                        isSuccess = key;
                    }
                }
                BarcodeScannerCameraMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Camera")}:{obj.Name},{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(isSuccess ?
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
                var infoModels = await _barcodeScannerCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                if (infoModels?.Any() == true) {
                    foreach (var model in infoModels) {
                        model.IsShowRealTimeImage = IsShowRealTimeImage;
                    }

                    var updateRange = await _barcodeScannerCameraConfigRepository.UpdateRange(infoModels);
                    if (updateRange) {
                        var list = infoModels?.Select(s => new CameraParametersModifiedEventArgs {
                            Type = BoundCameraType.BarcodeScannerCamera,
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
                BarcodeScannerCameraMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(isSuccess ?
                    Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
            });
        }

        private async void LoadCameraItems() {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                BarcodeScannerCameraItems.Clear();
                await Task.Delay(100);
                var infoModels = await _barcodeScannerCameraConfigRepository.Select(s => s.Id > 0, o => o.Id);
                var itemInfoModels = infoModels.Select((s, i) => new BarcodeScannerCameraItemInfoModel {
                    ConnectionType = (ConnectionType)s.ConnectionType,
                    CameraType = (CameraType)s.CameraType,
                    IpAddress = s.IpAddress,
                    IsShowRealTimeImage = s.IsShowRealTimeImage,
                    Model = s.Model,
                    Name = s.Name,
                    Num = i + 1,
                    SerialNumber = s.SerialNumber,
                    Version = s.Version
                })?.ToList();
                BarcodeScannerCameraItems.AddRange(itemInfoModels);
            });
        }
    }
}