using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.License {

    [Table("App_LicenseAppLicenseInfo", Schema = "dbo")]
    public class LicenseAppLicenseInfo : BaseLicenseModel {

        [Column("UserId")]
        public long? UserId { get; set; }

        [ForeignKey("Id")]
        public virtual LicenseUserInfo? UserInfo { get; set; }

        [Column("LicensePermissionTemplateInfoId")]
        public long? LicensePermissionTemplateInfoId { get; set; }

        [ForeignKey("Id")]
        public virtual LicensePermissionTemplateInfo? LicensePermissionTemplateInfo { get; set; }

        /// <summary>
        /// 授权码上限
        /// </summary>
        [Column("MaxLicenseCodeCount")]
        public int MaxLicenseCodeCount { get; set; }
    }
}