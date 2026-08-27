using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig {

    [Table("Conf_ WeightRuleInfo", Schema = "dbo")]
    public class WeightRuleInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 重量分拣Id
        /// </summary>
        [Column("WeightSortingId"), Required, InsertOrUpdate]
        public long WeightSortingId { get; set; }

        /*/// <summary>
        /// 运算符
        /// </summary>
        [Column("Operator"), Required, InsertOrUpdate]
        public ComparisonOperator Operator { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        [Column("Value"), Required, InsertOrUpdate]
        public decimal Value { get; set; }*/

        /// <summary>
        /// 规则
        /// </summary>
        [Column("Formula"), Required, InsertOrUpdate]
        public string Formula { get; set; } = string.Empty;

        [ForeignKey(nameof(WeightSortingId))]
        public virtual WeightSortingInfoModel? WeightSortingInfo { get; set; }
    }

    /// <summary>
    /// 运算符
    /// </summary>
    public enum ComparisonOperator {

        /// <summary>
        /// 大于
        /// </summary>
        GreaterThan,

        /// <summary>
        /// 小于
        /// </summary>
        LessThan,

        /// <summary>
        /// 等于
        /// </summary>
        Equal,

        /// <summary>
        /// 不等于
        /// </summary>
        NotEqual
    }
}
