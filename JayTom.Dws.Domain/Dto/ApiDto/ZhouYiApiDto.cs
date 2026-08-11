using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace JayTom.Dws.Domain.Dto.ApiDto {

    public class ZhouYiApiDto {

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; } = "http://api.zygp.site/openapi/express/fjUpload";

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut { get; set; } = 10000;

        [JsonProperty("AppId")]
        public string ApplicationCode { get; set; } = string.Empty;
        public string AppKey { get; set; } = string.Empty;
        public bool NeedUpload { get; set; }
        public bool IsFstCode { get; set; }
    }
}
