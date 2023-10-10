using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations.Schema;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig {

    [Table("Conf_LogisticsSortingInfo", Schema = "dbo")]
    public class LogisticsSortingInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 绑定出口代码
        /// </summary>
        [Column("ExitId"), InsertOrUpdata]
        public long ExitId { get; set; }

        public virtual ICollection<LogisticsRuleInfoModel>? LogisticsRuleItems { get; set; }
    }
}