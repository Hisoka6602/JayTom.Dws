using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.CommunicationsSettingsModel {
    public class DeviceControlSettingsInfoModel : BindableBase {
        private bool _isUseCreatePackageByDevice;
        private bool _isUseRemovePackageByDevice;
        private bool _isUseStartDeviceByDevice;
        private bool _isUseStopDeviceByDevice;

        /// <summary>
        /// 是否由下位机创建包裹
        /// </summary>
        public bool IsUseCreatePackageByDevice {
            get => _isUseCreatePackageByDevice;
            set => SetProperty(ref _isUseCreatePackageByDevice, value);
        }

        /// <summary>
        /// 是否由下位机移除包裹
        /// </summary>
        public bool IsUseRemovePackageByDevice {
            get => _isUseRemovePackageByDevice;
            set => SetProperty(ref _isUseRemovePackageByDevice, value);
        }

        /// <summary>
        /// 是否由下位机启动运行
        /// </summary>
        public bool IsUseStartDeviceByDevice {
            get => _isUseStartDeviceByDevice;
            set => SetProperty(ref _isUseStartDeviceByDevice, value);
        }

        /// <summary>
        /// 是否由下位机停止运行
        /// </summary>
        public bool IsUseStopDeviceByDevice {
            get => _isUseStopDeviceByDevice;
            set => SetProperty(ref _isUseStopDeviceByDevice, value);
        }
    }
}
