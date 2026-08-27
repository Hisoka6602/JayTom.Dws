using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig {

    [Table("Conf_LogisticsRegexInfo", Schema = "dbo")]
    public class LogisticsRegexInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 物流Id
        /// </summary>
        [Column("LogisticsId"), Required, InsertOrUpdate]
        public long LogisticsId { get; set; }

        /// <summary>
        /// 正则表达式
        /// </summary>
        [Column("RegexPattern"), Required, InsertOrUpdate]
        public string RegexPattern { get; set; } = string.Empty;

        [ForeignKey(nameof(LogisticsId))]
        public virtual LogisticsCodeRecognitionInfoModel? LogisticsCodeInfo { get; set; }
    }
}
