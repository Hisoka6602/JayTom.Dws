using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.Converters
{

    public class PackageTypeToBorderThicknessConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not null)
            {
                if (Enum.TryParse(value.ToString(), out PackageType selectedType))
                {
                    if (Enum.TryParse(parameter.ToString(), out PackageType result))
                    {
                        if (selectedType == result)
                        {
                            return new Thickness(0);
                        }
                    }
                }
            }
            return new Thickness(1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}