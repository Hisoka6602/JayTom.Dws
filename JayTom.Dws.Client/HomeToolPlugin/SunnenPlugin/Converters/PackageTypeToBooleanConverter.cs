using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

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