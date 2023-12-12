using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.CloudDto {

    public class CloudVideoDto {

        /// <summary>
        /// 是否开启云视频上传
        /// </summary>
        public bool IsUseCloudVideoUpload { get; set; }

        /// <summary>
        /// 失败重试次数
        /// </summary>
        public int RetryAttempts { get; set; }

        /// <summary>
        /// 是否自动上传未同步的数据
        /// </summary>
        public bool AutoUploadUnsyncedData { get; set; }

        /// <summary>
        /// 并发数 (1-10)
        /// </summary>
        public int Concurrency { get; set; }

        /// <summary>
        /// 是否上传扫码图
        /// </summary>
        public bool UploadScanImage { get; set; }

        /// <summary>
        ///  是否上传全景图
        /// </summary>
        public bool UploadPanoramaImage { get; set; }

        /// <summary>
        /// url
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 请求超时时间
        /// </summary>
        public int RequestTimeout { get; set; }

        /// <summary>
        /// 节点名称
        /// </summary>
        public string NodeName { get; set; } = string.Empty;

        /// <summary>
        ///  登录名
        /// </summary>
        public string LoginName { get; set; } = string.Empty;
    }
}