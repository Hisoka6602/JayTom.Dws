using JayTom.Dws.LicenseApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class ExtendLicenseCodeValidityDo {

        [LicenseCodeExists(IsExists = true, ErrorMessage = "授权码不存在!")]
        public string LicenseCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "到期时间不能为空!")]
        public DateTime ExpirationDate { get; set; }
    }
}