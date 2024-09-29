using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Size = System.Windows.Size;
using JayTom.Dws.Client.Attributes;
using JayTom.Dws.Client.Models.Cameras.CameraConfiguration;

namespace JayTom.Dws.Client.Converters.CameraConverter {

    public class VideoPlayerSizeConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is int itemCount) {
                var size = new Size(1200, 675);
                switch (itemCount) {
                    case 1:
                        size = new Size(1200, 675);
                        break;

                    case > 1 and <= 4:
                        size = new Size(614, 346);
                        break;

                    case > 4:
                        size = new Size(449, 253);
                        break;
                }

                return parameter.ToString()?.Equals("Width", StringComparison.CurrentCultureIgnoreCase) == true
                     ? size.Width
                     : size.Height;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}