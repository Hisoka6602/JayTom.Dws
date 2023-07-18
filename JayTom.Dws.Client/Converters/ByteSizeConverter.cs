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
                var conversionRate = parameter is "1000" ? 1000 : 1024;
                if (bytes == 0)
                    return 0;

                var sizeIndex = (int)Math.Floor(Math.Log(bytes, conversionRate));
                return bytes / Math.Pow(conversionRate, sizeIndex);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is long bytes) {
                var conversionRate = parameter is "1000" ? 1000 : 1024;
                if (bytes == 0)
                    return 0;

                var sizeIndex = (int)Math.Floor(Math.Log(bytes, conversionRate));
                return bytes / Math.Pow(conversionRate, sizeIndex);
            }
            return 0;
        }
    }
}