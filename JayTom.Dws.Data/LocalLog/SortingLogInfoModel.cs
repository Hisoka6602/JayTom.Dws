using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {

    [Table("Log_SortingLogInfo", Schema = "dbo")]
    public class SortingLogInfoModel : BaseLogInfoModel {

        /// <summary>
        /// 目的地
        /// </summary>
        [Column("Destination")]
        public string Destination { get; set; } = string.Empty;

        /// <summary>
        /// 通讯类型
        /// </summary>
        [Column("CommunicationType")]
        public CommunicationType? CommunicationType { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        [Column("DataFormatType")]
        public DataFormatType? DataFormatType { get; set; }
    }
}