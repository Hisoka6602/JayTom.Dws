using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JayTom.Dws.Client.Converters
{

    public class MemoryRemainingColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float rate)
            {
                if (rate is <= 10 and > 1)
                {
                    return new SolidColorBrush(Colors.DarkOrange);
                }
                else if (rate <= 1)
                {
                    return new SolidColorBrush(Colors.Red);
                }

                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float rate)
            {
                if (rate is <= 10 and > 1)
                {
                    return new SolidColorBrush(Colors.DarkOrange);
                }
                else if (rate <= 1)
                {
                    return new SolidColorBrush(Colors.Red);
                }

                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
        }
    }
}