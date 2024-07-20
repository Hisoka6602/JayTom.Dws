using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.Cameras;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    /// <summary>
    /// IPC/NVR管理
    /// </summary>
    public class NvrIpcDeviceManagementViewModel : BindableBase {
        private IpcNvrItemInfoModel _ipcNvrItemInfo = new();

        public IpcNvrItemInfoModel IpcNvrItemInfo {
            get => _ipcNvrItemInfo;
            set => SetProperty(ref _ipcNvrItemInfo, value);
        }
    }
}