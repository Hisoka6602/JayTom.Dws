using JayTom.Dws.Domain.Dto;
using System;
using System.Globalization;
using System.Windows.Data;

namespace JayTom.Dws.Client.Converters.SettingsConverter
{

    public class CharacterTypeToBooleanConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!string.IsNullOrEmpty(value?.ToString()) && Enum.TryParse(value.ToString(), out CharacterType selectedType))
            {
                if (Enum.TryParse(parameter.ToString(), out CharacterType result))
                {
                    return selectedType == result;
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Enum.TryParse(parameter.ToString(), out CharacterType result))
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