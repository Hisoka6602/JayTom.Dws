using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.LogsItemModels
{

    public class ApiLogItemModel : BaseLogItemModel
    {
        private string _requestContent = string.Empty;
        private string _responseContent = string.Empty;
        private DateTime _requestTime;
        private DateTime _responseTime;
        private double _duration;
        private string _apiParameters = string.Empty;
        private string _exceptionMsg = string.Empty;
        private string _url = string.Empty;

        /// <summary>
        /// 请求内容
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
        /// 上传时间
        /// </summary>
        public DateTime RequestTime
        {
            get => _requestTime;
            set => SetProperty(ref _requestTime, value);
        }

        /// <summary>
        /// 返回时间
        /// </summary>
        public DateTime ResponseTime
        {
            get => _responseTime;
            set => SetProperty(ref _responseTime, value);
        }

        /// <summary>
        /// 耗时(秒)
        /// </summary>
        public double Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        /// <summary>
        /// 接口参数
        /// </summary>
        public string ApiParameters
        {
            get => _apiParameters;
            set => SetProperty(ref _apiParameters, value);
        }

        /// <summary>
        /// 异常信息
        /// </summary>
        public string ExceptionMsg
        {
            get => _exceptionMsg;
            set => SetProperty(ref _exceptionMsg, value);
        }

        /// <summary>
        /// Url
        /// </summary>
        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }
    }
}