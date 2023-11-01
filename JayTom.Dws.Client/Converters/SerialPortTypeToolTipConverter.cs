using JayTom.Dws.Client.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace JayTom.Dws.Client.Converters {

    public class SerialPortTypeToolTipConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                SerialPortType.Camera => "相机",
                SerialPortType.Controller => "下位机",
                SerialPortType.Scale => "磅秤",
                SerialPortType.Other => "未知",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                SerialPortType.Camera => "相机",
                SerialPortType.Controller => "下位机",
                SerialPortType.Scale => "磅秤",
                SerialPortType.Other => "未知",
                _ => string.Empty,
            };
        }
    }
}