using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalConf.PackageSortingConfig.ConnectionParams {

    [Table("Conf_DeviceExtensionConfigInfo", Schema = "dbo")]
    public class DeviceExtensionConfigInfoModel : BasePackageSortingConfig {

        [Column("CommunicationConnectionId")]
        public long CommunicationConnectionId { get; set; }

        [ForeignKey(nameof(CommunicationConnectionId))]
        public virtual CommunicationConnectionConfigInfoModel? CommunicationConnectionConfigInfo { get; set; }

        /// <summary>
        /// 是否验证下位机应答
        /// </summary>
        [Column("ValidateDeviceResponse"), InsertOrUpdate]
        public bool ValidateDeviceResponse { get; set; }

        /// <summary>
        /// 验证超时时间
        /// </summary>
        [Column("ValidationTimeout"), InsertOrUpdate]
        public int ValidationTimeout { get; set; }

        /// <summary>
        /// 最大重试次数
        /// </summary>
        [Column("MaxRetryCount"), InsertOrUpdate]
        public int MaxRetryCount { get; set; }
    }
}
