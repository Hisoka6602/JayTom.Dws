using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using JayTom.Dws.PluginInterface;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    internal class PluginStatusColorConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                PluginStatus.Installed => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CE79C")),
                PluginStatus.Upgradeable => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64B1FF")),
                PluginStatus.Invalid => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE5A3")),
                PluginStatus.BugFound => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F55B65")),
                _ => Colors.Transparent,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                PluginStatus.Installed => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CE79C")),
                PluginStatus.Upgradeable => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64B1FF")),
                PluginStatus.Invalid => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE5A3")),
                PluginStatus.BugFound => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F55B65")),
                _ => Colors.Transparent,
            };
        }
    }
}