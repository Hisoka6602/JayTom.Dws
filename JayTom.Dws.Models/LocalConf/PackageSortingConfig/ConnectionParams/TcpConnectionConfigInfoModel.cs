using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams {

    [Table("Conf_TcpConnectionConfigInfo", Schema = "dbo")]
    public class TcpConnectionConfigInfoModel : BasePackageSortingConfig {

        [Column("CommunicationConnectionId")]
        public long CommunicationConnectionId { get; set; }

        [ForeignKey(nameof(CommunicationConnectionId))]
        public virtual CommunicationConnectionConfigInfoModel? CommunicationConnectionConfigInfo { get; set; }

        /// <summary>
        /// 连接模式 0=客户端、1=服务端
        /// </summary>
        [Column("ConnectionMode"), Required, InsertOrUpdate]
        public int ConnectionMode { get; set; }

        /// <summary>
        /// Tcp信息
        /// </summary>
        public virtual ICollection<TcpConfigInfoModel>? TcpConfigItems { get; set; }

        /// <summary>
        /// 数据格式
        /// </summary>
        [Column("DataFormat"), InsertOrUpdate]
        public int DataFormat { get; set; }
    }
}
