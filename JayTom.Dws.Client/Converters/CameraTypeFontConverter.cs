using JayTom.Dws.Client.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace JayTom.Dws.Client.Converters {

    public class CameraTypeFontConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                CameraType.IndustrialCamera => "\xe9f5",
                CameraType.PanoramaCamera => "\xe605",
                CameraType.SmartCamera => "\xe6ef",
                CameraType.ThreeDCamera => "\xea1a",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                CameraType.IndustrialCamera => "\xe9f5",
                CameraType.PanoramaCamera => "\xe605",
                CameraType.SmartCamera => "\xe6ef",
                CameraType.ThreeDCamera => "\xea1a",
                _ => string.Empty,
            };
        }
    }
}