using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class ByteSizeConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is long bytes) {
                if (bytes == 0)
                    return 0;

                var sizeIndex = (int)Math.Floor(Math.Log(bytes, 1024));
                return bytes / Math.Pow(1024, sizeIndex);
            }
            else if (value is double dBytes) {
                if (dBytes == 0)
                    return 0;

                var sizeIndex = (int)Math.Floor(Math.Log(dBytes, 1024));
                if (sizeIndex > 0) {
                    return dBytes / Math.Pow(1024, sizeIndex);
                }
                else {
                    return dBytes;
                }
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is long bytes) {
                if (bytes == 0)
                    return 0;

                var sizeIndex = (int)Math.Floor(Math.Log(bytes, 1024));
                return bytes / Math.Pow(1024, sizeIndex);
            }
            else if (value is double dBytes) {
                if (dBytes == 0)
                    return 0;

                var sizeIndex = (int)Math.Floor(Math.Log(dBytes, 1024));
                if (sizeIndex > 0) {
                    return dBytes / Math.Pow(1024, sizeIndex);
                }
                else {
                    return dBytes;
                }
            }

            return 0;
        }
    }
}