using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.ComponentModel;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Attributes.WinClientAttributes;

namespace JayTom.Dws.Client.Models.ContentInputSettingsModels {

    public class TcpInputBindingInfoModel : BindableBase {
        private string _ipAddress = "127.0.0.1";
        private int _port = 2000;
        private TcpConnectionStatus _connectionStatus = TcpConnectionStatus.Disconnected;
        private bool _isBound;
        private int _num;

        public int Num {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        /// <summary>
        /// Ip
        /// </summary>
        public string IpAddress {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        /// <summary>
        /// 是否已连接
        /// </summary>
        public TcpConnectionStatus ConnectionStatus {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        /// <summary>
        /// 是否已绑定
        /// </summary>
        public bool IsBound {
            get => _isBound;
            set => SetProperty(ref _isBound, value);
        }
    }

    public enum TcpConnectionStatus {

        /// <summary>
        /// 未连接
        /// </summary>
        [Description("未连接"), BackgroundColor("#D3D3D3")]
        Disconnected,

        /// <summary>
        /// 已连接
        /// </summary>
        [Description("已连接"), BackgroundColor("#31C731")]
        Connected,

        /// <summary>
        /// 连接失败
        /// </summary>
        [Description("连接失败"), BackgroundColor("#FF0000")]
        ConnectionFailed,

        /// <summary>
        /// 连接中
        /// </summary>
        [Description("连接中"), BackgroundColor("#FF8C00")]
        Connecting
    }
}