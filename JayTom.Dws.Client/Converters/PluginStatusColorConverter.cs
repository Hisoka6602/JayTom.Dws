using JayTom.Dws.PluginInterface;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JayTom.Dws.Client.Converters {

    internal class PluginStatusColorConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                PluginStatus.Installed => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CE79C")),
                PluginStatus.Upgradeable => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64B1FF")),
                PluginStatus.Invalid => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE5A3")),
                PluginStatus.BugFound => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F55B65")),
                _ => new SolidColorBrush(Colors.Transparent),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                PluginStatus.Installed => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CE79C")),
                PluginStatus.Upgradeable => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64B1FF")),
                PluginStatus.Invalid => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE5A3")),
                PluginStatus.BugFound => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F55B65")),
                _ => new SolidColorBrush(Colors.Transparent),
            };
        }
    }
}