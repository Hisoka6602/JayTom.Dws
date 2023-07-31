using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters.CameraConverter {

    public class BoundCameraTypeCornerRadiusConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is CameraType cameraType) {
                if (parameter?.ToString()?.Equals("PanoramicCamera") == true) {
                    return new CornerRadius(5, 0, 0, 5);
                }
                else if (parameter?.ToString()?.Equals("BarcodeScannerCamera") == true) {
                    return new CornerRadius(0, 20, 20, 0);
                }
                else if (parameter?.ToString()?.Equals("VolumeCamera") == true) {
                    return new CornerRadius(5, 20, 20, 5);
                }
            }
            return new CornerRadius(0, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is CameraType cameraType) {
                if (parameter?.ToString()?.Equals("PanoramicCamera") == true) {
                    return new CornerRadius(5, 0, 0, 5);
                }
                else if (parameter?.ToString()?.Equals("BarcodeScannerCamera") == true) {
                    return new CornerRadius(0, 20, 20, 0);
                }
                else if (parameter?.ToString()?.Equals("VolumeCamera") == true) {
                    return new CornerRadius(5, 20, 20, 5);
                }
            }
            return new CornerRadius(0, 0, 0, 0);
        }
    }
}