using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Service {

    public interface IPackageApi {

        /// <summary>
        /// 申请分拣回调
        /// </summary>
        /// <param name="info"></param>
        /// <param name="other"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<UploadResponse> RequestSortCallbackAsync(PackageInfoModel info, object? other = null, CancellationToken token = default);

        /// <summary>
        /// 提交分拣报告
        /// </summary>
        /// <param name="info"></param>
        /// <param name="other"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<UploadResponse> SubmitSortReportAsync(PackageInfoModel info, object? other = null, CancellationToken token = default);

        /// <summary>
        /// 上传信息
        /// </summary>
        /// <param name="info"></param>
        /// <param name="other"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<UploadResponse> SubmitPackageInfoAsync(PackageInfoModel info, object? other = null, CancellationToken token = default);

        /// <summary>
        /// 上传图片信息
        /// </summary>
        /// <param name="info"></param>
        /// <param name="other"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> UploadImageAsync(PackageInfoModel info, object? other = null, CancellationToken token = default);

        /// <summary>
        /// 设置接口参数
        /// </summary>
        /// <param name="params"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        bool SetInterfaceParams(BaseInterfaceParams @params, CancellationToken token = default);

        /// <summary>
        /// 设置接口参数
        /// </summary>
        /// <param name="paramsJson"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        bool SetInterfaceParams(string @paramsJson, CancellationToken token = default);

        /// <summary>
        /// 集包上传
        /// </summary>
        /// <param name="packageExit"></param>
        /// <param name="aggregatePackageCode"></param>
        /// <param name="packagingTime"></param>
        /// <param name="packageItems"></param>
        /// <param name="other"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<UploadResponse> PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime,
            List<string> packageItems, object? other = null, CancellationToken token = default);
    }

    public class UploadResponse {

        /// <summary>
        /// 请求内容
        /// </summary>
        public string RequestContent { get; set; } = string.Empty; // 请求内容

        /// <summary>
        /// 响应内容
        /// </summary>
        public string ResponseContent { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTime RequestTime { get; set; }

        /// <summary>
        /// 返回时间
        /// </summary>
        public DateTime ResponseTime { get; set; }

        /// <summary>
        /// 耗时(秒)
        /// </summary>
        public double Duration { get; set; }

        /// <summary>
        /// 接口参数
        /// </summary>
        public string ApiParameters { get; set; } = string.Empty;

        /// <summary>
        /// 请求地址
        /// </summary>
        public string RequestUrl { get; set; } = string.Empty;

        /// <summary>
        /// 异常信息
        /// </summary>
        public string ExceptionMsg { get; set; } = string.Empty;

        /// <summary>
        /// Api异常类型
        /// </summary>
        public ApiExceptionType ApiExceptionType { get; set; } = ApiExceptionType.None;

        /// <summary>
        /// 请求类型
        /// </summary>
        public RequestApiType RequestApiType { get; set; }
    }

    public enum RequestApiType {

        /// <summary>
        /// 格口请求
        /// </summary>
        [Description("格口请求")]
        ExitRequest,

        /// <summary>
        /// 分拣报告
        /// </summary>
        [Description("分拣报告")]
        SortingReport,

        /// <summary>
        /// 集包报告
        /// </summary>
        [Description("集包报告")]
        PackageAggregationReport,

        /// <summary>
        /// 信息上传
        /// </summary>
        [Description("信息上传")]
        InfoUpload
    }

    public enum ApiExceptionType {

        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// 访问超时
        /// </summary>
        Timeout = 1,

        /// <summary>
        /// Url无法访问
        /// </summary>
        UnreachableUrl = 2,

        /// <summary>
        /// 未通过逻辑效验
        /// </summary>
        LogicValidationFailed = 3,

        /// <summary>
        /// 内容解析异常
        /// </summary>
        ContentParsingException = 4,

        /// <summary>
        /// 其他
        /// </summary>
        Other = 5
    }

    public class BaseInterfaceParams {

        /// <summary>
        /// 接口URL
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 请求超时时间（毫秒）
        /// </summary>
        public int Timeout { get; set; } = 1000;

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// 重试间隔（毫秒）
        /// </summary>
        public int RetryInterval { get; set; } = 1000;

        /// <summary>
        /// 成功效验正则表达式
        /// </summary>
        public string SuccessValidationRegex { get; set; } = string.Empty;

        /// <summary>
        /// 参数规则名称
        /// </summary>
        public string ParamRuleName { get; set; } = string.Empty;
    }
}