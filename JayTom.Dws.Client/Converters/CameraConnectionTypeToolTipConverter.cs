using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class CameraConnectionTypeToolTipConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                ConnectionType.Ethernet => "网口连接",
                ConnectionType.SerialPort => "串口连接",
                ConnectionType.Tcp => "Tcp连接",
                ConnectionType.Usb => "Usb连接",
                ConnectionType.Bluetooth => "蓝牙连接",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                ConnectionType.Ethernet => "网口连接",
                ConnectionType.SerialPort => "串口连接",
                ConnectionType.Tcp => "Tcp连接",
                ConnectionType.Usb => "Usb连接",
                ConnectionType.Bluetooth => "蓝牙连接",
                _ => string.Empty,
            };
        }
    }
}