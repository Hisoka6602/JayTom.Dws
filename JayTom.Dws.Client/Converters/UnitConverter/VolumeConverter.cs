using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Converters;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace JayTom.Dws.Client.Converters.UnitConverter {

    public class VolumeConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length > 1) {
                var tryParse = double.TryParse(values[0].ToString(), out var volume);
                if (Enum.TryParse(values[1]?.ToString(), out VolumeUnit unit)) {
                    if (tryParse) {
                        return unit switch {
                            VolumeUnit.Centimeter => volume / 10,
                            VolumeUnit.Meter => volume / 1000,
                            VolumeUnit.Millimeter => volume,
                            _ => 0
                        };
                    }
                }
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}