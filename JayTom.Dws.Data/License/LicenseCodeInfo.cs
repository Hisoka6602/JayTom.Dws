using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.License {

    [Table("Code_LicenseCodeInfo", Schema = "dbo")]
    public class LicenseCodeInfo : BaseLicenseModel {

        [Column("UserId")]
        public long UserId { get; set; }

        [ForeignKey("Id")]
        public virtual LicenseUserInfo? UserInfo { get; set; }

        [Column("LicenseApplicationInfoId")]
        public long LicenseApplicationInfoId { get; set; }

        [ForeignKey("Id")]
        public virtual LicenseApplicationInfo? LicenseApplicationInfo { get; set; }

        /// <summary>
        /// 授权码
        /// </summary>
        [Required, Column("LicenseCode"), UpdateBy]
        public string LicenseCode { get; set; } = string.Empty;

        /// <summary>
        /// 客户端上限数量
        /// </summary>
        [Required, Column("MaxClientCount"), InsertOrUpdata]
        public int MaxClientCount { get; set; }

        /// <summary>
        /// 已激活数量
        /// </summary>
        [Required, Column("ActivatedClientCount"), InsertOrUpdata]
        public int ActivatedClientCount { get; set; }

        /// <summary>
        /// 到期时间
        /// </summary>
        [Required, Column("ExpirationDate"), InsertOrUpdata]
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// 客户
        /// </summary>
        [Required, Column("ClientName"), InsertOrUpdata]
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// 是否可用
        /// </summary>
        [Required, Column("IsAvailable"), InsertOrUpdata]
        public bool IsAvailable { get; set; }

        public ICollection<LicenseClientBindingInfo>? LicenseClientBindingInfo { get; set; }
    }
}