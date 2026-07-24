using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.IpcNvrConfig {

    [Table("Conf_NvrWatermarkConfigInfo", Schema = "dbo")]
    public class NvrWatermarkConfigInfoModel : BaseModel {

        /// <summary>
        /// 通道Id
        /// </summary>
        [Column("IpAddress"), Required]
        public int ChannelId { get; set; }

        /// <summary>
        /// 前景色
        /// </summary>
        [Column("ForegroundColorHex"), Required]
        public string ForegroundColorHex { get; set; } = string.Empty;

        /// <summary>
        /// 背景色
        /// </summary>
        [Column("BackgroundColorHex"), Required]
        public string BackgroundColorHex { get; set; } = string.Empty;

        /// <summary>
        /// 持续时间
        /// </summary>
        [Column("Duration"), Required]
        public int Duration { get; set; }

        /// <summary>
        /// 水印位置
        /// </summary>
        [Column("Position"), Required]
        public int Position { get; set; }

        /// <summary>
        /// 显示方式(叠加、单一)
        /// </summary>
        [Column("DisplayMode"), Required]
        public int DisplayMode { get; set; }

        [Column("IpcNvrConfigId")]
        public long IpcNvrConfigId { get; set; }

        [ForeignKey(nameof(IpcNvrConfigId))]
        public virtual IpcNvrConfigInfoModel? IpcNvrConfigInfo { get; set; }
    }
}
