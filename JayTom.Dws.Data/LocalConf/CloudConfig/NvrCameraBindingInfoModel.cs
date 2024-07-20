using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Attributes;
using System.Collections.Generic;
using System.Text.Json.Serialization;
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

        /*/// <summary>
        /// 扫码相机序列号
        /// </summary>
        [Column("BarcodeScannerSerialNumber"), Required]
        public string BarcodeScannerSerialNumber { get; set; } = string.Empty;*/

        [Column("ScannerCameraConfigInfoModelId"), JsonIgnore]
        public long ScannerCameraConfigInfoModelId { get; set; }

        [ForeignKey("Id")]
        public virtual BarcodeScannerCameraConfigInfoModel? BarcodeScannerCameraConfigInfoModel { get; set; }
    }
}