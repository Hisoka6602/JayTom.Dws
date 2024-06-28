using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using JayTom.Dws.CrossCutting.SignalR;

namespace JayTom.Dws.SystemStatusMonitorService.SignalR {

    public interface ISystemStatusMonitorMessageHub : IBaseServerMessageHub {

        /// <summary>
        /// 获取版本号
        /// </summary>
        /// <returns>系统版本号</returns>
        [HubMethodName("GetVersion")]
        string GetVersion();

        /// <summary>
        /// 获取版本说明
        /// </summary>
        /// <returns>系统版本说明</returns>
        [HubMethodName("GetVersionDescription")]
        string GetVersionDescription();
    }
}