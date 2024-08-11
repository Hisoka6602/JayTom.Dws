using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
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

        private ObservableCollection<NvrBindingItemModel> _nvrBindingItems = new();

        private List<IpcNvrConfigInfoModel>? _ipcNvrConfigInfoModels;
        private NvrBindingParamInfoModel _nvrBindingParamInfoModel = new();

        //private List<BarcodeScannerCameraConfigInfoModel>? _scannerCameraConfigInfoModels;
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
            INvrCameraBindingRepository nvrCameraBindingRepository) {
            _ipcNvrConfigRepository = ipcNvrConfigRepository;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            _ipcNvrConfigInfoModels = await _ipcNvrConfigRepository.MemoryCacheData();

            var bindingInfoModels = await _nvrCameraBindingRepository.MemoryCacheData();
            var nvrBindingItemModels = _ipcNvrConfigInfoModels
                .Where(w => w.Type == (int)DeviceType.NVR)
                .SelectMany((s, i) => Enumerable.Range(1, s.ChannelCount).Select(channelIndex => new NvrBindingItemModel {
                    Num = i + 1,
                    Brand = s.Brand,
                    IpAddress = s.IpAddress,
                    Name = s.Name,
                    SerialNumber = s.SerialNumber,
                    IsConfigured = true,
                    Channel = channelIndex,
                    CustomName = s.Name,
                    Type = (DeviceType)s.Type,
                    Username = s.Username,
                    Password = s.Password,
                    Port = s.Port,
                    /*IsNvrBound = _scannerCameraConfigInfoModels
                        .FirstOrDefault(f => f.SerialNumber.Equals(CameraFinderItemInfo.SerialNumber) &&
                            f.NvrCameraBindingInfos?.Any(a => a.IpAddress.Equals(s.IpAddress) && a.Channel == channelIndex) == true) != null,*/
                    IsNvrBound = bindingInfoModels.Any(a => a.SerialNumber.Equals(NvrBindingParamInfoModel.SerialNumber) &&
                                                          a.Channel == channelIndex &&
                                                          a.IpAddress.Equals(s.IpAddress))
                }))
                ?.ToList();

            await Application.Current.Dispatcher.InvokeAsync(async () => {
                NvrBindingItems.Clear();
                await Task.Delay(200);
                NvrBindingItems.AddRange(nvrBindingItemModels);
                Parallel.ForEach(NvrBindingItems.Where(w =>
                    !w.Username.Equals(string.Empty) &&
                    !w.Password.Equals(string.Empty) &&
                    !w.Brand.Equals(string.Empty)), async device => {
                        //登录
                        if (device.Brand.Equals("DaHua", StringComparison.InvariantCultureIgnoreCase)) {
                            //大华登录
                            await Application.Current.Dispatcher.InvokeAsync(() => {
                                device.Status = NvrStatus.LoggingIn;
                                return Task.CompletedTask;
                            });
                            var baseDaHuatech = BaseDaHuatech.CreateInstance();
                            var (key, value) = await baseDaHuatech.LogIn(device.SerialNumber, device.Username, device.Password);
                            await Application.Current.Dispatcher.InvokeAsync(() => {
                                device.Status = key ? NvrStatus.Online : NvrStatus.LoginFailed;
                                return Task.CompletedTask;
                            });
                        }
                    });
            });
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
                        LoadedDelegate(obj);
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
                    LoadedDelegate(obj);
                }
            }
        }
    }
}