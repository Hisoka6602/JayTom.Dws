using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class CameraGridHeightConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var itemCount = (int)value;

            return itemCount <= 4 ? 360 : 300;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            var itemCount = (int)value;

            return itemCount <= 4 ? 360 : 300;
        }
    }
}