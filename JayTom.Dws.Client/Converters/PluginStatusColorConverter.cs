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
                PluginStatus.Installed => new SolidColorBrush(Colors.ForestGreen),
                PluginStatus.Upgradeable => new SolidColorBrush(Colors.DodgerBlue),
                PluginStatus.Invalid => new SolidColorBrush(Colors.Goldenrod),
                PluginStatus.BugFound => new SolidColorBrush(Colors.OrangeRed),
                _ => Colors.Transparent,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                PluginStatus.Installed => new SolidColorBrush(Colors.ForestGreen),
                PluginStatus.Upgradeable => new SolidColorBrush(Colors.DodgerBlue),
                PluginStatus.Invalid => new SolidColorBrush(Colors.Goldenrod),
                PluginStatus.BugFound => new SolidColorBrush(Colors.OrangeRed),
                _ => Colors.Transparent,
            };
        }
    }
}