using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JayTom.Dws.Integrations.Contracts {

    /// <summary>定义外部接口地址与请求时限的不可变基础参数。</summary>
    public record BaseApiParameters {

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; init; } = string.Empty;

        /// <summary>
        /// 请求超时时间，单位为毫秒
        /// </summary>
        [JsonPropertyName("TimeOut")]
        public int TimeoutMilliseconds { get; init; }
    }
}
