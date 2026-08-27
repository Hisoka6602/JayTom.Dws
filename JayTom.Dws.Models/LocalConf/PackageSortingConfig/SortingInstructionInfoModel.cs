using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalConf.PackageSortingConfig {

    [Table("Conf_SortingInstructionInfo", Schema = "dbo")]
    public class SortingInstructionInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 绑定Id
        /// </summary>
        [Column("InstructionBindingId"), Required, InsertOrUpdate]
        public long InstructionBindingId { get; set; }

        [ForeignKey(nameof(InstructionBindingId))]
        public virtual SortingInstructionBindingInfoModel? SortingInstructionBindingInfo { get; set; }

        /// <summary>
        /// 指令
        /// </summary>
        [Column("Instruction"), Required, InsertOrUpdate]
        public string Instruction { get; set; } = string.Empty;

        /// <summary>
        /// 应答内容
        /// </summary>
        [Column("ReplyContent"), Required, InsertOrUpdate]
        public string ReplyContent { get; set; } = string.Empty;
    }
}
