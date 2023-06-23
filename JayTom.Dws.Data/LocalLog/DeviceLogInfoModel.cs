using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {

    [Table("Log_DeviceLogInfo", Schema = "dbo")]
    public class DeviceLogInfoModel : BaseModel {

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("CreateTime"), Required]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 是否异常
        /// </summary>
        [Column("IsException")]
        public bool IsException { get; set; }

        /// <summary>
        /// 收发类型
        /// </summary>
        [Column("TransmitType")]
        public int TransmitType { get; set; }

        /// <summary>
        /// 指令内容
        /// </summary>
        [Column("InstructionContent")]
        public string InstructionContent { get; set; } = string.Empty;

        /// <summary>
        /// 提示内容
        /// </summary>
        [Column("PromptContent")]
        public string PromptContent { get; set; } = string.Empty;
    }
}