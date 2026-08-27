using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalConf.PackageSortingConfig.ConnectionParams {

    [Table("Conf_TcpConfigInfo", Schema = "dbo")]
    public class TcpConfigInfoModel : BasePackageSortingConfig {

        [Column("TcpConnectionConfigId")]
        public long TcpConnectionConfigId { get; set; }

        [ForeignKey(nameof(TcpConnectionConfigId))]
        public virtual TcpConnectionConfigInfoModel? TcpConnectionConfigInfoInfo { get; set; }

        /// <summary>
        /// 0=客户端、1=服务端
        /// </summary>
        [Column("Type"), InsertOrUpdate]
        public int Type { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        [Column("IpAddress"), InsertOrUpdate]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 端口
        /// </summary>
        [Column("Port"), InsertOrUpdate]
        public int Port { get; set; }
    }
}
