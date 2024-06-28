using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using JayTom.Dws.CrossCutting.SignalR;

namespace JayTom.Dws.SystemStatusMonitorService.SignalR {

    public class SystemStatusMonitorMessageHub : BaseServerMessageHub, ISystemStatusMonitorMessageHub {

        public SystemStatusMonitorMessageHub(IHubContext<BaseServerMessageHub> hubContext, ILogger<BaseServerMessageHub> logger) : base(hubContext, logger) {
        }

        public string GetVersion() {
            return "1.0.0.0";
        }

        public string GetVersionDescription() {
            return "Initial release version.";
        }
    }
}