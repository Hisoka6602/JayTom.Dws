using JayTom.Dws.Domain.Repository.License;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Attributes {

    public class LicenseCodesAllExistsAttribute : ValidationAttribute {
        public bool IsExists { get; set; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is List<string> codes) {
                var licenseCodeRepository = validationContext.GetService<ILicenseCodeRepository>();
                if (licenseCodeRepository is not null) {
                    var licenseUserInfos = licenseCodeRepository?.MemoryCacheData().ConfigureAwait(false).GetAwaiter().GetResult();
                    var list = licenseUserInfos?.Select(item => item.LicenseCode)?.Distinct()?.ToList();
                    var excepts = codes.Except(list ?? new List<string>())?.ToList();
                    if (excepts?.Any() == true) {
                        return new ValidationResult($"{string.Join(",", excepts)}{this.ErrorMessage}");
                    }
                }
                else {
                    return new ValidationResult("获取配置参数错误!");
                }
            }

            return ValidationResult.Success;
        }
    }
}