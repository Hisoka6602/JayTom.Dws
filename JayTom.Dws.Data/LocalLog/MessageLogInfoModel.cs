using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {

    [Table("Log_MessageLogInfo", Schema = "dbo")]
    public class MessageLogInfoModel : BaseLogInfoModel {

        /// <summary>
        /// 通讯类型（接收、发送）
        /// </summary>
        [Column("CommunicationType")]
        public CommunicationType CommunicationType { get; set; }

        /// <summary>
        /// 数据格式（字符串、十六进制）
        /// </summary>
        [Column("DataFormat")]
        public DataFormatType DataFormat { get; set; }

        /// <summary>
        /// 通讯方式（串口、Tcp、其他）
        /// </summary>
        [Column("CommunicationMethod")]
        public CommunicationMethod CommunicationMethod { get; set; }

        /// <summary>
        /// 目标地址
        /// </summary>
        [Column("TargetAddress")]
        public string TargetAddress { get; set; } = string.Empty;

        /// <summary>
        /// 来源地址
        /// </summary>
        [Column("SourceAddress")]
        public string SourceAddress { get; set; } = string.Empty;

        /// <summary>
        /// 详细内容
        /// </summary>
        [Column("Content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 关键字
        /// </summary>
        [Column("Keywords")]
        public string Keywords { get; set; } = string.Empty;

        /// <summary>
        /// 终端
        /// </summary>
        [Column("Terminal")]
        public string Terminal { get; set; } = string.Empty;
    }
}