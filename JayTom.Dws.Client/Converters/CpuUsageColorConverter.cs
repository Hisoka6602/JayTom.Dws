using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class CpuUsageColorConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is float rate) {
                if (rate is >= 80 and < 95) {
                    return new SolidColorBrush(Colors.DarkOrange);
                }
                else if (rate >= 95) {
                    return new SolidColorBrush(Colors.Red);
                }

                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is float rate) {
                if (rate is >= 80 and < 95) {
                    return new SolidColorBrush(Colors.DarkOrange);
                }
                else if (rate >= 95) {
                    return new SolidColorBrush(Colors.Red);
                }

                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
        }
    }
}