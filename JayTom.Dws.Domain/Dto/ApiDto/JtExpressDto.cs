using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.ApiDto {
    public class JtExpressDto {

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; } = "https://opa.jtexpress.com.cn";

        /// <summary>
        /// 账号
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// AppKey
        /// </summary>
        public string AppKey { get; set; } = "default";

        /// <summary>
        /// AppSecret
        /// </summary>
        public string AppSecret { get; set; } = "default";

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut { get; set; } = 1000;

        /// <summary>
        /// 条码类型
        /// </summary>
        public string ScanTypeCode { get; set; } = string.Empty;

        /// <summary>
        /// 运输方式id
        /// </summary>
        public string TransportTypeCode { get; set; } = string.Empty;

        /// <summary>
        /// 设备编号
        /// </summary>
        public string ScanPda { get; set; } = string.Empty;

        /// <summary>
        /// 扫描类型
        /// </summary>
        public int ScanType { get; set; }

        /// <summary>
        /// 重量标识
        /// </summary>
        public string WeightFlag { get; set; } = string.Empty;

        /// <summary>
        /// Url
        /// </summary>
        public string SegmentCodeUrl { get; set; } = "https://opa.jtexpress.com.cn";

        /// <summary>
        /// 超时
        /// </summary>
        public int SegmentCodeTimeOut { get; set; } = 1000;

        /// <summary>
        /// 业务类型
        /// </summary>
        public BusinessType BusinessType { get; set; }

        /// <summary>
        /// 是否三段码返回后上传
        /// </summary>
        public bool IsUploadAfterReturn { get; set; }
        /// <summary>
        /// 是否启用拦截件
        /// </summary>

        public bool InterceptorEnabled { get; set; }
    }

    /// <summary>
    /// 业务类型
    /// </summary>
    public enum BusinessType {

        /// <summary>
        /// 到件扫描
        /// </summary>
        ArrivalScan = 0,

        /// <summary>
        /// 出仓扫描
        /// </summary>
        DepartureScan = 1
    }
}
