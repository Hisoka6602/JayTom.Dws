using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.IpcNvrConfig {

    //需要有自己单独的仓储
    [Table("Conf_IpcNvrConfigInfo", Schema = "dbo")]
    public class IpcNvrConfigInfoModel : BaseModel {

        /// <summary>
        /// IP地址
        /// </summary>
        [Column("IpAddress"), Required, UpdateBy]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 端口号
        /// </summary>
        [Column("Port"), Required, InsertOrUpdata]
        public int Port { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [Column("Username"), Required, InsertOrUpdata]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        [Column("Password"), Required, InsertOrUpdata]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 通道
        /// </summary>
        [Column("Channel"), Required, InsertOrUpdata]
        public int Channel { get; set; }

        /// <summary>
        /// 类型 [IPC/NVR]
        /// </summary>
        [Column("Type"), Required, InsertOrUpdata]
        public DeviceType Type { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        [Column("Name"), Required, InsertOrUpdata]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 已绑定的相机序列号
        /// </summary>
        [Column("BindingCameraSerialNumbers"), Required, InsertOrUpdata]
        public List<string> BindingCameraSerialNumbers { get; set; } = new();
    }

    public enum DeviceType {

        [Description("IPC相机")]
        IPC,

        [Description("NVR视频设备")]
        NVR
    }
}