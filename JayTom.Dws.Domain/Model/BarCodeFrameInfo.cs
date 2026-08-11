using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Abstractions.Imaging;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Model {

    public class BarCodeFrameInfo {
        public long Timestamp { get; set; }
        public long Frame { get; set; }
        public BarCodeInfoModel? BarCodeInfo { get; set; }

        /// <summary>
        /// 图片
        /// </summary>
        public ImageHandle? Image { get; set; }
    }
}
