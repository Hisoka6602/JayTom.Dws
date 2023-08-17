using System;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Client.ValidationRules {

    public class RegexValidationRule : ValidationRule {
        public string Pattern { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            var inputValue = value as string;
            try {
                if (!string.IsNullOrEmpty(inputValue) && !Regex.IsMatch(inputValue, Pattern)) {
                    return new ValidationResult(false, ErrorMessage);
                }

                return ValidationResult.ValidResult;
            }
            catch (Exception e) {
                return new ValidationResult(false, e.Message);
            }
        }
    }
}