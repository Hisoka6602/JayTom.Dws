using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Client.Converters
{

    public class CommunicationMethodConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CommunicationsType communicationsType)
            {
                return communicationsType switch
                {
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}