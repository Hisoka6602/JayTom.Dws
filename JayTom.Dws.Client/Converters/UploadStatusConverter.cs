using Polly;
using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class UploadStatusConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is string status) {
                if (parameter?.ToString()?.Equals("Background") == true) {
                    return status switch {
                        "成功" => new SolidColorBrush(Colors.DarkGreen),
                        "失败" => new SolidColorBrush(Colors.DarkRed),
                        _ => new SolidColorBrush(Colors.RoyalBlue)
                    };
                }

                if (parameter?.ToString()?.Equals("FontText") == true) {
                    return status switch {
                        "成功" => "\xe8bd",
                        "失败" => "\xe677",
                        _ => "\xe653"
                    };
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is string status) {
                if (parameter?.ToString()?.Equals("Background") == true) {
                    return status switch {
                        "成功" => new SolidColorBrush(Colors.DarkGreen),
                        "失败" => new SolidColorBrush(Colors.DarkRed),
                        _ => new SolidColorBrush(Colors.RoyalBlue)
                    };
                }

                if (parameter?.ToString()?.Equals("FontText") == true) {
                    return status switch {
                        "成功" => "\xe8bd",
                        "失败" => "\xe677",
                        _ => "\xe653"
                    };
                }
            }

            return DependencyProperty.UnsetValue;
        }
    }
}