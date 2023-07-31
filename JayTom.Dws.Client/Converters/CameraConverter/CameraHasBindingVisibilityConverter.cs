using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters.CameraConverter {

    public class CameraHasBindingVisibilityConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                bool hasBinding when parameter?.ToString()?.Equals("BindButton") == true => hasBinding
                    ? Visibility.Collapsed
                    : Visibility.Visible,
                bool hasBinding when parameter?.ToString()?.Equals("UnbindButton") == true => hasBinding
                    ? Visibility.Visible
                    : Visibility.Collapsed,
                _ => Visibility.Collapsed
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                bool hasBinding when parameter?.ToString()?.Equals("BindButton") == true => hasBinding
                    ? Visibility.Collapsed
                    : Visibility.Visible,
                bool hasBinding when parameter?.ToString()?.Equals("UnbindButton") == true => hasBinding
                    ? Visibility.Visible
                    : Visibility.Collapsed,
                _ => Visibility.Collapsed
            };
        }
    }
}