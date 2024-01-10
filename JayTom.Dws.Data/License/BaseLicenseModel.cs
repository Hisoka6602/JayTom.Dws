using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.License {

    public class BaseLicenseModel : BaseModel {

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required, Column("CreateTime")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [Required, Column("ModifyTime"), InsertOrUpdata]
        public DateTime ModifyTime { get; set; }

        /// <summary>
        /// 修改IP
        /// </summary>
        [Required, Column("ModifyIp"), InsertOrUpdata]
        public string ModifyIp { get; set; } = string.Empty;
    }
}