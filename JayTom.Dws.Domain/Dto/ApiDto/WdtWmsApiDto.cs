using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.ApiDto {

    public class WdtWmsApiDto {
        public string Url { get; set; } = string.Empty;
        public string Sid { get; set; } = string.Empty;
        public string AppKey { get; set; } = string.Empty;
        public string AppSecret { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut { get; set; } = 1000;
    }
}