using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Interface {
    public class NetworkTime : INetworkTime {
        private readonly IHttpClientFactory _httpClientFactory;

        public NetworkTime(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<DateTime> GetTime() {
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(3000);
                var resultContent = await httpClient.GetStringAsync("http://worldtimeapi.org/api/ip");

                // 解析 JSON 数据
                var jsonObject = JObject.Parse(resultContent);
                var unixTime = jsonObject.Value<DateTime>("datetime");

                return unixTime;
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            return DateTime.Now;
        }
    }
}