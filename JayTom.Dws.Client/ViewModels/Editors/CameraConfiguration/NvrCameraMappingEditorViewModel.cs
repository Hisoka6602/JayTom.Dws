using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Data.LocalConf.IpcNvrConfig;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CloudConfig;

namespace JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration {

    public class NvrCameraMappingEditorViewModel : BindableBase {
        private readonly IIpcNvrConfigRepository _ipcNvrConfigRepository;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly INvrCameraBindingRepository _nvrCameraBindingRepository;
        private List<IpcNvrConfigInfoModel>? _ipcNvrConfigInfoModels;
        private List<BarcodeScannerCameraConfigInfoModel>? _scannerCameraConfigInfoModels;

        private ObservableCollection<NvrCameraMappingItemInfoModel> _nvrCameraMappingItemInfos = new()
        {
            new NvrCameraMappingItemInfoModel()
            {
                IpAddress = "192.168.0.1",
                CustomName = "相机1",
                SerialNumber = "测试序列号",
                Num = 1,
                Model = "型号"
            }
        };

        private IpcNvrItemInfoModel _ipcNvrItemInfo = new();

        public string Identifier { get; set; } = string.Empty;

        public IpcNvrItemInfoModel IpcNvrItemInfo {
            get => _ipcNvrItemInfo;
            set => SetProperty(ref _ipcNvrItemInfo, value);
        }

        public ObservableCollection<NvrCameraMappingItemInfoModel> NvrCameraMappingItemInfos {
            get => _nvrCameraMappingItemInfos;
            set => SetProperty(ref _nvrCameraMappingItemInfos, value);
        }

        public NvrCameraMappingEditorViewModel(IIpcNvrConfigRepository ipcNvrConfigRepository,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            INvrCameraBindingRepository nvrCameraBindingRepository) {
            _ipcNvrConfigRepository = ipcNvrConfigRepository;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            _ipcNvrConfigInfoModels = await _ipcNvrConfigRepository.MemoryCacheData();
            _scannerCameraConfigInfoModels = await _barcodeScannerCameraConfigRepository.MemoryCacheData();

            var nvrCameraBindingInfoModels = await _nvrCameraBindingRepository.Select(s => s.IpAddress.Equals(IpcNvrItemInfo.IpAddress),
                o => o.Id);

            var cameraMappingItemInfoModels = nvrCameraBindingInfoModels.Select((s, i) => new NvrCameraMappingItemInfoModel {
                Num = i + 1,
                CameraType = (CameraType)(_scannerCameraConfigInfoModels.FirstOrDefault(f =>
                    f.NvrCameraBindingInfos?.Any(a => a.IpAddress.Equals(s.IpAddress) &&
                                                      a.Channel.Equals(s.Channel)) == true)?.CameraType ?? 0),
                Channel = s.Channel,
                IpAddress = s.IpAddress,
                Name = _scannerCameraConfigInfoModels.FirstOrDefault(f => f.NvrCameraBindingInfos?.Any(a =>
                    a.IpAddress.Equals(s.IpAddress) &&
                    a.Channel.Equals(s.Channel)) == true)?.Name ?? string.Empty,
                ConnectionType = (CameraConnectionType)(_scannerCameraConfigInfoModels.FirstOrDefault(f =>
                    f.NvrCameraBindingInfos?.Any(a => a.IpAddress.Equals(s.IpAddress) &&
                                                      a.Channel.Equals(s.Channel)) == true)?.ConnectionType ?? 0),
                CustomName = _scannerCameraConfigInfoModels.FirstOrDefault(f => f.NvrCameraBindingInfos?.Any(a =>
                    a.IpAddress.Equals(s.IpAddress) &&
                    a.Channel.Equals(s.Channel)) == true)?.CustomName ?? string.Empty,
                Model = _scannerCameraConfigInfoModels.FirstOrDefault(f => f.NvrCameraBindingInfos?.Any(a =>
                    a.IpAddress.Equals(s.IpAddress) &&
                    a.Channel.Equals(s.Channel)) == true)?.Model ?? string.Empty,
                SerialNumber = _scannerCameraConfigInfoModels.FirstOrDefault(f => f.NvrCameraBindingInfos?.Any(a =>
                    a.IpAddress.Equals(s.IpAddress) &&
                    a.Channel.Equals(s.Channel)) == true)?.SerialNumber ?? string.Empty,
            })?.ToList();

            await Application.Current.Dispatcher.InvokeAsync(async () => {
                NvrCameraMappingItemInfos.Clear();
                await Task.Delay(200);
                NvrCameraMappingItemInfos.AddRange(cameraMappingItemInfoModels);
            });
        }

        public ICommand CloseDialogCommand => new DelegateCommand<object>(CloseDialogDelegate);

        private void CloseDialogDelegate(object obj) {
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand UnbindCameraCommand => new DelegateCommand<object>(UnbindCameraDelegate);

        private async void UnbindCameraDelegate(object obj) {
            if (obj is NvrCameraMappingItemInfoModel info) {
                var model = _scannerCameraConfigInfoModels?.FirstOrDefault(f =>
                    f.SerialNumber.Equals(info.SerialNumber));
                if (model is not null) {
                    var infoModel = await _nvrCameraBindingRepository.FirstOrDefault(f => f.IpAddress.Equals(info.IpAddress) &&
                        f.Channel.Equals(info.Channel) &&
                        f.SerialNumber.Equals(model.SerialNumber));
                    if (infoModel is not null) {
                        var delete = await _nvrCameraBindingRepository.Delete(infoModel);
                        if (delete) {
                            _barcodeScannerCameraConfigRepository.UpdateMemoryCache();
                            LoadedDelegate(obj);
                        }
                    }
                }
            }
        }
    }
}