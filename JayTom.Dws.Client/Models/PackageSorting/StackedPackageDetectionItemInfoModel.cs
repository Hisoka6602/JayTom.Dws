using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Client.Models.CommunicationsSettingsModel;
using JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub;

namespace JayTom.Dws.Client.Models.PackageSorting {

    public class StackedPackageDetectionItemInfoModel : BindableBase {
        private bool _isStackedPackageDetection;
        private SerialPortConfigItemInfoModel? _serialPortConfigInfo;
        private TcpConnectionConfigItemInfoModel? _tcpConnectionConfigInfo;
        private string _regularExpression = string.Empty;
        private string _checkerContent = string.Empty;
        private int _timeout;
        private bool _isAutoExceptionSorting;

        /// <summary>
        /// 是否监测叠包
        /// </summary>
        public bool IsStackedPackageDetection {
            get => _isStackedPackageDetection;
            set => SetProperty(ref _isStackedPackageDetection, value);
        }

        /// <summary>
        /// 通讯方式
        /// </summary>
        public CommunicationsTypeInfoModel CommunicationsType { get; set; } = new();

        /// <summary>
        /// 串口配置
        /// </summary>
        public SerialPortConfigItemInfoModel? SerialPortConfigInfo {
            get => _serialPortConfigInfo;
            set => SetProperty(ref _serialPortConfigInfo, value);
        }

        /// <summary>
        /// Tcp配置
        /// </summary>
        public TcpConnectionConfigItemInfoModel? TcpConnectionConfigInfo {
            get => _tcpConnectionConfigInfo;
            set => SetProperty(ref _tcpConnectionConfigInfo, value);
        }

        /// <summary>
        /// 判断正则表达式
        /// </summary>
        public string RegularExpression {
            get => _regularExpression;
            set => SetProperty(ref _regularExpression, value);
        }

        /// <summary>
        /// 判断的字符
        /// </summary>
        public string CheckerContent {
            get => _checkerContent;
            set => SetProperty(ref _checkerContent, value);
        }

        /// <summary>
        /// 判断超时时间
        /// </summary>
        public int Timeout {
            get => _timeout;
            set => SetProperty(ref _timeout, value);
        }

        /// <summary>
        /// 是否自动异常口
        /// </summary>
        public bool IsAutoExceptionSorting {
            get => _isAutoExceptionSorting;
            set => SetProperty(ref _isAutoExceptionSorting, value);
        }
    }
}