using JayTom.Dws.Client.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

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