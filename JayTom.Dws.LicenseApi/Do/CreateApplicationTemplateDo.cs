using JayTom.Dws.LicenseApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class CreateApplicationTemplateDo {

        [LicenseApplicationIdExists(IsExists = true, ErrorMessage = "应用程序不存在!")]
        public long LicenseApplicationInfoId { get; set; }

        [Required(ErrorMessage = "应用程序名称不能为空!"),
         MaxLength(30, ErrorMessage = "应用程序名称不能超过30个字符"),
        AppTemplateExistsName(IsExists = false, ErrorMessage = "该模板名称已存在")]
        public string TemplateName { get; set; } = string.Empty;
    }
}