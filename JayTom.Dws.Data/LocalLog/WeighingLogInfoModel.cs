using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {

    [Table("Log_WeighingLogInfo", Schema = "dbo")]
    public class WeighingLogInfoModel : BaseLogInfoModel {

        /// <summary>
        /// 源数据
        /// </summary>
        [Column("Source")]
        public string? Source { get; set; }

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

        /// <summary>
        /// 数据来源类型
        /// </summary>
        [Column("DataSourceType")]
        public DataSourceType? DataSourceType { get; set; }

        /// <summary>
        /// 格式化后的重量
        /// </summary>
        [Column("FormatWeight")]
        public double FormatWeight { get; set; }
    }
}