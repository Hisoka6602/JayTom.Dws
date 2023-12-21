using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.CloudSettingModel {

    public class NvrClientSettingsModel : BindableBase {
        private string _ip = string.Empty;
        private int _port;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private int _channel;
        private bool _isUseBarcodeWatermark;
        private int _maxWatermarkTime;

        /// <summary>
        /// IP地址
        /// </summary>
        public string Ip {
            get => _ip;
            set => SetProperty(ref _ip, value);
        }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>
        /// 条码水印
        /// </summary>
        public bool IsUseBarcodeWatermark {
            get => _isUseBarcodeWatermark;
            set => SetProperty(ref _isUseBarcodeWatermark, value);
        }

        /// <summary>
        /// 最长水印时间
        /// </summary>
        public int MaxWatermarkTime {
            get => _maxWatermarkTime;
            set => SetProperty(ref _maxWatermarkTime, value);
        }
    }
}