using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations.Schema;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig {

    [Table("Conf_ApiSortingInfo", Schema = "dbo")]
    public class ApiSortingInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 绑定出口代码
        /// </summary>
        [Column("ExitId"), InsertOrUpdate]
        public long ExitId { get; set; }

        [Column("SortingName"), InsertOrUpdate]
        public string SortingName { get; set; } = string.Empty;

        public virtual ICollection<ApiRuleInfoModel>? ApiRuleItems { get; set; }
    }
}