using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto {

    public class TcpInfo {

        /// <summary>
        /// Ip地址
        /// </summary>
        public string IpAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }
    }
}