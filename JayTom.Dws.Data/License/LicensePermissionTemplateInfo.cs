using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.License {

    /// <summary>
    /// 应用程序权限模板
    /// </summary>
    [Table("App_LicensePermissionTemplateInfo", Schema = "dbo")]
    public class LicensePermissionTemplateInfo : BaseLicenseModel {

        /// <summary>
        /// 模板名称
        /// </summary>
        [Required, Column("TemplateName"), UpdateBy]
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// 应用程序
        /// </summary>
        public virtual LicenseApplicationInfo? LicenseApplicationInfo { get; set; }
    }
}