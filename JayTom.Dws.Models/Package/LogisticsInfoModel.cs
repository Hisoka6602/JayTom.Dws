using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    [Table("Data_LogisticsInfo", Schema = "dbo")]
    public class LogisticsInfoModel : BasePackageForeignKeyInfoModel {

        /// <summary>
        /// 物流代码
        /// </summary>
        [Column("LogisticsCode")]
        public string LogisticsCode { get; set; } = string.Empty;

        /// <summary>
        /// 物流名称
        /// </summary>
        [Column("LogisticsName")]
        public string LogisticsName { get; set; } = string.Empty;
    }
}