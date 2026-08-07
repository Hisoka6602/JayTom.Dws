using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub;

namespace JayTom.Dws.Client.Models.PackageSorting
{

    public class GrayscaleDeviceInfoModel : BindableBase
    {
        private bool _isUseGrayscaleDetector;
        private bool _isCheckPackageOrientation;
        private TcpConnectionConfigItemInfoModel? _tcpConnectionConfigInfo;
        private Rectangle _mainFrameRegion;
        private Rectangle _additionalFrameRegion;
        private int _regionCarCount;
        private int _timeOut;
        private int _lineCarCount;
        private int _carNumberOffset;
        private bool _isDirectionReversed;
        private int _additionalBoxSpacePercentage;
        private int _minSendInterval;
        private int _mainBoxPackageRatio;

        /// <summary>
        /// 是否使用灰度仪检测包裹
        /// </summary>
        public bool IsUseGrayscaleDetector
        {
            get => _isUseGrayscaleDetector;
            set => SetProperty(ref _isUseGrayscaleDetector, value);
        }

        /// <summary>
        /// 是否判断包裹偏向
        /// </summary>
        public bool IsCheckPackageOrientation
        {
            get => _isCheckPackageOrientation;
            set => SetProperty(ref _isCheckPackageOrientation, value);
        }

        /// <summary>
        /// Tcp连接参数
        /// </summary>

        public TcpConnectionConfigItemInfoModel? TcpConnectionConfigInfo
        {
            get => _tcpConnectionConfigInfo;
            set => SetProperty(ref _tcpConnectionConfigInfo, value);
        }

        /// <summary>
        /// 主框架区域(矩形区域4个点)
        /// </summary>
        public Rectangle MainFrameRegion
        {
            get => _mainFrameRegion;
            set => SetProperty(ref _mainFrameRegion, value);
        }

        /// <summary>
        /// 附加框架区域(矩形区域4个点)
        /// </summary>
        public Rectangle AdditionalFrameRegion
        {
            get => _additionalFrameRegion;
            set => SetProperty(ref _additionalFrameRegion, value);
        }

        /// <summary>
        /// 区域内包含的小车数量
        /// </summary>
        public int RegionCarCount
        {
            get => _regionCarCount;
            set => SetProperty(ref _regionCarCount, value);
        }

        /// <summary>
        /// 超时时间
        /// </summary>
        public int TimeOut
        {
            get => _timeOut;
            set => SetProperty(ref _timeOut, value);
        }

        /// <summary>
        /// 线体小车数量
        /// </summary>
        public int LineCarCount
        {
            get => _lineCarCount;
            set => SetProperty(ref _lineCarCount, value);
        }

        /// <summary>
        /// 小车取数偏移
        /// </summary>
        public int CarNumberOffset
        {
            get => _carNumberOffset;
            set => SetProperty(ref _carNumberOffset, value);
        }

        /// <summary>
        /// 方向是否取反
        /// </summary>
        public bool IsDirectionReversed
        {
            get => _isDirectionReversed;
            set => SetProperty(ref _isDirectionReversed, value);
        }

        /// <summary>
        /// 占用附加框属性百分比
        /// </summary>
        public int AdditionalBoxSpacePercentage
        {
            get => _additionalBoxSpacePercentage;
            set => SetProperty(ref _additionalBoxSpacePercentage, value);
        }

        /// <summary>
        /// 最小发送间隔，单位为毫秒
        /// </summary>
        public int MinSendInterval
        {
            get => _minSendInterval;
            set => SetProperty(ref _minSendInterval, value);
        }

        /// <summary>
        /// 主框包裹包裹判断占比
        /// </summary>
        public int MainBoxPackageRatio
        {
            get => _mainBoxPackageRatio;
            set => SetProperty(ref _mainBoxPackageRatio, value);
        }
    }
}