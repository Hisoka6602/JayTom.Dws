using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalLog {
    [Table("Log_InputLogInfo", Schema = "dbo")]
    public class InputLogInfoModel : BaseLogInfoModel {

        /// <summary>
        /// 输入类型
        /// </summary>
        [Column("DataSourceType")]
        public DataSourceType DataSourceType { get; set; }

        /// <summary>
        /// 输入内容
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
        /// <summary>
        /// Tcp输入
        /// </summary>
        TcpOutput,
        /// <summary>
        /// 控件输入
        /// </summary>
        ControlInput
    }
}