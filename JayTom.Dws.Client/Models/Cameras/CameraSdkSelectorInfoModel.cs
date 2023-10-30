using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.Cameras {
    public class CameraSdkSelectorInfoModel : BindableBase {
        private bool _isUseHikvisionSmartCameraSdk;
        private bool _isUseHikvisionIndustrialCameraSdk;
        private bool _isUseDaHuaSmartCameraSdk;
        private bool _isUseDaHuaSecurityCameraSdk;
        private bool _isUseWayzimSmartCameraSdk;
        private bool _isUseWayzimIndustrialCameraSdk;
        private bool _isUseHikvisionVolumeCameraSdk;
        private bool _isUseDaHuaVolumeCameraSdk;

        /// <summary>
        /// 是否使用海康智能相机SDK
        /// </summary>
        public bool IsUseHikvisionSmartCameraSdk {
            get => _isUseHikvisionSmartCameraSdk;
            set => SetProperty(ref _isUseHikvisionSmartCameraSdk, value);
        }

        /// <summary>
        /// 是否使用海康工业相机SDK
        /// </summary>
        public bool IsUseHikvisionIndustrialCameraSdk {
            get => _isUseHikvisionIndustrialCameraSdk;
            set => SetProperty(ref _isUseHikvisionIndustrialCameraSdk, value);
        }

        /// <summary>
        /// 是否使用大华智能相机SDK
        /// </summary>
        public bool IsUseDaHuaSmartCameraSdk {
            get => _isUseDaHuaSmartCameraSdk;
            set => SetProperty(ref _isUseDaHuaSmartCameraSdk, value);
        }

        /// <summary>
        /// 是否使用大华安防相机SDK
        /// </summary>
        public bool IsUseDaHuaSecurityCameraSdk {
            get => _isUseDaHuaSecurityCameraSdk;
            set => SetProperty(ref _isUseDaHuaSecurityCameraSdk, value);
        }

        /// <summary>
        /// 是否使用中科微至智能相机SDK
        /// </summary>
        public bool IsUseWayzimSmartCameraSdk {
            get => _isUseWayzimSmartCameraSdk;
            set => SetProperty(ref _isUseWayzimSmartCameraSdk, value);
        }

        /// <summary>
        /// 是否使用中科微至工业相机SDK
        /// </summary>
        public bool IsUseWayzimIndustrialCameraSdk {
            get => _isUseWayzimIndustrialCameraSdk;
            set => SetProperty(ref _isUseWayzimIndustrialCameraSdk, value);
        }
        /// <summary>
        /// 是否使用海康体积相机
        /// </summary>
        public bool IsUseHikvisionVolumeCameraSdk {
            get => _isUseHikvisionVolumeCameraSdk;
            set => SetProperty(ref _isUseHikvisionVolumeCameraSdk, value);
        }
        /// <summary>
        /// 是否使用大华体积相机
        /// </summary>
        public bool IsUseDaHuaVolumeCameraSdk {
            get => _isUseDaHuaVolumeCameraSdk;
            set => SetProperty(ref _isUseDaHuaVolumeCameraSdk, value);
        }
    }
}