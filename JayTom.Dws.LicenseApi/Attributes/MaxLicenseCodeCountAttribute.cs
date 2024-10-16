using JayTom.Dws.Data.License;
using JayTom.Dws.LicenseApi.Do;
using JayTom.Dws.Domain.Repository.License;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Attributes {

    public class MaxLicenseCodeCountAttribute : ValidationAttribute {

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is int maxLicenseCodeCount) {
                var code = string.Empty;
                //获取租户信息
                if (validationContext.ObjectInstance is UpdateTenantLicenseMaxCountDo model) {
                    code = model.UserCode;
                }

                //获取已创建数量
                var licenseUserRepository = validationContext.GetService<ILicenseUserRepository>();
                if (licenseUserRepository is not null) {
                    var (key, o) = licenseUserRepository.DetailsInfo(code ?? string.Empty, CancellationToken.None).
                        ConfigureAwait(false).GetAwaiter().GetResult();
                    if (key && o is LicenseUserInfo licenseUserInfo) {
                        var sum = licenseUserInfo.LicenseCodeInfos
                            ?.Sum(s => s.MaxClientCount) ?? 0;

                        if (sum > maxLicenseCodeCount) {
                            return new ValidationResult(ErrorMessage);
                        }
                    }
                }
            }

            return ValidationResult.Success;
        }
    }
}