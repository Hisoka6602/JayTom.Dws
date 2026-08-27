using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Client.Converters
{

    public class SortingModeConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SortMode sortMode)
            {
                return sortMode switch
                {
                    SortMode.None => "无",
                    SortMode.BarcodeSorting => "条码分拣",
                    SortMode.WeightSorting => "重量分拣",
                    SortMode.VolumeSorting => "体积分拣",
                    SortMode.LogisticsSorting => "物流分拣",
                    SortMode.OcrSorting => "Ocr分拣",
                    SortMode.ApiResponseSorting => "Api分拣",
                    SortMode.CombinedWorkflowSorting => "组合工作流分拣",
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