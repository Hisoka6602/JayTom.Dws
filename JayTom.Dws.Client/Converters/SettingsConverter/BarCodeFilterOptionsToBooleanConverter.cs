using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters.SettingsConverter
{

    public class BarCodeFilterOptionsToBooleanConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!string.IsNullOrEmpty(value?.ToString()) && Enum.TryParse(value.ToString(), out BarCodeFilterOptions barCodeFilterOptions))
            {
                if (Enum.TryParse(parameter.ToString(), out BarCodeFilterOptions result))
                {
                    return barCodeFilterOptions == result;
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Enum.TryParse(parameter.ToString(), out BarCodeFilterOptions result))
            {
                if (value is true)
                {
                    return result;
                }
            }
            return BarCodeFilterOptions.None;
        }
    }
}