using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.CommunicationsSettings {
    public class DeviceControlSettingsInfo {
        /// <summary>
        /// 是否由下位机创建包裹
        /// </summary>
        public bool IsUseCreatePackageByDevice { get; set; }
        /// <summary>
        /// 是否由下位机移除包裹
        /// </summary>
        public bool IsUseRemovePackageByDevice { get; set; }
        /// <summary>
        /// 是否由下位机启动运行
        /// </summary>
        public bool IsUseStartDeviceByDevice { get; set; }
        /// <summary>
        /// 是否由下位机停止运行
        /// </summary>
        public bool IsUseStopDeviceByDevice { get; set; }
    }
}
