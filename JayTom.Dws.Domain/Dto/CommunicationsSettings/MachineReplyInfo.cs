using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.CommunicationsSettings {

    public class MachineReplyInfo {

        /// <summary>
        /// 获取或设置一个值，指示是否启用下位机回复的验证功能。
        /// </summary>
        public bool IsVerificationEnabled { get; set; }

        /// <summary>
        /// 获取或设置验证超时时间（以毫秒为单位）。
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// 获取或设置最大重试次数。
        /// </summary>
        public int MaxRetryCount { get; set; }
    }
}