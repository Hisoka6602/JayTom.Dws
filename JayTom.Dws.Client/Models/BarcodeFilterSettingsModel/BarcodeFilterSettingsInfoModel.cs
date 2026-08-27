using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Plugin.Excel.Attributes;

namespace JayTom.Dws.Client.Models.BarcodeFilterSettingsModel
{

    public class BarcodeFilterSettingsInfoModel : BindableBase
    {
        private BasicFilterInfoModel _basicFilterInfo = new();
        private int _scanInterval;
        private int _duplicateBarcodeFilterCount;
        private FilterOutputType _filterOutputType = FilterOutputType.NotOutput;
        private int _mergeTimeout = 300;
        private string _multiBarcodeDelimiter = "_";
        private BarCodeFilterOptions _barCodeFilterOptions = BarCodeFilterOptions.None;
        private bool _isUseCustomRegexReplacement;
        private bool _isUseFilteredBarcodeTypes;
        private ObservableCollection<CustomRegexFilterItemInfoModel> _customRegexFilterItems = new();
        private ObservableCollection<CustomRegexReplacementItemInfoModel> _customRegexReplacementItems = new();

        public BasicFilterInfoModel BasicFilterInfo
        {
            get => _basicFilterInfo;
            set => SetProperty(ref _basicFilterInfo, value);
        }

        /// <summary>
        /// 扫码时间间隔
        /// </summary>
        public int ScanInterval
        {
            get => _scanInterval;
            set => SetProperty(ref _scanInterval, value);
        }

        /// <summary>
        /// 重复条码过滤数量
        /// </summary>
        public int DuplicateBarcodeFilterCount
        {
            get => _duplicateBarcodeFilterCount;
            set => SetProperty(ref _duplicateBarcodeFilterCount, value);
        }

        /// <summary>
        /// 过滤输出类型
        /// </summary>
        public FilterOutputType FilterOutputType
        {
            get => _filterOutputType;
            set => SetProperty(ref _filterOutputType, value);
        }

        /// <summary>
        /// 融合超时时间
        /// </summary>
        public int MergeTimeout
        {
            get => _mergeTimeout;
            set => SetProperty(ref _mergeTimeout, value);
        }

        /// <summary>
        /// 多条码分隔符
        /// </summary>
        public string MultiBarcodeDelimiter
        {
            get => _multiBarcodeDelimiter;
            set => SetProperty(ref _multiBarcodeDelimiter, value);
        }

        /// <summary>
        /// 过滤类别
        /// </summary>
        public BarCodeFilterOptions BarCodeFilterOptions
        {
            get => _barCodeFilterOptions;
            set => SetProperty(ref _barCodeFilterOptions, value);
        }

        /// <summary>
        /// 是否使用正则替换
        /// </summary>
        public bool IsUseCustomRegexReplacement
        {
            get => _isUseCustomRegexReplacement;
            set => SetProperty(ref _isUseCustomRegexReplacement, value);
        }

        /// <summary>
        /// 是否使用过滤条码码种类
        /// </summary>
        public bool IsUseFilteredBarcodeTypes
        {
            get => _isUseFilteredBarcodeTypes;
            set => SetProperty(ref _isUseFilteredBarcodeTypes, value);
        }

        /// <summary>
        /// 自定义正则表达式列表
        /// </summary>
        public ObservableCollection<CustomRegexFilterItemInfoModel> CustomRegexFilterItems
        {
            get => _customRegexFilterItems;
            set => SetProperty(ref _customRegexFilterItems, value);
        }

        /// <summary>
        /// 自定义正则表达式替换列表
        /// </summary>
        public ObservableCollection<CustomRegexReplacementItemInfoModel> CustomRegexReplacementItems
        {
            get => _customRegexReplacementItems;
            set => SetProperty(ref _customRegexReplacementItems, value);
        }
    }

