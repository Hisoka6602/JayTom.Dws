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

        /// <summary>
        /// 起始前置序号
        /// </summary>
        public int StartPrecedingNumber { get; set; } = 1;

        /// <summary>
        /// 前置信号极限值
        /// </summary>
        public int PrecedingSignalMaxValue { get; set; } = 100;

        /// <summary>
        /// 是否等待前置信号回复再创建新包裹
        /// </summary>
        public bool IsWaitForPrecedingSignalReplyBeforeCreatingNewPackage { get; set; } = true;

        /// <summary>
        /// 是否等待绑定车号信号再完成包裹
        /// </summary>
        public bool IsWaitForBindingCarSignalToCompletePackage { get; set; } = true;

        /// <summary>
        /// 前置回复信号超时时间
        /// </summary>
        public int PrecedingReplySignalTimeout { get; set; } = 5000;
    }
}