using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class CameraListBoxWidthConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is int and <= 4 ? 600 : 900;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is int and <= 4 ? 600 : 900;
        }
    }
}