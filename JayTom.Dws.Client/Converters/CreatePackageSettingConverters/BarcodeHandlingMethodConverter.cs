using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters.CreatePackageSettingConverters {

    public class BarcodeHandlingMethodConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is BarcodeHandlingMethodEnum method) {
                if (parameter?.ToString()?.Equals("UseOneBarcode") == true && method == BarcodeHandlingMethodEnum.UseOneBarcode) {
                    return true;
                }
                else if (parameter?.ToString()?.Equals("MergeBarcodes") == true && method == BarcodeHandlingMethodEnum.MergeBarcodes) {
                    return true;
                }
                return false;
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool boolValue) {
                if (parameter?.ToString()?.Equals("UseOneBarcode") == true && boolValue) {
                    return BarcodeHandlingMethodEnum.UseOneBarcode;
                }
                else if (parameter?.ToString()?.Equals("MergeBarcodes") == true && boolValue) {
                    return BarcodeHandlingMethodEnum.MergeBarcodes;
                }
                return BarcodeHandlingMethodEnum.UseOneBarcode;
            }
            return Binding.DoNothing;
        }
    }
}