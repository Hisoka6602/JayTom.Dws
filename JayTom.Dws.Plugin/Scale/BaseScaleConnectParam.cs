using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Scale {

    public class BaseScaleConnectParam {

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get; set; }

        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits { get; set; }

        /// <summary>
        /// 效验位
        /// </summary>
        public Parity Parity { get; set; }

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits { get; set; }

        /// <summary>
        /// 串口名
        /// </summary>
        public string PortName { get; set; } = string.Empty;
    }
}