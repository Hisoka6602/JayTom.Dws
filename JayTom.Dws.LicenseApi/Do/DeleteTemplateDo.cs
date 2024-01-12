using JayTom.Dws.LicenseApi.Attributes;

namespace JayTom.Dws.LicenseApi.Do {

    public class DeleteTemplateDo {

        [TemplateIdExists(IsExists = true, ErrorMessage = "模板Id不存在!")]
        public long DeleteTemplateId { get; set; }
    }
}