using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using JayTom.Dws.Client.Models;

namespace JayTom.Dws.Client.Converters.CameraConverter {
    public class BoundCameraTypeLocationConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is CameraType cameraType) {
                if (parameter?.ToString()?.Equals("PanoramicCamera") == true) {
                    return cameraType switch {
                        CameraType.VideoCamera => 3,
                        _ => 0
                    };
                }
                else if (parameter?.ToString()?.Equals("BarcodeScannerCamera") == true) {
                    return cameraType switch {
                        CameraType.IndustrialCamera => 2,
                        CameraType.SmartCamera => 2,
                        _ => Visibility.Collapsed
                    };
                }
                else if (parameter?.ToString()?.Equals("VolumeCamera") == true) {
                    return cameraType switch {
                        CameraType.ThreeDCamera => 3,
                        _ => 2
                    };
                }

                return 0;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}