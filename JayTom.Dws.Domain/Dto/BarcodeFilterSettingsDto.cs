using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto {

    public class BarcodeFilterSettingsDto {

        /// <summary>
        /// 最小条码位数
        /// </summary>
        public int MinimumLength { get; set; }

        /// <summary>
        /// 最大条码位数
        /// </summary>
        public int MaximumLength { get; set; }

        /// <summary>
        /// 开头字符类型
        /// </summary>
        public CharacterType StartCharacterType { get; set; }

        /// <summary>
        /// 结尾字符类型
        /// </summary>
        public CharacterType EndCharacterType { get; set; }

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
        /// 扫码时间间隔
        /// </summary>
        public int ScanInterval { get; set; }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegularExpression { get; set; } = string.Empty;

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