using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.Cameras {

    public class NvrCameraMappingItemInfoModel : BindableBase {
        private string _ipAddress = string.Empty;
        private int _port;
        private string _username = string.Empty;
        private string _serialNumber = string.Empty;
        private string _displayIdentifier = string.Empty;
        private SourceType _bindingSource = SourceType.None;
        private string _remarks = string.Empty;
        private int _channel;
        private int _num;

        public int Num {
            get => _num;
            set => SetProperty(ref _num, value);
        }

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
        /// 输入序列号(来源设备唯一标识)
        /// </summary>
        public string SerialNumber {
            get => _serialNumber;
            set => SetProperty(ref _serialNumber, value);
        }

        /// <summary>
        /// 显示标识
        /// </summary>
        public string DisplayIdentifier {
            get => _displayIdentifier;
            set => SetProperty(ref _displayIdentifier, value);
        }

        /// <summary>
        /// 绑定源
        /// </summary>
        public SourceType BindingSource {
            get => _bindingSource;
            set => SetProperty(ref _bindingSource, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks {
            get => _remarks;
            set => SetProperty(ref _remarks, value);
        }

        /// <summary>
        /// 取流通道
        /// </summary>
        public int Channel {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }
    }
}