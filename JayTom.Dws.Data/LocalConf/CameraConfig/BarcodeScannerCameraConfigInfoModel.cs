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
    }
}