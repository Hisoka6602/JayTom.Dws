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
    public class PanoramaCameraConfigViewModel : BindableBase {

        private ObservableCollection<PanoramaCameraItemInfoModel> _panoramaCameraItems = new()
        {
            new PanoramaCameraItemInfoModel() {
                Num = 1,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.IndustrialCamera,
                CaptureDelayTime = 5000,
                SerialNumber = "1111-2222-3333-4444",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
            },
            new PanoramaCameraItemInfoModel() {
                Num = 2,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.IndustrialCamera,
                CaptureDelayTime = 5000,
                SerialNumber = "1111-2222-3333-4444",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
            },
        };

        public ObservableCollection<PanoramaCameraItemInfoModel> PanoramaCameraItems {
            get => _panoramaCameraItems;
            set => SetProperty(ref _panoramaCameraItems, value);
        }
    }
}