using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalLog {

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
        /// 程序运行日志
        /// </summary>
        AppLog,

        /// <summary>
        /// 相机日志
        /// </summary>
        CameraLog,

        /// <summary>
        /// 分拣日志
        /// </summary>
        SortingLog,

        /// <summary>
        /// 称重日志
        /// </summary>
        WeighingLog,

        /// <summary>
        /// 体积日志
        /// </summary>
        VolumeLog,

        /// <summary>
        /// API日志
        /// </summary>
        ApiLog,

        /// <summary>
        /// 输出日志
        /// </summary>
        OutputLog,

        /// <summary>
        /// 输入日志
        /// </summary>
        InputLog,

        /// <summary>
        /// OCR日志
        /// </summary>
        OcrLog,

        /// <summary>
        /// FTP日志
        /// </summary>
        FtpLog,

        /// <summary>
        /// 异常日志
        /// </summary>
        ExceptionLog
    }
}