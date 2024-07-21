using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class EnumToVisibilityInvertedConverter : IValueConverter {

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value == null || parameter == null) {
                return Visibility.Collapsed;
            }

            var enumValue = value.ToString() ?? string.Empty;
            var targetValue = parameter.ToString();

            // Check if the value matches the parameter
            var isVisible = enumValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);

            // Return inverted visibility
            return isVisible ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}