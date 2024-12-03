using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    [Table("Data_ApiInfo", Schema = "dbo")]
    public class ApiInfoModel : BasePackageForeignKeyInfoModel {

        [Column("ApiMethodName"), Required]
        public string ApiMethodName { get; set; } = string.Empty;

        /// <summary>
        /// 上传内容
        /// </summary>
        [Column("RequestContent"), Required]
        public string RequestContent { get; set; } = string.Empty;

        /// <summary>
        /// 响应内容
        /// </summary>
        [Column("ResponseContent")]
        public string ResponseContent { get; set; } = string.Empty;

        /// <summary>
        /// 请求时间
        /// </summary>
        [Column("RequestTime"), Required]
        public DateTime RequestTime { get; set; }

        /// <summary>
        /// 响应时间
        /// </summary>
        [Column("ResponseTime"), Required]
        public DateTime ResponseTime { get; set; }

        /// <summary>
        /// 耗时(秒)
        /// </summary>
        [Column("DurationInSeconds"), Required]
        public double DurationInSeconds { get; set; }

        /// <summary>
        /// 接口参数
        /// </summary>
        [Column("InterfaceParameters")]
        public string InterfaceParameters { get; set; } = string.Empty;

        /// <summary>
        /// 请求地址
        /// </summary>
        [Column("RequestUrl"), Required]
        public string RequestUrl { get; set; } = string.Empty;

        /// <summary>
        /// 异常信息
        /// </summary>
        [Column("ExceptionMessage")]
        public string ExceptionMessage { get; set; } = string.Empty;

        /// <summary>
        /// 是否格口请求
        /// </summary>
        [Column("IsExitRequest")]
        public bool IsExitRequest { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [Column("RequestStatus")]
        public UploadStatus RequestStatus { get; set; } = UploadStatus.NotUploaded;
    }
}