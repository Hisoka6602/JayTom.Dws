using System.Windows.Media;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Plugin.Excel.Attributes;
using JayTom.Dws.Client.Models.PackageSorting.Rule;

namespace JayTom.Dws.Client.Models.PackageSorting
{

    public class LogisticsCodeRecognitionItemInfoModel : BasePackageSortingItemInfoModel
    {
        private string _logisticsCode = string.Empty;
        private string _logisticsName = string.Empty;
        private byte[]? _soundBytes;
        private ImageSource? _icon;
        private string _regexPattern = string.Empty;
        private string? _soundName;
        private string _iconName = string.Empty;
        private string? _soundFileReference;
        private string? _iconFileReference;
        private ObservableCollection<LogisticsRegexItemInfoModel> _logisticsRegexItems = new();

        /// <summary>
        /// 物流代码
        /// </summary>
        [DisplayName("物流代码"), MemberNotNull, ExcelInfo(Width = 5000)]
        public string LogisticsCode
        {
            get => _logisticsCode;
            set => SetProperty(ref _logisticsCode, value);
        }

        /// <summary>
        /// 物流名称
        /// </summary>
        [DisplayName("物流名称"), MemberNotNull, ExcelInfo(Width = 5000)]
        public string LogisticsName
        {
            get => _logisticsName;
            set => SetProperty(ref _logisticsName, value);
        }

        /// <summary>
        /// 声音
        /// </summary>
        public byte[]? SoundBytes
        {
            get => _soundBytes;
            set => SetProperty(ref _soundBytes, value);
        }

        /// <summary>
        /// 声音文件名
        /// </summary>
        [DisplayName("声音文件名"), MemberNotNull, ExcelInfo(Width = 5000)]
        public string? SoundName
        {
            get => _soundName;
            set => SetProperty(ref _soundName, value);
        }

        /// <summary>获取或设置数据库外部声音资源引用。</summary>
        public string? SoundFileReference
        {
            get => _soundFileReference;
            set => SetProperty(ref _soundFileReference, value);
        }

        /// <summary>
        /// 图标
        /// </summary>
        public ImageSource? Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        /// <summary>
        /// 图标名称
        /// </summary>
        [DisplayName("图标名称"), MemberNotNull, ExcelInfo(Width = 5000)]
        public string IconName
        {
            get => _iconName;
            set => SetProperty(ref _iconName, value);
        }

        /// <summary>获取或设置数据库外部图标资源引用。</summary>
        public string? IconFileReference
        {
            get => _iconFileReference;
            set => SetProperty(ref _iconFileReference, value);
        }

        /// <summary>
        /// 正则表达式
        /// </summary>
        [DisplayName("正则表达式"), MemberNotNull, ExcelInfo(Width = 8000)]
        public string RegexPattern
        {
            get => _regexPattern;
            set => SetProperty(ref _regexPattern, value);
        }

        /// <summary>
        /// 正则列表
        /// </summary>
        public ObservableCollection<LogisticsRegexItemInfoModel> LogisticsRegexItems
        {
            get => _logisticsRegexItems;
            set => SetProperty(ref _logisticsRegexItems, value);
        }
    }
}
