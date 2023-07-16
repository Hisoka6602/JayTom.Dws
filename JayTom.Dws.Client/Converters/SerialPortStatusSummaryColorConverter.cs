using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.Converters {

    public class SerialPortStatusSummaryColorConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is not ObservableCollection<SerialPortInfoModel> items)
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
            return items.Any(a =>
                a.Status is SerialPortStatus.NotConnected) ? new SolidColorBrush(Colors.Red) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is not ObservableCollection<SerialPortInfoModel> items)
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
            return items.Any(a =>
                a.Status is SerialPortStatus.NotConnected) ? new SolidColorBrush(Colors.Red) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
        }
    }
}