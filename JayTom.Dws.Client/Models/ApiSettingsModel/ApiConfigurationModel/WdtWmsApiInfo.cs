using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel {

    public class WdtWmsApiInfo : BindableBase {
        private string _url = string.Empty;
        private string _sid = string.Empty;
        private string _appKey = string.Empty;
        private string _appSecret = string.Empty;
        private string _method = string.Empty;
        private int _timeOut = 1000;
        private bool _mustIncludeBoxBarcode;
        private string _anyStartCodes = string.Empty;

        public string Url {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        public string Sid {
            get => _sid;
            set => SetProperty(ref _sid, value);
        }

        public string AppKey {
            get => _appKey;
            set => SetProperty(ref _appKey, value);
        }

        public string AppSecret {
            get => _appSecret;
            set => SetProperty(ref _appSecret, value);
        }

        public string Method {
            get => _method;
            set => SetProperty(ref _method, value);
        }

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut {
            get => _timeOut;
            set => SetProperty(ref _timeOut, value);
        }

        /// <summary>
        /// 表示是否必须包含包装条码
        /// </summary>
        public bool MustIncludeBoxBarcode {
            get => _mustIncludeBoxBarcode;
            set => SetProperty(ref _mustIncludeBoxBarcode, value);
        }

        /// <summary>
        /// 指定条码开头
        /// </summary>
        public string AnyStartCodes {
            get => _anyStartCodes;
            set => SetProperty(ref _anyStartCodes, value);
        }
    }
}