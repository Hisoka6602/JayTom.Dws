using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using JayTom.Dws.Client.Models.CommunicationsSettingsModel;
using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Client.Converters.PackageSortingConfiguration
{

    public class CommunicationsTypeToVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CommunicationsTypeInfoModel model)
            {
                if (model is not null && model.Value != CommunicationsType.None)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}