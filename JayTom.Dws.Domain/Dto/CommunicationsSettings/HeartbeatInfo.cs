using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.CommunicationsSettings {

    public class HeartbeatInfo {

        /// <summary>
        /// 获取或设置一个值，指示是否启用心跳包功能。
        /// </summary>
        public bool IsHeartbeatEnabled { get; set; }

        /// <summary>
        /// 获取或设置心跳包内容。
        /// </summary>
        public string HeartbeatData { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置心跳包的发送间隔。
        /// </summary>
        public int HeartbeatInterval { get; set; }
    }
}