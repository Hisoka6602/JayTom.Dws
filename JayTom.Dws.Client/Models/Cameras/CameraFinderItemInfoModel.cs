using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.Cameras {

    public class CameraFinderItemInfoModel : BaseCameraItemInfoModel {

        /// <summary>
        /// 是否已被选择
        /// </summary>
        public bool IsSelected { get; set; }
    }
}