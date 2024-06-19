using System;
using System.Linq;
using System.Text;
using System.Drawing;
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

        /// <summary>
        /// 主框架区域(矩形区域4个点)
        /// </summary>
        public Rectangle MainFrameRegion { get; set; } = new(0, 0, 300, 600);

        /// <summary>
        /// 附加框架区域(矩形区域4个点)
        /// </summary>
        public Rectangle AdditionalFrameRegion { get; set; } = new(0, 0, 300, 600);

        /// <summary>
        /// 区域内包含的小车数量
        /// </summary>
        public int RegionCarCount { get; set; } = 1;

        /// <summary>
        /// 超时时间
        /// </summary>
        public int TimeOut { get; set; } = 200;
    }
}