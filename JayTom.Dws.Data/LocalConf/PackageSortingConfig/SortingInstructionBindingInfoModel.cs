using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig {

    [Table("Conf_SortingInstructionBindingInfo", Schema = "dbo")]
    public class SortingInstructionBindingInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 分拣指令组(使用换行符分割)
        /// </summary>
        [Column("SortingInstructionGroup"), Required, UpdateBy]
        public string SortingInstructionGroup { get; set; } = string.Empty;

        /// <summary>
        /// 出口代码
        /// </summary>
        [Column("ExitCode"), Required, InsertOrUpdata]
        public string? ExitCode { get; set; }

        /// <summary>
        /// 延迟发送(ms)
        /// </summary>
        [Column("DelaySendMilliseconds"), Required, InsertOrUpdata]
        public int DelaySendMilliseconds { get; set; }

        /// <summary>
        /// 发送间隔(ms)
        /// </summary>
        [Column("SendIntervalMilliseconds"), Required, InsertOrUpdata]
        public int SendIntervalMilliseconds { get; set; }

        /// <summary>
        /// 是否生效
        /// </summary>
        [Column("IsActive"), Required, InsertOrUpdata]
        public bool IsActive { get; set; }
    }
}