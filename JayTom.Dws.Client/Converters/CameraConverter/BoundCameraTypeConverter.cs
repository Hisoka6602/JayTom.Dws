using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.Cameras;

namespace JayTom.Dws.Client.Converters.CameraConverter {

    public class BoundCameraTypeConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is BoundCameraType cameraType) {
                if (parameter?.ToString()?.Equals("Text") == true) {
                    return cameraType switch {
                        BoundCameraType.PanoramicCamera => "全景相机",
                        BoundCameraType.BarcodeScannerCamera => "扫码相机",
                        BoundCameraType.VolumeCamera => "体积相机",
                        _ => string.Empty
                    };
                }
                else if (parameter?.ToString()?.Equals("Color") == true) {
                    return cameraType switch {
                        BoundCameraType.PanoramicCamera => new SolidColorBrush(Colors.BlueViolet),
                        BoundCameraType.BarcodeScannerCamera => new SolidColorBrush(Colors.RoyalBlue),
                        BoundCameraType.VolumeCamera => new SolidColorBrush(Colors.DodgerBlue),
                        _ => string.Empty
                    };
                }
                else if (parameter?.ToString()?.Equals("FontText") == true) {
                    return cameraType switch {
                        BoundCameraType.PanoramicCamera => "\xe605",
                        BoundCameraType.BarcodeScannerCamera => "\xe9f5",
                        BoundCameraType.VolumeCamera => "\xea1a",
                        _ => string.Empty
                    };
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is BoundCameraType cameraType) {
                if (parameter?.ToString()?.Equals("Text") == true) {
                    return cameraType switch {
                        BoundCameraType.PanoramicCamera => "全景相机",
                        BoundCameraType.BarcodeScannerCamera => "扫码相机",
                        BoundCameraType.VolumeCamera => "体积相机",
                        _ => string.Empty
                    };
                }
                else if (parameter?.ToString()?.Equals("Color") == true) {
                    return cameraType switch {
                        BoundCameraType.PanoramicCamera => new SolidColorBrush(Colors.BlueViolet),
                        BoundCameraType.BarcodeScannerCamera => new SolidColorBrush(Colors.RoyalBlue),
                        BoundCameraType.VolumeCamera => new SolidColorBrush(Colors.DodgerBlue),
                        _ => string.Empty
                    };
                }
                else if (parameter?.ToString()?.Equals("FontText") == true) {
                    return cameraType switch {
                        BoundCameraType.PanoramicCamera => "\xe605",
                        BoundCameraType.BarcodeScannerCamera => "\xe9f5",
                        BoundCameraType.VolumeCamera => "\xea1a",
                        _ => string.Empty
                    };
                }
                else if (parameter?.ToString()?.Equals("Visibility") == true) {
                    return cameraType switch {
                        BoundCameraType.PanoramicCamera => "\xe605",
                        BoundCameraType.BarcodeScannerCamera => "\xe9f5",
                        BoundCameraType.VolumeCamera => "\xea1a",
                        _ => string.Empty
                    };
                }
            }
            return string.Empty;
        }
    }
}