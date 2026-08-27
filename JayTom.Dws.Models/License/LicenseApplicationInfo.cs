using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.License {

    /// <summary>
    /// 应用程序信息
    /// </summary>
    [Table("App_LicenseApplicationInfo", Schema = "dbo")]
    public class LicenseApplicationInfo : BaseLicenseModel {
        /// <summary>
        /// 模板Id
        /// </summary>

        [Column("LicensePermissionTemplateId")]
        public long? LicensePermissionTemplateId { get; set; }

        [ForeignKey("Id")]
        public virtual LicensePermissionTemplateInfo? LicensePermissionTemplate { get; set; }

        /// <summary>
        /// 应用程序名称
        /// </summary>
        [Required, Column("ApplicationName"), UpdateBy]
        public string ApplicationName { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        [Required, Column("Description"), InsertOrUpdate]
        public string Description { get; set; } = string.Empty;

        public virtual ICollection<LicenseFeatureInfo>? LicenseFeatureInfos { get; set; }
    }
}