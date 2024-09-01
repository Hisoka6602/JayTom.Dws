using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.VideoApi.Attributes {

    public class JsonValidationAttribute : ValidationAttribute {

        protected override ValidationResult IsValid(object value, ValidationContext validationContext) {
            if (value is string jsonString) {
                try {
                    JsonDocument.Parse(jsonString); // 尝试解析 JSON
                    return ValidationResult.Success;
                }
                catch (JsonException) {
                    return new ValidationResult(ErrorMessage);
                }
            }
            return new ValidationResult("配置内容不能为空");
        }
    }
}