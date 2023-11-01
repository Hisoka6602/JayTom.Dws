using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel {

    public class WdtWmsApiInfo : BindableBase {
        private string _url = string.Empty;
        private string _sid = string.Empty;
        private string _appKey = string.Empty;
        private string _appSecret = string.Empty;
        private string _method = string.Empty;
        private int _timeOut = 1000;

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
    }
}