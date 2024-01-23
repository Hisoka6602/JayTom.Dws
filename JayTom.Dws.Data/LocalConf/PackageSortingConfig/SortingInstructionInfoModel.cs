using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig {

    [Table("Conf_SortingInstructionInfo", Schema = "dbo")]
    public class SortingInstructionInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 绑定Id
        /// </summary>
        [Column("InstructionBindingId"), Required, InsertOrUpdata]
        public long InstructionBindingId { get; set; }

        [ForeignKey("Id")]
        public virtual SortingInstructionBindingInfoModel? SortingInstructionBindingInfo { get; set; }

        /// <summary>
        /// 指令
        /// </summary>
        [Column("Instruction"), Required, InsertOrUpdata]
        public string Instruction { get; set; } = string.Empty;

        /// <summary>
        /// 应答内容
        /// </summary>
        [Column("ReplyContent"), Required, InsertOrUpdata]
        public string ReplyContent { get; set; } = string.Empty;
    }
}