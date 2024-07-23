using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.Cameras {

    public class NvrCameraMappingItemInfoModel : BaseCameraItemInfoModel {

        /// <summary>
        /// 取流通道
        /// </summary>
        public int Channel { get; set; }
    }
}