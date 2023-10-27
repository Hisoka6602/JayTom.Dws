using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig {

    /// <summary>
    /// 格口信息
    /// </summary>
    [Table("Conf_PackageExitDefinitionInfo", Schema = "dbo")]
    public class PackageExitDefinitionInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 出口名称
        /// </summary>
        [Column("ExitName"), Required, InsertOrUpdata]
        public string ExitName { get; set; } = string.Empty;

        /// <summary>
        /// 出口类型(异常出口只能生效一个)
        /// </summary>
        [Column("Type"), Required, InsertOrUpdata]
        public ExitType Type { get; set; } = ExitType.PackageExit;

        /// <summary>
        /// 是否生效
        /// </summary>
        [Column("IsActive"), Required, InsertOrUpdata]
        public bool IsActive { get; set; }
    }

    public enum ExitType {

        /// <summary>
        /// 包裹出口
        /// </summary>
        PackageExit = 0,

        /// <summary>
        /// 异常出口
        /// </summary>
        AbnormalExit = 1
    }
}