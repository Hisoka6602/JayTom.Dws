using Prism.Mvvm;
using System.Windows.Media;

namespace JayTom.Dws.Client.Models {

    public class IconInfoModel : BindableBase {

        /// <summary>
        /// 图标代码
        /// </summary>
        public string IconCode { get; set; } = string.Empty;

        /// <summary>
        /// 图标字体
        /// </summary>
        public string IconFont { get; set; } = string.Empty;

        /// <summary>
        /// 图标大小
        /// </summary>
        public int IconSize { get; set; }

        /// <summary>
        /// 图标颜色
        /// </summary>
        public SolidColorBrush IconColor { get; set; } = new(Colors.White);
    }
}