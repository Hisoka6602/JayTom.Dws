using JayTom.Dws.Client.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace JayTom.Dws.Client.Converters.CameraConverter {

    public class BoundCameraTypeVisibilityConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is CameraType cameraType) {
                if (parameter?.ToString()?.Equals("PanoramicCamera") == true) {
                    return cameraType switch {
                        CameraType.PanoramicCamera => Visibility.Visible,
                        CameraType.IndustrialCamera => Visibility.Visible,
                        CameraType.SmartCamera => Visibility.Visible,
                        CameraType.VideoCamera => Visibility.Visible,
                        _ => Visibility.Collapsed
                    };
                }
                else if (parameter?.ToString()?.Equals("BarcodeScannerCamera") == true) {
                    return cameraType switch {
                        CameraType.IndustrialCamera => Visibility.Visible,
                        CameraType.SmartCamera => Visibility.Visible,
                        _ => Visibility.Collapsed
                    };
                }
                else if (parameter?.ToString()?.Equals("VolumeCamera") == true) {
                    return cameraType switch {
                        CameraType.ThreeDCamera => Visibility.Visible,
                        _ => Visibility.Collapsed
                    };
                }

                return Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is CameraType cameraType) {
                if (parameter?.ToString()?.Equals("PanoramicCamera") == true) {
                    return cameraType switch {
                        CameraType.PanoramicCamera => Visibility.Visible,
                        CameraType.IndustrialCamera => Visibility.Visible,
                        CameraType.SmartCamera => Visibility.Visible,
                        _ => Visibility.Collapsed
                    };
                }
                else if (parameter?.ToString()?.Equals("BarcodeScannerCamera") == true) {
                    return cameraType switch {
                        CameraType.IndustrialCamera => Visibility.Visible,
                        CameraType.SmartCamera => Visibility.Visible,
                        _ => Visibility.Collapsed
                    };
                }
                else if (parameter?.ToString()?.Equals("VolumeCamera") == true) {
                    return cameraType switch {
                        CameraType.ThreeDCamera => Visibility.Visible,
                        _ => Visibility.Collapsed
                    };
                }

                return Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }
    }
}