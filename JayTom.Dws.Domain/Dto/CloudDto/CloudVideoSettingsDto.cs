using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.CloudDto {

    public class CloudVideoSettingsDto {

        /// <summary>
        /// 是否开启云视频上传
        /// </summary>
        public bool IsUseCloudVideoUpload { get; set; }

        /// <summary>
        /// 失败重试次数
        /// </summary>
        public int RetryAttempts { get; set; } = 5;

        /// <summary>
        /// 是否自动上传未同步的数据
        /// </summary>
        public bool AutoUploadUnsyncedData { get; set; }

        /// <summary>
        /// 并发数 (1-10)
        /// </summary>
        public int Concurrency { get; set; } = 2;

        /// <summary>
        /// url
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 请求超时时间
        /// </summary>
        public int RequestTimeout { get; set; } = 2000;

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