using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {

    [Table("Log_InstructionLogInfo", Schema = "dbo")]
    public class InstructionLogInfoModel : BaseModel {

        /// <summary>
        /// 时间戳Id
        /// </summary>
        [Column("TimestampedGuid"), Required]
        public long TimestampedGuid { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        [Column("Type")]
        public int Type { get; set; }

        /// <summary>
        /// 下位机指令内容
        /// </summary>
        [Column("InstructionContent")]
        public string? InstructionContent { get; set; }

        /// <summary>
        /// 指令产生时间
        /// </summary>
        [Column("InstructionCreateTime")]
        public DateTime InstructionCreateTime { get; set; }

        /// <summary>
        /// 目标地址
        /// </summary>
        [Column("DestinationAddress")]
        public string? DestinationAddress { get; set; }

        /// <summary>
        /// 源地址
        /// </summary>
        [Column("SourceAddress")]
        public string? SourceAddress { get; set; }

        /// <summary>
        /// 设备
        /// </summary>
        [Column("Device")]
        public string? Device { get; set; }
    }
}