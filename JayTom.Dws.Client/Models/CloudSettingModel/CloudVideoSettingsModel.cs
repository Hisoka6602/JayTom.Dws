using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.CloudSettingModel
{

    public class CloudVideoSettingsModel : BindableBase
    {
        private bool _isUseCloudVideoUpload;
        private int _retryAttempts = 5;
        private bool _isAutoUploadUnsyncedData;
        private int _concurrency = 2;

        //private string _url = string.Empty;
        private int _requestTimeout = 2000;

        private string _nodeName = string.Empty;
        private string _loginName = string.Empty;
        private string _webDoMain = string.Empty;
        private int _uploadIntervalInSeconds = 20;

        /// <summary>
        /// 是否开启云视频上传
        /// </summary>
        public bool IsUseCloudVideoUpload
        {
            get => _isUseCloudVideoUpload;
            set => SetProperty(ref _isUseCloudVideoUpload, value);
        }

        /// <summary>
        /// 失败重试次数
        /// </summary>
        public int RetryAttempts
        {
            get => _retryAttempts;
            set => SetProperty(ref _retryAttempts, value);
        }

        /// <summary>
        /// 是否自动上传未同步的数据
        /// </summary>
        public bool IsAutoUploadUnsyncedData
        {
            get => _isAutoUploadUnsyncedData;
            set => SetProperty(ref _isAutoUploadUnsyncedData, value);
        }

        /// <summary>
        /// 并发数 (1-10)
        /// </summary>
        public int Concurrency
        {
            get => _concurrency;
            set => SetProperty(ref _concurrency, value);
        }

        /*/// <summary>
        /// url
        /// </summary>
        public string Url {
            get => _url;
            set => SetProperty(ref _url, value);
        }*/

        /// <summary>
        /// 请求超时时间
        /// </summary>
        public int RequestTimeout
        {
            get => _requestTimeout;
            set => SetProperty(ref _requestTimeout, value);
        }

        /// <summary>
        /// 节点名称
        /// </summary>
        public string NodeName
        {
            get => _nodeName;
            set => SetProperty(ref _nodeName, value);
        }

        /// <summary>
        ///  登录名
        /// </summary>
        public string LoginName
        {
            get => _loginName;
            set => SetProperty(ref _loginName, value);
        }

        /// <summary>
        /// ip/域名
        /// </summary>
        public string WebDoMain
        {
            get => _webDoMain;
            set => SetProperty(ref _webDoMain, value);
        }

        /// <summary>
        /// 上传间隔
        /// </summary>
        public int UploadIntervalInSeconds
        {
            get => _uploadIntervalInSeconds;
            set => SetProperty(ref _uploadIntervalInSeconds, value);
        }
    }
}