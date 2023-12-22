using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.VideoApiClient.Models {

    public class NvrCameraBindingItemInfo : BindableBase {
        private string _ipAddress = string.Empty;
        private int _port;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private int _channel;
        private string _barcodeScannerSerialNumber = string.Empty;

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        /// <summary>
        /// 端口号
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
        /// 通道
        /// </summary>
        public int Channel {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        /// <summary>
        /// 扫码相机序列号
        /// </summary>
        public string BarcodeScannerSerialNumber {
            get => _barcodeScannerSerialNumber;
            set => SetProperty(ref _barcodeScannerSerialNumber, value);
        }
    }
}