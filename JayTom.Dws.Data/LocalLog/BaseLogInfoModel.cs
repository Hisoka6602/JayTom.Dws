using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {

    public class BaseLogInfoModel : BaseModel {

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("CreateTime")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }

    public enum CommunicationMethod {

        /// <summary>
        /// 串口
        /// </summary>
        SerialPort,

        /// <summary>
        /// Tcp
        /// </summary>
        Tcp,

        /// <summary>
        /// 其他
        /// </summary>
        Other
    }

    public enum DataFormatType {
        Hex,
        Ascii
    }

    // 通讯类型枚举
    public enum CommunicationType {
        Receive,  // 接收
        Send      // 发送
    }

    // 类型枚举（信息、警告、异常）
    public enum LogType {
        Information,
        Warning,
        Exception
    }
}