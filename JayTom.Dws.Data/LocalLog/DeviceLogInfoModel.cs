using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {

    [Table("Log_DeviceLogInfo", Schema = "dbo")]
    public class DeviceLogInfoModel : BaseLogInfoModel {

        /// <summary>
        /// 设备类型（相机、磅秤、其他终端）
        /// </summary>
        [Column("DeviceType")]
        public DeviceType DeviceType { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        [Column("DeviceName")]
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// 通讯方式（串口、Tcp、其他）
        /// </summary>
        [Column("CommunicationMethod")]
        public CommunicationMethod CommunicationMethod { get; set; }

        /// <summary>
        /// 数据格式（字符串、十六进制）
        /// </summary>
        [Column("DataFormat")]
        public DataFormatType DataFormat { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        [Column("Content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 类型（信息、警告、异常）
        /// </summary>
        [Column("Type")]
        public LogType Type { get; set; }
    }

    public enum DeviceType {
        Camera,
        Scale,
        OtherTerminal
    }
}