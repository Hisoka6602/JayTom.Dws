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
                        return "条码";

                    case "{Weight}":
                        return "重量";

                    case "{Volume}":
                        return "体积";

                    case "{Length}":
                        return "长";

                    case "{Width}":
                        return "宽";

                    case "{Height}":
                        return "高";

                    case "{ScanTime}":
                        return "扫码时间";

                    case "{TimestampedGuid}":
                        return "扫码时间戳";

                    case "{CameraSerialNumber}":
                        return "相机序列号";
                    case "{ImageType}":
                        return "存图类型";
                    case "{Year}":
                        return "年份";
                    case "{Month}":
                        return "月份";
                    case "{Day}":
                        return "日期";
                    case "{Hour}":
                        return "小时";
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