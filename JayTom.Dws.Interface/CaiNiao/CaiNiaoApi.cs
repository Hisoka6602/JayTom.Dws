using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Security.Policy;
using System.Collections.Generic;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Text.RegularExpressions;
using static JayTom.Dws.Interface.CaiNiao.CaiNiaoApi;

namespace JayTom.Dws.Interface.CaiNiao {

    public class CaiNiaoApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameters Parameters { get; private set; } = new();

        public CaiNiaoApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            //请求格口
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var requestTime = DateTime.Now;
            var data = new {
                source = Parameters.Source,
                version = Parameters.Version,
                requestId = new DateTimeOffset(requestTime).ToUnixTimeSeconds(),
                data = new object[]
                {
                    new
                    {
                        command="sorter.dest_request",
                        @params=new
                        {
                            barCode=barcode,
                            weight=0,
                            length=0,
                            width=0,
                            height=0,
                            bcrCode= Parameters.BcrCode,
                            bcrName=Parameters.BcrName,
                            foldFlag=(other is true)?0:1
                        }
                    }
                },
            };

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                    HttpResponseMessage message;
                    using (Stream dataStream =
                           new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");
                            message = await httpClient.PostAsync(Parameters.Url, content, token)
                                .ConfigureAwait(false);
                        }
                    }

                    resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    resultContent = Regex.Unescape(resultContent);
                    if (!string.IsNullOrWhiteSpace(resultContent)) {
                        //判断
                        var jObject = JObject.Parse(resultContent);

                        if (jObject["result"] is not null) {
                            var jArray = JArray.Parse(jObject["result"]?.ToString() ?? string.Empty);
                            isSuccess = jArray.FirstOrDefault()?["code"]?.ToString() == "0";
                        }
                    }
                    //判断是否成功条件
                }
            }
            catch (HttpRequestException e) {
                isSuccess = false;
                resultContent += exceptionMsg = e.Message;
            }
            catch (AggregateException) {
                isSuccess = false;
                resultContent += exceptionMsg = "接口访问异常!";
            }
            catch (JsonException) {
                isSuccess = false;
                resultContent += exceptionMsg = "报文解析异常!";
            }
            catch (TaskCanceledException) {
                isSuccess = false;
                resultContent += exceptionMsg = "接口访问返回超时!";
            }
            catch (Exception e) {
                isSuccess = false;
                resultContent += exceptionMsg = e.Message;
            }
            finally {
                stopwatch.Stop();
                response = new UploadResponse() {
                    ExceptionMsg = exceptionMsg,
                    ApiParameters = JsonConvert.SerializeObject(this),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(data),
                    RequestTime = requestTime,
                    RequestUrl = Parameters.Url,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, DateTime scanTime, double length = default, double width = default,
            double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            //请求格口
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var requestTime = DateTime.Now;
            var data = new {
                source = Parameters.Source,
                version = Parameters.Version,
                requestId = new DateTimeOffset(requestTime).ToUnixTimeSeconds(),
                data = new object[]
                {
                    new
                    {
                        command="sorter.dest_request",
                        @params=new
                        {
                            barCode=barcode,
                            weight=0,
                            length=0,
                            width=0,
                            height=0,
                            bcrCode= Parameters.BcrCode,
                            bcrName=Parameters.BcrName,
                            foldFlag=(other is true)?0:1
                        }
                    }
                },
            };

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                    HttpResponseMessage message;
                    using (Stream dataStream =
                           new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");
                            message = await httpClient.PostAsync(Parameters.Url, content, token)
                                .ConfigureAwait(false);
                        }
                    }

                    resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    resultContent = Regex.Unescape(resultContent);
                    if (!string.IsNullOrWhiteSpace(resultContent)) {
                        //判断
                        var jObject = JObject.Parse(resultContent);

                        if (jObject["result"] is not null) {
                            var jArray = JArray.Parse(jObject["result"]?.ToString() ?? string.Empty);
                            isSuccess = jArray.FirstOrDefault()?["code"]?.ToString() == "0";
                        }
                    }
                    //判断是否成功条件
                }
            }
            catch (HttpRequestException e) {
                isSuccess = false;
                resultContent += exceptionMsg = e.Message;
            }
            catch (AggregateException) {
                isSuccess = false;
                resultContent += exceptionMsg = "接口访问异常!";
            }
            catch (JsonException) {
                isSuccess = false;
                resultContent += exceptionMsg = "报文解析异常!";
            }
            catch (TaskCanceledException) {
                isSuccess = false;
                resultContent += exceptionMsg = "接口访问返回超时!";
            }
            catch (Exception e) {
                isSuccess = false;
                resultContent += exceptionMsg = e.Message;
            }
            finally {
                stopwatch.Stop();
                response = new UploadResponse() {
                    ExceptionMsg = exceptionMsg,
                    ApiParameters = JsonConvert.SerializeObject(this),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(data),
                    RequestTime = requestTime,
                    RequestUrl = Parameters.Url,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            if (parameters is ApiParameters param) {
                Parameters.Url = param.Url;
                Parameters.TimeOut = param.TimeOut;
                Parameters.Source = param.Source;
                Parameters.Version = param.Version;
                Parameters.BcrCode = param.BcrCode;
                Parameters.BcrName = param.BcrName;
                return Task.FromResult(new KeyValuePair<bool, string>(true, string.Empty));
            }
            else {
                return Task.FromResult(new KeyValuePair<bool, string>(true, "参数类型不匹配"));
            }
        }

        public async void UploadInBackground(string barcode, double weight, DateTime scanTime, double length = default,
            double width = default, double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            if (other is ReportChuteInfo reportChuteInfo) {
                var resultContent = string.Empty;
                var requestTime = DateTime.Now;
                var data = new {
                    source = Parameters.Source,
                    version = Parameters.Version,
                    requestId = new DateTimeOffset(requestTime).ToUnixTimeSeconds(),
                    data = new object[]
                    {
                    new
                    {
                        command="sorter.sort_report",
                        @params=new
                        {
                            barCode=barcode,
                            chuteCode=new string(reportChuteInfo.ChuteCode.Where(char.IsDigit).ToArray()),
                            chuteCodePhysical=new string(reportChuteInfo.ChuteCodePhysical.Where(char.IsDigit).ToArray()),
                            status=reportChuteInfo.Status,
                            errorReson=reportChuteInfo.ErrorReson,
                            bcrCode= Parameters.BcrCode,
                            bcrName=Parameters.BcrName,
                        }
                    }
                    },
                };

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                try {
                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                        HttpResponseMessage message;
                        using (Stream dataStream =
                               new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync(Parameters.Url, content, token)
                                    .ConfigureAwait(false);
                            }
                        }

                        resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                        resultContent = Regex.Unescape(resultContent);
                    }
                }
                finally {
                    stopwatch.Stop();
                }
            }
        }

        public async void PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
            var resultContent = string.Empty;
            var requestTime = DateTime.Now;
            var data = new {
                source = Parameters.Source,
                version = Parameters.Version,
                requestId = new DateTimeOffset(requestTime).ToUnixTimeSeconds(),
                data = new object[]
                {
                    new
                    {
                        command="sorter.batch_report",
                        @params=new
                        {
                            barCodeList=packageItems,
                            chuteCode=new string(packageExit.Where(char.IsDigit).ToArray()) ,
                            bcrCode= Parameters.BcrCode,
                            bcrName=Parameters.BcrName,
                        }
                    }
                },
            };

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                    HttpResponseMessage message;
                    using (Stream dataStream =
                           new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");
                            message = await httpClient.PostAsync(Parameters.Url, content, token)
                                .ConfigureAwait(false);
                        }
                    }

                    resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    resultContent = Regex.Unescape(resultContent);
                }
            }
            finally {
                stopwatch.Stop();
            }
        }

        public class ApiParameters {

            /// <summary>
            /// Url
            /// </summary>
            public string Url { get; set; } = "http://10.220.64.463:10002/ucs/api";

            /// <summary>
            /// 超时
            /// </summary>
            public int TimeOut { get; set; } = 1000;

            /// <summary>
            /// SignKey
            /// </summary>
            public string Source { get; set; } = "test";

            /// <summary>
            /// 版本
            /// </summary>
            public int Version { get; set; } = 1;

            /// <summary>
            /// 设备代码
            /// </summary>
            public string BcrCode { get; set; } = "BCR02";

            /// <summary>
            /// 设备名称
            /// </summary>
            public string BcrName { get; set; } = "sorter";
        }

        public class ReportChuteInfo {
            public string ChuteCode { get; set; } = string.Empty;
            public string ChuteCodePhysical { get; set; } = string.Empty;
            public string ErrorReson { get; set; } = string.Empty;
            public int Status { get; set; }
        }
    }
}