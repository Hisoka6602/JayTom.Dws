using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters.LogConverters {

    public class FtpCommunicationTypeConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is FtpCommunicationType type) {
                if (parameter?.ToString()?.Equals("Color") == true) {
                    switch (type) {
                        case FtpCommunicationType.Connect:
                            return new SolidColorBrush(Colors.DodgerBlue);

                        case FtpCommunicationType.Upload:
                            return new SolidColorBrush(Colors.LawnGreen);

                        case FtpCommunicationType.Download:
                            return new SolidColorBrush(Colors.OrangeRed);

                        default:
                            return Binding.DoNothing;
                    }
                }
                else if (parameter?.ToString()?.Equals("Text") == true) {
                    switch (type) {
                        case FtpCommunicationType.Connect:
                            return "连接";

                        case FtpCommunicationType.Upload:
                            return "上传";

                        case FtpCommunicationType.Download:
                            return "下载";

                        default:
                            return Binding.DoNothing;
                    }
                }
                else if (parameter?.ToString()?.Equals("Font") == true) {
                    switch (type) {
                        case FtpCommunicationType.Connect:
                            return "\xe7f6";

                        case FtpCommunicationType.Upload:
                            return "\xe651";

                        case FtpCommunicationType.Download:
                            return "\xe650";

                        default:
                            return Binding.DoNothing;
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