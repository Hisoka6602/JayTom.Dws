using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class SerialPortStatusColorConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                SerialPortStatus.Running => new SolidColorBrush(Colors.LimeGreen),
                SerialPortStatus.NotConnected => new SolidColorBrush(Colors.OrangeRed),
                _ => new SolidColorBrush(Colors.DarkGray),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                SerialPortStatus.Running => new SolidColorBrush(Colors.LimeGreen),
                SerialPortStatus.NotConnected => new SolidColorBrush(Colors.OrangeRed),
                _ => new SolidColorBrush(Colors.DarkGray),
            };
        }
    }
}