using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Dto.ApiDto {
    public class SzjyApiDto {
        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 账号
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 机器码
        /// </summary>
        public string Machine { get; set; } = string.Empty;

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut { get; set; } = 1000;

    }
}
