using JayTom.Dws.Domain.Repository.License;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Attributes {

    public class LicenseCodeExistsAttribute : ValidationAttribute {
        public bool IsExists { get; set; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is not null) {
                var licenseCodeRepository = validationContext.GetService<ILicenseCodeRepository>();
                if (licenseCodeRepository is not null) {
                    var licenseUserInfos = licenseCodeRepository?.MemoryCacheData().ConfigureAwait(false).GetAwaiter().GetResult();
                    var licenseUserInfo = licenseUserInfos?.FirstOrDefault(f => f.LicenseCode.Equals(value.ToString()));

                    return (licenseUserInfo != null != IsExists) ? new ValidationResult(this.ErrorMessage) : ValidationResult.Success;
                }
                else {
                    return new ValidationResult("获取配置参数错误!");
                }
            }

            return ValidationResult.Success;
        }
    }
}