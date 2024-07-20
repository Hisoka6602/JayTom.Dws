using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Camera;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.Cameras {

    public class IpcNvrItemInfoModel : BaseCameraItemInfoModel {

        /// <summary>
        /// 端口
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
        /// 取流通道
        /// </summary>
        public int Channel { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public DeviceType Type { get; set; }

        /// <summary>
        /// 通道数
        /// </summary>
        public int ChannelCount { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public NvrStatus Status { get; set; }

        /// <summary>
        /// 已绑定相机列表
        /// </summary>
        public ICollection<BarcodeScannerCameraItemInfoModel> BindingCameraSerialNumbers { get; set; } = new List<BarcodeScannerCameraItemInfoModel>();
    }

    public enum NvrStatus {

        /// <summary>
        /// 离线状态
        /// </summary>
        [Description("离线"),]
        Offline,

        /// <summary>
        /// 未验证状态
        /// </summary>
        [Description("未验证")]
        Unverified,

        /// <summary>
        /// 登录失败状态
        /// </summary>
        [Description("登录失败")]
        LoginFailed,

        /// <summary>
        /// 在线状态
        /// </summary>
        [Description("在线")]
        Online
    }
}