using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters.LogConverters
{

    public class CommunicationTypeConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CommunicationType type)
            {
                if (parameter?.ToString()?.Equals("Color") == true)
                {
                    switch (type)
                    {
                        case CommunicationType.Send:
                            return new SolidColorBrush(Colors.DodgerBlue);

                        case CommunicationType.Receive:
                            return new SolidColorBrush(Colors.LawnGreen);

                        default:
                            return Binding.DoNothing;
                    }
                }
                else if (parameter?.ToString()?.Equals("Text") == true)
                {
                    switch (type)
                    {
                        case CommunicationType.Send:
                            return "发送";

                        case CommunicationType.Receive:
                            return "接收";

                        default:
                            return Binding.DoNothing;
                    }
                }
                else if (parameter?.ToString()?.Equals("Font") == true)
                {
                    switch (type)
                    {
                        case CommunicationType.Send:
                            return "\xe838";

                        case CommunicationType.Receive:
                            return "\xeb0a";

                        default:
                            return Binding.DoNothing;
                    }
                }
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}