using JayTom.Dws.LicenseApi.Do;
using NPOI.SS.Formula.Functions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Attributes {

    public class TimeSpanRangeAttribute : ValidationAttribute {

        /// <summary>
        /// 类型
        /// </summary>
        public TimeType TimeType { get; set; }

        /// <summary>
        /// 最大时间跨度
        /// </summary>
        public int MaxDuration { get; set; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is DateTime date) {
                //后续修改获取TimeType验证，而不是直接获取类型
                if (validationContext.ObjectInstance is LicenseAuthorizationLogDo model) {
                    if (model.StartTime is null ||
                        model.EndTime is null) {
                        return new ValidationResult(ErrorMessage);
                    }

                    if (model.EndTime.Value.Subtract(model.StartTime.Value).TotalDays > MaxDuration) {
                        return new ValidationResult(ErrorMessage);
                    }
                }
            }
            return ValidationResult.Success;
        }
    }

    public enum TimeType {
        Min, Max
    }
}