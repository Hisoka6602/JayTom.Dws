using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams {

    [Table("Conf_DeviceExtensionConfigInfo", Schema = "dbo")]
    public class DeviceExtensionConfigInfoModel : BasePackageSortingConfig {

        [Column("CommunicationConnectionId")]
        public long CommunicationConnectionId { get; set; }

        [ForeignKey("Id")]
        public virtual CommunicationConnectionConfigInfoModel? CommunicationConnectionConfigInfo { get; set; }

        /// <summary>
        /// 是否由下位机创建包裹
        /// </summary>
        [Column("CreatePackageByDevice"), InsertOrUpdata]
        public bool CreatePackageByDevice { get; set; }

        /// <summary>
        /// 是否由下位机移除包裹
        /// </summary>
        [Column("RemovePackageByDevice"), InsertOrUpdata]
        public bool RemovePackageByDevice { get; set; }

        /// <summary>
        /// 是否由下位机启动运行
        /// </summary>
        [Column("StartRunningByDevice"), InsertOrUpdata]
        public bool StartRunningByDevice { get; set; }

        /// <summary>
        /// 是否由下位机停止运行
        /// </summary>
        [Column("StopRunningByDevice"), InsertOrUpdata]
        public bool StopRunningByDevice { get; set; }

        /// <summary>
        /// 是否验证下位机应答
        /// </summary>
        [Column("ValidateDeviceResponse"), InsertOrUpdata]
        public bool ValidateDeviceResponse { get; set; }

        /// <summary>
        /// 验证超时时间
        /// </summary>
        [Column("ValidationTimeout"), InsertOrUpdata]
        public int ValidationTimeout { get; set; }

        /// <summary>
        /// 最大重试次数
        /// </summary>
        [Column("MaxRetryCount"), InsertOrUpdata]
        public int MaxRetryCount { get; set; }
    }
}