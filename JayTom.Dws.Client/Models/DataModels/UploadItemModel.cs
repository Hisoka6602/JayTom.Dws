using System;
using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.DataModels
{
    public class UploadItemModel : BindableBase
    {
        private bool _isSuccess;
        private string _requestContent = string.Empty;
        private string _responseContent = string.Empty;
        private DateTime? _requestTime;
        private DateTime? _responseTime;
        private double _durationInSeconds;
        private string _interfaceParameters = string.Empty;
        private string _requestUrl = string.Empty;
        private string _exceptionMessage = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        /// <summary>
        /// 上传内容
        /// </summary>
        public string RequestContent
        {
            get => _requestContent;
            set => SetProperty(ref _requestContent, value);
        }

        /// <summary>
        /// 响应内容
        /// </summary>
        public string ResponseContent
        {
            get => _responseContent;
            set => SetProperty(ref _responseContent, value);
        }

        /// <summary>
        /// 请求时间
        /// </summary>
        public DateTime? RequestTime
        {
            get => _requestTime;
            set => SetProperty(ref _requestTime, value);
        }

        /// <summary>
        /// 响应时间
        /// </summary>
        public DateTime? ResponseTime
        {
            get => _responseTime;
            set => SetProperty(ref _responseTime, value);
        }

        /// <summary>
        /// 耗时(秒)
        /// </summary>
        public double DurationInSeconds
        {
            get => _durationInSeconds;
            set => SetProperty(ref _durationInSeconds, value);
        }

        /// <summary>
        /// 接口参数
        /// </summary>
        public string InterfaceParameters
        {
            get => _interfaceParameters;
            set => SetProperty(ref _interfaceParameters, value);
        }

        /// <summary>
        /// 请求地址
        /// </summary>
        public string RequestUrl
        {
            get => _requestUrl;
            set => SetProperty(ref _requestUrl, value);
        }

        /// <summary>
        /// 异常信息
        /// </summary>
        public string ExceptionMessage
        {
            get => _exceptionMessage;
            set => SetProperty(ref _exceptionMessage, value);
        }
    }
}