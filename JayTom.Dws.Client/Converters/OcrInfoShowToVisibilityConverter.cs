using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.OcrSettingsModel;

namespace JayTom.Dws.Client.Converters {

    public class OcrInfoShowToVisibilityConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is OcrSettingsInfoModel model) {
                return model is { IsShowReceiverInfo: false, IsShowSenderInfo: false } ? Visibility.Collapsed : Visibility.Visible;
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}