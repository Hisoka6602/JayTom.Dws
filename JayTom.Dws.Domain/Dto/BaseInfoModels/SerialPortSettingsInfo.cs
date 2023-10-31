using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.BaseInfoModels {

    public class SerialPortSettingsInfo {

        /// <summary>
        /// 串口名称
        /// </summary>
        public string PortName { get; set; } = string.Empty;     // 串口名称

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get; set; }

        /// <summary>
        /// 效验位
        /// </summary>
        public Parity Parity { get; set; }

        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits { get; set; }

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits { get; set; }

        /// <summary>
        /// 数据格式
        /// </summary>
        public DataFormatType DataFormat { get; set; } = DataFormatType.Ascii;
    }
}