using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations.Schema;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig {

    [Table("Conf_WeightSortingInfo", Schema = "dbo")]
    public class WeightSortingInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 绑定出口代码
        /// </summary>
        [Column("ExitId"), InsertOrUpdate]
        public long ExitId { get; set; }

        [Column("SortingName"), InsertOrUpdate]
        public string SortingName { get; set; } = string.Empty;

        public virtual ICollection<WeightRuleInfoModel>? WeightRuleItems { get; set; }
    }
}