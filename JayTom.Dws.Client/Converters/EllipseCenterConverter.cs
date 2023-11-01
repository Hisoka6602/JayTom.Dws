using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace JayTom.Dws.Client.Converters {

    public class EllipseCenterConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            var width = (double)values[0];
            var height = (double)values[1];
            if (parameter?.ToString()?.ToLower()?.Equals("left") == true) {
                return new Point(0, height - 50);
            }
            else if (parameter?.ToString()?.ToLower()?.Equals("right") == true) {
                return new Point(width, height - 50);
            }
            return new Point(0, height - 50);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}