using JayTom.Dws.Client.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace JayTom.Dws.Client.Converters {

    public class CameraTypeToolTipConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                CameraType.IndustrialCamera => Languages.Language.ResourceManager.GetString("IndustrialCamera") ?? string.Empty,
                CameraType.PanoramaCamera => Languages.Language.ResourceManager.GetString("PanoramaCamera") ?? string.Empty,
                CameraType.SmartCamera => Languages.Language.ResourceManager.GetString("SmartCamera") ?? string.Empty,
                CameraType.ThreeDCamera => Languages.Language.ResourceManager.GetString("3DCamera/VolumeCamera") ?? string.Empty,
                CameraType.VideoCamera => Languages.Language.ResourceManager.GetString("VideoCameraSecurityCamera") ?? string.Empty,
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                CameraType.IndustrialCamera => Languages.Language.ResourceManager.GetString("IndustrialCamera") ?? string.Empty,
                CameraType.PanoramaCamera => Languages.Language.ResourceManager.GetString("PanoramaCamera") ?? string.Empty,
                CameraType.SmartCamera => Languages.Language.ResourceManager.GetString("SmartCamera") ?? string.Empty,
                CameraType.ThreeDCamera => Languages.Language.ResourceManager.GetString("3DCamera/VolumeCamera") ?? string.Empty,
                CameraType.VideoCamera => Languages.Language.ResourceManager.GetString("VideoCameraSecurityCamera") ?? string.Empty,
                _ => string.Empty,
            };
        }
    }
}