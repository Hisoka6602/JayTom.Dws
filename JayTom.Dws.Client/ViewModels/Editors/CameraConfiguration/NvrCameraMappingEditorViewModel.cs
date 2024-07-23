using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;

namespace JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration {

    public class NvrCameraMappingEditorViewModel : BindableBase {

        private ObservableCollection<BaseCameraItemInfoModel> _baseCameraItemInfos = new()
        {
            new BaseCameraItemInfoModel()
            {
                IpAddress = "192.168.0.1",
                CustomName = "相机1",
                SerialNumber = "测试序列号",
                Num = 1,
                Model = "型号"
            }
        };

        public string Identifier { get; set; } = string.Empty;

        public ObservableCollection<BaseCameraItemInfoModel> BaseCameraItemInfos {
            get => _baseCameraItemInfos;
            set => SetProperty(ref _baseCameraItemInfos, value);
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

        public ICommand UnbindCameraCommand => new DelegateCommand<object>(UnbindCameraDelegate);

        private async void UnbindCameraDelegate(object obj) {
            //解绑
            //从数据库删除
            //刷新显示
        }
    }
}