using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    [Table("Data_ExitInfo", Schema = "dbo")]
    public class ExitInfoModel : BasePackageForeignKeyInfoModel {

        /// <summary>
        /// 理论格口
        /// </summary>
        [Column("TheoreticalExit")]
        public string TheoreticalExit { get; set; } = string.Empty;

        /// <summary>
        /// 物理格口
        /// </summary>
        [Column("PhysicalExit")]
        public string PhysicalExit { get; set; } = string.Empty;

        /// <summary>
        /// 物理格口Id
        /// </summary>
        [Column("PhysicalExitId")]
        public long PhysicalExitId { get; set; }
    }
}