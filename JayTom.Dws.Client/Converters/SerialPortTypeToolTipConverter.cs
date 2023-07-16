using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;

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