using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Client.Converters
{

    public class SourceTypeConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SourceType sourceType)
            {
                return sourceType switch
                {
                    SourceType.SerialPort => "串口",
                    SourceType.Tcp => "Tcp",
                    SourceType.Input => "输入",
                    SourceType.Camera => "相机",
                    _ => string.Empty
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}