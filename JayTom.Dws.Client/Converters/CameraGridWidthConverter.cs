using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class CameraGridWidthConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var itemCount = (int)value;

            //return itemCount <= 4 ? new Size(312, 360) : new Size(1, 1);
            if (itemCount <= 1) {
                return 500;
            }
            return itemCount <= 4 ? 312 : 260;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            var itemCount = (int)value;
            if (itemCount <= 1) {
                return 500;
            }
            return itemCount <= 4 ? 312 : 260;
        }
    }
}