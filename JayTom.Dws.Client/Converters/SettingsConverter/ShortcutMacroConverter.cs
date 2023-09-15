using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters.SettingsConverter {

    public class ShortcutMacroConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is string content) {
                switch (content) {
                    case "{BarCode}":
                        return Languages.Language.ResourceManager.GetString("TableHeaderBarCode") ?? string.Empty;

                    case "{Weight}":
                        return Languages.Language.ResourceManager.GetString("TableHeaderWeight") ?? string.Empty;

                    case "{Volume}":
                        return Languages.Language.ResourceManager.GetString("TableHeaderVolume") ?? string.Empty;

                    case "{Length}":
                        return Languages.Language.ResourceManager.GetString("TableHeaderLength") ?? string.Empty;

                    case "{Width}":
                        return Languages.Language.ResourceManager.GetString("TableHeaderWidth") ?? string.Empty;

                    case "{Height}":
                        return Languages.Language.ResourceManager.GetString("TableHeaderHeight") ?? string.Empty;

                    case "{ScanTime}":
                        return Languages.Language.ResourceManager.GetString("TableHeaderScanTime") ?? string.Empty;

                    case "{TimestampedGuid}":
                        return Languages.Language.ResourceManager.GetString("TableHeaderTimestampGuid") ?? string.Empty;

                    case "{CameraSerialNumber}":
                        return Languages.Language.ResourceManager.GetString("TableHeaderSerialNumber") ?? string.Empty;

                    case "{ImageType}":
                        return Languages.Language.ResourceManager.GetString("TableHeaderImageType") ?? string.Empty;

                    case "{Year}":
                        return Languages.Language.ResourceManager.GetString("Year") ?? string.Empty;

                    case "{Month}":
                        return Languages.Language.ResourceManager.GetString("Month") ?? string.Empty;

                    case "{Day}":
                        return Languages.Language.ResourceManager.GetString("Day") ?? string.Empty;

                    case "{Hour}":
                        return Languages.Language.ResourceManager.GetString("Hour") ?? string.Empty;

                    default:
                        return "null";
                }
            }

            return "null";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}