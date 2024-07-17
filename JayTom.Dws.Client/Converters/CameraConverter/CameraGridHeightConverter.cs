using System;
using System.Windows.Data;
using System.Globalization;

namespace JayTom.Dws.Client.Converters.CameraConverter {

    public class CameraGridHeightConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var itemCount = (int)value;
            if (itemCount <= 1) {
                return 576;
            }
            return itemCount <= 4 ? 360 : 300;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            var itemCount = (int)value;
            if (itemCount <= 1) {
                return 576;
            }
            return itemCount <= 4 ? 360 : 300;
        }
    }
}