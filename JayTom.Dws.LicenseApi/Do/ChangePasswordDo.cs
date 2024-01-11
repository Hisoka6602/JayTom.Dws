using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class ChangePasswordDo {

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空!"),
         MaxLength(20, ErrorMessage = "密码长度不能超过20个字符"),
         MinLength(8, ErrorMessage = "密码长度不能小于8个字符"),
         RegularExpression("^[^\\u4e00-\\u9fa5]*$", ErrorMessage = "密码不能包含中文")]
        public string OldPassWord { get; set; } = string.Empty;

        [Required(ErrorMessage = "密码不能为空!"),
         MaxLength(20, ErrorMessage = "密码长度不能超过20个字符"),
         MinLength(8, ErrorMessage = "密码长度不能小于8个字符"),
         RegularExpression("^[^\\u4e00-\\u9fa5]*$", ErrorMessage = "密码不能包含中文")]
        public string NewPassWord { get; set; } = string.Empty;
    }
}