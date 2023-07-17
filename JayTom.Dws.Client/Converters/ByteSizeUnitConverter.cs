using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class ByteSizeUnitConverter : IValueConverter {
        private readonly string[] _sizes = { "KB", "MB", "GB", "TB" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is long bytes) {
                if (bytes == 0)
                    return _sizes[0];

                var sizeIndex = (int)Math.Floor(Math.Log(bytes, 1024));
                sizeIndex = sizeIndex < 0 ? 0 : sizeIndex;
                return _sizes[sizeIndex];
            }
            else if (value is double dBytes) {
                if (dBytes == 0)
                    return _sizes[0];

                var sizeIndex = (int)Math.Floor(Math.Log(dBytes, 1024));
                sizeIndex = sizeIndex < 0 ? 0 : sizeIndex;
                return _sizes[sizeIndex];
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is long bytes) {
                if (bytes == 0)
                    return _sizes[0];

                var sizeIndex = (int)Math.Floor(Math.Log(bytes, 1024));
                return _sizes[sizeIndex];
            }
            else if (value is double dBytes) {
                if (dBytes == 0)
                    return _sizes[0];

                var sizeIndex = (int)Math.Floor(Math.Log(dBytes, 1024));
                return _sizes[sizeIndex];
            }
            return string.Empty;
        }
    }
}