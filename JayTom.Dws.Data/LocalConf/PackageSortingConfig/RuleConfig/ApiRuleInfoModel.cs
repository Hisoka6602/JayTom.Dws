using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig {

    [Table("Conf_ApiRuleInfo", Schema = "dbo")]
    public class ApiRuleInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// Api分拣Id
        /// </summary>
        [Column("ApiSortingId"), Required, InsertOrUpdata]
        public long ApiSortingId { get; set; }

        /// <summary>
        /// Json内容
        /// </summary>
        [Column("JsonContent"), Required, InsertOrUpdata]
        public string JsonContent { get; set; } = string.Empty;

        [ForeignKey("Id")]
        public virtual ApiSortingInfoModel? ApiSortingInfo { get; set; }
    }
}