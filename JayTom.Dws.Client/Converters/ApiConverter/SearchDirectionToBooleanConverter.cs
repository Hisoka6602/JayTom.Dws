using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Legacy.Contracts.Dto.ApiDto;

namespace JayTom.Dws.Client.Converters.ApiConverter
{

    public class SearchDirectionToBooleanConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not null)
            {
                if (Enum.TryParse(value.ToString(), out SearchDirection selectedType))
                {
                    if (Enum.TryParse(parameter.ToString(), out SearchDirection result))
                    {
                        return selectedType == result;
                    }
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Enum.TryParse(parameter.ToString(), out SearchDirection result))
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