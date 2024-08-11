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
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CloudConfig;

namespace JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration {

    public class NvrCameraMappingEditorViewModel : BindableBase {
        private readonly INvrCameraBindingRepository _nvrCameraBindingRepository;

        private ObservableCollection<NvrCameraMappingItemInfoModel> _nvrCameraMappingItemInfos = new() {
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

        public NvrCameraMappingEditorViewModel(INvrCameraBindingRepository nvrCameraBindingRepository) {
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            var nvrCameraBindingInfoModels = await _nvrCameraBindingRepository.MemoryCacheData();

            var cameraMappingItemInfoModels = nvrCameraBindingInfoModels.Select((s, i) => new NvrCameraMappingItemInfoModel {
                Num = i + 1,
                DisplayIdentifier = s.DisplayIdentifier,
                Channel = s.Channel,
                IpAddress = s.IpAddress,
                Username = s.Username,
                SerialNumber = s.SerialNumber,
                BindingSource = s.BindingSource,
                Remarks = s.Remarks,
                Port = s.Port,
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
                var infoModel = await _nvrCameraBindingRepository.FirstOrDefault(f => f.IpAddress.Equals(info.IpAddress) &&
                    f.Channel.Equals(info.Channel) &&
                    f.SerialNumber.Equals(info.SerialNumber));
                if (infoModel is not null) {
                    var delete = await _nvrCameraBindingRepository.Delete(infoModel);
                    if (delete) {
                        LoadedDelegate(obj);
                    }
                }
            }
        }
    }
}