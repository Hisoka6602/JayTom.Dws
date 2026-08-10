using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams {

    [Table("Conf_SerialPortConfigInfo", Schema = "dbo")]
    public class SerialPortConfigInfoModel : BasePackageSortingConfig {

        [Column("CommunicationConnectionId")]
        public long CommunicationConnectionId { get; set; }

        [ForeignKey(nameof(CommunicationConnectionId))]
        public virtual CommunicationConnectionConfigInfoModel? CommunicationConnectionConfigInfo { get; set; }

        /// <summary>
        /// 串口名
        /// </summary>
        [Column("PortName"), InsertOrUpdate]
        public string PortName { get; set; } = string.Empty;

        /// <summary>
        /// 波特率
        /// </summary>
        [Column("BaudRate"), InsertOrUpdate]
        public int BaudRate { get; set; }

        /// <summary>
        /// 数据位
        /// </summary>
        [Column("DataBits"), InsertOrUpdate]
        public int DataBits { get; set; }

        /// <summary>
        /// 效验位
        /// </summary>
        [Column("Parity"), InsertOrUpdate]
        public int Parity { get; set; }

        /// <summary>
        /// 停止位
        /// </summary>
        [Column("StopBits"), InsertOrUpdate]
        public int StopBits { get; set; }

        /// <summary>
        /// 数据格式
        /// </summary>
        [Column("DataFormat"), InsertOrUpdate]
        public int DataFormat { get; set; }
    }
}
