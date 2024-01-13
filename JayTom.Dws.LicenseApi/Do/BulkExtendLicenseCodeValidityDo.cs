using JayTom.Dws.LicenseApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class BulkExtendLicenseCodeValidityDo {

        [Required(ErrorMessage = "授权码不能为空"),
         LicenseCodesAllExists(IsExists = true, ErrorMessage = "不存在!")]
        public List<string>? LicenseCodes { get; set; }

        [Required(ErrorMessage = "到期时间不能为空!")]
        public DateTime ExpirationDate { get; set; }
    }
}