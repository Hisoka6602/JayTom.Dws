using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace JayTom.Dws.VideoApiClient.Converters {

    public class FileExistsToVisibilityConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            string? filePath = value as string;

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath)) {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}