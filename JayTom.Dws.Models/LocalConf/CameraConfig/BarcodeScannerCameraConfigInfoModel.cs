using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using JayTom.Dws.Models.LocalConf.CloudConfig;
using JayTom.Dws.Models.LocalConf.IpcNvrConfig;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalConf.CameraConfig {

    [Table("Conf_BarcodeScannerCameraConfigInfo", Schema = "dbo")]
    public class BarcodeScannerCameraConfigInfoModel : BaseCameraConfigInfoModel {

        /// <summary>
        /// 是否显示实时图像
        /// </summary>
        [Column("IsShowRealTimeImage"), InsertOrUpdate]
        public bool IsShowRealTimeImage { get; set; }

        /// <summary>
        /// 连接参数
        /// </summary>
        [Column("CameraConnectionParameters"), InsertOrUpdate]
        public string CameraConnectionParameters { get; set; } = string.Empty;

        /// <summary>
        /// 是否使用Ocr算法
        /// </summary>
        [Column("IsOcrSupported"), InsertOrUpdate]
        public bool IsOcrSupported { get; set; }

        /// <summary>
        /// Nvr
        /// </summary>
        [Description("Nvr")]
        public virtual ICollection<NvrCameraBindingInfoModel>? NvrCameraBindingInfos { get; set; }
    }
}