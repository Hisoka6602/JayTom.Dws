using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class CommunicationMethodConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is CommunicationsType communicationsType) {
                return communicationsType switch {
                    CommunicationsType.None => "无",
                    CommunicationsType.SerialPort => "串口通信",
                    CommunicationsType.TCP => "TCP通信",
                    CommunicationsType.USB => "USB通信",
                    CommunicationsType.Ethernet => "Ethernet通信",
                    CommunicationsType.CAN => "CAN总线通信",
                    CommunicationsType.SPI => "SPI通信",
                    CommunicationsType.I2C => "I2C通信",
                    _ => string.Empty
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}