using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace JayTom.Dws.Client.Converters
{
    public class ListBackgroundVisibleConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int and > 0 ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int and > 0 ? Visibility.Hidden : Visibility.Visible;
        }
    }
}