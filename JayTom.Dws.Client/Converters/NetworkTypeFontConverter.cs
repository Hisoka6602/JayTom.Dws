using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class NetworkTypeFontConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                NetworkType.Bluetooth => "\xec4a",
                NetworkType.Ethernet => "\xe631",
                NetworkType.Tunnel => "\xe683",
                NetworkType.Wifi => "\xe68a",
                NetworkType.Wman => "\xe65a",
                _ => "\xe680",
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
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