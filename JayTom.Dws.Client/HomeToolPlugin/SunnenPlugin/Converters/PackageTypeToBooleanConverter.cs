using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.ViewModels;

namespace JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.Converters {

    public class PackageTypeToBooleanConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is not null) {
                if (Enum.TryParse(value.ToString(), out PackageType selectedType)) {
                    if (Enum.TryParse(parameter.ToString(), out PackageType result)) {
                        return selectedType == result;
                    }
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (Enum.TryParse(parameter.ToString(), out PackageType result)) {
                if (value is true) {
                    return result;
                }
            }
            return Binding.DoNothing;
        }
    }
}