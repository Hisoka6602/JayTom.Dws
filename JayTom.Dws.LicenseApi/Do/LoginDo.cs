using JayTom.Dws.LicenseApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class LoginDo {

        [Required(ErrorMessage = "用户名或手机号不能为空!"),
         LoginCodeExists(IsExists = true, ErrorMessage = "不存在该用户名或手机号!")]
        public string LoginCode { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空!"),
         MaxLength(20, ErrorMessage = "密码长度不能超过20个字符"),
         MinLength(8, ErrorMessage = "密码长度不能小于8个字符"),
         RegularExpression("^[^\\u4e00-\\u9fa5]*$", ErrorMessage = "密码不能包含中文")]
        public string PassWord { get; set; } = string.Empty;
    }
}