using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {

    [Table("Log_LogCleaningLogInfo", Schema = "dbo")]
    public class LogCleaningLogInfoModel : BaseLogInfoModel {

        [Column("CleaningType")]
        public CleaningType CleaningType { get; set; }

        [Column("LogDataType")]
        public LogDataType LogDataType { get; set; }
    }

    public enum CleaningType {

        /// <summary>
        /// 自动
        /// </summary>
        Automatic,

        /// <summary>
        /// 手动
        /// </summary>
        Manual,

        /// <summary>
        /// 最低保障
        /// </summary>
        MinimumGuarantee
    }

    public enum LogDataType {

        /// <summary>
        /// 收发日志
        /// </summary>
        MessageLog,

        /// <summary>
        /// Ocr日志
        /// </summary>
        OcrLog,

        /// <summary>
        /// 输出日志
        /// </summary>
        OutputLog,

        /// <summary>
        /// 输入日志
        /// </summary>
        InputLog,

        /// <summary>
        /// 指令日志
        /// </summary>
        InstructionLog,

        /// <summary>
        /// 设备日志
        /// </summary>
        DeviceLogInfo
    }
}