using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Client.Models.SettingsCommomModels;

namespace JayTom.Dws.Client.Models.VolumeSettingsModel {

    public class VolumeInformationRequesterInfoModel : BindableBase {
        private VolumeTriggerPosition _volumeTriggerPosition = VolumeTriggerPosition.None;
        private int _sendDelay;
        private string _sendContent = string.Empty;
        private TcpSettingsInfo _tcpSettingsInfo = new();
        private SerialPortSettingsInfoModel _serialPortSettingsInfo = new();
        private VolumeRequesterType _volumeRequesterType;

        /// <summary>
        /// 触发位置
        /// </summary>
        public VolumeTriggerPosition VolumeTriggerPosition {
            get => _volumeTriggerPosition;
            set => SetProperty(ref _volumeTriggerPosition, value);
        }

        /// <summary>
        /// 发送延迟（单位：毫秒）
        /// </summary>
        public int SendDelay {
            get => _sendDelay;
            set => SetProperty(ref _sendDelay, value);
        }

        /// <summary>
        /// 发送内容
        /// </summary>
        public string SendContent {
            get => _sendContent;
            set => SetProperty(ref _sendContent, value);
        }

        /// <summary>
        /// 发送模式
        /// </summary>
        public VolumeRequesterType VolumeRequesterType {
            get => _volumeRequesterType;
            set => SetProperty(ref _volumeRequesterType, value);
        }

        /// <summary>
        /// Tcp设置
        /// </summary>
        public TcpSettingsInfo TcpSettingsInfo {
            get => _tcpSettingsInfo;
            set => SetProperty(ref _tcpSettingsInfo, value);
        }

        /// <summary>
        /// 串口设置
        /// </summary>
        public SerialPortSettingsInfoModel SerialPortSettingsInfo {
            get => _serialPortSettingsInfo;
            set => SetProperty(ref _serialPortSettingsInfo, value);
        }
    }
}