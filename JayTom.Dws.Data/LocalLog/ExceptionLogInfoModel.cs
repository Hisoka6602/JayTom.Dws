using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {

    [Table("Log_ExceptionLogInfo", Schema = "dbo")]
    public class ExceptionLogInfoModel : BaseLogInfoModel {

        [Column("ExceptionSource")]
        public ExceptionSource ExceptionSource { get; set; }
    }

    public enum ExceptionSource {

        /// <summary>
        /// 设备
        /// </summary>
        Device,

        /// <summary>
        /// 逻辑
        /// </summary>
        Logic,

        /// <summary>
        /// 程序
        /// </summary>
        App,

        /// <summary>
        /// 未知
        /// </summary>
        Unknown
    }
}