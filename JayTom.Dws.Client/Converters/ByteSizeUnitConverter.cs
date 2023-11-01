using System;
using System.Globalization;
using System.Windows.Data;

namespace JayTom.Dws.Client.Converters {

    public class ByteSizeUnitConverter : IValueConverter {
        private readonly string[] _sizes = { "KB", "MB", "GB", "TB" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var conversionRate = parameter is "1000" ? 1000 : 1024;
            if (value is long bytes) {
                if (bytes == 0)
                    return _sizes[0];

                var sizeIndex = (int)Math.Floor(Math.Log(bytes, conversionRate));
                sizeIndex = sizeIndex < 0 ? 0 : sizeIndex;
                return _sizes[sizeIndex];
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            var conversionRate = parameter is "1000" ? 1000 : 1024;
            if (value is long bytes) {
                if (bytes == 0)
                    return _sizes[0];

                var sizeIndex = (int)Math.Floor(Math.Log(bytes, conversionRate));
                return _sizes[sizeIndex];
            }
            return string.Empty;
        }
    }
}