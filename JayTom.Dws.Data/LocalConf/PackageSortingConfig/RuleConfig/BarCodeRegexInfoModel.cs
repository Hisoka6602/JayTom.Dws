using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig {

    [Table("Conf_BarCodeRegexInfo", Schema = "dbo")]
    public class BarCodeRegexInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 条码分拣Id
        /// </summary>
        [Column("BarCodeSortingId"), Required, InsertOrUpdata]
        public long BarCodeSortingId { get; set; }

        /// <summary>
        /// 正则表达式
        /// </summary>
        [Column("RegexPattern"), Required, InsertOrUpdata]
        public string RegexPattern { get; set; } = string.Empty;

        [ForeignKey("Id")]
        public virtual BarCodeSortingInfoModel? BarCodeSortingInfo { get; set; }
    }
}