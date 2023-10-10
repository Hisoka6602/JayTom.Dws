using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig {

    [Table("Conf_ WeightRuleInfo", Schema = "dbo")]
    public class WeightRuleInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 重量分拣Id
        /// </summary>
        [Column("WeightSortingId"), Required, InsertOrUpdata]
        public long WeightSortingId { get; set; }

        /*/// <summary>
        /// 运算符
        /// </summary>
        [Column("Operator"), Required, InsertOrUpdata]
        public ComparisonOperator Operator { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        [Column("Value"), Required, InsertOrUpdata]
        public float Value { get; set; }*/

        /// <summary>
        /// 规则
        /// </summary>
        [Column("Formula"), Required, InsertOrUpdata]
        public string Formula { get; set; } = string.Empty;

        [ForeignKey("Id")]
        public virtual WeightSortingInfoModel WeightSortingInfo { get; set; }
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