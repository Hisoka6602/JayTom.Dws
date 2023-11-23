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

        [ForeignKey("Id")]
        public virtual CommunicationConnectionConfigInfoModel? CommunicationConnectionConfigInfo { get; set; }

        [Column("IsUseServer"), Required, InsertOrUpdata]
        public bool IsUseServer { get; set; }

        /// <summary>
        /// 服务端信息
        /// </summary>
        public virtual TcpConfigInfoModel? ServerParameter { get; set; }

        [Column("IsUseClient"), Required, InsertOrUpdata]
        public bool IsUseClient { get; set; }

        /// <summary>
        /// 客户端信息
        /// </summary>
        public virtual TcpConfigInfoModel? ClientParameter { get; set; }
    }
}