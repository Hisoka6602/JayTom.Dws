using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub {

    public class DeviceExtensionConfigItemInfoModel : BasePackageSortingItemInfoModel {
        /*private bool _createPackageByDevice;
        private bool _removePackageByDevice;
        private bool _startRunningByDevice;
        private bool _stopRunningByDevice;*/
        private bool _validateDeviceResponse;
        private int _validationTimeout;
        private int _maxRetryCount;

        /*
        /// <summary>
        /// 是否由下位机创建包裹
        /// </summary>
        public bool CreatePackageByDevice {
            get => _createPackageByDevice;
            set => SetProperty(ref _createPackageByDevice, value);
        }

        /// <summary>
        /// 是否由下位机移除包裹
        /// </summary>
        public bool RemovePackageByDevice {
            get => _removePackageByDevice;
            set => SetProperty(ref _removePackageByDevice, value);
        }

        /// <summary>
        /// 是否由下位机启动运行
        /// </summary>
        public bool StartRunningByDevice {
            get => _startRunningByDevice;
            set => SetProperty(ref _startRunningByDevice, value);
        }

        /// <summary>
        /// 是否由下位机停止运行
        /// </summary>
        public bool StopRunningByDevice {
            get => _stopRunningByDevice;
            set => SetProperty(ref _stopRunningByDevice, value);
        }
        */

        /// <summary>
        /// 是否验证下位机应答
        /// </summary>
        public bool ValidateDeviceResponse {
            get => _validateDeviceResponse;
            set => SetProperty(ref _validateDeviceResponse, value);
        }

        /// <summary>
        /// 验证超时时间
        /// </summary>
        public int ValidationTimeout {
            get => _validationTimeout;
            set => SetProperty(ref _validationTimeout, value);
        }

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount {
            get => _maxRetryCount;
            set => SetProperty(ref _maxRetryCount, value);
        }
    }
}