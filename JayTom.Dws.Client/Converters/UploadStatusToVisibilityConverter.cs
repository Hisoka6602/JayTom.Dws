using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Client.Converters
{

    public class UploadStatusToVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UploadStatus uploadStatus)
            {
                return uploadStatus switch
                {
                    UploadStatus.NotUploaded => Visibility.Collapsed,
                    _ => Visibility.Visible,
                };
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}