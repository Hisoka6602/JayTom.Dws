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
        public int PrecedingReplySignalTimeout { get; set; } = 2000;

        /// <summary>
        /// 绑定信号回复超时时间
        /// </summary>
        public int BindingCarSignalReplyTimeout { get; set; } = 2000;

        /// <summary>
        /// 前置信号超时后移除包裹
        /// </summary>
        public bool RemovePackageAfterSignalTimeout { get; set; }

        /// <summary>
        /// 复位后是否清空包裹
        /// </summary>
        public bool ClearPackagesOnReset { get; set; }

        /// <summary>
        /// 移除包裹后是否重置过滤
        /// </summary>
        public bool ResetFilterAfterRemovingPackage { get; set; }
    }

    /// <summary>
    /// 供包台信号类
    /// </summary>
    public class SupplyCounterPackageSignal {

        /// <summary>获取信号创建时的单调时钟时间戳，仅用于超时判断。</summary>
        public long CreatedAtMonotonicTimestamp { get; } = System.Diagnostics.Stopwatch.GetTimestamp();

        /// <summary>
        /// 指令
        /// </summary>
        public string Instruction { get; set; } = string.Empty;

        /// <summary>
        /// 信号类型
        /// </summary>
        public SignalType Type { get; set; }

        /// <summary>
        /// 时间
        /// </summary>
        public DateTime Time { get; set; }
    }

    /// <summary>
    /// 供包台信息
    /// </summary>
    public enum SignalType {

        /// <summary>
        /// 发送的前置信号
        /// </summary>
        SendingPreSignal,

        /// <summary>
        /// 返回的前置信号
        /// </summary>
        ReturningPreSignal,

        /// <summary>
        /// 发送的赋值完成信号
        /// </summary>
        SendingAssignmentCompleteSignal,

        /// <summary>
        /// 返回的绑定信号
        /// </summary>
        ReturningBindingSignal
    }
}
