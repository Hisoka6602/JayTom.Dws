using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;

namespace JayTom.Dws.Domain.Dto.ApiDto {

    public class DefaultApiDto {

        /// <summary>
        /// 数据模板
        /// </summary>
        public List<ItemTemplateInfo> DataTemplate { get; set; } = new();

        /// <summary>
        /// 是否使用Json上传
        /// </summary>
        public bool IsUseJsonUpload { get; set; }

        /// <summary>
        /// 字符串模板
        /// </summary>
        public string StringTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Json模板
        /// </summary>
        public string JsonTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 请求超时时间
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 验证模式
        /// </summary>
        public ResponseValidationMode ValidationMode { get; set; } = ResponseValidationMode.StringContains;

        /// <summary>
        /// 完全匹配的内容
        /// </summary>
        public string CompleteMatch { get; set; } = string.Empty;

        /// <summary>
        /// 包含字符串的内容
        /// </summary>
        public string StringContains { get; set; } = string.Empty;

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegularExpression { get; set; } = string.Empty;
    }

    public enum ResponseValidationMode {

        /// <summary>
        /// 完全匹配
        /// </summary>
        CompleteMatch = 0,

        /// <summary>
        /// 包含字符串
        /// </summary>
        StringContains = 1,

        /// <summary>
        /// 正则表达式
        /// </summary>
        RegularExpression = 2
    }
}