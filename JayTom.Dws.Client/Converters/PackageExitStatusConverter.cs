using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.DataModels;

namespace JayTom.Dws.Client.Converters {

    public class PackageExitStatusConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is PackageExitStatus status) {
                if (parameter?.ToString()?.Equals("Foreground") == true) {
                    return status switch {
                        PackageExitStatus.Normal => new SolidColorBrush(Colors.LawnGreen),
                        PackageExitStatus.Abnormal => new SolidColorBrush(Colors.OrangeRed),
                        _ => new SolidColorBrush(Colors.White),
                    };
                }
                else if (parameter?.ToString()?.Equals("FontWeight") == true) {
                    return status switch {
                        PackageExitStatus.Normal => FontWeights.Bold,
                        PackageExitStatus.Abnormal => FontWeights.Bold,
                        _ => FontWeights.Normal,
                    };
                }
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}