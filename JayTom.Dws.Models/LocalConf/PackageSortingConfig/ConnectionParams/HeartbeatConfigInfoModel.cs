using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams {

    [Table("Conf_HeartbeatConfigInfo", Schema = "dbo")]
    public class HeartbeatConfigInfoModel : BasePackageSortingConfig {

        [Column("CommunicationConnectionId")]
        public long CommunicationConnectionId { get; set; }

        [ForeignKey(nameof(CommunicationConnectionId))]
        public virtual CommunicationConnectionConfigInfoModel? CommunicationConnectionConfigInfo { get; set; }

        /// <summary>
        /// 是否使用心跳包
        /// </summary>
        [Column("IsHeartbeatEnabled"), InsertOrUpdate]
        public bool IsHeartbeatEnabled { get; set; }

        /// <summary>
        /// 是否主动发送心跳包
        /// </summary>
        [Column("IsHeartbeatActive"), InsertOrUpdate]
        public bool IsHeartbeatActive { get; set; }

        /// <summary>
        /// 是否固定心跳包内容
        /// </summary>
        [Column("IsFixedHeartbeatContent"), InsertOrUpdate]
        public bool IsFixedHeartbeatContent { get; set; }

        /// <summary>
        /// 心跳包内容
        /// </summary>
        [Column("HeartbeatContent"), InsertOrUpdate]
        public string HeartbeatContent { get; set; } = string.Empty;

        /// <summary>
        /// 心跳包间隔
        /// </summary>
        [Column("HeartbeatInterval"), InsertOrUpdate]
        public int HeartbeatInterval { get; set; }
    }
}
