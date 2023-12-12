using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalData {

    [Table("Data_ImageInfo", Schema = "dbo")]
    public class ImageInfoModel : BaseBarCodeForeignKeyInfo {

        /// <summary>
        /// 相机名称
        /// </summary>
        [Column("CameraName")]
        public string CameraName { get; set; } = string.Empty;

        /// <summary>
        /// 相机自定义名
        /// </summary>
        [Column("CustomCameraName")]
        public string CustomCameraName { get; set; } = string.Empty;

        /// <summary>
        /// 相机序列号
        /// </summary>
        [Column("CameraSerialNumber"), Required]
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 图片类型(0=扫码、1=全景、2=体积云点、3=面单抠图)
        /// </summary>
        [Column("Type"), Required]
        public int Type { get; set; }

        /// <summary>
        /// 图片本地路径
        /// </summary>
        [Column("Type"), Required]
        public string LocalPath { get; set; } = string.Empty;

        /// <summary>
        /// 图片网络路径
        /// </summary>
        [Column("ImageUrl")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}