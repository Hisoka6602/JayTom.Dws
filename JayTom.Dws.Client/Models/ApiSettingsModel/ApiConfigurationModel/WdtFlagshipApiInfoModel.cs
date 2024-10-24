using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel {

    public class WdtFlagshipApiInfoModel : BindableBase {
        private string _url = string.Empty;
        private string _key = string.Empty;
        private string _appsecret = string.Empty;
        private string _sid = string.Empty;
        private string _method = string.Empty;
        private string _v = string.Empty;
        private string _salt = string.Empty;
        private int _packagerId;
        private string _operateTableName = string.Empty;
        private bool _force;
        private int _timeOut = 1000;
        private string _packagerNo = string.Empty;

        /// <summary>
        /// Url
        /// </summary>
        public string Url {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        /// <summary>
        /// Key
        /// </summary>
        public string Key {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        /// <summary>
        /// appsecret
        /// </summary>
        public string Appsecret {
            get => _appsecret;
            set => SetProperty(ref _appsecret, value);
        }

        /// <summary>
        /// sid
        /// </summary>
        public string Sid {
            get => _sid;
            set => SetProperty(ref _sid, value);
        }

        /// <summary>
        /// method
        /// </summary>
        public string Method {
            get => _method;
            set => SetProperty(ref _method, value);
        }

        /// <summary>
        /// v版本号
        /// </summary>
        public string V {
            get => _v;
            set => SetProperty(ref _v, value);
        }

        /// <summary>
        /// salt(加密)
        /// </summary>
        public string Salt {
            get => _salt;
            set => SetProperty(ref _salt, value);
        }

        /// <summary>
        /// 打包员Id
        /// </summary>
        public int PackagerId {
            get => _packagerId;
            set => SetProperty(ref _packagerId, value);
        }

        /// <summary>
        /// 打包员编号
        /// </summary>
        public string PackagerNo {
            get => _packagerNo;
            set => SetProperty(ref _packagerNo, value);
        }

        /// <summary>
        /// 打包台名称
        /// </summary>
        public string OperateTableName {
            get => _operateTableName;
            set => SetProperty(ref _operateTableName, value);
        }

        /// <summary>
        /// 是否强制称重
        /// </summary>
        public bool Force {
            get => _force;
            set => SetProperty(ref _force, value);
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