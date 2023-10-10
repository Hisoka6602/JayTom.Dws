using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig {

    [Table("Conf_OcrRuleInfo", Schema = "dbo")]
    public class OcrRuleInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// Ocr分拣Id
        /// </summary>
        [Column("OcrSortingId"), Required, InsertOrUpdata]
        public long OcrSortingId { get; set; }

        /// <summary>
        /// 正则表达式
        /// </summary>
        [Column("RegexPattern"), Required, InsertOrUpdata]
        public string RegexPattern { get; set; } = string.Empty;

        [ForeignKey("Id")]
        public virtual OcrSortingInfoModel OcrSortingInfo { get; set; }
    }
}