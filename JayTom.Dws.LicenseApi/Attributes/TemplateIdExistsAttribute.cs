using JayTom.Dws.Domain.Repository.License;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Attributes {

    public class TemplateIdExistsAttribute : ValidationAttribute {
        public bool IsExists { get; set; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is long id) {
                var licensePermissionTemplateRepository = validationContext.GetService<ILicensePermissionTemplateRepository>();
                if (licensePermissionTemplateRepository is not null) {
                    var licensePermissionTemplateInfos = licensePermissionTemplateRepository?.MemoryCacheData().ConfigureAwait(false).GetAwaiter().GetResult();
                    var licensePermissionTemplateInfo = licensePermissionTemplateInfos?.FirstOrDefault(f => f.Id.Equals(id));

                    return (licensePermissionTemplateInfo != null != IsExists) ? new ValidationResult(this.ErrorMessage) : ValidationResult.Success;
                }
                else {
                    return new ValidationResult("获取配置参数错误!");
                }
            }

            return ValidationResult.Success;
        }
    }
}