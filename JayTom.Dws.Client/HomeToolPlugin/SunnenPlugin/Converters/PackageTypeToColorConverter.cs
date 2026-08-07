using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.Converters
{

    public class PackageTypeToColorConverter : IValueConverter
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
                            return Brushes.RoyalBlue;
                        }
                    }
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}