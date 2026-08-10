using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Data.SqlTypes;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.License {

    /// <summary>
    /// 用户基础信息
    /// </summary>
    [Table("Sys_LicenseUserInfo", Schema = "dbo")]
    public class LicenseUserInfo : BaseLicenseModel {

        /// <summary>
        /// pid
        /// </summary>
        [Required, Column("Pid")]
        public long Pid { get; set; }

        /// <summary>
        /// 用户代码
        /// </summary>
        [Required, Column("UserCode"), UpdateBy]
        public string UserCode { get; set; } = string.Empty;

        /// <summary>
        /// 用户名
        /// </summary>
        [Required, Column("UserName"), InsertOrUpdate]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        [Required, Column("PassWord"), InsertOrUpdate, JsonIgnore]
        public string PassWord { get; set; } = string.Empty;

        /// <summary>
        /// 手机号
        /// </summary>
        [Required, Column("Phone"), InsertOrUpdate]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// 角色
        /// </summary>
        [Required, Column("Role")]
        public UserRole Role { get; set; } = UserRole.None;

        /// <summary>
        /// 用户状态
        /// </summary>
        [Required, Column("Status")]
        public UserStatus Status { get; set; } = UserStatus.Active;

        /// <summary>
        /// 用户图片
        /// </summary>
        [Column("UserIcon")]
        public string? UserIcon { get; set; }

        /// <summary>
        /// 详细信息
        /// </summary>
        public virtual LicenseUserDetailsInfo? UserDetailsInfo { get; set; }

        public ICollection<LicenseCodeInfo>? LicenseCodeInfos { get; set; }

        public ICollection<LicenseAppLicenseInfo>? AppLicenseInfos { get; set; }
    }

    [Flags]
    public enum UserRole {

        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// 超级管理员
        /// </summary>
        SuperAdmin = 1 << 0,

        /// <summary>
        /// 租户
        /// </summary>
        Tenant = 1 << 1,

        /// <summary>
        /// 客户
        /// </summary>
        Customer = 1 << 2
    }

    public enum UserStatus {

        /// <summary>
        /// 激活
        /// </summary>
        Active = 0,

        /// <summary>
        /// 冻结
        /// </summary>
        Frozen = 1,

        /// <summary>
        /// 失效
        /// </summary>
        Invalid = 2
    }
}