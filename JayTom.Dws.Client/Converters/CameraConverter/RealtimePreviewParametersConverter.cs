using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using EFCore.BulkExtensions;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.Cameras.CameraConfiguration;

namespace JayTom.Dws.Client.Converters.CameraConverter {

    public class RealtimePreviewParametersConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values[0] is NvrRealTimePreviewItemInfo itemInfo &&
                values[1] is NvrPreviewAction action &&
                values[2] is NvrPreviewOperationType type) {
                return new RealtimePreviewOperationParameters(itemInfo, action, type);
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}