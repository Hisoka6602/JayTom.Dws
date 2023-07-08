using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using JayTom.Dws.PluginInterface;

namespace JayTom.Dws.Client.Converters {

    public class CameraTypeFontConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                CameraType.IndustrialCamera => "\xe9f5",
                CameraType.PanoramicCamera => "\xe605",
                CameraType.SmartCamera => "\xe6ef",
                CameraType.ThreeDCamera => "\xea1a",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                CameraType.IndustrialCamera => "\xe9f5",
                CameraType.PanoramicCamera => "\xe605",
                CameraType.SmartCamera => "\xe6ef",
                CameraType.ThreeDCamera => "\xea1a",
                _ => string.Empty,
            };
        }
    }
}