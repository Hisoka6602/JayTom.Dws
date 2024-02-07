using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class RegisterAccountForm {
        /// <summary>
        /// 用户代码
        /// </summary>

        [Required(ErrorMessage = "用户代码不能为空!"),
         MaxLength(15, ErrorMessage = "代码长度不能超过15个字符"),
         RegularExpression("^[a-zA-Z][a-zA-Z0-9]*$", ErrorMessage = "只允许字母和数字,并且开头必须为字母")]
        public string UserCode { get; set; } = string.Empty;

        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空!"),
         MaxLength(15, ErrorMessage = "名称长度不能超过15个字符")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空!"),
         MaxLength(20, ErrorMessage = "密码长度不能超过20个字符"),
         MinLength(8, ErrorMessage = "密码长度不能小于8个字符"),
         RegularExpression("^[^\\u4e00-\\u9fa5]*$", ErrorMessage = "密码不能包含中文")]
        public string PassWord { get; set; } = string.Empty;

        /// <summary>
        /// 手机号
        /// </summary>
        [Required(ErrorMessage = "手机号不能为空!"),
         RegularExpression("^1[3-9]\\d{9}$", ErrorMessage = "手机号格式错误")]
        public string Phone { get; set; } = string.Empty;
    }
}