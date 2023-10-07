using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Dto.CommunicationsSettings;
using JayTom.Dws.Client.Models.SettingsCommomModels;

namespace JayTom.Dws.Client.Models.CommunicationsSettingsModel {

    public class CommunicationsSettingsInfoModel : BindableBase {
        private TcpSettingsInfoModel _tcpSettingsInfo = new();
        private SerialPortSettingsInfoModel _serialPortSettingsInfo = new();
        private CommunicationProtocol _protocol = CommunicationProtocol.None;
        private CommunicationsType _type = CommunicationsType.None;
        private MachineReplyInfoModel _machineReplyInfo = new();
        private HeartbeatInfoModel _heartbeatInfo = new();

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
    }
}