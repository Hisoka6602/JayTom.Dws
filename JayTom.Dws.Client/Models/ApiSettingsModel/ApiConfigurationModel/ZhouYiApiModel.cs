using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel
{

    public class ZhouYiApiModel : BindableBase
    {
        private string _url = "http://api.zygp.site/openapi/express/fjUpload";
        private int _timeOut = 10000;
        private string _applicationCode = string.Empty;
        private string _appKey = string.Empty;
        private bool _needUpload;
        private bool _isFstCode;

        /// <summary>
        /// Url
        /// </summary>
        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut
        {
            get => _timeOut;
            set => SetProperty(ref _timeOut, value);
        }

        [Newtonsoft.Json.JsonProperty("AppId")]
        public string ApplicationCode
        {
            get => _applicationCode;
            set => SetProperty(ref _applicationCode, value);
        }

        public string AppKey
        {
            get => _appKey;
            set => SetProperty(ref _appKey, value);
        }

        public bool NeedUpload
        {
            get => _needUpload;
            set => SetProperty(ref _needUpload, value);
        }

        public bool IsFstCode
        {
            get => _isFstCode;
            set => SetProperty(ref _isFstCode, value);
        }
    }
}
