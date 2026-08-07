using System;
using System.Linq;
using System.Text;
using System.Windows;
using JayTom.Dws.Camera;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters.CameraConverter
{

    public class SupportedBindingTypeVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CameraBindingType type)
            {
                if (parameter?.ToString()?.Equals("PanoramaCamera") == true)
                {
                    return type.HasFlag(CameraBindingType.PanoramaCamera) ? Visibility.Visible : Visibility.Collapsed;
                }

                if (parameter?.ToString()?.Equals("BarcodeScannerCamera") == true)
                {
                    return type.HasFlag(CameraBindingType.ScannerCamera) ? Visibility.Visible : Visibility.Collapsed;
                }

                if (parameter?.ToString()?.Equals("VolumeCamera") == true)
                {
                    return type.HasFlag(CameraBindingType.VolumeCamera) ? Visibility.Visible : Visibility.Collapsed;
                }

                return Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}