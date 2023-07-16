using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class SerialPortTypeFontConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                SerialPortType.Camera => "\xe9f5",
                SerialPortType.Controller => "\xe606",
                SerialPortType.Scale => "\xe6ba",
                SerialPortType.Other => "\xe62c",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                SerialPortType.Camera => "\xe9f5",
                SerialPortType.Controller => "\xe606",
                SerialPortType.Scale => "\xe6ba",
                SerialPortType.Other => "\xe62c",
                _ => string.Empty,
            };
        }
    }
}