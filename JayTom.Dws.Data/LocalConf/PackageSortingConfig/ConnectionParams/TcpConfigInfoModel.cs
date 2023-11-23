using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams {

    [Table("Conf_TcpConfigInfo", Schema = "dbo")]
    public class TcpConfigInfoModel : BasePackageSortingConfig {

        [Column("TcpConnectionConfigId")]
        public long TcpConnectionConfigId { get; set; }

        [ForeignKey("Id")]
        public virtual TcpConnectionConfigInfoModel? TcpConnectionConfigInfoInfo { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        [Column("IpAddress"), InsertOrUpdata]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 端口
        /// </summary>
        [Column("Port"), InsertOrUpdata]
        public int Port { get; set; }
    }
}