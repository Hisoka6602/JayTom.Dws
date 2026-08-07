using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters.CloudConverters
{

    public class IsUploadedToCloudVideoTagColorConvert : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isUploaded)
            {
                if (isUploaded)
                {
                    return new SolidColorBrush(Colors.DodgerBlue);
                }
                else
                {
                    return new SolidColorBrush(Colors.DarkGray);
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