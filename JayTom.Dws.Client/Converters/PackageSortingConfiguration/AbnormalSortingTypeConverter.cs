using System;
using System.Linq;
using System.Text;
using TouchSocket.Core;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.Models.Package;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Converters.PackageSortingConfiguration
{

    public class AbnormalSortingTypeConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AbnormalSortingType type)
            {
                return type.GetDescription();
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}