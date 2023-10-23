using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.CommunicationsSettingsModel {
    public class HeartbeatInfoModel : BindableBase {
        private bool _isHeartbeatEnabled;
        private string _heartbeatData = string.Empty;
        private int _heartbeatInterval;
        private bool _isHeartbeatActive;

        /// <summary>
        /// 获取或设置一个值，指示是否启用心跳包功能。
        /// </summary>
        public bool IsHeartbeatEnabled {
            get => _isHeartbeatEnabled;
            set => SetProperty(ref _isHeartbeatEnabled, value);
        }

        /// <summary>
        /// 获取或设置心跳包内容。
        /// </summary>
        public string HeartbeatData {
            get => _heartbeatData;
            set => SetProperty(ref _heartbeatData, value);
        }

        /// <summary>
        /// 获取或设置心跳包的发送间隔。
        /// </summary>
        public int HeartbeatInterval {
            get => _heartbeatInterval;
            set => SetProperty(ref _heartbeatInterval, value);
        }

        /// <summary>
        /// 是否主动发送心跳包
        /// </summary>
        public bool IsHeartbeatActive {
            get => _isHeartbeatActive;
            set => SetProperty(ref _isHeartbeatActive, value);
        }
    }
}