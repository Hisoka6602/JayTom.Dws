using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {

    [Table("Log_InputLogInfo", Schema = "dbo")]
    public class InputLogInfoModel : BaseLogInfoModel {

        /// <summary>
        /// 输出类型
        /// </summary>
        [Column("InputType")]
        public InputType InputType { get; set; }

        /// <summary>
        /// 输出内容
        /// </summary>
        [Column("InputContent")]
        public string InputContent { get; set; } = string.Empty;

        /// <summary>
        /// 目的地
        /// </summary>
        [Column("Destination")]
        public string Destination { get; set; } = string.Empty;
    }

    public enum InputType {
        TcpOutput,
        ControlInput
    }
}