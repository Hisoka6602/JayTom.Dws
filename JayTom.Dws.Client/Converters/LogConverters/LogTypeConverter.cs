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

    public class LogTypeConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogType type)
            {
                if (parameter?.ToString()?.Equals("Color") == true)
                {
                    return type switch
                    {
                        LogType.Exception => new SolidColorBrush(Colors.OrangeRed),
                        LogType.Warning => new SolidColorBrush(Colors.Goldenrod),
                        LogType.Information => new SolidColorBrush(Colors.DodgerBlue),
                        _ => new SolidColorBrush(Colors.White)
                    };
                }
                else if (parameter?.ToString()?.Equals("Text") == true)
                {
                    return type switch
                    {
                        LogType.Exception => "异常",
                        LogType.Warning => "警告",
                        LogType.Information => "信息",
                        _ => "未知"
                    };
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