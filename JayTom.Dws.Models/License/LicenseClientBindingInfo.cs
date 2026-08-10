using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.License {

    [Table("Code_LicenseClientBindingInfo", Schema = "dbo")]
    public class LicenseClientBindingInfo : BaseLicenseModel {
        /*
        [Column("UserId")]
        public long UserId { get; set; }*/

        /*[ForeignKey("Id")]
        public virtual LicenseUserInfo? UserInfo { get; set; }*/

        [Column("LicenseCodeId")]
        public long LicenseCodeId { get; set; }

        [ForeignKey("Id")]
        public virtual LicenseCodeInfo? LicenseCodeInfo { get; set; }

        /// <summary>
        /// 机器码
        /// </summary>
        [Required, Column("MachineCode"), UpdateBy]
        public string MachineCode { get; set; } = string.Empty;

        /// <summary>
        /// 首次激活时间
        /// </summary>
        [Required, Column("FirstActivatedDate"), InsertOrUpdate]
        public DateTime FirstActivatedDate { get; set; }

        /// <summary>
        /// 最后效验时间
        /// </summary>

        [Required, Column("LastVerifiedDate"), InsertOrUpdate]
        public DateTime LastVerifiedDate { get; set; }
    }
}