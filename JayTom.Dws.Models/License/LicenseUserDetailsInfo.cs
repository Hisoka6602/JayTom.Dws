using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalData;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.License {

    /// <summary>
    /// 用户详细信息
    /// </summary>
    [Table("Sys_LicenseUserDetailsInfo", Schema = "dbo")]
    public class LicenseUserDetailsInfo : BaseLicenseModel {

        [Column("UserId")]
        public long UserId { get; set; }

        [ForeignKey("Id")]
        public virtual LicenseUserInfo? UserInfo { get; set; }

        /// <summary>
        /// 公司名称
        /// </summary>
        [Required, Column("CompanyName")]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// 公司地址
        /// </summary>
        [Required, Column("CompanyAddress")]
        public string CompanyAddress { get; set; } = string.Empty;

        /// <summary>
        /// 联系邮箱
        /// </summary>
        [Required, Column("ContactEmail")]
        public string ContactEmail { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        [Required, Column("Description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 合同文件路径
        /// </summary>
        [Required, Column("ContractFilePath")]
        public string ContractFilePath { get; set; } = string.Empty;

        /// <summary>
        /// 营业执照文件路径
        /// </summary>
        [Required, Column("BusinessLicenseFilePath")]
        public string BusinessLicenseFilePath { get; set; } = string.Empty;
    }
}