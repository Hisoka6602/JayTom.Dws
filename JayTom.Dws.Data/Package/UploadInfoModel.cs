using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    [Table("Data_UploadInfo", Schema = "dbo")]
    public class UploadInfoModel : BasePackageForeignKeyInfoModel {

        /// <summary>
        /// 上传状态(1成功、2失败、0未上传)
        /// </summary>
        [Column("RequestStatus")]
        public UploadStatus RequestStatus { get; set; } = UploadStatus.NotUploaded;

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

        /// <summary>
        /// Api异常类型
        /// </summary>
        [Column("ApiExceptionType")]
        public ApiExceptionType ApiExceptionType { get; set; } = ApiExceptionType.None;
    }

    public enum ApiExceptionType {

        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// 访问超时
        /// </summary>
        Timeout = 1,

        /// <summary>
        /// Url无法访问
        /// </summary>
        UnreachableUrl = 2,

        /// <summary>
        /// 未通过逻辑效验
        /// </summary>
        LogicValidationFailed = 3,

        /// <summary>
        /// 内容解析异常
        /// </summary>
        ContentParsingException = 4,

        /// <summary>
        /// 其他
        /// </summary>
        Other = 5
    }

    public enum UploadStatus {

        /// <summary>
        /// 上传成功
        /// </summary>
        Succeeded = 0,

        /// <summary>
        /// 上传失败
        /// </summary>
        Failed = 1,

        /// <summary>
        /// 未上传
        /// </summary>
        NotUploaded = 2
    }
}