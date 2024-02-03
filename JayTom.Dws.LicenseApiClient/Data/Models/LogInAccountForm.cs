using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class LogInAccountForm {

        [Required(ErrorMessage = "账号不能为空")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "密码不能为空")]
        public string Password { get; set; } = string.Empty;
    }
}