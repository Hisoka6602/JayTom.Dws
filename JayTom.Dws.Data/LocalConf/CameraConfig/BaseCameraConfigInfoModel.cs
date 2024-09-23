using System.ComponentModel;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.CameraConfig {

    public class BaseCameraConfigInfoModel : BaseModel {

        /// <summary>
        /// 相机名称
        /// </summary>
        [Column("Name"), Required, InsertOrUpdata]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 自定义相机名称
        /// </summary>
        [Column("CustomName"), InsertOrUpdata]
        public string CustomName { get; set; } = string.Empty;

        /// <summary>
        /// 相机序列号
        /// </summary>
        [Column("SerialNumber"), Required, UpdateBy]
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 相机型号
        /// </summary>
        [Column("Model"), InsertOrUpdata]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 相机固件版本
        /// </summary>
        [Column("Version"), InsertOrUpdata]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 相机 IP 地址
        /// </summary>
        [Column("IpAddress"), InsertOrUpdata]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 相机类型
        /// </summary>
        [Column("CameraType"), Required, InsertOrUpdata]
        public int CameraType { get; set; }

        /// <summary>
        /// 连接方式
        /// </summary>
        [Column("ConnectionType"), Required, InsertOrUpdata]
        public int ConnectionType { get; set; } = 0;

        /// <summary>
        /// 相机显示方式
        /// </summary>
        [Column("CameraDisplayStatus"), Required, InsertOrUpdata]
        public CameraDisplayStatus CameraDisplayStatus { get; set; } = CameraDisplayStatus.Visible;
    }

    public enum CameraDisplayStatus {

        /// <summary>
        /// 显示
        /// </summary>
        [Description("显示")]
        Visible,

        /// <summary>
        /// 隐藏
        /// </summary>
        [Description("隐藏")]
        Hidden
    }
}