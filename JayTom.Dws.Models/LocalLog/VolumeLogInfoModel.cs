using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalLog {
    [Table("Log_VolumeLogInfo", Schema = "dbo")]
    public class VolumeLogInfoModel : BaseLogInfoModel {
        /// <summary>
        /// 数据来源类型
        /// </summary>
        [Column("DataSourceType")]
        public DataSourceType DataSourceType { get; set; }
    }

}
