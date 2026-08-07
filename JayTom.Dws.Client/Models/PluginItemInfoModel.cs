using JayTom.Dws.PluginInterface;
using Prism.Mvvm;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace JayTom.Dws.Client.Models
{
    public class PluginItemInfoModel : BindableBase
    {
        private ICommand? _clickCommand;
        private ImageSource? _icon;
        private string _name = string.Empty;
        private PluginStatus _status;
        private PluginType _type;
        private string _description = string.Empty;
        private DateTime _releaseDate;
        private Version _clientVersionDependency = new();
        private string _author = string.Empty;
        private bool _isSelected;
        private bool _isInstallable;
        private Version _version = new();

        /// <summary>
        /// 插件图标
        /// </summary>
        public ImageSource? Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        /// <summary>
        /// 插件名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 插件状态
        /// </summary>
        public PluginStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// 插件类型
        /// </summary>
        public PluginType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// 发行日期
        /// </summary>

        public DateTime ReleaseDate
        {
            get => _releaseDate;
            set => SetProperty(ref _releaseDate, value);
        }

        /// <summary>
        /// 客户端依赖版本
        /// </summary>
        public Version ClientVersionDependency
        {
            get => _clientVersionDependency;
            set => SetProperty(ref _clientVersionDependency, value);
        }
        /// <summary>
        /// 版本
        /// </summary>
        public Version Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        /// <summary>
        /// 作者
        /// </summary>
        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// 是否可安装
        /// </summary>
        public bool IsInstallable
        {
            get => _isInstallable;
            set => SetProperty(ref _isInstallable, value);
        }

        /// <summary>
        /// 点击事件
        /// </summary>
        public ICommand? ClickCommand
        {
            get => _clickCommand;
            set => SetProperty(ref _clickCommand, value);
        }
    }
}