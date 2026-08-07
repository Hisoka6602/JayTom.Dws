using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters.CameraConverter
{

    public class NvrPreviewViewSizeConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int itemCount)
            {
                var size = new Size(768, 432);
                if (itemCount > 1)
                {
                    size = new Size(449, 253);
                }

                return parameter.ToString()?.Equals("Width", StringComparison.CurrentCultureIgnoreCase) == true
                    ? size.Width
                    : size.Height;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}