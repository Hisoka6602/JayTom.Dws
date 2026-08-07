using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Client.Converters.PackageSortingConfiguration
{

    public class CommunicationsTypeFontConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                CommunicationsType.Ethernet => "\xe631",
                CommunicationsType.SerialPort => "\xe62c",
                CommunicationsType.TCP => "\xe62f",
                CommunicationsType.USB => "\xe7c5",
                CommunicationsType.CAN => "\xe862",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                CommunicationsType.Ethernet => "\xe631",
                CommunicationsType.SerialPort => "\xe62c",
                CommunicationsType.TCP => "\xe62f",
                CommunicationsType.USB => "\xe7c5",
                CommunicationsType.CAN => "\xe862",
                _ => string.Empty,
            };
        }
    }
}