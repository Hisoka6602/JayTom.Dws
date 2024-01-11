using JayTom.Dws.LicenseApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class UpdateProfileDo {

        /// <summary>
        /// 用户名
        /// </summary>
        [MaxLength(15, ErrorMessage = "名称长度不能超过15个字符"),
         UserNameExists(IsExists = false, ErrorMessage = "用户名已存在!")]
        public string? UserName { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        [RegularExpression("^1[3-9]\\d{9}$", ErrorMessage = "手机号格式错误")]
        public string? Phone { get; set; }

        public string CompanyName { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ContractFilePath { get; set; } = string.Empty;
        public string BusinessLicenseFilePath { get; set; } = string.Empty;
    }
}