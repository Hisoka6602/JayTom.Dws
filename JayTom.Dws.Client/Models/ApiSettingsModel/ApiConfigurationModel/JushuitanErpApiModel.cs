using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel {

    public class JushuitanErpApiModel : BindableBase {
        private string _url = string.Empty;
        private int _timeOut = 1000;
        private string _appKey = string.Empty;
        private string _appSecret = string.Empty;
        private string _accessToken = string.Empty;
        private int _version = 2;
        private bool _isUploadWeight = true;
        private int _type = 1;
        private bool _isUnLid = false;
        private string _channel = string.Empty;
        private DateTime? _lastTokenUpdateTime;
        private DateTime? _tokenExpireTime;

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
        /// AppKey
        /// </summary>
        public string AppKey {
            get => _appKey;
            set => SetProperty(ref _appKey, value);
        }

        /// <summary>
        /// AppSecret
        /// </summary>
        public string AppSecret {
            get => _appSecret;
            set => SetProperty(ref _appSecret, value);
        }

        /// <summary>
        /// AccessToken
        /// </summary>
        public string AccessToken {
            get => _accessToken;
            set => SetProperty(ref _accessToken, value);
        }

        /// <summary>
        /// 版本
        /// </summary>
        public int Version {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        /// <summary>
        /// 是否上传重量（默认值 true）
        /// </summary>
        public bool IsUploadWeight {
            get => _isUploadWeight;
            set => SetProperty(ref _isUploadWeight, value);
        }

        /// <summary>
        /// 称重类型（默认值为 1）
        /// 0: 验货后称重
        /// 1: 验货后称重并发货
        /// 2: 无须验货称重
        /// 3: 无须验货称重并发货
        /// 4: 发货后称重
        /// 5: 自动判断称重并发货
        /// </summary>
        public int Type {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// 是否为国际运单号（默认值 false，表示国内快递）
        /// </summary>
        public bool IsUnLid {
            get => _isUnLid;
            set => SetProperty(ref _isUnLid, value);
        }

        /// <summary>
        /// 称重来源备注（会显示在订单操作日志中）
        /// </summary>
        public string Channel {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        /// <summary>
        /// 上次更新 Token 的时间
        /// </summary>
        public DateTime? LastTokenUpdateTime {
            get => _lastTokenUpdateTime;
            set => SetProperty(ref _lastTokenUpdateTime, value);
        }

        /// <summary>
        /// Token 到期时间
        /// </summary>
        public DateTime? TokenExpireTime {
            get => _tokenExpireTime;
            set => SetProperty(ref _tokenExpireTime, value);
        }
    }
}