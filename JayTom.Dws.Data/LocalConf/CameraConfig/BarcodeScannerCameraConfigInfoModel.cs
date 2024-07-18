using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.CameraConfig {

    [Table("Conf_BarcodeScannerCameraConfigInfo", Schema = "dbo")]
    public class BarcodeScannerCameraConfigInfoModel : BaseCameraConfigInfoModel {

        /// <summary>
        /// 是否显示实时图像
        /// </summary>
        [Column("IsShowRealTimeImage"), InsertOrUpdata]
        public bool IsShowRealTimeImage { get; set; }

        /// <summary>
        /// 连接参数
        /// </summary>
        [Column("CameraConnectionParameters"), InsertOrUpdata]
        public string CameraConnectionParameters { get; set; } = string.Empty;

        /// <summary>
        /// 是否使用Ocr算法
        /// </summary>
        [Column("IsOcrSupported"), InsertOrUpdata]
        public bool IsOcrSupported { get; set; }

        //绑定对应的Nvr(可空)
    }
}