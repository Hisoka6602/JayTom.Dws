using System;
using System.Globalization;
using System.Windows.Data;

namespace JayTom.Dws.Client.Converters {

    public class NegativeNumberConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var number = (double)value;

            // 自定义处理负数的格式
            return number < 0 ? string.Format(culture, "-{0:0.#}", -number) : string.Format(culture, "{0:0.#}", number);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}