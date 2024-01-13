using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Attributes {

    public class ImageAttribute : ValidationAttribute {

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is null) return ValidationResult.Success;
            var service = validationContext.GetService<IConfiguration>();
            var imageFileTypeList = service?.GetSection("ImageFileType")
                ?.GetChildren()?.Select(s => s.Value)?.ToList();
            if (imageFileTypeList?.Any() == true) {
                if (value is IFormFile formFile) {
                    if (imageFileTypeList?.Select(s => s.ToUpper())?.Any(a => new FileInfo(formFile.FileName).Extension
                            .Replace(".", string.Empty).ToUpper().Equals(a)) != true) {
                        return new ValidationResult($"图片限定格式为：{string.Join("、", imageFileTypeList ?? new List<string?>())}");
                    }
                }
            }

            return ValidationResult.Success;
        }
    }
}