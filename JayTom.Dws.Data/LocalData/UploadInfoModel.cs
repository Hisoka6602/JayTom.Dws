using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalData {

    [Table("Data_UploadInfo", Schema = "dbo")]
    public class UploadInfoModel : BaseBarCodeForeignKeyInfo {

        /// <summary>
        /// 是否成功
        /// </summary>
        [Column("IsSuccess")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 上传内容
        /// </summary>
        [Column("RequestContent")]
        public string RequestContent { get; set; } = string.Empty;

        /// <summary>
        /// 响应内容
        /// </summary>
        [Column("ResponseContent")]
        public string ResponseContent { get; set; } = string.Empty;

        /// <summary>
        /// 请求时间
        /// </summary>
        [Column("RequestTime")]
        public DateTime RequestTime { get; set; }

        /// <summary>
        /// 响应时间
        /// </summary>
        [Column("ResponseTime")]
        public DateTime ResponseTime { get; set; }

        /// <summary>
        /// 耗时(秒)
        /// </summary>
        [Column("DurationInSeconds")]
        public double DurationInSeconds { get; set; }

        /// <summary>
        /// 接口参数
        /// </summary>
        [Column("InterfaceParameters")]
        public string InterfaceParameters { get; set; } = string.Empty;

        /// <summary>
        /// 请求地址
        /// </summary>
        [Column("RequestUrl")]
        public string RequestUrl { get; set; } = string.Empty;

        /// <summary>
        /// 异常信息
        /// </summary>
        [Column("ExceptionMessage")]
        public string ExceptionMessage { get; set; } = string.Empty;
    }
}