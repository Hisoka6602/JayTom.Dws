using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace JayTom.Dws.Client.Models {

    public class LanguageInfoModel {

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