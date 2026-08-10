using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.License {

    /// <summary>
    /// 应用程序功能
    /// </summary>
    [Table("App_LicenseFeatureInfo", Schema = "dbo")]
    public class LicenseFeatureInfo : BaseLicenseModel {

        [Column("LicenseApplicationInfoId")]
        public long LicenseApplicationInfoId { get; set; }

        [ForeignKey("Id")]
        public virtual LicenseApplicationInfo? LicenseApplicationInfo { get; set; }

        [Required, Column("Pid")]
        public long Pid { get; set; }

        /// <summary>
        /// 功能名称
        /// </summary>
        [Required, Column("FeatureName")]
        public string FeatureName { get; set; } = string.Empty;

        /// <summary>
        /// 功能名称
        /// </summary>
        [Required, Column("FeatureGuid")]
        public string FeatureGuid { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        [Required, Column("Description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 是否激活
        /// </summary>
        [Required, Column("IsActive")]
        public bool IsActive { get; set; } = true;
    }
}