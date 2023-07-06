using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.PluginInterface;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class PluginStatusIconFontConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                PluginStatus.NotInstalled => string.Empty,
                PluginStatus.Installed => "\xe8bd",
                PluginStatus.Upgradeable => "\xe627",
                PluginStatus.Invalid => "\xe602",
                PluginStatus.BugFound => "\xe60d",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                PluginStatus.NotInstalled => string.Empty,
                PluginStatus.Installed => "\xe8bd",
                PluginStatus.Upgradeable => "\xe627",
                PluginStatus.Invalid => "\xe602",
                PluginStatus.BugFound => "\xe60d",
                _ => string.Empty,
            };
        }
    }
}