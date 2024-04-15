using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto {

    public class SupplyCounterSettingsDto {

        /// <summary>
        /// 是否使用供包台模式
        /// </summary>
        public bool IsUseSupplyCounterMode { get; set; }

        /// <summary>
        /// 是否发送前置序号
        /// </summary>
        public bool SendPreSequenceNumber { get; set; }

        /// <summary>
        /// 是否等待体积信息
        /// </summary>
        public bool WaitForVolumeInformation { get; set; }
    }
}