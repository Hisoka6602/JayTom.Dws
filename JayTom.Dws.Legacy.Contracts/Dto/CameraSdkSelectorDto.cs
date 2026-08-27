using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Dto {
    public class CameraSdkSelectorDto {

        /// <summary>
        /// 是否使用海康智能相机SDK
        /// </summary>
        public bool IsUseHikvisionSmartCameraSdk { get; set; }

        /// <summary>
        /// 是否使用海康工业相机SDK
        /// </summary>
        public bool IsUseHikvisionIndustrialCameraSdk { get; set; }

        /// <summary>
        /// 是否使用大华智能相机SDK
        /// </summary>
        public bool IsUseDaHuaSmartCameraSdk { get; set; }

        /// <summary>
        /// 是否使用大华安防相机SDK
        /// </summary>
        public bool IsUseDaHuaSecurityCameraSdk { get; set; }

        /// <summary>
        /// 是否使用中科微至智能相机SDK
        /// </summary>
        public bool IsUseWayzimSmartCameraSdk { get; set; }

        /// <summary>
        /// 是否使用中科微至工业相机SDK
        /// </summary>
        public bool IsUseWayzimIndustrialCameraSdk { get; set; }
        /// <summary>
        /// 是否使用海康体积相机
        /// </summary>
        public bool IsUseHikvisionVolumeCameraSdk { get; set; }
        /// <summary>
        /// 是否使用大华体积相机
        /// </summary>
        public bool IsUseDaHuaVolumeCameraSdk { get; set; }
        /// <summary>
        /// 是否使用量房体积相机
        /// </summary>

        public bool IsUseDimensionVolumeCameraSdk { get; set; }
        /// <summary>
        /// 是否使用Usb相机
        /// </summary>

        public bool IsUsbCameraSdk { get; set; }
    }
}