using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.CommunicationsSettingsModel;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub;

namespace JayTom.Dws.Client.Models.PackageSorting {
    public class CommunicationConnectionItemInfoModel : BasePackageSortingItemInfoModel {
        private string _connectionName = string.Empty;
        private bool _isActive;
        private CommunicationsTypeInfoModel _communicationType = new() {
            Name = "None",
            Value = CommunicationsType.None
        };
        private SerialPortConfigItemInfoModel? _serialPortConfigInfo = new();
        private TcpConnectionConfigItemInfoModel? _tcpConnectionConfigInfo = new();
        private CommunicationProtocolInfoModel _communicationProtocol = new();
        private bool _isUsePackageValidityPeriod;
        private int _validityPeriodInMilliseconds;
        private bool _isAutoReconnect;
        private int _maxReconnectAttempts;
        private DeviceExtensionConfigItemInfoModel? _deviceExtensionConfigInfo = new();
        private HeartbeatConfigItemInfoModel? _heartbeatConfigInfo = new();
        private int _connectionCount;

        /// <summary>
        /// 连接名称
        /// </summary>
        public string ConnectionName {
            get => _connectionName;
            set => SetProperty(ref _connectionName, value);
        }

        /// <summary>
        /// 是否生效
        /// </summary>
        public bool IsActive {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// 通讯类型
        /// </summary>
        public CommunicationsTypeInfoModel CommunicationType {
            get => _communicationType;
            set => SetProperty(ref _communicationType, value);
        }

        /// <summary>
        /// 串口配置
        /// </summary>
        public SerialPortConfigItemInfoModel? SerialPortConfigInfo {
            get => _serialPortConfigInfo;
            set => SetProperty(ref _serialPortConfigInfo, value);
        }

        /// <summary>
        /// Tcp配置
        /// </summary>
        public TcpConnectionConfigItemInfoModel? TcpConnectionConfigInfo {
            get => _tcpConnectionConfigInfo;
            set => SetProperty(ref _tcpConnectionConfigInfo, value);
        }

        /*/// <summary>
        /// Usb配置
        /// </summary>
        public UsbCommunicationConfigInfo UsbCommunicationConfigInfo { get; set; }

        /// <summary>
        /// Can总线配置
        /// </summary>
        public CanBusCommunicationConfigInfo CanBusCommunicationConfigInfo { get; set; }*/

        /// <summary>
        /// 通讯协议
        /// </summary>
        public CommunicationProtocolInfoModel CommunicationProtocol {
            get => _communicationProtocol;
            set => SetProperty(ref _communicationProtocol, value);
        }

        /// <summary>
        /// 是否使用包裹有效周期
        /// </summary>
        public bool IsUsePackageValidityPeriod {
            get => _isUsePackageValidityPeriod;
            set => SetProperty(ref _isUsePackageValidityPeriod, value);
        }

        /// <summary>
        /// 有效时间
        /// </summary>
        public int ValidityPeriodInMilliseconds {
            get => _validityPeriodInMilliseconds;
            set => SetProperty(ref _validityPeriodInMilliseconds, value);
        }

        /// <summary>
        /// 是否自动重连
        /// </summary>
        public bool IsAutoReconnect {
            get => _isAutoReconnect;
            set => SetProperty(ref _isAutoReconnect, value);
        }

        /// <summary>
        /// 重连最大重试次数
        /// </summary>
        public int MaxReconnectAttempts {
            get => _maxReconnectAttempts;
            set => SetProperty(ref _maxReconnectAttempts, value);
        }

        /// <summary>
        /// 下位机设置
        /// </summary>
        public DeviceExtensionConfigItemInfoModel? DeviceExtensionConfigInfo {
            get => _deviceExtensionConfigInfo;
            set => SetProperty(ref _deviceExtensionConfigInfo, value);
        }

        /// <summary>
        /// 心跳包设置
        /// </summary>
        public HeartbeatConfigItemInfoModel? HeartbeatConfigInfo {
            get => _heartbeatConfigInfo;
            set => SetProperty(ref _heartbeatConfigInfo, value);
        }

        /// <summary>
        /// 连接数
        /// </summary>
        public int ConnectionCount {
            get => _connectionCount;
            set => SetProperty(ref _connectionCount, value);
        }

        /// <summary>
        /// 修改
        /// </summary>
        public ICommand? ModifyCommand { get; set; }

        /// <summary>
        /// 删除
        /// </summary>
        public ICommand? DeleteCommand { get; set; }
    }
}