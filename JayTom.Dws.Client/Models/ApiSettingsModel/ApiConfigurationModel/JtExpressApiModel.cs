using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel {

    public class JtExpressApiModel : BindableBase {
        private string _url = "https://opa.jtexpress.com.cn";
        private string _userName = string.Empty;
        private string _password = string.Empty;
        private string _appKey = "default";
        private string _appSecret = "default";
        private int _timeOut = 1000;
        private StringItemModel _scanTypeCode = new();
        private StringItemModel _transportTypeCode = new();
        private string _scanPda = string.Empty;
        private IntegerItemModel _scanType = new();
        private StringItemModel _weightFlag = new();
        private string _segmentCodeUrl = "https://opa.jtexpress.com.cn";
        private int _segmentCodeTimeOut = 1000;
        private IntegerItemModel _businessType = new();

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
        /// 超时
        /// </summary>
        public int TimeOut {
            get => _timeOut;
            set => SetProperty(ref _timeOut, value);
        }

        /// <summary>
        /// 条码类型
        /// </summary>
        public StringItemModel ScanTypeCode {
            get => _scanTypeCode;
            set => SetProperty(ref _scanTypeCode, value);
        }

        /// <summary>
        /// 运输方式id
        /// </summary>
        public StringItemModel TransportTypeCode {
            get => _transportTypeCode;
            set => SetProperty(ref _transportTypeCode, value);
        }

        /// <summary>
        /// 设备编号
        /// </summary>
        public string ScanPda {
            get => _scanPda;
            set => SetProperty(ref _scanPda, value);
        }

        /// <summary>
        /// 扫描类型
        /// </summary>
        public IntegerItemModel ScanType {
            get => _scanType;
            set => SetProperty(ref _scanType, value);
        }

        /// <summary>
        /// 重量标识
        /// </summary>
        public StringItemModel WeightFlag {
            get => _weightFlag;
            set => SetProperty(ref _weightFlag, value);
        }

        /// <summary>
        /// Url
        /// </summary>
        public string SegmentCodeUrl {
            get => _segmentCodeUrl;
            set => SetProperty(ref _segmentCodeUrl, value);
        }

        /// <summary>
        /// 超时
        /// </summary>
        public int SegmentCodeTimeOut {
            get => _segmentCodeTimeOut;
            set => SetProperty(ref _segmentCodeTimeOut, value);
        }

        /// <summary>
        /// 业务类型
        /// </summary>
        public IntegerItemModel BusinessType {
            get => _businessType;
            set => SetProperty(ref _businessType, value);
        }
    }
}