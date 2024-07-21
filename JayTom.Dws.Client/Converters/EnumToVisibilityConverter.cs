using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class EnumToVisibilityConverter : IValueConverter {

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value == null || parameter == null) {
                return Visibility.Collapsed;
            }

            var enumValue = value.ToString() ?? string.Empty;
            var targetValue = parameter.ToString();

            return enumValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}