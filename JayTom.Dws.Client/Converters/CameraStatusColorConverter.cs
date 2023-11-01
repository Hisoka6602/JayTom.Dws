using JayTom.Dws.Client.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JayTom.Dws.Client.Converters {

    public class CameraStatusColorConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                CameraStatus.Running => new SolidColorBrush(Colors.LimeGreen),
                CameraStatus.Disconnected => new SolidColorBrush(Colors.DarkGray),
                CameraStatus.Failure => new SolidColorBrush(Colors.OrangeRed),
                CameraStatus.Paused => new SolidColorBrush(Colors.DarkOrange),
                _ => new SolidColorBrush(Colors.DarkGray),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                CameraStatus.Running => new SolidColorBrush(Colors.LimeGreen),
                CameraStatus.Disconnected => new SolidColorBrush(Colors.DarkGray),
                CameraStatus.Failure => new SolidColorBrush(Colors.OrangeRed),
                CameraStatus.Paused => new SolidColorBrush(Colors.DarkOrange),
                _ => new SolidColorBrush(Colors.DarkGray),
            };
        }
    }
}