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

        public async Task<DateTimeOffset> GetLocalTimeAsync(CancellationToken cancellationToken = default) {
            using var httpClient = _httpClientFactory.CreateClient(global::JayTom.Dws.Interface.ApiHttpClientNames.ExternalApi);
            httpClient.Timeout = TimeSpan.FromMilliseconds(3000);
            var resultContent = await httpClient
                .GetStringAsync("http://worldtimeapi.org/api/ip", cancellationToken)
                .ConfigureAwait(false);

            var jsonObject = JObject.Parse(resultContent);
            var value = jsonObject.Value<string>("datetime");
            if (!DateTimeOffset.TryParse(value, out var networkTime)) {
                throw new FormatException("网络时间响应不包含有效的 datetime 字段。");
            }

            return networkTime.ToLocalTime();
        }
    }
}
