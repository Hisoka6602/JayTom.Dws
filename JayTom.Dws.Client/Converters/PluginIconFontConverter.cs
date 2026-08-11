using JayTom.Dws.PluginInterface;
using System;
using System.Globalization;
using System.Windows.Data;
using PluginType = JayTom.Dws.Plugin.Contracts.PluginType;

namespace JayTom.Dws.Client.Converters
{

    public class PluginIconFontConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                PluginType.ExtensionPackage => "\xe638",
                PluginType.Home => "\xe8a1",
                PluginType.Inner => "\xe731",
                PluginType.Dialog => "\xe61d",
                PluginType.Control => "\xe645",
                PluginType.Tool => "\xe797",
                PluginType.Api => "\xe664",
                PluginType.Filter => "\xe675",
                PluginType.Process => "\xe6e0",
                PluginType.Initialize => "\xe8b1",
                PluginType.Background => "\xe603",
                PluginType.Device => "\xeb01",
                PluginType.HomeTool => "\xe61f",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                PluginType.ExtensionPackage => "\xe638",
                PluginType.Home => "\xe8a1",
                PluginType.Inner => "\xe731",
                PluginType.Dialog => "\xe61d",
                PluginType.Control => "\xe645",
                PluginType.Tool => "\xe797",
                PluginType.Api => "\xe664",
                PluginType.Filter => "\xe675",
                PluginType.Process => "\xe6e0",
                PluginType.Initialize => "\xe8b1",
                PluginType.Background => "\xe603",
                PluginType.Device => "\xeb01",
                PluginType.HomeTool => "\xe61f",
                _ => string.Empty,
            };
        }
    }
}
