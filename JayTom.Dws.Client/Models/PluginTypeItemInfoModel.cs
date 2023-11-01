using Prism.Mvvm;
using System.Windows.Input;

namespace JayTom.Dws.Client.Models {

    public class PluginTypeItemInfoModel : BindableBase {
        private string _fontIcon = string.Empty;
        private string _toolTip = string.Empty;

        /// <summary>
        /// 图标
        /// </summary>
        public string FontIcon {
            get => _fontIcon;
            set => SetProperty(ref _fontIcon, value);
        }

        /// <summary>
        /// 提示
        /// </summary>
        public string ToolTip {
            get => _toolTip;
            set => SetProperty(ref _toolTip, value);
        }

        /// <summary>
        /// 点击事件
        /// </summary>
        public ICommand? ClickCommand { get; set; }
    }
}