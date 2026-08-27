using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalLog {
    [Table("Log_FtpLogInfo", Schema = "dbo")]
    public class FtpLogInfoModel : BaseLogInfoModel {

        /// <summary>
        /// Ftp通讯类型
        /// </summary>
        [Column("FtpCommunicationType")]
        public FtpCommunicationType FtpCommunicationType { get; set; }
    }

    public enum FtpCommunicationType {

        /// <summary>
        /// 连接
        /// </summary>
        Connect,

        /// <summary>
        /// 上传
        /// </summary>
        Upload,

        /// <summary>
        /// 下载
        /// </summary>
        Download
    }
}