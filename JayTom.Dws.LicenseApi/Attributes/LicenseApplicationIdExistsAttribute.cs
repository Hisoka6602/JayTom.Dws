using JayTom.Dws.Domain.Repository.License;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Attributes {

    public class LicenseApplicationIdExistsAttribute : ValidationAttribute {
        public bool IsExists { get; set; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is long id) {
                var licenseApplicationRepository = validationContext.GetService<ILicenseApplicationRepository>();
                if (licenseApplicationRepository is not null) {
                    var licenseApplicationInfos = licenseApplicationRepository?.MemoryCacheData().ConfigureAwait(false).GetAwaiter().GetResult();

                    var licenseApplicationInfo = licenseApplicationInfos?.FirstOrDefault(f => f.LicensePermissionTemplateId == null && f.Id.Equals(id));

                    return (licenseApplicationInfo != null != IsExists) ? new ValidationResult(this.ErrorMessage) : ValidationResult.Success;
                }
                else {
                    return new ValidationResult("获取配置参数错误!");
                }
            }

            return ValidationResult.Success;
        }
    }
}