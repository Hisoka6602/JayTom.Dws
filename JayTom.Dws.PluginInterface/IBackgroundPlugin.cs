using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;

namespace JayTom.Dws.PluginInterface {

    /// <summary>
    /// 后台运行服务插件
    /// </summary>
    public interface IBackgroundPlugin : IHostedService, IPlugin {
        //
    }
}