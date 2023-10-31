using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {

    [Table("Log_OutputLogInfo", Schema = "dbo")]
    public class OutputLogInfoModel : BaseLogInfoModel {

        /// <summary>
        /// 输出类型
        /// </summary>
        [Column("OutputType")]
        public OutputType OutputType { get; set; }

        /// <summary>
        /// 输出内容
        /// </summary>
        [Column("OutputContent")]
        public string OutputContent { get; set; } = string.Empty;

        /// <summary>
        /// 目的地
        /// </summary>
        [Column("Destination")]
        public string Destination { get; set; } = string.Empty;
    }

    public enum OutputType {
        TcpOutput,
        SerialPortOutput,
        AudioOutput,
        LocationOutput
    }
}