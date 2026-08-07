using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Service.DefaultConfiguration
{

    public interface IDefaultConfigurationService
    {

        /// <summary>
        /// 写默认配置
        /// </summary>
        Task WriteDefaultConfiguration();
    }
}