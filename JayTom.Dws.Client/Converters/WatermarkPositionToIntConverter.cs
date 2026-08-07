using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.SaveImage;

namespace JayTom.Dws.Client.Converters
{

    public class WatermarkPositionToIntConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 将枚举类型转换为整数类型
            if (value is WatermarkPosition position)
            {
                return (int)position;
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 将整数类型转换为枚举类型
            if (value is int index)
            {
                return (WatermarkPosition)index;
            }

            return Binding.DoNothing;
        }
    }
}