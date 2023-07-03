using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace JayTom.Dws.Client.Models {

    public class MenuItemInfoModel : BindableBase {
        private ICommand? _clickCommand;
        private IconInfoModel? _iconFont;
        private BitmapImage? _icon;
        private string _title = string.Empty;
        private string _description = string.Empty;
        private string _pageClassName = string.Empty;
        private bool _isSelected;

        /// <summary>
        /// 字体图标
        /// </summary>
        public IconInfoModel? IconFont {
            get => _iconFont;
            set => SetProperty(ref _iconFont, value);
        }

        /// <summary>
        /// 图片图标
        /// </summary>
        public BitmapImage? Icon {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 说明
        /// </summary>
        public string Description {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// 页面类名
        /// </summary>
        public string PageClassName {
            get => _pageClassName;
            set => SetProperty(ref _pageClassName, value);
        }

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// 点击事件
        /// </summary>
        public ICommand? ClickCommand {
            get => _clickCommand;
            set => SetProperty(ref _clickCommand, value);
        }
    }
}