    public class BasicFilterInfoModel : BindableBase
    {
        private int _minimumLength;
        private int _maximumLength;
        private CharacterType _startCharacterType = CharacterType.Any;
        private CharacterType _endCharacterType = CharacterType.Any;
        private string _disallowedCharacters = string.Empty;
        private string _requiredCharacters = string.Empty;
        private string _anyCharacters = string.Empty;
        private string _anyStartCodes = string.Empty;
        private string _regularExpression = string.Empty;

        /// <summary>
        /// 最小条码位数
        /// </summary>
        public int MinimumLength
        {
            get => _minimumLength;
            set => SetProperty(ref _minimumLength, value);
        }

        /// <summary>
        /// 最大条码位数
        /// </summary>
        public int MaximumLength
        {
            get => _maximumLength;
            set => SetProperty(ref _maximumLength, value);
        }

        /// <summary>
        /// 开头字符类型
        /// </summary>
        public CharacterType StartCharacterType
        {
            get => _startCharacterType;
            set => SetProperty(ref _startCharacterType, value);
        }

        /// <summary>
        /// 结尾字符类型
        /// </summary>
        public CharacterType EndCharacterType
        {
            get => _endCharacterType;
            set => SetProperty(ref _endCharacterType, value);
        }

        /// <summary>
        /// 不能包含的字符
        /// </summary>
        public string DisallowedCharacters
        {
            get => _disallowedCharacters;
            set => SetProperty(ref _disallowedCharacters, value);
        }

        /// <summary>
        /// 必须包含的字符
        /// </summary>
        public string RequiredCharacters
        {
            get => _requiredCharacters;
            set => SetProperty(ref _requiredCharacters, value);
        }

        /// <summary>
        /// 任意字符
        /// </summary>
        public string AnyCharacters
        {
            get => _anyCharacters;
            set => SetProperty(ref _anyCharacters, value);
        }

        /// <summary>
        /// 开头字符
        /// </summary>
        public string AnyStartCodes
        {
            get => _anyStartCodes;
            set => SetProperty(ref _anyStartCodes, value);
        }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegularExpression
        {
            get => _regularExpression;
            set => SetProperty(ref _regularExpression, value);
        }
    }

    public class CustomRegexFilterItemInfoModel : BindableBase
    {
        private bool _isActive;
        private string _regexPattern = string.Empty;
        private int _num;

        [DisplayName("序号"), ExcelInfo(Width = 2000)]
        public int Num
        {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        /// <summary>
        /// 是否生效
        /// </summary>
        [DisplayName("是否生效(1=是、0=否)"), MemberNotNull, ExcelInfo(Width = 6000, IsBooleanToInt = true)]
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// 正则表达式
        /// </summary>
        [DisplayName("正则表达式"), MemberNotNull, ExcelInfo(Width = 20000)]
        public string RegexPattern
        {
            get => _regexPattern;
            set => SetProperty(ref _regexPattern, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注"), MemberNotNull, ExcelInfo(Width = 10000)]
        public string Remarks { get; set; } = string.Empty;
    }

    public class CustomRegexReplacementItemInfoModel : BindableBase
    {
        private bool _isActive;
        private string _regexPattern = string.Empty;
        private string _replaceContent = string.Empty;
        private int _num;

        [DisplayName("序号"), ExcelInfo(Width = 2000)]
        public int Num
        {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        /// <summary>
        /// 是否生效
        /// </summary>
        [DisplayName("是否生效(1=是、0=否)"), MemberNotNull, ExcelInfo(Width = 6000, IsBooleanToInt = true)]
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// 正则表达式
        /// </summary>
        [DisplayName("正则表达式"), MemberNotNull, ExcelInfo(Width = 20000)]
        public string RegexPattern
        {
            get => _regexPattern;
            set => SetProperty(ref _regexPattern, value);
        }

        /// <summary>
        /// 替换的内容
        /// </summary>
        [DisplayName("替换的内容"), MemberNotNull, ExcelInfo(Width = 6000)]
        public string ReplaceContent
        {
            get => _replaceContent;
            set => SetProperty(ref _replaceContent, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注"), MemberNotNull, ExcelInfo(Width = 10000)]
        public string Remarks { get; set; } = string.Empty;
    }
}