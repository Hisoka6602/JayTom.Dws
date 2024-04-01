using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.StatusBarModels;

namespace JayTom.Dws.Client.Converters.StatusBarConverters {

    public class StatusBarConnectionTypeConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is ConnectionType type) {
                if (parameter?.ToString()?.ToLower()?.Equals("font") == true) {
                    switch (type) {
                        case ConnectionType.TCP:
                            return "\xe62f";

                        case ConnectionType.SerialPort:
                            return "\xe62c";

                        case ConnectionType.Audio:
                            return "\xe6ff";

                        case ConnectionType.FTP:
                            return "\xe6c9";

                        case ConnectionType.Location:
                            return "\xe6f7";

                        case ConnectionType.Custom:
                            return "\xe6dd";
                    }
                }
                else if (parameter?.ToString()?.ToLower()?.Equals("text") == true) {
                    switch (type) {
                        case ConnectionType.TCP:
                            return "Tcp连接";

                        case ConnectionType.SerialPort:
                            return "串口连接";

                        case ConnectionType.Audio:
                            return "音频输出";

                        case ConnectionType.FTP:
                            return "Ftp连接";

                        case ConnectionType.Location:
                            return "位置输出";

                        case ConnectionType.Custom:
                            return "控件输入";
                    }
                }
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}