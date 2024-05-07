using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub;

namespace JayTom.Dws.Client.Models.PackageSorting {

    public class GrayscaleDeviceInfoModel : BindableBase {
        private bool _isUseGrayscaleDetector;
        private bool _isCheckPackageOrientation;
        private TcpConnectionConfigItemInfoModel? _tcpConnectionConfigInfo;

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
    }
}