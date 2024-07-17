using System;
using System.Windows;
using JayTom.Dws.Camera;
using System.Windows.Data;
using System.Globalization;
using JayTom.Dws.Client.Models;

namespace JayTom.Dws.Client.Converters.CameraConverter {

    public class BoundCameraTypeCornerRadiusConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is CameraType cameraType) {
                if (parameter?.ToString()?.Equals("PanoramaCamera") == true) {
                    return cameraType switch {
                        CameraType.IndustrialCamera => new CornerRadius(5, 0, 0, 5),
                        CameraType.SmartCamera => new CornerRadius(5, 0, 0, 5),
                        CameraType.VideoCamera => new CornerRadius(5),
                        _ => new CornerRadius(5)
                    };
                }
                else if (parameter?.ToString()?.Equals("BarcodeScannerCamera") == true) {
                    return cameraType switch {
                        CameraType.IndustrialCamera => new CornerRadius(0, 5, 5, 0),
                        CameraType.SmartCamera => new CornerRadius(0, 5, 5, 0),
                        _ => new CornerRadius(5)
                    };
                }
                else if (parameter?.ToString()?.Equals("VolumeCamera") == true) {
                    return cameraType switch {
                        CameraType.ThreeDCamera => new CornerRadius(5),
                        _ => new CornerRadius(5)
                    };
                }
            }
            return new CornerRadius(0, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is CameraType cameraType) {
                if (parameter?.ToString()?.Equals("PanoramaCamera") == true) {
                    return new CornerRadius(5, 0, 0, 5);
                }
                else if (parameter?.ToString()?.Equals("BarcodeScannerCamera") == true) {
                    return new CornerRadius(0, 5, 5, 0);
                }
                else if (parameter?.ToString()?.Equals("VolumeCamera") == true) {
                    return new CornerRadius(5, 5, 5, 5);
                }
            }
            return new CornerRadius(0, 0, 0, 0);
        }
    }
}