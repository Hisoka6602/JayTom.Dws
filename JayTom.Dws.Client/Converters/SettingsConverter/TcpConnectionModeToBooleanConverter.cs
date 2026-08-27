using JayTom.Dws.Legacy.Contracts.Dto.BaseInfoModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace JayTom.Dws.Client.Converters.SettingsConverter
{

    public class TcpConnectionModeToBooleanConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not null)
            {
                if (Enum.TryParse(value.ToString(), out TcpConnectionMode selectedType))
                {
                    if (Enum.TryParse(parameter.ToString(), out TcpConnectionMode result))
                    {
                        return selectedType == result;
                    }
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Enum.TryParse(parameter.ToString(), out TcpConnectionMode result))
            {
                if (value is true)
                {
                    return result;
                }
            }
            return Binding.DoNothing;
        }
    }
}