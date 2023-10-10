using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig {

    [Table("Conf_LogisticsRuleInfo", Schema = "dbo")]
    public class LogisticsRuleInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 物流分拣Id
        /// </summary>
        [Column("LogisticsSortingId"), Required, InsertOrUpdata]
        public long LogisticsSortingId { get; set; }

        /// <summary>
        /// 物流Id
        /// </summary>
        [Column("LogisticsId"), Required, InsertOrUpdata]
        public long LogisticsId { get; set; }

        /// <summary>
        /// 规则名称
        /// </summary>
        [Column("RuleName"), Required, InsertOrUpdata]
        public string RuleName { get; set; } = string.Empty;

        [ForeignKey("Id")]
        public virtual LogisticsSortingInfoModel LogisticsSortingInfo { get; set; }
    }
}