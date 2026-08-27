using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Model {

    public class PanoramaCameraImageInfo {

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 是否已存在
        /// </summary>
        public bool IsExists { get; set; }
    }
}