using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalConf.PackageSortingConfig {

    [Table("Conf_SortingInstructionBindingInfo", Schema = "dbo")]
    public class SortingInstructionBindingInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 出口代码
        /// </summary>
        [Column("ExitId"), Required, UpdateBy]
        public long? ExitId { get; set; }

        /// <summary>
        /// 延迟发送(ms)
        /// </summary>
        [Column("DelaySendMilliseconds"), Required, InsertOrUpdate]
        public int DelaySendMilliseconds { get; set; }

        /// <summary>
        /// 发送间隔(ms)
        /// </summary>
        [Column("SendIntervalMilliseconds"), Required, InsertOrUpdate]
        public int SendIntervalMilliseconds { get; set; }

        /// <summary>
        /// 是否生效
        /// </summary>
        [Column("IsActive"), Required, UpdateBy]
        public bool IsActive { get; set; }

        public virtual ICollection<SortingInstructionInfoModel>? InstructionItems { get; set; }
    }
}