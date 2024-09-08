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
using JayTom.Dws.Client.Attributes;

namespace JayTom.Dws.Client.Converters.CameraConverter {

    public class CameraConnectionTypeConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is CameraConnectionType type) {
                if (parameter?.ToString()?.Equals("font", StringComparison.CurrentCultureIgnoreCase) == true) {
                    return type.GetCameraFontIcon();
                }
                else if (parameter?.ToString()?.Equals("color", StringComparison.CurrentCultureIgnoreCase) == true) {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(type.GetCameraBackgroundColor()));
                }
                else if (parameter?.ToString()?.Equals("ToolTip", StringComparison.CurrentCultureIgnoreCase) == true) {
                    return type.GetDescription();
                }
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}