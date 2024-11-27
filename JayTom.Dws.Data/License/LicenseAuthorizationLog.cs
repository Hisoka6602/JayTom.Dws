using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.License {

    [Table("Log_LicenseAuthorizationLog", Schema = "dbo")]
    public class LicenseAuthorizationLog : BaseLicenseModel {

        [Column("UserCode"), Required]
        public string UserCode { get; set; } = string.Empty;

        /// <summary>
        /// 授权码
        /// </summary>
        [Column("LicenseCode"), Required]
        public string LicenseCode { get; set; } = string.Empty;

        /// <summary>
        /// 操作时间
        /// </summary>
        [Column("OperationTime")]
        public DateTime OperationTime { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        [Column("OperationType")]
        public LicenseOperationType OperationType { get; set; }

        /// <summary>
        /// 消耗的授权码数量
        /// </summary>
        [Column("ConsumedLicenseCount")]
        public int ConsumedLicenseCount { get; set; }

        /// <summary>
        /// 操作IP (Operation IP)
        /// </summary>
        [Column("OperationIp")]
        public string OperationIp { get; set; } = string.Empty;

        /// <summary>
        /// 操作用户 (Operation User)
        /// </summary>
        [Column("OperationUser")]
        public string OperationUser { get; set; } = string.Empty;

        /// <summary>
        /// 扫码器上限
        /// </summary>
        [Column("MaxBindingScannerCount")]
        public int MaxBindingScannerCount { get; set; }

        /// <summary>
        /// 客户 (Customer)
        /// </summary>
        [Column("Customer")]
        public string Customer { get; set; } = string.Empty;
    }

    public enum LicenseOperationType {

        /// <summary>
        /// 创建
        /// </summary>
        Created,

        /// <summary>
        /// 修改
        /// </summary>
        Modified
    }
}