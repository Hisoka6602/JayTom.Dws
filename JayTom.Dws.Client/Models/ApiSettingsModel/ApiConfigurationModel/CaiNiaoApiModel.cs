using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel
{

    public class CaiNiaoApiModel : BindableBase
    {
        private string _url = "http://10.220.64.463:10002/ucs/api";
        private int _timeOut = 1000;
        private string _source = "test";
        private int _version = 1;
        private string _bcrCode = "BCR02";
        private string _bcrName = "sorter";

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

        /// <summary>
        /// SignKey
        /// </summary>
        public string Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        /// <summary>
        /// 版本
        /// </summary>
        public int Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        /// <summary>
        /// 设备代码
        /// </summary>
        public string BcrCode
        {
            get => _bcrCode;
            set => SetProperty(ref _bcrCode, value);
        }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string BcrName
        {
            get => _bcrName;
            set => SetProperty(ref _bcrName, value);
        }
    }
}