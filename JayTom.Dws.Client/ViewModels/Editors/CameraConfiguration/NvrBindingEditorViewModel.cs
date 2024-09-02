using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using JayTom.Dws.Data.Package;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Data.LocalConf.CloudConfig;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Data.LocalConf.IpcNvrConfig;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CloudConfig;

namespace JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration {

    public class NvrBindingEditorViewModel : BindableBase {
        private readonly IIpcNvrConfigRepository _ipcNvrConfigRepository;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly INvrCameraBindingRepository _nvrCameraBindingRepository;
        private readonly IDialogService _dialogService;

        private ObservableCollection<NvrBindingItemModel> _nvrBindingItems = new();

        private List<IpcNvrConfigInfoModel>? _ipcNvrConfigInfoModels;
        private NvrBindingParamInfoModel _nvrBindingParamInfoModel = new();
        private SnackbarMessageQueue _nvrBindingEditorViewMessageQueue = new(TimeSpan.FromSeconds(1));

        public SnackbarMessageQueue NvrBindingEditorViewMessageQueue {
            get => _nvrBindingEditorViewMessageQueue;
            set => SetProperty(ref _nvrBindingEditorViewMessageQueue, value);
        }

        public string Identifier { get; set; } = string.Empty;

        public ObservableCollection<NvrBindingItemModel> NvrBindingItems {
            get => _nvrBindingItems;
            set => SetProperty(ref _nvrBindingItems, value);
        }

        public NvrBindingParamInfoModel NvrBindingParamInfoModel {
            get => _nvrBindingParamInfoModel;
            set => SetProperty(ref _nvrBindingParamInfoModel, value);
        }

        public NvrBindingEditorViewModel(IIpcNvrConfigRepository ipcNvrConfigRepository,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            INvrCameraBindingRepository nvrCameraBindingRepository,
            IDialogService dialogService) {
            _ipcNvrConfigRepository = ipcNvrConfigRepository;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
            _dialogService = dialogService;
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object? obj) {
            _ipcNvrConfigInfoModels = await _ipcNvrConfigRepository.MemoryCacheData();
            var bindingInfoModels = await _nvrCameraBindingRepository.MemoryCacheData();
            if (obj is not null || NvrBindingItems.Any(a => a.Status != NvrStatus.Online && a.Status != NvrStatus.LoginFailed)) {
                var nvrBindingItemModels = _ipcNvrConfigInfoModels
                    .Where(w => w.Type == (int)DeviceType.NVR)
                    .SelectMany((s, i) => Enumerable.Range(1, s.ChannelCount).Select(channelIndex => new NvrBindingItemModel {
                        Num = i + 1,
                        Brand = s.Brand,
                        IpAddress = s.IpAddress,
                        Name = s.Name,
                        SerialNumber = s.SerialNumber,
                        IsConfigured = true,
                        Channel = (channelIndex - 1),
                        CustomName = s.Name,
                        Type = (DeviceType)s.Type,
                        Username = s.Username,
                        Password = s.Password,
                        Port = s.Port,
                        DeviceName = $"通道{channelIndex}",
                        /*IsNvrBound = _scannerCameraConfigInfoModels
                            .FirstOrDefault(f => f.SerialNumber.Equals(CameraFinderItemInfo.SerialNumber) &&
                                f.NvrCameraBindingInfos?.Any(a => a.IpAddress.Equals(s.IpAddress) && a.Channel == channelIndex) == true) != null,*/
                        IsNvrBound = bindingInfoModels.Any(a => a.SerialNumber.Equals(NvrBindingParamInfoModel.SerialNumber) &&
                                                              a.Channel == (channelIndex - 1) &&
                                                              a.IpAddress.Equals(s.IpAddress))
                    }))
                    ?.ToList();

                await Application.Current.Dispatcher.InvokeAsync(async () => {
                    NvrBindingItems.Clear();
                    await Task.Delay(200);
                    NvrBindingItems.AddRange(nvrBindingItemModels);
                    foreach (var model in NvrBindingItems.Where(w =>
                                 !w.Username.Equals(string.Empty) &&
                                 !w.Password.Equals(string.Empty) &&
                                 !w.Brand.Equals(string.Empty))) {
                        await Application.Current.Dispatcher.InvokeAsync(() => {
                            model.Status = NvrStatus.LoggingIn;
                            return Task.CompletedTask;
                        });
                    }
                    //后面以下的针对性代码都需要替换
                    //初始化设备
                    var baseDaHuatech = BaseDaHuatech.CreateInstance();
                    //枚举设备
                    await BaseDaHuatech.EnumDevices();
                    //不需要每个都登录(组合SerialNumber、相同的SerialNumber只登录一次)

                    var devices = NvrBindingItems.Where(w =>
                        !w.Username.Equals(string.Empty) &&
                        !w.Password.Equals(string.Empty) &&
                        !w.Brand.Equals(string.Empty)).GroupBy(g => new { g.SerialNumber, g.Username, g.Password, g.Brand }).ToList();
                    Parallel.ForEach(devices, async device => {
                        if (device.Key.Brand.Equals("DaHua", StringComparison.InvariantCultureIgnoreCase)) {
                            //大华登录
                            var (key, value) = await baseDaHuatech.LogIn(device.Key.SerialNumber, device.Key.Username, device.Key.Password);
                            await Application.Current.Dispatcher.InvokeAsync(() => {
                                foreach (var model in NvrBindingItems.Where(w =>
                                             w.SerialNumber.Equals(device.Key.SerialNumber))) {
                                    model.Status = key ? NvrStatus.Online : NvrStatus.LoginFailed;
                                }
                            });
                        }
                    });

                    foreach (var model in NvrBindingItems.Where(w => w.Status == NvrStatus.LoggingIn)) {
                        model.Status = NvrStatus.LoginFailed;
                    }
                });
            }
            else {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    foreach (var model in NvrBindingItems) {
                        model.IsNvrBound = bindingInfoModels.Any(a =>
                            a.SerialNumber.Equals(NvrBindingParamInfoModel.SerialNumber) &&
                            a.Channel == model.Channel &&
                            a.IpAddress.Equals(model.IpAddress));
                    }
                });
            }

