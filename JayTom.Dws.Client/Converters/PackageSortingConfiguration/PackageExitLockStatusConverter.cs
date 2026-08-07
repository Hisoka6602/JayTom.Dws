using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Converters.PackageSortingConfiguration
{

    public class PackageExitLockStatusConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ExitLockStatus status)
            {
                if (parameter.ToString()?.Equals("Font") == true)
                {
                    switch (status)
                    {
                        case ExitLockStatus.Lock:
                            return "\xe93b";

                        case ExitLockStatus.Unlock:
                            return "\xe940";
                    }
                }
                else if (parameter.ToString()?.Equals("Color") == true)
                {
                    switch (status)
                    {
                        case ExitLockStatus.Lock:
                            return new SolidColorBrush(Colors.OrangeRed);

                        case ExitLockStatus.Unlock:
                            return new SolidColorBrush(Colors.LimeGreen);
                    }
                }
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}