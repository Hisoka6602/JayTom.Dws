using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Camera;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Camera.Attributes;
using JayTom.Dws.PluginInterface.Utils;

namespace JayTom.Dws.Client.Converters.CameraConverter {

    public class CameraStatusConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is CameraStatus status) {
                if (parameter?.ToString()?.Equals("font", StringComparison.CurrentCultureIgnoreCase) == true) {
                    return status.GetCameraFontIcon();
                }
                else if (parameter?.ToString()?.Equals("color", StringComparison.CurrentCultureIgnoreCase) == true) {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(status.GetCameraBackgroundColor()));
                }
                else if (parameter?.ToString()?.Equals("ToolTip", StringComparison.CurrentCultureIgnoreCase) == true) {
                    return status.GetDescription();
                }
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}