using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Client.Models.Cameras {

    public class VolumeCameraItemInfoModel : BaseCameraItemInfoModel {

        /// <summary>
        /// 体积测量模式
        /// </summary>
        public int VolumeMeasurementMode { get; set; }

        /// <summary>
        /// 最小同步时间（单位：毫秒）
        /// </summary>
        public int MinSyncTime { get; set; }

        /// <summary>
        /// 最大同步时间（单位：毫秒）
        /// </summary>
        public int MaxSyncTime { get; set; }

        /// <summary>
        /// 最小长度
        /// </summary>
        public double MinLength { get; set; }

        /// <summary>
        /// 最大长度
        /// </summary>
        public double MaxLength { get; set; }

        /// <summary>
        /// 触发模式
        /// </summary>
        public int TriggerMode { get; set; }
    }
}