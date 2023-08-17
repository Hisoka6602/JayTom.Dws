using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.ResultOutputSettingsModel {

    public class HttpUploadSettingsInfoModel : BindableBase {
        private string _url = string.Empty;
        private string _successResponseContent = string.Empty;
        private int _timeout = 2000;

        /// <summary>
        /// Url地址
        /// </summary>
        public string Url {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        /// <summary>
        /// 成功回调内容
        /// </summary>
        public string SuccessResponseContent {
            get => _successResponseContent;
            set => SetProperty(ref _successResponseContent, value);
        }

        /// <summary>
        /// 请求超时时间
        /// </summary>
        public int Timeout {
            get => _timeout;
            set => SetProperty(ref _timeout, value);
        }
    }
}