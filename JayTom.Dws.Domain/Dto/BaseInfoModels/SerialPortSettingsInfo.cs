using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using JayTom.Dws.Abstractions.Devices;

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
        public SerialParity Parity { get; set; } = SerialParity.None;

        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// 停止位
        /// </summary>
        public SerialStopBits StopBits { get; set; } = SerialStopBits.One;

        /// <summary>
        /// 数据格式
        /// </summary>
        public DataFormatType DataFormat { get; set; } = DataFormatType.Ascii;
    }
}
