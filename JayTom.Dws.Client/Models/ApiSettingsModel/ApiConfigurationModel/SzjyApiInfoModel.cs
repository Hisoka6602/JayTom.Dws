using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel {

    public class SzjyApiInfoModel : BindableBase {
        private string _url = string.Empty;
        private string _userName = string.Empty;
        private string _password = string.Empty;
        private string _machine = string.Empty;
        private int _timeOut = 1000;

        /// <summary>
        /// Url
        /// </summary>
        public string Url {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        /// <summary>
        /// 账号
        /// </summary>
        public string UserName {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>
        /// 机器码
        /// </summary>
        public string Machine {
            get => _machine;
            set => SetProperty(ref _machine, value);
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