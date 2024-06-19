using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub;

namespace JayTom.Dws.Client.Models.PackageSorting {

    public class GrayscaleDeviceInfoModel : BindableBase {
        private bool _isUseGrayscaleDetector;
        private bool _isCheckPackageOrientation;
        private TcpConnectionConfigItemInfoModel? _tcpConnectionConfigInfo;
        private Rectangle _mainFrameRegion;
        private Rectangle _additionalFrameRegion;
        private int _regionCarCount;
        private int _timeOut;

        /// <summary>
        /// 是否使用灰度仪检测包裹
        /// </summary>
        public bool IsUseGrayscaleDetector {
            get => _isUseGrayscaleDetector;
            set => SetProperty(ref _isUseGrayscaleDetector, value);
        }

        /// <summary>
        /// 是否判断包裹偏向
        /// </summary>
        public bool IsCheckPackageOrientation {
            get => _isCheckPackageOrientation;
            set => SetProperty(ref _isCheckPackageOrientation, value);
        }

        /// <summary>
        /// Tcp连接参数
        /// </summary>

        public TcpConnectionConfigItemInfoModel? TcpConnectionConfigInfo {
            get => _tcpConnectionConfigInfo;
            set => SetProperty(ref _tcpConnectionConfigInfo, value);
        }

        /// <summary>
        /// 主框架区域(矩形区域4个点)
        /// </summary>
        public Rectangle MainFrameRegion {
            get => _mainFrameRegion;
            set => SetProperty(ref _mainFrameRegion, value);
        }

        /// <summary>
        /// 附加框架区域(矩形区域4个点)
        /// </summary>
        public Rectangle AdditionalFrameRegion {
            get => _additionalFrameRegion;
            set => SetProperty(ref _additionalFrameRegion, value);
        }

        /// <summary>
        /// 区域内包含的小车数量
        /// </summary>
        public int RegionCarCount {
            get => _regionCarCount;
            set => SetProperty(ref _regionCarCount, value);
        }

        /// <summary>
        /// 超时时间
        /// </summary>
        public int TimeOut {
            get => _timeOut;
            set => SetProperty(ref _timeOut, value);
        }
    }
}