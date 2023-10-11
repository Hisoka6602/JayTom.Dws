using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig {

    public class ApiRuleInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// Api分拣Id
        /// </summary>
        [Column("ApiSortingId"), Required, InsertOrUpdata]
        public long ApiSortingId { get; set; }

        /// <summary>
        /// 正则表达式
        /// </summary>
        [Column("RegexPattern"), Required, InsertOrUpdata]
        public string RegexPattern { get; set; } = string.Empty;

        [ForeignKey("Id")]
        public virtual ApiSortingInfoModel ApiSortingInfo { get; set; }
    }
}