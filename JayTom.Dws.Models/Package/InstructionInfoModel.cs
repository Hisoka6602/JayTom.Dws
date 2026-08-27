using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.Package {

    [Table("Data_InstructionInfo", Schema = "dbo")]
    public class InstructionInfoModel : BaseModel {

        [Column("SortingInfoId"), JsonIgnore]
        public long SortingInfoId { get; set; }

        [ForeignKey("Id")]
        public virtual SortingInfoModel? SortingInfo { get; set; }

        /// <summary>
        /// 指令内容
        /// </summary>
        [Column("InstructionContent")]
        public string InstructionContent { get; set; } = string.Empty;

        /// <summary>
        /// 指令产生时间
        /// </summary>
        [Column("InstructionGeneratedTime")]
        public DateTime InstructionGeneratedTime { get; set; }

        /// <summary>
        /// 指令类型
        /// </summary>
        [Column("InstructionType")]
        public InstructionType InstructionType { get; set; } = InstructionType.None;
    }

    public enum InstructionType {

        [Description("无")]
        None,

        /// <summary>
        /// 创建包裹
        /// </summary>
        [Description("创建包裹")]
        CreatePackage,

        /// <summary>
        /// 发送分拣
        /// </summary>
        [Description("发送分拣")]
        SendSorting,

        /// <summary>
        /// 信号回调(分拣后、移除包裹)
        /// </summary>
        [Description("落格回调")]
        SignalCallback,

        /// <summary>
        /// 心跳
        /// </summary>
        [Description("心跳")]
        Heartbeat,

        /// <summary>
        /// 设备指令
        /// </summary>
        [Description("设备指令")]
        DeviceOperation,

        /// <summary>
        /// 包裹异常
        /// </summary>
        [Description("包裹异常")]
        PackageException,

        /// <summary>
        /// 包裹异常(需要判断操作)
        /// </summary>
        [Description("包裹异常")]
        PackageExceptionEx,

        /// <summary>
        /// 其他
        /// </summary>
        [Description("其他")]
        Other,

        /// <summary>
        /// 发送前置信号
        /// </summary>
        [Description("发送前置信号")]
        SendPreSignal,

        /// <summary>
        /// 接收前置信号回复
        /// </summary>
        [Description("接收前置信号回复")]
        ReceivePreSignalReply,

        /// <summary>
        /// 包裹信息赋值完成
        /// </summary>
        [Description("包裹信息赋值完成")]
        PackageInfoCompletedSignal,

        /// <summary>
        /// 序号绑定回复
        /// </summary>
        [Description("序号绑定回复")]
        SequenceBindingReply,

        /// <summary>
        /// 复位按钮触发
        /// </summary>
        [Description("复位按钮触发")]
        ResetButtonTrigger,

        /// <summary>
        /// 包裹居中
        /// </summary>
        [Description("包裹居中")]
        PackageCenter
    }
}