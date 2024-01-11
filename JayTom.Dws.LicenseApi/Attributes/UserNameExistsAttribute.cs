using JayTom.Dws.Domain.Repository.License;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Attributes {

    public class UserNameExistsAttribute : ValidationAttribute {
        public bool IsExists { get; set; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is not null) {
                var licenseUserRepository = validationContext.GetService<ILicenseUserRepository>();
                if (licenseUserRepository is not null) {
                    var licenseUserInfos = licenseUserRepository?.MemoryCacheData().ConfigureAwait(false).GetAwaiter().GetResult();
                    var licenseUserInfo = licenseUserInfos?.FirstOrDefault(f => f.UserName.Equals(value.ToString()));

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