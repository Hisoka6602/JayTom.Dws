using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Attributes {

    public class DebugMachineCodeAttribute : ValidationAttribute {

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is null) return ValidationResult.Success;
            var service = validationContext.GetService<IConfiguration>();
            var debugMachineCodes = service?.GetSection("DebugMachineCodes")
                ?.GetChildren()?.Select(s => s.Value)?.ToList();
            if (debugMachineCodes?.Any(a => a != null && a.Equals(value.ToString())) != true) {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}