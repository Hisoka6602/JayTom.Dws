using JayTom.Dws.Client.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace JayTom.Dws.Client.Converters
{
    public class ComputerColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ComputerInfoModel model)
            {
                switch (model.CpuInfo.UsagePercentage)
                {
                    //Cpu使用率
                    case >= 80 and < 95:
                        return new SolidColorBrush(Colors.DarkOrange);

                    case >= 95:
                        return new SolidColorBrush(Colors.Red);
                }

                switch (model.CpuInfo.CpuTemperature)
                {
                    //Cpu温度
                    case >= 70 and < 85:
                        return new SolidColorBrush(Colors.DarkOrange);

                    case > 85:
                        return new SolidColorBrush(Colors.Red);
                }

                switch (model.MemoryInfo.UsedPercentage)
                {
                    //内存
                    case >= 90 and < 99:
                        return new SolidColorBrush(Colors.DarkOrange);

                    case >= 99:
                        return new SolidColorBrush(Colors.Red);
                }
                switch (model.GpuInfo.UsagePercentage)
                {
                    //Gpu
                    case >= 90 and < 99:
                        return new SolidColorBrush(Colors.DarkOrange);

                    case >= 99:
                        return new SolidColorBrush(Colors.Red);
                }
                //硬盘
                if (model.HardDiskList?.Any(a => a.UsedSpacePercentage is >= 80 and < 95) == true)
                {
                    return new SolidColorBrush(Colors.DarkOrange);
                }
                if (model.HardDiskList?.Any(a => a.UsedSpacePercentage >= 95) == true)
                {
                    return new SolidColorBrush(Colors.Red);
                }
            }

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ComputerInfoModel model)
            {
                switch (model.CpuInfo.UsagePercentage)
                {
                    //Cpu使用率
                    case >= 80 and < 95:
                        return new SolidColorBrush(Colors.DarkOrange);

                    case >= 95:
                        return new SolidColorBrush(Colors.Red);
                }

                switch (model.CpuInfo.CpuTemperature)
                {
                    //Cpu温度
                    case >= 70 and < 85:
                        return new SolidColorBrush(Colors.DarkOrange);

                    case > 85:
                        return new SolidColorBrush(Colors.Red);
                }

                switch (model.MemoryInfo.UsedPercentage)
                {
                    //内存
                    case >= 90 and < 99:
                        return new SolidColorBrush(Colors.DarkOrange);

                    case >= 99:
                        return new SolidColorBrush(Colors.Red);
                }
                switch (model.GpuInfo.UsagePercentage)
                {
                    //Gpu
                    case >= 90 and < 99:
                        return new SolidColorBrush(Colors.DarkOrange);

                    case >= 99:
                        return new SolidColorBrush(Colors.Red);
                }
                //硬盘
                if (model.HardDiskList?.Any(a => a.UsedSpacePercentage is >= 80 and < 95) == true)
                {
                    return new SolidColorBrush(Colors.DarkOrange);
                }
                if (model.HardDiskList?.Any(a => a.UsedSpacePercentage >= 95) == true)
                {
                    return new SolidColorBrush(Colors.Red);
                }
            }

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31C731"));
        }
    }
}