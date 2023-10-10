using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig {

    [Table("Conf_LogisticsRegexInfo", Schema = "dbo")]
    public class LogisticsRegexInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 物流Id
        /// </summary>
        [Column("LogisticsId"), Required, InsertOrUpdata]
        public long LogisticsId { get; set; }

        /// <summary>
        /// 正则表达式
        /// </summary>
        [Column("RegexPattern"), Required, InsertOrUpdata]
        public string RegexPattern { get; set; } = string.Empty;

        [ForeignKey("Id")]
        public virtual LogisticsCodeRecognitionInfoModel LogisticsCodeInfo { get; set; }
    }
}