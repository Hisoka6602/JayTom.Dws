using JayTom.Dws.Client.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace JayTom.Dws.Client.Converters {

    public class CameraStatusReadyFontConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                CameraStatus.Running => "\xe693",
                CameraStatus.Disconnected => "\xe612",
                CameraStatus.Failure => "\xe612",
                CameraStatus.Paused => "\xea82",
                _ => "\xe612",
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                CameraStatus.Running => "\xe693",
                CameraStatus.Disconnected => "\xe612",
                CameraStatus.Failure => "\xe612",
                CameraStatus.Paused => "\xea82",
                _ => "\xe612",
            };
        }
    }
}