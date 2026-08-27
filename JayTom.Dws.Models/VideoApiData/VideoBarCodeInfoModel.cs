using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.VideoApiData {

    [Table("Data_VideoBarCodeInfo", Schema = "dbo")]
    public class VideoBarCodeInfoModel : BaseModel {

        /// <summary>
        /// 时间戳Id
        /// </summary>
        [Column("TimestampedGuid"), Required]
        public long TimestampedGuid { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        [Column("Barcode"), Required]
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 扫码时间
        /// </summary>
        [Column("ScanTime", TypeName = "datetime"), Required]
        public DateTime ScanTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 节点信息
        /// </summary>
        public virtual ICollection<VideoScanNodeInfoModel>? VideoScanNodeInfos { get; set; }
    }
}