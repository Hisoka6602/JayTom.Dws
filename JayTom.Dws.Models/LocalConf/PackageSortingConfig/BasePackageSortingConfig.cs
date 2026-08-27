using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalConf.PackageSortingConfig {

    public class BasePackageSortingConfig : BaseModel {

        /// <summary>
        /// 备注
        /// </summary>
        [Column("Remarks"), Required, InsertOrUpdate]
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("CreateTime"), Required]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 修改时间
        /// </summary>
        [Column("ModifyTime"), Required, InsertOrUpdate]
        public DateTime ModifyTime { get; set; } = DateTime.Now;
    }
}