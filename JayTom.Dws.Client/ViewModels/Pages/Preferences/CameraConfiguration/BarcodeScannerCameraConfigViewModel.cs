using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    public class BarcodeScannerCameraConfigViewModel : BindableBase {

        private ObservableCollection<BarcodeScannerCameraItemInfoModel> _barcodeScannerCameraItems = new()
        {
            new BarcodeScannerCameraItemInfoModel() {
                Num = 1,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.IndustrialCamera,
                IsShowRealTimeImage = true,
                SerialNumber = "1111-2222-3333-4444",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
            },
            new BarcodeScannerCameraItemInfoModel() {
                Num = 2,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.IndustrialCamera,
                IsShowRealTimeImage = true,
                SerialNumber = "1111-2222-3333-4444",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
            },
        };

        public ObservableCollection<BarcodeScannerCameraItemInfoModel> BarcodeScannerCameraItems {
            get => _barcodeScannerCameraItems;
            set => SetProperty(ref _barcodeScannerCameraItems, value);
        }
    }
}