using Newtonsoft.Json;
using System.Linq.Expressions;
using JayTom.Dws.Domain.Repository.License;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.JavaScript;

namespace JayTom.Dws.CloudApi.Attributes {

    public class JsonValidationAttribute<T> : ValidationAttribute {

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is not null) {
                try {
                    var deserializedValue = JsonConvert.DeserializeObject<T>(value.ToString() ?? string.Empty);
                    return deserializedValue != null ? ValidationResult.Success : new ValidationResult(ErrorMessage);
                }
                catch (Exception e) {
                    return new ValidationResult(ErrorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }
}