using NLog;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PostSoapCoreService.Service {

    public class CommFjjService : ICommFjjService {

        public Task<string> getLXGK(string arg0) {
            // 使用NLog记录请求内容
            LogManager.GetCurrentClassLogger().Info(arg0);

            // 返回指定的响应格式
            var response = "#MSG::1::成功::||#END";
            return Task.FromResult(response);
        }

        public Task<string> getGKCX(string arg0) {
            // 使用NLog记录请求内容
            LogManager.GetCurrentClassLogger().Info(arg0);

            // 返回指定的响应格式
            var response = $"#HEAD::202405WS43400001FJ000000001::2143004019::18::40000000::重庆市::156::0011000000000000::5350.0::0::0::1::*::*::0000000000000000::43400100::43000164::*::TestTime:{DateTime.Now:yyyy-MM-dd HH:mm:ss}||#END";
            return Task.FromResult(response);
        }

        public Task<string> getGKSG(string arg0) {
            var response = "#MSG::1::成功::||#END";
            return Task.FromResult(response);
        }
    }
}