using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class DoubleToDateTimeConverter : IValueConverter {
        private readonly DateTime _referenceTime = new DateTime(1900, 1, 1); // 基准时间，可根据需要修改

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value is DateTime dateTimeValue) {
                return (dateTimeValue - _referenceTime).TotalSeconds;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value is double doubleValue) {
                return _referenceTime.AddSeconds(doubleValue);
            }
            return _referenceTime;
        }
    }
}