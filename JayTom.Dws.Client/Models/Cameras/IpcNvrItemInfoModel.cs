using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Camera;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Attributes.WinClientAttributes;

namespace JayTom.Dws.Client.Models.Cameras {

    public class IpcNvrItemInfoModel : BaseCameraItemInfoModel {
        private int _port;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private int _channel;
        private DeviceType _type;
        private int _channelCount;
        private NvrStatus _status = NvrStatus.Unverified;
        private ObservableCollection<BarcodeScannerCameraItemInfoModel> _bindingCameraSerialNumbers = new();
        private bool _isSelect;
        private bool _isConfigured;
        private string _deviceName = string.Empty;

        public bool IsSelect {
            get => _isSelect;
            set => SetProperty(ref _isSelect, value);
        }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>
        /// 取流通道
        /// </summary>
        public int Channel {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        /// <summary>
        /// 类型
        /// </summary>
        public DeviceType Type {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// 通道数
        /// </summary>
        public int ChannelCount {
            get => _channelCount;
            set => SetProperty(ref _channelCount, value);
        }

        /// <summary>
        /// 状态
        /// </summary>
        public NvrStatus Status {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// 是否存在配置中
        /// </summary>
        public bool IsConfigured {
            get => _isConfigured;
            set => SetProperty(ref _isConfigured, value);
        }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName {
            get => _deviceName;
            set => SetProperty(ref _deviceName, value);
        }

        /// <summary>
        /// 已绑定相机列表
        /// </summary>
        public ObservableCollection<BarcodeScannerCameraItemInfoModel> BindingCameraSerialNumbers {
            get => _bindingCameraSerialNumbers;
            set => SetProperty(ref _bindingCameraSerialNumbers, value);
        }
    }

    public enum NvrStatus {

        /// <summary>
        /// 离线状态
        /// </summary>
        [Description("离线"), BackgroundColor("#D3D3D3")]
        Offline,

        /// <summary>
        /// 未验证状态
        /// </summary>
        [Description("未验证"), BackgroundColor("#A9A9A9")]
        Unverified,

        /// <summary>
        /// 登录失败状态
        /// </summary>
        [Description("登录失败"), BackgroundColor("#FF0000")]
        LoginFailed,

        /// <summary>
        /// 在线状态
        /// </summary>
        [Description("在线"), BackgroundColor("#31C731")]
        Online,

        /// <summary>
        /// 登录中状态
        /// </summary>
        [Description("登录中"), BackgroundColor("#FF8C00")]
        LoggingIn
    }
}