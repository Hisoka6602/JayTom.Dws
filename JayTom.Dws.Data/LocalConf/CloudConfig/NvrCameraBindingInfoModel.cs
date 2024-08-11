using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.CloudConfig {
    //这里的外键需要连扫码相机(可空外键)

    [Table("Conf_NvrCameraBindingInfo", Schema = "dbo")]
    public class NvrCameraBindingInfoModel : BaseModel {

        /// <summary>
        /// IP地址
        /// </summary>
        [Column("IpAddress"), Required]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 端口号
        /// </summary>
        [Column("Port"), Required]
        public int Port { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [Column("Username"), Required]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        [Column("Password"), Required]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 通道
        /// </summary>
        [Column("Channel"), Required]
        public int Channel { get; set; }

        /// <summary>
        /// 输入序列(来源设备唯一标识)
        /// </summary>
        [Column("SerialNumber")]
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 显示标识
        /// </summary>
        [Column("DisplayIdentifier")]
        public string DisplayIdentifier { get; set; } = string.Empty;

        /// <summary>
        /// 绑定源
        /// </summary>
        [Column("BindingSource")]
        public SourceType BindingSource { get; set; } = SourceType.None;

        /// <summary>
        /// 备注
        /// </summary>
        [Column("Remarks")]
        public string Remarks { get; set; } = string.Empty;
    }
}