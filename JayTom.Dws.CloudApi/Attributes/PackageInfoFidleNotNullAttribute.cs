using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using JayTom.Dws.Domain.Dto.CloudApiDto;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.CloudApi.Attributes {

    public class PackageInfoFidleNotNullAttribute : ValidationAttribute {

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is not null) {
                try {
                    var packageDto = JsonConvert.DeserializeObject<PackageDto>(value.ToString() ?? string.Empty);
                    if (packageDto == null) {
                        return new ValidationResult("Json内容错误");
                    }
                    if (packageDto?.BarCodeInfo is null) {
                        return new ValidationResult("BarCodeInfo 字段不能为空");
                    }

                    if (string.IsNullOrEmpty(packageDto?.BarCodeInfo?.Barcode)) {
                        return new ValidationResult("BarCodeInfo.Barcode 内容不能为空");
                    }

                    if (packageDto?.WeightInfo is null) {
                        return new ValidationResult("WeightInfo 字段不能为空");
                    }
                    if (packageDto?.VolumeInfo is null) {
                        return new ValidationResult("VolumeInfo 字段不能为空");
                    }
                }
                catch (Exception e) {
                    return new ValidationResult("Json内容错误");
                }
            }

            return ValidationResult.Success;
        }
    }
}