using JayTom.Dws.Client.Models.SettingsCommomModels;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.VolumeSettingsModel {
    public class VolumeInformationRequesterInfoModel : BindableBase {
        private VolumeTriggerPosition _volumeTriggerPosition = VolumeTriggerPosition.None;
        private int _sendDelay;
        private string _sendContent = string.Empty;
        private TcpSettingsInfo _tcpSettingsInfo = new();
        private SerialPortSettingsInfoModel _serialPortSettingsInfo = new();
        private VolumeRequesterType _volumeRequesterType;
        private int _sendCount;
        private int _sendInterval;

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
        /// 发送次数
        /// </summary>
        public int SendCount {
            get => _sendCount;
            set => SetProperty(ref _sendCount, value);
        }

        /// <summary>
        /// 发送间隔
        /// </summary>
        public int SendInterval {
            get => _sendInterval;
            set => SetProperty(ref _sendInterval, value);
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