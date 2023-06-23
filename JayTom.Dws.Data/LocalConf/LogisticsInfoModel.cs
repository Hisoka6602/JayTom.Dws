using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf {

    [Table("Conf_LogisticsInfo", Schema = "dbo")]
    public class LogisticsInfoModel : BaseModel {

        /// <summary>
        /// 物流公司名称
        /// </summary>
        [Column("LogisticsName"), Required]
        public string LogisticsName { get; set; } = string.Empty;

        /// <summary>
        /// 物流公司代码
        /// </summary>
        [Column("LogisticsCode"), Required]
        public string LogisticsCode { get; set; } = string.Empty;

        /// <summary>
        /// 物流公司语音
        /// </summary>
        [Column("Voice")]
        public byte[]? Voice { get; set; }

        /// <summary>
        /// 物流公司图标
        /// </summary>
        [Column("Icon")]
        public byte[]? Icon { get; set; }
    }
}