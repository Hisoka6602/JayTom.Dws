using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalData;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.VideoApiData {

    [Table("Data_VideoNodeImageInfo", Schema = "dbo")]
    public class VideoNodeImageInfoModel : BaseModel {

        [Column("ScanNodeId"), Required]
        public long ScanNodeId { get; set; }

        [ForeignKey("Id")]
        public virtual VideoScanNodeInfoModel? ScanNodeInfo { get; set; }

        /// <summary>
        /// 图片名称
        /// </summary>
        [Column("Name"), Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 图片路径
        /// </summary>
        [Column("Path"), Required]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 图片类型(0=扫码图、1=全景图)
        /// </summary>
        [Column("ImageType"), Required]
        public int ImageType { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        [Column("CameraSerialNumber")]
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 相机名称
        /// </summary>
        [Column("CameraName")]
        public string CameraName { get; set; } = string.Empty;
    }
}