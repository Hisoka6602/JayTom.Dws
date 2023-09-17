using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalData {

    [Table("Data_PanoramaImageInfo", Schema = "dbo")]
    public class PanoramaImageInfoModel : BaseModel {

        /// <summary>
        /// 全景图片保存路径
        /// </summary>
        [Column("PanoramaImagePath"), Required]
        public string? PanoramaImagePath { get; set; }

        [Column("BarcodeInfoId")]
        public long BarcodeInfoId { get; set; }

        [ForeignKey("Id")]
        public virtual BarCodeInfoModel BarcodeInfo { get; set; }
    }
}