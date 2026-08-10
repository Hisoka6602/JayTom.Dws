using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.VideoApiData {

    [Table("Data_VideoScanNodeInfo", Schema = "dbo")]
    public class VideoScanNodeInfoModel : BaseModel {

        [Column("BarcodeId"), Required]
        public long BarcodeId { get; set; }

        [ForeignKey("Id")]
        public virtual VideoBarCodeInfoModel? BarCodeInfo { get; set; }

        /// <summary>
        /// 节点名称
        /// </summary>
        [Column("Name"), Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 节点扫描时间
        /// </summary>
        [Column("ScanTime", TypeName = "datetime"), Required]
        public DateTime ScanTime { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        [Column("Description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 节点图片信息
        /// </summary>
        public virtual ICollection<VideoNodeImageInfoModel>? VideoNodeImageInfos { get; set; }

        /// <summary>
        /// NVR信息
        /// </summary>
        public virtual ICollection<VideoNvrCameraBindingInfoModel>? VideoNvrCameraBindingInfos { get; set; }
    }
}