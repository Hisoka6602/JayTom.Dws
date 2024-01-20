using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    [Table("Data_CloudVideoUploadInfo", Schema = "dbo")]
    public class CloudVideoUploadInfoModel : BasePackageForeignKeyInfoModel {

        /// <summary>
        /// 上传时间
        /// </summary>
        [Column("UploadTime")]
        public DateTime? UploadTime { get; set; }

        /// <summary>
        /// 上传内容
        /// </summary>
        [Column("UploadContent")]
        public string? UploadContent { get; set; }

        /// <summary>
        /// 返回内容
        /// </summary>
        [Column("ResponseContent")]
        public string? ResponseContent { get; set; }

        /// <summary>
        /// 上传耗时
        /// </summary>
        [Column("UploadDuration")]
        public int? UploadDuration { get; set; }

        /// <summary>
        /// 目标地址
        /// </summary>
        [Column("TargetAddress")]
        public string? TargetAddress { get; set; }

        /// <summary>
        /// 上传扫码图数量
        /// </summary>
        [Column("ScanImageCount")]
        public int ScanImageCount { get; set; }

        /// <summary>
        /// 上传全景图数量
        /// </summary>
        [Column("PanoramaImageCount")]
        public int PanoramaImageCount { get; set; }
    }
}