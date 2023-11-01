using JayTom.Dws.Data.LocalData;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace JayTom.Dws.Client.Converters {
    public class UploadStatusConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is UploadStatus status) {
                if (parameter?.ToString()?.Equals("Background") == true) {
                    return status switch {
                        UploadStatus.Succeeded => new SolidColorBrush(Colors.DarkGreen),
                        UploadStatus.Failed => new SolidColorBrush(Colors.DarkRed),
                        _ => new SolidColorBrush(Colors.RoyalBlue)
                    };
                }

                if (parameter?.ToString()?.Equals("FontText") == true) {
                    return status switch {
                        UploadStatus.Succeeded => "\xe8bd",
                        UploadStatus.Failed => "\xe7a4",
                        _ => "\xe7a7"
                    };
                }
                if (parameter?.ToString()?.Equals("Text") == true) {
                    return status switch {
                        UploadStatus.Succeeded => Languages.Language.ResourceManager.GetString("ApiSuccess") ?? string.Empty,
                        UploadStatus.Failed => Languages.Language.ResourceManager.GetString("ApiFailure") ?? string.Empty,
                        _ => Languages.Language.ResourceManager.GetString("ApiNotUploaded") ?? string.Empty
                    };
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is UploadStatus status) {
                if (parameter?.ToString()?.Equals("Background") == true) {
                    return status switch {
                        UploadStatus.Succeeded => new SolidColorBrush(Colors.DarkGreen),
                        UploadStatus.Failed => new SolidColorBrush(Colors.DarkRed),
                        _ => new SolidColorBrush(Colors.RoyalBlue)
                    };
                }

                if (parameter?.ToString()?.Equals("FontText") == true) {
                    return status switch {
                        UploadStatus.Succeeded => "\xe8bd",
                        UploadStatus.Failed => "\xe7a4",
                        _ => "\xe7a7"
                    };
                }
                if (parameter?.ToString()?.Equals("Text") == true) {
                    return status switch {
                        UploadStatus.Succeeded => Languages.Language.ResourceManager.GetString("ApiSuccess") ?? string.Empty,
                        UploadStatus.Failed => Languages.Language.ResourceManager.GetString("ApiFailure") ?? string.Empty,
                        _ => Languages.Language.ResourceManager.GetString("ApiNotUploaded") ?? string.Empty
                    };
                }
            }

            return DependencyProperty.UnsetValue;
        }
    }
}