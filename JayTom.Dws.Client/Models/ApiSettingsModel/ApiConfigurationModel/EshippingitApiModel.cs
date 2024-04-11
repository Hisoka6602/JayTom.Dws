using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel {

    public class EshippingitApiModel : BindableBase {
        private string _domain = string.Empty;
        private int _timeOut;
        private string _authorization = string.Empty;
        private string _endpoint = string.Empty;
        private string _bucketName = string.Empty;
        private int _retryCount = 2;
        private int _retryInterval = 1;

        /// <summary>
        /// 域名
        /// </summary>
        public string Domain {
            get => _domain;
            set => SetProperty(ref _domain, value);
        }

        /// <summary>
        /// 超时时间
        /// </summary>
        public int TimeOut {
            get => _timeOut;
            set => SetProperty(ref _timeOut, value);
        }

        public string Authorization {
            get => _authorization;
            set => SetProperty(ref _authorization, value);
        }

        public string Endpoint {
            get => _endpoint;
            set => SetProperty(ref _endpoint, value);
        }

        public string BucketName {
            get => _bucketName;
            set => SetProperty(ref _bucketName, value);
        }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount {
            get => _retryCount;
            set => SetProperty(ref _retryCount, value);
        }

        /// <summary>
        /// 重试间隔(秒)
        /// </summary>
        public int RetryInterval {
            get => _retryInterval;
            set => SetProperty(ref _retryInterval, value);
        }
    }
}