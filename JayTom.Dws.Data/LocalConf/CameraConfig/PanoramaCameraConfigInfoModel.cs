using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.CameraConfig {

    [Table("Conf_PanoramaCameraConfigInfo", Schema = "dbo")]
    public class PanoramaCameraConfigInfoModel : BaseCameraConfigInfoModel {

        /// <summary>
        /// 延迟时间拍照时间（单位：秒）
        /// </summary>
        [Column("CaptureDelayTime"), Required, InsertOrUpdata]
        public int CaptureDelayTime { get; set; }

        /// <summary>
        /// 相机连接参数(部分相机使用)
        /// </summary>
        [Column("CameraConnectionParameters"), InsertOrUpdata]
        public string CameraConnectionParameters { get; set; } = string.Empty;
    }
}