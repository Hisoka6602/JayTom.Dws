using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;

namespace JayTom.Dws.Domain.Dto {

    public class GrayscaleDeviceSettingsDto {

        /// <summary>
        /// 是否使用灰度仪检测包裹
        /// </summary>
        public bool IsUseGrayscaleDetector { get; set; }

        /// <summary>
        /// 是否判断包裹偏向
        /// </summary>
        public bool IsCheckPackageOrientation { get; set; }

        /// <summary>
        /// Tcp配置
        /// </summary>
        public TcpSettingsInfo? TcpConnectionConfigInfo { get; set; }
    }
}