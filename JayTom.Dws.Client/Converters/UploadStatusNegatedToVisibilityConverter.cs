using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class UploadStatusNegatedToVisibilityConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is UploadStatus status) {
                return status != UploadStatus.NotUploaded ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}