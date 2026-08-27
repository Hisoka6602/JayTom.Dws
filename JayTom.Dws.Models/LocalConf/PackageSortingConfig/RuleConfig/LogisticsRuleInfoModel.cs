using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig {

    [Table("Conf_LogisticsRuleInfo", Schema = "dbo")]
    public class LogisticsRuleInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 物流分拣Id
        /// </summary>
        [Column("LogisticsSortingId"), Required, InsertOrUpdate]
        public long LogisticsSortingId { get; set; }

        /// <summary>
        /// 物流Id
        /// </summary>
        [Column("LogisticsId"), Required, InsertOrUpdate]
        public long LogisticsId { get; set; }

        /*/// <summary>
        /// 规则名称
        /// </summary>
        [Column("RuleName"), Required, InsertOrUpdate]
        public string RuleName { get; set; } = string.Empty;*/

        [ForeignKey(nameof(LogisticsSortingId))]
        public virtual LogisticsSortingInfoModel? LogisticsSortingInfo { get; set; }
    }
}
