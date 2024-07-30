namespace JayTom.Dws.CloudApiClient.Data.Models.ConfigModels {

    public class ExceptionMatchItemModel {

        /// <summary>
        /// 编号
        /// </summary>
        public int Num { get; set; }

        /// <summary>
        /// 异常匹配表唯一标识符
        /// </summary>
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
        public DataSourceType DataSource { get; set; } = DataSourceType.ResponseContent;

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

    public enum DataSourceType {

        /// <summary>
        /// 条码
        /// </summary>
        Barcode,

        /// <summary>
        ///提交内容
        /// </summary>
        RequestContent,

        /// <summary>
        /// 响应内容
        /// </summary>
        ResponseContent
    }
}