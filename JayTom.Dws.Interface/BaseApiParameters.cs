using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JayTom.Dws.Interface {

    public class BaseApiParameters {

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 请求超时时间，单位为毫秒
        /// </summary>
        [JsonPropertyName("TimeOut")]
        public int TimeoutMilliseconds { get; set; }
    }
}
