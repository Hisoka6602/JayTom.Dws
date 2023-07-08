using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class CameraTypeToolTipConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                CameraType.IndustrialCamera => "工业相机",
                CameraType.PanoramicCamera => "全景相机",
                CameraType.SmartCamera => "智能相机",
                CameraType.ThreeDCamera => "3D相机/体积相机",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                CameraType.IndustrialCamera => "工业相机",
                CameraType.PanoramicCamera => "全景相机",
                CameraType.SmartCamera => "智能相机",
                CameraType.ThreeDCamera => "3D相机/体积相机",
                _ => string.Empty,
            };
        }
    }
}