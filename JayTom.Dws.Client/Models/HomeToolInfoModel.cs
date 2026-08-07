using Prism.Mvvm;
using System.Windows.Input;

namespace JayTom.Dws.Client.Models
{

    public class HomeToolInfoModel : BindableBase
    {
        private string _name = string.Empty;
        private string _iconFont = string.Empty;
        private ICommand _openCommand;
        private string _brief = string.Empty;
        private bool _isModal;
        private string _controlClassName = string.Empty;
        private bool _isRunnable;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 字体图标
        /// </summary>
        public string IconFont
        {
            get => _iconFont;
            set => SetProperty(ref _iconFont, value);
        }

        /// <summary>
        /// 打开事件
        /// </summary>
        public ICommand OpenCommand
        {
            get => _openCommand;
            set => SetProperty(ref _openCommand, value);
        }

        /// <summary>
        /// 简介
        /// </summary>
        public string Brief
        {
            get => _brief;
            set => SetProperty(ref _brief, value);
        }

        /// <summary>
        /// 是否模态窗口
        /// </summary>
        public bool IsModal
        {
            get => _isModal;
            set => SetProperty(ref _isModal, value);
        }

        /// <summary>
        /// 控件类名
        /// </summary>
        public string ControlClassName
        {
            get => _controlClassName;
            set => SetProperty(ref _controlClassName, value);
        }

        /// <summary>
        /// 是否能被运行
        /// </summary>
        public bool IsRunnable
        {
            get => _isRunnable;
            set => SetProperty(ref _isRunnable, value);
        }
    }
}