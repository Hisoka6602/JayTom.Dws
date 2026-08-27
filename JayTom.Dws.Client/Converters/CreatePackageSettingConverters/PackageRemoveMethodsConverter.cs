using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters.CreatePackageSettingConverters
{

    public class PackageRemoveMethodsConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PackageRemoveMethodsEnum method)
            {
                if (parameter?.ToString()?.Equals("FillInformation") == true && method == PackageRemoveMethodsEnum.FillInformation)
                {
                    return true;
                }
                else if (parameter?.ToString()?.Equals("LowerMachineRemoval") == true && method == PackageRemoveMethodsEnum.LowerMachineRemoval)
                {
                    return true;
                }
                else if (parameter?.ToString()?.Equals("None") == true && method == PackageRemoveMethodsEnum.None)
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
                if (parameter?.ToString()?.Equals("FillInformation") == true && boolValue)
                {
                    return PackageRemoveMethodsEnum.FillInformation;
                }
                else if (parameter?.ToString()?.Equals("LowerMachineRemoval") == true && boolValue)
                {
                    return PackageRemoveMethodsEnum.LowerMachineRemoval;
                }
                else if (parameter?.ToString()?.Equals("None") == true && boolValue)
                {
                    return PackageRemoveMethodsEnum.None;
                }
                return PackageRemoveMethodsEnum.None;
            }
            return Binding.DoNothing;
        }
    }
}