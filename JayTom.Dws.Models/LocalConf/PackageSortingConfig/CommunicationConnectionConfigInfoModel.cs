using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig {

    [Table("Conf_CommunicationConnectionConfigInfo", Schema = "dbo")]
    public class CommunicationConnectionConfigInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 连接名称
        /// </summary>
        [Column("ConnectionName"), InsertOrUpdate]
        public string ConnectionName { get; set; } = string.Empty;

        /// <summary>
        /// 是否生效
        /// </summary>
        [Column("IsActive"), Required, InsertOrUpdate]
        public bool IsActive { get; set; }

        /// <summary>
        /// 通讯类型
        /// </summary>
        [Column("CommunicationType"), InsertOrUpdate]
        public int CommunicationType { get; set; }

        /// <summary>
        /// 串口配置
        /// </summary>
        public virtual SerialPortConfigInfoModel? SerialPortConfigInfo { get; set; }

        /// <summary>
        /// Tcp配置
        /// </summary>
        public virtual TcpConnectionConfigInfoModel? TcpConnectionConfigInfo { get; set; }

        /*/// <summary>
        /// Usb配置
        /// </summary>
        public UsbCommunicationConfigInfo UsbCommunicationConfigInfo { get; set; }

        /// <summary>
        /// Can总线配置
        /// </summary>
        public CanBusCommunicationConfigInfo CanBusCommunicationConfigInfo { get; set; }*/

        /// <summary>
        /// 通讯协议
        /// </summary>
        [Column("CommunicationProtocol"), InsertOrUpdate]
        public string CommunicationProtocol { get; set; } = string.Empty;

        /// <summary>
        /// 是否自动重连
        /// </summary>
        [Column("IsAutoReconnect"), InsertOrUpdate]
        public bool IsAutoReconnect { get; set; }

        /// <summary>
        /// 重连最大重试次数
        /// </summary>
        [Column("MaxReconnectAttempts"), InsertOrUpdate]
        public int MaxReconnectAttempts { get; set; }

        /// <summary>
        /// 下位机设置
        /// </summary>
        public virtual DeviceExtensionConfigInfoModel? DeviceExtensionConfigInfo { get; set; }

        /// <summary>
        /// 心跳包设置
        /// </summary>
        public virtual HeartbeatConfigInfoModel? HeartbeatConfigInfo { get; set; }

        public virtual ICollection<PackageExitDefinitionInfoModel>? PackageExitDefinitionItems { get; set; }
    }
}