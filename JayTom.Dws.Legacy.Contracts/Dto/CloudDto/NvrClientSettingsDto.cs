using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Dto.CloudDto {

    public class NvrClientSettingsDto {

        /// <summary>
        /// IP地址
        /// </summary>
        public string Ip { get; set; } = string.Empty;

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 条码水印
        /// </summary>
        public bool IsUseBarcodeWatermark { get; set; }

        /// <summary>
        /// 最长水印时间
        /// </summary>
        public int MaxWatermarkTime { get; set; }
    }
}