using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Dto.CloudApiDto {

    public class ExceptionRuleDto {
        public long Id { get; set; }

        /// <summary>
        /// 包含关键字
        /// </summary>
        public string Keywords { get; set; } = string.Empty;

        /// <summary>
        /// 自定义正则表达式
        /// </summary>
        public string CustomRegex { get; set; } = string.Empty;

        /// <summary>
        /// 数据源
        /// </summary>
        public int DataSource { get; set; }

        /// <summary>
        /// 异常类型唯一标识符
        /// </summary>
        public long ExceptionTypeId { get; set; }

        /// <summary>
        /// 判断优先级
        /// </summary>
        public int Priority { get; set; }
    }
}