using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

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
        None,

        /// <summary>
        /// 创建包裹
        /// </summary>
        CreatePackage,

        /// <summary>
        /// 发送分拣
        /// </summary>
        SendSorting,

        /// <summary>
        /// 信号回调(分拣后、移除包裹)
        /// </summary>
        SignalCallback,

        /// <summary>
        /// 心跳
        /// </summary>
        Heartbeat,

        /// <summary>
        /// 设备指令
        /// </summary>
        DeviceOperation,

        /// <summary>
        /// 包裹异常
        /// </summary>
        PackageException,

        /// <summary>
        /// 其他
        /// </summary>
        Other,

        /// <summary>
        /// 发送前置信号
        /// </summary>
        SendPreSignal,

        /// <summary>
        /// 接收前置信号回复
        /// </summary>
        ReceivePreSignalReply,

        /// <summary>
        /// 包裹信息赋值完成
        /// </summary>
        PackageInfoCompletedSignal,

        /// <summary>
        /// 序号绑定回复
        /// </summary>
        SequenceBindingReply
    }
}