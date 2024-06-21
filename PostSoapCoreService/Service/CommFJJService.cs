using NLog;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace PostSoapCoreService.Service {

    public class CommFjjService : ICommFjjService {
        private readonly ILogger<CommFjjService> _logger;
        private ConcurrentDictionary<string, bool> _exitItems = new();

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
            var response = "#MSG::-1::操作失败::||#END";
            if (!string.IsNullOrWhiteSpace(arg0)) {
                var pattern = @"#HEAD::(.*?)#END";
                var match = Regex.Match(arg0, pattern);
                if (match.Success) {
                    var content = match.Groups[1].Value;
                    var parts = content.Split(new string[] { "::" }, StringSplitOptions.None);

                    if (parts.Length >= 4 && int.TryParse(parts[3], out var exit) &&
                        int.TryParse(parts[4], out var status)) {
                        _exitItems.AddOrUpdate(exit.ToString(),
                            _ => status == 1,
                            (_, _) => status == 1);
                        response = $"#MSG::0::{(status == 1 ? "锁格" : "解锁")}成功::||#END";
                    }
                }
            }
            return Task.FromResult(response);
        }

        public Task<string> GetGkzt(string arg0) {
            NLog.LogManager.GetCurrentClassLogger().Info(arg0);
            int.TryParse(arg0, out var exit);
            var (key, value) = _exitItems.FirstOrDefault(f => f.Key.Equals(exit.ToString()));
            var response = key == null ? "#MSG::0::未锁格::||#END" : $"#MSG::{(value ? "-1" : "0")}::{(value ? "已锁格" : "未锁格")}::||#END";
            return Task.FromResult(response);
        }
    }
}