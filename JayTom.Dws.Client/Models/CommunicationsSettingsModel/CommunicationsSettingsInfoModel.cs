using Prism.Mvvm;
using JayTom.Dws.Data.LocalData;
using JayTom.Dws.Client.Models.SettingsCommomModels;

namespace JayTom.Dws.Client.Models.CommunicationsSettingsModel {

    public class CommunicationsSettingsInfoModel : BindableBase {
        private TcpSettingsInfoModel _tcpSettingsInfo = new();
        private SerialPortSettingsInfoModel _serialPortSettingsInfo = new();
        private CommunicationProtocol _protocol = CommunicationProtocol.None;
        private CommunicationsType _type = CommunicationsType.None;
        private MachineReplyInfoModel _machineReplyInfo = new();
        private HeartbeatInfoModel _heartbeatInfo = new();
        private int _packageExpiryTime;
        private bool _isUsePackageExpiry;
        private DeviceControlSettingsInfoModel _deviceControlSettingsInfo = new();

        public TcpSettingsInfoModel TcpSettingsInfo {
            get => _tcpSettingsInfo;
            set => SetProperty(ref _tcpSettingsInfo, value);
        }

        /// <summary>
        /// 串口通讯参数
        /// </summary>
        public SerialPortSettingsInfoModel SerialPortSettingsInfo {
            get => _serialPortSettingsInfo;
            set => SetProperty(ref _serialPortSettingsInfo, value);
        }

        /// <summary>
        /// 通讯协议
        /// </summary>
        public CommunicationProtocol Protocol {
            get => _protocol;
            set => SetProperty(ref _protocol, value);
        }

        /// <summary>
        /// 通讯类型
        /// </summary>
        public CommunicationsType Type {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// 下位机回复
        /// </summary>
        public MachineReplyInfoModel MachineReplyInfo {
            get => _machineReplyInfo;
            set => SetProperty(ref _machineReplyInfo, value);
        }

        /// <summary>
        /// 心跳包
        /// </summary>
        public HeartbeatInfoModel HeartbeatInfo {
            get => _heartbeatInfo;
            set => SetProperty(ref _heartbeatInfo, value);
        }

        /// <summary>
        /// 下位机设置
        /// </summary>
        public DeviceControlSettingsInfoModel DeviceControlSettingsInfo {
            get => _deviceControlSettingsInfo;
            set => SetProperty(ref _deviceControlSettingsInfo, value);
        }

        /// <summary>
        /// 是否使用包裹过期
        /// </summary>
        public bool IsUsePackageExpiry {
            get => _isUsePackageExpiry;
            set => SetProperty(ref _isUsePackageExpiry, value);
        }

        /// <summary>
        /// 包裹过期时间(设置为0则不验证)
        /// </summary>
        public int PackageExpiryTime {
            get => _packageExpiryTime;
            set => SetProperty(ref _packageExpiryTime, value);
        }
    }
}