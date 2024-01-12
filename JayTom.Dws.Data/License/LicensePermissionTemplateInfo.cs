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
        /// 创建人
        /// </summary>
        [Required, Column("CreateBy")]
        public string CreateBy { get; set; } = string.Empty;

        /// <summary>
        /// 应用程序
        /// </summary>
        [ForeignKey("Id")]
        public virtual LicenseApplicationInfo? LicenseApplicationInfo { get; set; }

        /// <summary>
        /// 授权码
        /// </summary>
        public ICollection<LicenseCodeInfo>? LicenseCodeInfos { get; set; }
    }
}