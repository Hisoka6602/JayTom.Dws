using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig {

    [Table("Conf_LogisticsRegexInfo", Schema = "dbo")]
    public class LogisticsRegexInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 物流代码
        /// </summary>
        [Column("LogisticsCode"), Required, InsertOrUpdata]
        public string LogisticsCode { get; set; } = string.Empty;

        /// <summary>
        /// 正则表达式
        /// </summary>
        [Column("RegexPattern"), Required, InsertOrUpdata]
        public string RegexPattern { get; set; } = string.Empty;
    }
}