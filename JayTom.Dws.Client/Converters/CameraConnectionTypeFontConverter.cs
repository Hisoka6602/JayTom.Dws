using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class CameraConnectionTypeFontConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                ConnectionType.Ethernet => "\xe631",
                ConnectionType.SerialPort => "\xe62c",
                ConnectionType.Tcp => "\xe62f",
                ConnectionType.Usb => "\xe7c5",
                ConnectionType.Bluetooth => "\xec4a",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                ConnectionType.Ethernet => "\xe631",
                ConnectionType.SerialPort => "\xe62c",
                ConnectionType.Tcp => "\xe62f",
                ConnectionType.Usb => "\xe7c5",
                ConnectionType.Bluetooth => "\xec4a",
                _ => string.Empty,
            };
        }
    }
}