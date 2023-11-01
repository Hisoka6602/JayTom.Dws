using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JayTom.Dws.Client.Converters {

    public class NullStatusFlagColorConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is int and <= 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is int and <= 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
        }
    }
}