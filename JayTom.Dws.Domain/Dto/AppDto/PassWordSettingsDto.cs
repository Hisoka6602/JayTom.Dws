using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.AppDto {

    public class PassWordSettingsDto {

        /// <summary>
        /// 保护模块
        /// </summary>
        public List<PasswordProtectionModuleInfo> PasswordProtectionModuleItems { get; set; } = new();

        /// <summary>
        /// 获取或设置一个值，该值指示是否使用密码保护。
        /// </summary>
        public bool IsUsePasswordProtection { get; set; }

        /// <summary>
        /// 获取或设置密码。
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置密码提示。
        /// </summary>
        public string PasswordHint { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置一个值，该值指示是否验证密码后，在本次启动期间的其他操作中不重复验证。
        /// </summary>
        public bool SkipPasswordValidationForThisSession { get; set; } = true;
    }

    public class PasswordProtectionModuleInfo {

        /// <summary>
        /// 是否使用密码保护
        /// </summary>
        public bool IsProtected { get; set; }

        /// <summary>
        /// 页面类名
        /// </summary>
        public string PageClassName { get; set; } = string.Empty;

        /// <summary>
        /// 描述信息
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}