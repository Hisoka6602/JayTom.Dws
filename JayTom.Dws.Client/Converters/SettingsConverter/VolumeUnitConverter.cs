using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Converters;

namespace JayTom.Dws.Client.Converters.SettingsConverter {

    public class VolumeUnitConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (Enum.TryParse(value?.ToString(), out VolumeUnit selectedType)) {
                return selectedType switch {
                    VolumeUnit.Millimeter => "mm",
                    VolumeUnit.Centimeter => "cm",
                    VolumeUnit.Meter => "m",
                    _ => "mm"
                };
            }
            return "mm";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}