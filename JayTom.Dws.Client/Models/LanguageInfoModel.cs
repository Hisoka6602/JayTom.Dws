using Prism.Mvvm;
using System.Windows.Media.Imaging;

namespace JayTom.Dws.Client.Models {

    public class LanguageInfoModel : BindableBase {

        /// <summary>
        /// 语言
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// 显示文字
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 国旗
        /// </summary>
        public BitmapImage? NationalFlag { get; set; }
    }
}