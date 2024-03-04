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

    /// <summary>
    /// 格口信息
    /// </summary>
    [Table("Conf_PackageExitDefinitionInfo", Schema = "dbo")]
    public class PackageExitDefinitionInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 连接Id
        /// </summary>
        [Column("CommunicationConnectionId"), Required, InsertOrUpdata]
        public long CommunicationConnectionId { get; set; }

        [ForeignKey("Id")]
        public virtual CommunicationConnectionConfigInfoModel? CommunicationConnectionConfigInfo { get; set; }

        /// <summary>
        /// Pid
        /// </summary>
        [Column("Pid"), Required, InsertOrUpdata]
        public long Pid { get; set; }

        /// <summary>
        /// 出口名称
        /// </summary>
        [Column("ExitName"), Required, InsertOrUpdata]
        public string ExitName { get; set; } = string.Empty;

        /// <summary>
        /// 出口类型(异常出口只能生效一个)
        /// </summary>
        [Column("Type"), Required, InsertOrUpdata]
        public ExitType Type { get; set; } = ExitType.PackageExit;

        /// <summary>
        /// 是否生效
        /// </summary>
        [Column("IsActive"), Required, InsertOrUpdata]
        public bool IsActive { get; set; }

        /// <summary>
        /// 是否锁格
        /// </summary>
        [Column("IsLockExit"), NotMapped]
        public bool IsLockExit { get; set; }

        public virtual PackageExitLockBindingInfoModel? PackageExitLockBindingInfo { get; set; }
    }

    public enum ExitType {

        /// <summary>
        /// 包裹出口
        /// </summary>
        PackageExit = 0,

        /// <summary>
        /// 异常出口
        /// </summary>
        AbnormalExit = 1,

        /// <summary>
        /// 备用格口
        /// </summary>
        ReservedExit = 2
    }
}