using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Dto.ApiDto {

    public class RoutDataApiDto {

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut { get; set; } = 1000;

        /// <summary>
        /// SignKey
        /// </summary>
        public string SignKey { get; set; } = string.Empty;

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 重试间隔
        /// </summary>
        public int RetryInterval { get; set; }

        /// <summary>
        /// 设备代码
        /// </summary>
        public string DeviceCode { get; set; } = string.Empty;

        /// <summary>
        /// 机构代码
        /// </summary>
        public string OrgCode { get; set; } = string.Empty;
    }
}