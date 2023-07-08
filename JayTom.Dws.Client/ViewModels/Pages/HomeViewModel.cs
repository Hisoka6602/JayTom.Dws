using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.ViewModels.Pages {

    public class HomeViewModel : BindableBase {

        private ObservableCollection<CameraItemInfoModel> _cameraItems = new()
        {
            new CameraItemInfoModel() {
                CameraName = "海康工业相机.1",
                Status = CameraStatus.Running,
                Type = CameraType.IndustrialCamera,
                ConnectionType = ConnectionType.Bluetooth,
            },
            new CameraItemInfoModel() {
                CameraName = "海康工业相机.2",
                Status = CameraStatus.Running,
                Type = CameraType.PanoramicCamera,
                ConnectionType = ConnectionType.Ethernet,
            },
            new CameraItemInfoModel() {
                CameraName = "海康工业相机.3",
                Status = CameraStatus.Failure,
                Type = CameraType.SmartCamera,
                ConnectionType = ConnectionType.SerialPort,
            },new CameraItemInfoModel() {
                CameraName = "大华3D相机.1",
                Status = CameraStatus.Paused,
                Type = CameraType.ThreeDCamera,
                ConnectionType = ConnectionType.Tcp,
            },new CameraItemInfoModel() {
                CameraName = "大华3D相机.2",
                Status = CameraStatus.Disconnected,
                Type = CameraType.ThreeDCamera,
                ConnectionType = ConnectionType.Usb,
            }, new CameraItemInfoModel() {
                CameraName = "大华3D相机.3",
                Status = CameraStatus.Disconnected,
                Type = CameraType.ThreeDCamera,
                ConnectionType = ConnectionType.Bluetooth,
            },
        };

        public ObservableCollection<CameraItemInfoModel> CameraItems {
            get => _cameraItems;
            set => SetProperty(ref _cameraItems, value);
        }
    }
}