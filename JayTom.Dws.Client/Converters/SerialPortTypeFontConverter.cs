using JayTom.Dws.Client.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace JayTom.Dws.Client.Converters
{

    public class SerialPortTypeFontConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                SerialPortType.Camera => "\xe9f5",
                SerialPortType.Controller => "\xe606",
                SerialPortType.Scale => "\xe6ba",
                SerialPortType.Other => "\xe62c",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                SerialPortType.Camera => "\xe9f5",
                SerialPortType.Controller => "\xe606",
                SerialPortType.Scale => "\xe6ba",
                SerialPortType.Other => "\xe62c",
                _ => string.Empty,
            };
        }
    }
}