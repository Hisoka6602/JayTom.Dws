using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Abstractions.Geometry;
using JayTom.Dws.Legacy.Contracts.Dto.BaseInfoModels;

namespace JayTom.Dws.Legacy.Contracts.Dto {

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
        public Rectangle2D MainFrameRegion { get; set; } = new(0, 0, 300, 600);

        /// <summary>
        /// 附加框架区域(矩形区域4个点)
        /// </summary>
        public Rectangle2D AdditionalFrameRegion { get; set; } = new(0, 0, 300, 600);

        /// <summary>
        /// 区域内包含的小车数量
        /// </summary>
        public int RegionCarCount { get; set; } = 1;

        /// <summary>
        /// 超时时间
        /// </summary>
        public int TimeOut { get; set; } = 200;

        /// <summary>
        /// 线体小车数量
        /// </summary>
        public int LineCarCount { get; set; } = 100;

        /// <summary>
        /// 小车取数偏移
        /// </summary>
        public int CarNumberOffset { get; set; } = 0;

        /// <summary>
        /// 方向是否取反
        /// </summary>
        public bool IsDirectionReversed { get; set; }

        /// <summary>
        /// 占用附加框属性百分比
        /// </summary>
        public int AdditionalBoxSpacePercentage { get; set; } = 20;

        /// <summary>
        /// 最小发送间隔，单位为毫秒
        /// </summary>
        public int MinSendInterval { get; set; } = 300;

        /// <summary>
        /// 主框包裹包裹判断占比
        /// </summary>
        public int MainBoxPackageRatio { get; set; } = 15;
    }
}
