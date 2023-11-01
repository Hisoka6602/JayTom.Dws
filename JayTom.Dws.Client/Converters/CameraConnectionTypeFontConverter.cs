using JayTom.Dws.Client.Models;
using System;
using System.Globalization;
using System.Windows.Data;

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