            //加载后需要再次登录以验证账号密码
        }

        public ICommand CloseDialogCommand => new DelegateCommand<object>(CloseDialogDelegate);

        private void CloseDialogDelegate(object obj) {
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand UnbindNvrCommand => new DelegateCommand<object>(UnbindNvrDelegate);

        private async void UnbindNvrDelegate(object obj) {
            if (obj is NvrBindingItemModel info) {
                var infoModel = await _nvrCameraBindingRepository.FirstOrDefault(f => f.IpAddress.Equals(info.IpAddress) &&
                    f.Channel.Equals(info.Channel) &&
                    f.SerialNumber.Equals(NvrBindingParamInfoModel.SerialNumber));
                if (infoModel is not null) {
                    var delete = await _nvrCameraBindingRepository.Delete(infoModel);
                    if (delete) {
                        _barcodeScannerCameraConfigRepository.UpdateMemoryCache();
                        LoadedDelegate(null);
                    }
                }
            }
        }

        public ICommand BindNvrCommand => new DelegateCommand<object>(BindNvrDelegate);

        private async void BindNvrDelegate(object obj) {
            if (obj is NvrBindingItemModel info) {
                var insert = await _nvrCameraBindingRepository.Insert(new NvrCameraBindingInfoModel() {
                    SerialNumber = NvrBindingParamInfoModel.SerialNumber,
                    Channel = info.Channel,
                    IpAddress = info.IpAddress,
                    Password = info.Password,
                    Username = info.Username,
                    Port = info.Port,
                    BindingSource = NvrBindingParamInfoModel.BindingSource,
                    DisplayIdentifier = NvrBindingParamInfoModel.DisplayIdentifier
                });
                if (insert) {
                    _barcodeScannerCameraConfigRepository.UpdateMemoryCache();
                    LoadedDelegate(null);
                }
            }
        }

        public ICommand PreviewViewCommand => new DelegateCommand<object>(PreviewViewDelegate);

        private void PreviewViewDelegate(object obj) {
            /*if (AppContext.GetData("IsRunning") is true) {
                NvrBindingEditorViewMessageQueue.Enqueue("请先停止运行再预览");
                return;
            }*/
            if (obj is NvrBindingItemModel { Status: NvrStatus.Online } info) {
                _dialogService.ShowDialog("NvrBindingPreviewViewDialog", new DialogParameters { { "NvrBindingItem", info } }, null);
            }
        }
    }
}