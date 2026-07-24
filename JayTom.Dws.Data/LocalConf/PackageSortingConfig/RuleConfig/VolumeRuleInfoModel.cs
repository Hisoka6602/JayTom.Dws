using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig {

    [Table("Conf_VolumeRuleInfo", Schema = "dbo")]
    public class VolumeRuleInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 体积分拣Id
        /// </summary>
        [Column("VolumeSortingId"), Required, InsertOrUpdata]
        public long VolumeSortingId { get; set; }

        /*/// <summary>
        /// 运算符
        /// </summary>
        [Column("Operator"), Required, InsertOrUpdata]
        public ComparisonOperator Operator { get; set; }

        /// <summary>
        /// 体积属性
        /// </summary>
        [Column("VolumeProperty"), Required, InsertOrUpdata]
        public VolumeProperty VolumeProperty { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        [Column("Value"), Required, InsertOrUpdata]
        public float Value { get; set; }

        /// <summary>
        /// 规则名称
        /// </summary>
        [Column("RuleName"), Required, InsertOrUpdata]
        public string RuleName { get; set; } = string.Empty;*/

        /// <summary>
        /// 规则
        /// </summary>
        [Column("Formula"), Required, InsertOrUpdata]
        public string Formula { get; set; } = string.Empty;

        [ForeignKey(nameof(VolumeSortingId))]
        public virtual VolumeSortingInfoModel? VolumeSortingInfo { get; set; }
    }

    public enum VolumeProperty {

        /// <summary>
        /// 长
        /// </summary>
        Length,

        /// <summary>
        /// 宽
        /// </summary>
        Width,

        /// <summary>
        /// 高
        /// </summary>
        Height,

        /// <summary>
        /// 体积
        /// </summary>
        Volume
    }
}
