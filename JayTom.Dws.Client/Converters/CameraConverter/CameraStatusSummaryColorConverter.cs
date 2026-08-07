using System;
using System.Linq;
using JayTom.Dws.Camera;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using JayTom.Dws.Client.Models;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.Converters.CameraConverter
{

    public class CameraStatusSummaryColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ObservableCollection<CameraItemInfoModel> items)
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
            return items.Any(a =>
                a.Status is CameraStatus.Disconnected or CameraStatus.Failure) ? new SolidColorBrush(Colors.Red) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ObservableCollection<CameraItemInfoModel> items)
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
            return items.Any(a =>
                a.Status is CameraStatus.Disconnected or CameraStatus.Failure) ? new SolidColorBrush(Colors.Red) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
        }
    }
}