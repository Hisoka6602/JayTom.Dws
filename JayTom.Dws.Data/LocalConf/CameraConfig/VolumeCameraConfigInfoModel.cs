using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.CameraConfig {

    [Table("Conf_VolumeCameraConfigInfo", Schema = "dbo")]
    public class VolumeCameraConfigInfoModel : BaseCameraConfigInfoModel {

        /// <summary>
        /// 体积测量模式
        /// </summary>
        [Column("VolumeMeasurementMode"), Required, InsertOrUpdata]
        public int VolumeMeasurementMode { get; set; }

        /// <summary>
        /// 最小同步时间（单位：毫秒）
        /// </summary>
        [Column("MinSyncTime"), Required, InsertOrUpdata]
        public int MinSyncTime { get; set; }

        /// <summary>
        /// 最大同步时间（单位：毫秒）
        /// </summary>
        [Column("MaxSyncTime"), Required, InsertOrUpdata]
        public int MaxSyncTime { get; set; }

        /// <summary>
        /// 最小长度
        /// </summary>
        [Column("MinLength"), Required, InsertOrUpdata]
        public double MinLength { get; set; }

        /// <summary>
        /// 最大长度
        /// </summary>
        [Column("MaxLength"), Required, InsertOrUpdata]
        public double MaxLength { get; set; }

        /// <summary>
        /// 触发模式
        /// </summary>
        [Column("TriggerMode"), Required, InsertOrUpdata]
        public int TriggerMode { get; set; }
    }
}