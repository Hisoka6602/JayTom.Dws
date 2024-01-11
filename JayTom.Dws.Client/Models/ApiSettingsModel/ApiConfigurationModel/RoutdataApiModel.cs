using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel {

    public class RoutDataApiModel : BindableBase {
        private string _url = string.Empty;
        private int _timeOut = 1000;
        private string _signKey = string.Empty;
        private int _retryCount;
        private int _retryInterval;
        private string _deviceCode = string.Empty;
        private string _orgCode = string.Empty;

        /// <summary>
        /// Url
        /// </summary>
        public string Url {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut {
            get => _timeOut;
            set => SetProperty(ref _timeOut, value);
        }

        /// <summary>
        /// SignKey
        /// </summary>
        public string SignKey {
            get => _signKey;
            set => SetProperty(ref _signKey, value);
        }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount {
            get => _retryCount;
            set => SetProperty(ref _retryCount, value);
        }

        /// <summary>
        /// 重试间隔
        /// </summary>
        public int RetryInterval {
            get => _retryInterval;
            set => SetProperty(ref _retryInterval, value);
        }

        /// <summary>
        /// 设备代码
        /// </summary>
        public string DeviceCode {
            get => _deviceCode;
            set => SetProperty(ref _deviceCode, value);
        }

        /// <summary>
        /// 机构代码
        /// </summary>
        public string OrgCode {
            get => _orgCode;
            set => SetProperty(ref _orgCode, value);
        }
    }
}