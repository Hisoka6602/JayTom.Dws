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

    public class VolumeCameraConfigViewModel : BindableBase {

        private ObservableCollection<VolumeCameraItemInfoModel> _volumeCameraItems = new()
        {
            new VolumeCameraItemInfoModel() {
                Num = 1,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.IndustrialCamera,
                SerialNumber = "1111-2222-3333-4444",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
                VolumeMeasurementMode=0,
                MinSyncTime=200,
                MaxSyncTime=3000,
                MinLength=1000,
                MaxLength=3000,
                TriggerMode=0,
            },
            new VolumeCameraItemInfoModel() {
                Num = 2,
                Name = "测试相机名称",
                ConnectionType = ConnectionType.Ethernet,
                CameraType = CameraType.IndustrialCamera,
                SerialNumber = "1111-2222-3333-4444",
                IpAddress = "192.168.0.1",
                Model = "HK-6565",
                VolumeMeasurementMode=0,
                MinSyncTime=200,
                MaxSyncTime=3000,
                MinLength=1000,
                MaxLength=3000,
                TriggerMode=0,
            },
        };

        private ObservableCollection<TriggerModeDisplay> _triggerModeDisplayItems = new()
        {
            new TriggerModeDisplay()
            {
                Display = "触发模式1",
                TriggerMode = 0
            },
            new TriggerModeDisplay()
            {
                Display = "触发模式2",
                TriggerMode = 1
            },
        };

        private ObservableCollection<VolumeMeasurementModeDisplay> _volumeMeasurementModeItems = new()
        {
            new VolumeMeasurementModeDisplay()
            {
                Display = "测量模式1",
                VolumeMeasurementMode = 0,
            },
            new VolumeMeasurementModeDisplay()
            {
                Display = "测量模式2",
                VolumeMeasurementMode = 1,
            }
        };

        public ObservableCollection<VolumeCameraItemInfoModel> VolumeCameraItems {
            get => _volumeCameraItems;
            set => SetProperty(ref _volumeCameraItems, value);
        }

        public ObservableCollection<TriggerModeDisplay> TriggerModeDisplayItems {
            get => _triggerModeDisplayItems;
            set => SetProperty(ref _triggerModeDisplayItems, value);
        }

        public ObservableCollection<VolumeMeasurementModeDisplay> VolumeMeasurementModeItems {
            get => _volumeMeasurementModeItems;
            set => SetProperty(ref _volumeMeasurementModeItems, value);
        }
    }

    public class TriggerModeDisplay : BindableBase {
        private int _triggerMode;
        private string _display = string.Empty;

        public int TriggerMode {
            get => _triggerMode;
            set => SetProperty(ref _triggerMode, value);
        }

        public string Display {
            get => _display;
            set => SetProperty(ref _display, value);
        }
    }

    public class VolumeMeasurementModeDisplay : BindableBase {
        private int _volumeMeasurementMode;
        private string _display = string.Empty;

        public int VolumeMeasurementMode {
            get => _volumeMeasurementMode;
            set => SetProperty(ref _volumeMeasurementMode, value);
        }

        public string Display {
            get => _display;
            set => SetProperty(ref _display, value);
        }
    }
}