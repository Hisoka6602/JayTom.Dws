using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Converters.PackageSortingConfiguration {

    public class AbnormalSortingTypeConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is AbnormalSortingType type) {
                switch (type) {
                    case AbnormalSortingType.None:
                        return "正常分拣";

                    case AbnormalSortingType.NetworkTimeout:
                        return "Api接口超时";

                    case AbnormalSortingType.ApiAccessError:
                        return "Api异常访问";

                    case AbnormalSortingType.NoRead:
                        return "无条码";

                    case AbnormalSortingType.MultipleBarCode:
                        return "多条码识别";

                    case AbnormalSortingType.NoSortingInstruction:
                        return "无分拣指令";

                    case AbnormalSortingType.NoPhysicalMailbox:
                        return "无匹配规则";

                    case AbnormalSortingType.LockExit:
                        return "锁格";

                    case AbnormalSortingType.StackedPackage:
                        return "叠包";

                    case AbnormalSortingType.PostNonLocalBarcode:
                        return "非本机构条码";

                    case AbnormalSortingType.PostSegmentNotFound:
                        return "查不到段道";
                }
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}