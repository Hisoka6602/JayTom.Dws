using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {

    [Table("Log_ApiLogInfo", Schema = "dbo")]
    public class ApiLogInfoModel : BaseLogInfoModel {

        /// <summary>
        /// 请求内容
        /// </summary>
        [Column("RequestContent")]
        public string RequestContent { get; set; } = string.Empty; // 请求内容

        /// <summary>
        /// 响应内容
        /// </summary>
        [Column("ResponseContent")]
        public string ResponseContent { get; set; } = string.Empty;

        /// <summary>
        /// 上传时间
        /// </summary>
        [Column("RequestTime")]
        public DateTime RequestTime { get; set; }

        /// <summary>
        /// 返回时间
        /// </summary>
        [Column("ResponseTime")]
        public DateTime ResponseTime { get; set; }

        /// <summary>
        /// 耗时(秒)
        /// </summary>
        [Column("Duration")]
        public double Duration { get; set; }

        /// <summary>
        /// 接口参数
        /// </summary>
        [Column("ApiParameters")]
        public string ApiParameters { get; set; } = string.Empty;

        /// <summary>
        /// 异常信息
        /// </summary>
        [Column("ExceptionMsg")]
        public string ExceptionMsg { get; set; } = string.Empty;

        /// <summary>
        /// Url
        /// </summary>
        [Column("Url")]
        public string Url { get; set; } = string.Empty;
    }
}