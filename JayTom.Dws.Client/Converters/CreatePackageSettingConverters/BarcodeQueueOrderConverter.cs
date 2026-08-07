using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters.CreatePackageSettingConverters
{

    public class BarcodeQueueOrderConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BarcodeQueueOrderEnum method)
            {
                if (parameter?.ToString()?.Equals("TimeAscending") == true && method == BarcodeQueueOrderEnum.TimeAscending)
                {
                    return true;
                }
                else if (parameter?.ToString()?.Equals("TimeDescending") == true && method == BarcodeQueueOrderEnum.TimeDescending)
                {
                    return true;
                }
                return false;
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter?.ToString()?.Equals("TimeAscending") == true && boolValue)
                {
                    return BarcodeQueueOrderEnum.TimeAscending;
                }
                else if (parameter?.ToString()?.Equals("TimeDescending") == true && boolValue)
                {
                    return BarcodeQueueOrderEnum.TimeDescending;
                }
                return BarcodeQueueOrderEnum.TimeAscending;
            }
            return Binding.DoNothing;
        }
    }
}