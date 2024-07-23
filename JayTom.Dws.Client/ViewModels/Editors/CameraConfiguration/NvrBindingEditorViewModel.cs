using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;

namespace JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration {
    public class NvrBindingEditorViewModel : BindableBase {
        private CameraFinderItemInfoModel _cameraFinderItemInfo = new();

        private ObservableCollection<NvrBindingItemModel> _nvrBindingItems = new()
        {
            new NvrBindingItemModel()
            {
                IpAddress = "10.200.211.98",
                Port = 37777,
                Username = "admin",
                Password = "a12345678",
                Status = NvrStatus.LoginFailed,
                IsConfigured = true,
                Type = DeviceType.NVR,
            },
            new NvrBindingItemModel()
            {
                IpAddress = "10.200.211.98",
                Port = 37777,
                Username = "admin",
                Password = "a12345678",
                Status = NvrStatus.LoginFailed,
                IsConfigured = true,
                Type = DeviceType.NVR,
                IsNvrBound = true
            },
        };

        public string Identifier { get; set; } = string.Empty;

        public ObservableCollection<NvrBindingItemModel> NvrBindingItems {
            get => _nvrBindingItems;
            set => SetProperty(ref _nvrBindingItems, value);
        }

        public CameraFinderItemInfoModel CameraFinderItemInfo {
            get => _cameraFinderItemInfo;
            set => SetProperty(ref _cameraFinderItemInfo, value);
        }

        public NvrBindingEditorViewModel() {
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private void LoadedDelegate(object obj) {
        }

        public ICommand CloseDialogCommand => new DelegateCommand<object>(CloseDialogDelegate);

        private void CloseDialogDelegate(object obj) {
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand UnbindNvrCommand => new DelegateCommand<object>(UnbindNvrDelegate);

        private void UnbindNvrDelegate(object obj) {
            //同步到库
            //解绑
            //刷新
        }

        public ICommand BindNvrCommand => new DelegateCommand<object>(BindNvrDelegate);

        private void BindNvrDelegate(object obj) {
            //同步到库
            //绑定
            //刷新
        }
    }
}