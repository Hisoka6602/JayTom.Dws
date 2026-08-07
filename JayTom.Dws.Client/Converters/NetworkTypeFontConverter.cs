using JayTom.Dws.Client.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace JayTom.Dws.Client.Converters
{

    public class NetworkTypeFontConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                NetworkType.Bluetooth => "\xec4a",
                NetworkType.Ethernet => "\xe631",
                NetworkType.Tunnel => "\xe683",
                NetworkType.Wifi => "\xe68a",
                NetworkType.Wman => "\xe65a",
                _ => "\xe680",
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                NetworkType.Bluetooth => "\xec4a",
                NetworkType.Ethernet => "\xe631",
                NetworkType.Tunnel => "\xe683",
                NetworkType.Wifi => "\xe68a",
                NetworkType.Wman => "\xe65a",
                _ => "\xe680",
            };
        }
    }
}