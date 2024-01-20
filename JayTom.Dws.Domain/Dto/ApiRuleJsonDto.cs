using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Domain.Dto
{
    public class ApiRuleJsonDto {

        /// <summary>
        /// Api响应状态
        /// </summary>
        public UploadStatus ResponseStatus { get; set; }

        /// <summary>
        /// 是否使用字符串判断
        /// </summary>
        public bool IsUseStringComparison { get; set; }

        /// <summary>
        /// 是否使用字符串查找
        /// </summary>
        public bool IsUseStringSearch { get; set; }

        /// <summary>
        /// 是否使用Json字段取值
        /// </summary>
        public bool IsUseJsonField { get; set; }

        /// <summary>
        /// 查找字符串内容
        /// </summary>
        public string SearchStringContent { get; set; } = string.Empty;

        /// <summary>
        /// Json字段
        /// </summary>
        public string JsonField { get; set; } = string.Empty;

        /// <summary>
        /// Json字段值
        /// </summary>
        public string JsonFieldValue { get; set; } = string.Empty;
    }
}