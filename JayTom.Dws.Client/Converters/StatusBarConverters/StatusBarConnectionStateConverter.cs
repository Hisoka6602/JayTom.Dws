using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.StatusBarModels;

namespace JayTom.Dws.Client.Converters.StatusBarConverters {

    public class StatusBarConnectionStateConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is ConnectionState type) {
                if (parameter?.ToString()?.ToLower()?.Equals("color") == true) {
                    return type switch {
                        ConnectionState.Connected => new SolidColorBrush(Colors.LimeGreen),
                        ConnectionState.Disconnected => new SolidColorBrush(Colors.DarkGray),
                        ConnectionState.ConnectionFailed => new SolidColorBrush(Colors.OrangeRed),
                        ConnectionState.Connecting => new SolidColorBrush(Colors.DarkOrange),
                        _ => new SolidColorBrush(Colors.DarkGray),
                    };
                }
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}