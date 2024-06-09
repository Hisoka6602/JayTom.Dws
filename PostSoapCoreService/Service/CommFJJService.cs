using NLog;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PostSoapCoreService.Service {

    public class CommFjjService : ICommFjjService {
        private readonly ILogger<CommFjjService> _logger;

        public CommFjjService(ILogger<CommFjjService> logger) {
            this._logger = logger;
        }

        public Task<string> GetLxgk(string arg0) {
            // 使用NLog记录请求内容
            NLog.LogManager.GetCurrentClassLogger().Info(arg0);

            // 返回指定的响应格式

            var response = "#MSG::0::成功::||#END";
            return Task.FromResult(response);
        }

        public Task<string> GetGkcx(string arg0) {
            // 使用NLog记录请求内容
            NLog.LogManager.GetCurrentClassLogger().Info(arg0);
            // 返回指定的响应格式

            var response = $"#HEAD::202405WS43400001FJ000000001::2143004019::18::40000000::重庆市::156::0011000000000000::5350.0::0::0::1::*::*::0000000000000000::43400100::43000164::*::TestTime:{DateTime.Now:yyyy-MM-dd HH:mm:ss}||#END";
            return Task.FromResult(response);
        }

        public Task<string> GetGksg(string arg0) {
            NLog.LogManager.GetCurrentClassLogger().Info(arg0);
            var response = "#MSG::0::成功::||#END";
            return Task.FromResult(response);
        }
    }
}