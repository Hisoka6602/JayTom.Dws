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
                ConnectionType.Ethernet => Languages.Language.ResourceManager.GetString("EthernetConnection") ?? string.Empty,
                ConnectionType.SerialPort => Languages.Language.ResourceManager.GetString("SerialPortConnection") ?? string.Empty,
                ConnectionType.Tcp => Languages.Language.ResourceManager.GetString("TCPConnection") ?? string.Empty,
                ConnectionType.Usb => Languages.Language.ResourceManager.GetString("USBConnection") ?? string.Empty,
                ConnectionType.Bluetooth => Languages.Language.ResourceManager.GetString("BluetoothConnection") ?? string.Empty,
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                ConnectionType.Ethernet => Languages.Language.ResourceManager.GetString("EthernetConnection") ?? string.Empty,
                ConnectionType.SerialPort => Languages.Language.ResourceManager.GetString("SerialPortConnection") ?? string.Empty,
                ConnectionType.Tcp => Languages.Language.ResourceManager.GetString("TCPConnection") ?? string.Empty,
                ConnectionType.Usb => Languages.Language.ResourceManager.GetString("USBConnection") ?? string.Empty,
                ConnectionType.Bluetooth => Languages.Language.ResourceManager.GetString("BluetoothConnection") ?? string.Empty,
                _ => string.Empty,
            };
        }
    }
}