using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf {

    [Table("Conf_InstructionInfo", Schema = "dbo")]
    public class InstructionInfoModel : BaseModel {

        /// <summary>
        /// 指令内容
        /// </summary>
        [Column("InstructionContent"), Required]
        public string InstructionContent { get; set; } = string.Empty;

        /// <summary>
        /// 类型
        /// </summary>
        [Column("Type"), Required]
        public InstructionType Type { get; set; }

        /// <summary>
        /// 注释
        /// </summary>
        [Column("Comment")]
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// 是否生效
        /// </summary>
        [Column("IsActive")]
        public bool IsActive { get; set; }

        /// <summary>
        /// 提示内容
        /// </summary>
        [Column("PromptContent")]
        public string PromptContent { get; set; } = string.Empty;

        /// <summary>
        /// 是否需要强提示并停机
        /// </summary>
        [Column("IsForcePromptAndShutdownRequired")]
        public bool IsForcePromptAndShutdownRequired { get; set; }

        /// <summary>
        /// 格口ID
        /// </summary>
        [Column("CompartmentId")]
        public int CompartmentId { get; set; }
    }

    public enum InstructionType {

        /// <summary>
        /// 启停指令
        /// </summary>
        StartStop,

        /// <summary>
        /// 下位机提示
        /// </summary>
        DownstreamPrompt,

        /// <summary>
        /// 格口
        /// </summary>
        Compartment,

        /// <summary>
        /// 心跳
        /// </summary>
        Heartbeat
    }
}