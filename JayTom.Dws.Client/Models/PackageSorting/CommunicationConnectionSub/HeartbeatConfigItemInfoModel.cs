using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub
{

    public class HeartbeatConfigItemInfoModel : BasePackageSortingItemInfoModel
    {
        private bool _isHeartbeatActive;
        private string _heartbeatContent = string.Empty;
        private int _heartbeatInterval;
        private bool _isHeartbeatEnabled;
        private bool _isFixedHeartbeatContent;

        /// <summary>
        /// 是否使用心跳包
        /// </summary>
        public bool IsHeartbeatEnabled
        {
            get => _isHeartbeatEnabled;
            set => SetProperty(ref _isHeartbeatEnabled, value);
        }

        /// <summary>
        /// 是否主动发送心跳包
        /// </summary>
        public bool IsHeartbeatActive
        {
            get => _isHeartbeatActive;
            set => SetProperty(ref _isHeartbeatActive, value);
        }

        /// <summary>
        /// 心跳包内容
        /// </summary>
        public string HeartbeatContent
        {
            get => _heartbeatContent;
            set => SetProperty(ref _heartbeatContent, value);
        }

        /// <summary>
        /// 心跳包间隔
        /// </summary>
        public int HeartbeatInterval
        {
            get => _heartbeatInterval;
            set => SetProperty(ref _heartbeatInterval, value);
        }

        /// <summary>
        /// 是否使用固定心跳包内容
        /// </summary>
        public bool IsFixedHeartbeatContent
        {
            get => _isFixedHeartbeatContent;
            set => SetProperty(ref _isFixedHeartbeatContent, value);
        }
    }
}