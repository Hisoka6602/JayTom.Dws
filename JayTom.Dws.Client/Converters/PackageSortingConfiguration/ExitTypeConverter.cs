using System;
using System.Windows.Data;
using System.Globalization;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Converters.PackageSortingConfiguration {

    public class ExitTypeConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is ExitType type) {
                switch (type) {
                    case ExitType.AbnormalExit:
                        return "异常格口";

                    case ExitType.PackageExit:
                        return "包裹格口";

                    case ExitType.ReservedExit:
                        return "备用格口";
                }
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}