using JayTom.Dws.Data.LocalLog;

namespace JayTom.Dws.CloudApi.Do.Conf {

    public class ExceptionMatchInfoDo {
        public long ExceptionRuleId { get; set; }

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
        /// 异常类型名称
        /// </summary>
        public string ExceptionTypeName { get; set; } = string.Empty;

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