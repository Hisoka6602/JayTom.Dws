using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters.SettingsConverter
{

    public class FilterOutputTypeToBooleanConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!string.IsNullOrEmpty(value?.ToString()) && Enum.TryParse(value.ToString(), out FilterOutputType selectedType))
            {
                if (Enum.TryParse(parameter.ToString(), out FilterOutputType result))
                {
                    return selectedType == result;
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Enum.TryParse(parameter.ToString(), out FilterOutputType result))
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