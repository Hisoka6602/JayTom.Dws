using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig {

    [Table("Conf_PackageExitLockBindingInfo", Schema = "dbo")]
    public class PackageExitLockBindingInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 格口Id
        /// </summary>
        [Column("ExitId"), Required, UpdateBy]
        public long ExitId { get; set; }

        [ForeignKey(nameof(ExitId))]
        public virtual PackageExitDefinitionInfoModel? PackageExitDefinitionInfo { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [Column("Address"), Required, InsertOrUpdate]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 长度
        /// </summary>
        [Column("Length"), Required, InsertOrUpdate]
        public int Length { get; set; }

        /// <summary>
        /// 锁定标识
        /// </summary>
        [Column("LockingFlag"), Required, InsertOrUpdate]
        public string LockingFlag { get; set; } = string.Empty;

        /// <summary>
        /// 解锁标识
        /// </summary>
        [Column("UnlockingFlag"), Required, InsertOrUpdate]
        public string UnlockingFlag { get; set; } = string.Empty;

        /// <summary>
        /// 当前状态
        /// </summary>
        [Column("UnlockingFlag"), NotMapped]
        public ExitLockStatus CurrentStatus { get; set; } = ExitLockStatus.Unlock;
    }

    public enum ExitLockStatus {

        /// <summary>
        /// 锁定
        /// </summary>
        Lock,

        /// <summary>
        /// 解锁
        /// </summary>
        Unlock
    }
}
