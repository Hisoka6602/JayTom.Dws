using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters.SettingsConverter {

    public class WeightAccessToVisibilityConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is WeightAccessInfoMode model) {
                if (parameter.ToString()?.Contains("|") == true) {
                    var list = parameter.ToString()?.Split("|")?.ToList();
                    if (list?.Any(a => a.Equals(model.Value.ToString())) == true) {
                        return Visibility.Visible;
                    }
                }
                else {
                    if (model.Value.ToString().Equals(parameter.ToString())) {
                        return Visibility.Visible;
                    }
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}