using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Legacy.Contracts.Dto.CloudDto {

    public class NvrCameraBindingDto {

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

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
        /// 通道
        /// </summary>
        public int Channel { get; set; }

        /// <summary>
        /// 扫码相机序列号
        /// </summary>
        public string BarcodeScannerSerialNumber { get; set; } = string.Empty;
    }
}