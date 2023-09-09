using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Domain.Dto.BaseInfoModels;

namespace JayTom.Dws.Client.Converters.ApiConverter {

    public class ResponseValidationModeToBooleanConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is not null) {
                if (Enum.TryParse(value.ToString(), out ResponseValidationMode selectedType)) {
                    if (Enum.TryParse(parameter.ToString(), out ResponseValidationMode result)) {
                        return selectedType == result;
                    }
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (Enum.TryParse(parameter.ToString(), out ResponseValidationMode result)) {
                if (value is true) {
                    return result;
                }
            }
            return Binding.DoNothing;
        }
    }
}