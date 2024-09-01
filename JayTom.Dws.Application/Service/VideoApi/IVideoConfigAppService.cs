using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;

namespace JayTom.Dws.Application.Service.VideoApi {

    public interface IVideoConfigAppService {

        /// <summary>
        /// 获取Video配置
        /// </summary>
        /// <param name="settingsName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> GetVideoConfig(string settingsName, CancellationToken token = default);

        /// <summary>
        /// 设置Video配置
        /// </summary>
        /// <param name="settingsName"></param>
        /// <param name="configJson"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> SetVideoConfig(string settingsName, string configJson, CancellationToken token = default);
    }
}