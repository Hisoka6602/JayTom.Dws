using System;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Converters {

    public class RunningStatusConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is true) {
                if (parameter.Equals("FontText")) {
                    return "\xe693";
                }
                else if (parameter.Equals("Text")) {
                    return Languages.Language.ResourceManager.GetString("Stop") ?? string.Empty;// "Stop"
                }
            }
            else {
                if (parameter.Equals("FontText")) {
                    return "\xea82";
                }
                else if (parameter.Equals("Text")) {
                    return Languages.Language.ResourceManager.GetString("Start") ?? string.Empty;//Start
                }
            }

            return new object();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}