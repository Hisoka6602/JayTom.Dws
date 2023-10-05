using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
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

        /// <summary>
        /// 指令
        /// </summary>
        [Column("Instruction"), Required, InsertOrUpdata]
        public string Instruction { get; set; } = string.Empty;

        [ForeignKey("Id")]
        public virtual SortingInstructionBindingInfoModel SortingInstructionBindingInfo { get; set; }
    }
}