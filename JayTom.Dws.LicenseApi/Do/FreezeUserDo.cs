using JayTom.Dws.LicenseApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class FreezeUserDo {

        [Required(ErrorMessage = "被操作的用户代码不能为空!"),
         UserCodeExists(IsExists = true, ErrorMessage = "该账号未注册!"),
         MaxLength(15, ErrorMessage = "代码长度不能超过15个字符"),
         RegularExpression("^[a-zA-Z][a-zA-Z0-9]*$", ErrorMessage = "只允许字母和数字,并且开头必须为字母")]
        public string UserCode { get; set; } = string.Empty;

        /// <summary>
        /// 是否冻结
        /// </summary>
        public bool IsFreeze { get; set; }
    }
}