using JayTom.Dws.LicenseApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class UpdateTenantLicenseMaxCountDo {

        [Required(ErrorMessage = "用户代码不能为空"), UserCodeExists(IsExists = true, ErrorMessage = "用户代码不存在")]
        public string UserCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "模板Id不能为空"), TemplateIdExists(IsExists = true, ErrorMessage = "模板Id不存在")]
        public long LicensePermissionTemplateInfoId { get; set; }

        [Required(ErrorMessage = "授权码上限不能为空"), Range(1, int.MaxValue, ErrorMessage = "授权码上限超出允许范围")]
        public int MaxLicenseCodeCount { get; set; }
    }
}