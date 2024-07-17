using System;
using System.Windows.Data;
using System.Globalization;

namespace JayTom.Dws.Client.Converters.CameraConverter {

    public class CameraListBoxWidthConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is int and <= 4 ? 600 : 900;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is int and <= 4 ? 600 : 900;
        }
    }
}