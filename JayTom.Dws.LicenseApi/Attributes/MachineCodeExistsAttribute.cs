using JayTom.Dws.Domain.Repository.License;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Attributes {

    public class MachineCodeExistsAttribute : ValidationAttribute {
        public bool IsExists { get; set; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is not null) {
                var licenseClientBindingRepository = validationContext.GetService<ILicenseClientBindingRepository>();
                if (licenseClientBindingRepository is not null) {
                    var licenseClientBindingInfos = licenseClientBindingRepository.Select(s => s.Id > 0, o => o.Id).ConfigureAwait(false).GetAwaiter().GetResult();
                    var licenseClientBindingInfo = licenseClientBindingInfos?.FirstOrDefault(f => f.MachineCode.Equals(value.ToString()));

                    return (licenseClientBindingInfo != null != IsExists) ? new ValidationResult(this.ErrorMessage) : ValidationResult.Success;
                }
                else {
                    return new ValidationResult("获取配置参数错误!");
                }
            }

            return ValidationResult.Success;
        }
    }
}