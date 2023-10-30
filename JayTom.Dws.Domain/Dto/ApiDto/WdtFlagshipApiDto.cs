using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.ApiDto {

    public class WdtFlagshipApiDto {

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Key
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// appsecret
        /// </summary>
        public string Appsecret { get; set; } = string.Empty;

        /// <summary>
        /// sid
        /// </summary>
        public string Sid { get; set; } = string.Empty;

        /// <summary>
        /// method
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// v版本号
        /// </summary>
        public string V { get; set; } = string.Empty;

        /// <summary>
        /// salt(加密)
        /// </summary>
        public string Salt { get; set; } = string.Empty;

        /// <summary>
        /// 打包员Id
        /// </summary>
        public int PackagerId { get; set; }

        /// <summary>
        /// 打包台名称
        /// </summary>
        public string OperateTableName { get; set; } = string.Empty;

        /// <summary>
        /// 是否强制称重
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut { get; set; } = 1000;
    }
}