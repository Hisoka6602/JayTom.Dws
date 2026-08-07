using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters
{

    public class DoubleToTimeToolTipConverter : IValueConverter
    {
        private readonly DateTime _referenceTime = new(1900, 1, 1); // 基准时间

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                DateTime dateTimeValue = _referenceTime.AddSeconds(doubleValue);
                return dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss"); // 格式化为时间字符串
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}