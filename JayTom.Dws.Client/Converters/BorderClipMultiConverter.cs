using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class BorderClipMultiConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length < 3 || values[0] == DependencyProperty.UnsetValue
                                  || values[1] == DependencyProperty.UnsetValue || values[2] == DependencyProperty.UnsetValue)
                return null;

            double width = (double)values[0];
            double height = (double)values[1];
            CornerRadius cornerRadius = (CornerRadius)values[2];

            return new RectangleGeometry(new Rect(0, 0, width, height), cornerRadius.TopLeft, cornerRadius.TopLeft);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}