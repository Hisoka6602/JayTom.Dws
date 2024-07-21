using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Reflection;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Attributes;
using JayTom.Dws.Camera.Attributes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace JayTom.Dws.Client.Converters.AttributeConverters {

    public class EnumAttributeConverter : IValueConverter {

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is Enum enumValue && parameter is string parameterString) {
                switch (parameterString.ToLowerInvariant()) {
                    case "description":
                        return enumValue.GetDescription();

                    case "auxiliarydescription":
                        return enumValue.GetAuxiliaryDescription();

                    case "backgroundcolor":
                        var bgColor = enumValue.GetBackgroundColor();
                        return !string.IsNullOrEmpty(bgColor)
                            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor))
                            : Brushes.Transparent;

                    case "fonticon":
                        return enumValue.GetFontIcon();

                    case "labelcolor":
                        var labelColor = enumValue.GetLabelColor();
                        return !string.IsNullOrEmpty(labelColor)
                            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(labelColor))
                            : Brushes.Transparent;

                    case "typeabbreviation":
                        return enumValue.GetTypeAbbreviation();

                    case "visibility":
                        return enumValue.GetVisibility() ? Visibility.Visible : Visibility.Collapsed;

                    case "camerabackgroundcolor":
                        var cameraBgColor = enumValue.GetCameraBackgroundColor();
                        return !string.IsNullOrEmpty(cameraBgColor)
                            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(cameraBgColor))
                            : Brushes.Transparent;

                    case "camerafonticon":
                        return enumValue.GetCameraFontIcon();

                    case "cameralabelcolor":
                        var cameraLabelColor = enumValue.GetCameraLabelColor();
                        return !string.IsNullOrEmpty(cameraLabelColor)
                            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(cameraLabelColor))
                            : Brushes.Transparent;

                    default:
                        return Binding.DoNothing;
                }
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}