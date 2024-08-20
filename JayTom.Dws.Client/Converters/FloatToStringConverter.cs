using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class FloatToStringConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            // 尝试解析输入的字符串为 float
            return float.TryParse(value as string, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : 0f; // 如果解析失败，返回0
        }
    }
}