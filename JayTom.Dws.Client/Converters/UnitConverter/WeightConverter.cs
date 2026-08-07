using JayTom.Dws.Domain.Converters;
using System;
using System.Globalization;
using System.Windows.Data;

namespace JayTom.Dws.Client.Converters.UnitConverter
{

    public class WeightConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 1)
            {
                var tryParse = double.TryParse(values[0].ToString(), out var weight);
                if (Enum.TryParse(values[1]?.ToString(), out WeightUnit unit))
                {
                    if (tryParse)
                    {
                        return unit switch
                        {
                            WeightUnit.Gram => weight * 1000,
                            WeightUnit.Kilogram => weight,
                            WeightUnit.Pound => (float)(weight * 2.20462),
                            _ => 0
                        };
                    }
                }
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}