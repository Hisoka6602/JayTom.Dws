using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Dto {

    public class BarcodeFilterSettingsDto {

        /// <summary>
        /// 常规过滤
        /// </summary>
        public BasicFilterInfo BasicFilterInfo { get; set; } = new();

        /// <summary>
        /// 扫码时间间隔
        /// </summary>
        public int ScanInterval { get; set; } = 1000;

        /// <summary>
        /// 重复条码过滤数量
        /// </summary>
        public int DuplicateBarcodeFilterCount { get; set; }

        /// <summary>
        /// 过滤输出类型
        /// </summary>
        public FilterOutputType FilterOutputType { get; set; } = FilterOutputType.NotOutput;

        /// <summary>
        /// 融合超时时间
        /// </summary>
        public int MergeTimeout { get; set; } = 300;

        /// <summary>
        /// 多条码分隔符
        /// </summary>
        public string MultiBarcodeDelimiter { get; set; } = "_";

        /// <summary>
        /// 过滤类别
        /// </summary>
        public BarCodeFilterOptions BarCodeFilterOptions { get; set; } = BarCodeFilterOptions.None;

        /// <summary>
        /// 是否使用正则替换
        /// </summary>
        public bool IsUseCustomRegexReplacement { get; set; }

        /// <summary>
        /// 是否使用过滤条码码种类
        /// </summary>
        public bool IsUseFilteredBarcodeTypes { get; set; }

        /// <summary>
        /// 自定义正则表达式列表
        /// </summary>
        public List<CustomRegexFilterInfo> CustomRegexFilterItems { get; set; } = new();

        /// <summary>
        /// 自定义正则表达式替换列表
        /// </summary>
        public List<CustomRegexReplacementInfo> CustomRegexReplacementItems { get; set; } = new();
    }

    public class BasicFilterInfo {

        /// <summary>
        /// 最小条码位数
        /// </summary>
        public int MinimumLength { get; set; } = 10;

        /// <summary>
        /// 最大条码位数
        /// </summary>
        public int MaximumLength { get; set; } = 22;

        /// <summary>
        /// 开头字符类型
        /// </summary>
        public CharacterType StartCharacterType { get; set; } = CharacterType.Alphanumeric;

        /// <summary>
        /// 结尾字符类型
        /// </summary>
        public CharacterType EndCharacterType { get; set; } = CharacterType.Number;

        /// <summary>
        /// 不能包含的字符
        /// </summary>
        public string DisallowedCharacters { get; set; } = string.Empty;

        /// <summary>
        /// 必须包含的字符
        /// </summary>
        public string RequiredCharacters { get; set; } = string.Empty;

        /// <summary>
        /// 任意字符
        /// </summary>
        public string AnyCharacters { get; set; } = string.Empty;

        /// <summary>
        /// 开头字符
        /// </summary>
        public string AnyStartCodes { get; set; } = string.Empty;

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegularExpression { get; set; } = "(?=^([0-9a-zA-Z]).*)(?=.*([0-9])$)(^.{10,22}$)";
    }

    public class CustomRegexFilterInfo {

        /// <summary>
        /// 是否生效
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegexPattern { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; } = string.Empty;
    }

    public class CustomRegexReplacementInfo {

        /// <summary>
        /// 是否生效
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegexPattern { get; set; } = string.Empty;

        /// <summary>
        /// 替换的内容
        /// </summary>
        public string ReplaceContent { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; } = string.Empty;
    }

    /// <summary>
    /// 过滤类别
    /// </summary>
    public enum BarCodeFilterOptions {

        /// <summary>
        /// 不过滤
        /// </summary>
        None = 0,

        /// <summary>
        /// 常规过滤
        /// </summary>
        BasicFilter = 1,

        /// <summary>
        /// 自定义正则过滤
        /// </summary>
        CustomRegexFilter = 2,
    }

    public enum CharacterType {
        Alphanumeric = 0, // 字母或数字
        Letter = 1,       // 字母
        Number = 2,       // 数字
        Any = 3           //任意
    }

    public enum FilterOutputType {
        NotOutput, // 不输出
        Filtered,  // 过滤输出
        NoRead     // 不可读
    }
}