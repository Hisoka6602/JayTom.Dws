using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
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

        /// <summary>
        /// 类型
        /// </summary>
        [Column("Type")]
        public LogType Type { get; set; }

        /// <summary>
        /// 信息
        /// </summary>
        [Column("Message")]
        public string Message { get; set; } = string.Empty;
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

        [Description("Hex")]
        Hex,

        [Description("Ascii")]
        Ascii
    }

    // 通讯类型枚举
    public enum CommunicationType {
        Send, Receive
    }

    // 类型枚举（信息、警告、异常）
    public enum LogType {
        Information,
        Warning,
        Exception
    }

    /// <summary>
    /// 输入类型
    /// </summary>
    public enum DataSourceType {

        /// <summary>
        /// 无
        /// </summary>
        None,

        /// <summary>
        /// 外部输入
        /// </summary>
        ExternalInput,

        /// <summary>
        /// 控件输入
        /// </summary>
        ControlInput,

        /// <summary>
        /// 设备输入
        /// </summary>
        DeviceInput
    }
}