using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using JayTom.Dws.Domain.Service;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JayTom.Dws.Interface.WeciMexicoDv;
using System.Reflection.PortableExecutable;
using static JayTom.Dws.Interface.Szjy188.SzjyApi;

namespace JayTom.Dws.Interface.Szjy188 {

    public class SzjyApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public static int? _uid = null;
        public ApiParameter ApiParameters { get; set; } = new();

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut { get; private set; } = 10000;

        public SzjyApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, double length = default,
            double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            length -= 5;
            width -= 5;
            height -= 5;
            if (barcode.ToLower().Equals("noread") ||
                weight <= 0 || length <= 0 || width <= 0 ||
                height <= 0 || volume <= 0) {
                return new UploadResponse() {
                    ExceptionMsg = "条码不能为空,并且重量、体积不能为0",
                    ResponseContent = "条码不能为空,并且重量、体积不能为0!",
                    IsSuccess = false
                };
            }
            if (_uid is null) {
                var (key, value) = await LogIn(ApiParameters.UserName, ApiParameters.Password, token);
                if (key && value is not null) {
                    if (value.Status != 0) {
                        return new UploadResponse() {
                            ExceptionMsg = value.Message,
                            RequestContent = value.Message,
                            IsSuccess = false
                        };
                    }
                    else {
                        _uid = value.Uid;
                    }
                }
                else {
                    return new UploadResponse() {
                        ExceptionMsg = "登录连接错误!",
                        RequestContent = "登录连接错误!",
                        IsSuccess = false
                    };
                }
            }
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var method = "/add-entry-big";
            Dictionary<string, object> param = new()
            {
                {"sendcode",barcode},
                {"weight",Math.Round(Convert.ToDecimal(weight), 3)},
                {"length",length},
                {"width",width},
                {"height",height},
                {"machine",ApiParameters.Machine},
                {"uid",_uid},
            };
            var urlJoin = string.Join("&", param.Select(s => $"{s.Key}={s.Value}"));
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(TimeOut);
                    var stringAsync = await httpClient.GetStringAsync($"{ApiParameters.Url}{method}?{urlJoin}", token);

                    resultContent = Regex.Unescape(stringAsync);
                    if (!string.IsNullOrWhiteSpace(resultContent)) {
                        //判断
                        var uploadResultsInfo = JsonConvert.DeserializeObject<UploadResultsInfo>(resultContent);
                        if (uploadResultsInfo is not null && uploadResultsInfo.Result) {
                            isSuccess = true;
                        }
                    }
                }
            }
            catch (HttpRequestException e) {
                isSuccess = false;
                resultContent += e.Message;
            }
            catch (AggregateException) {
                isSuccess = false;
                resultContent += "接口访问异常!";
            }
            catch (JsonException) {
                isSuccess = false;
                resultContent += "报文解析异常!";
            }
            catch (TaskCanceledException) {
                isSuccess = false;
                resultContent += "接口访问返回超时!";
            }
            catch (Exception e) {
                isSuccess = false;
                resultContent = e.Message;
            }
            finally {
                stopwatch.Stop();
                response = new UploadResponse() {
                    ExceptionMsg = exceptionMsg,
                    ApiParameters = JsonConvert.SerializeObject(this),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = $"{ApiParameters.Url}{method}?{urlJoin}",
                    RequestTime = requestTime,
                    RequestUrl = $"{ApiParameters.Url}{method}?{urlJoin}",
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
                //临时记录上传耗时
                NLog.LogManager.GetCurrentClassLogger().Warn($"上传耗时:{stopwatch.Elapsed.TotalMilliseconds}(ms)");
            }
            return response;
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, DateTime scanTime,
            double length = default, double width = default,
            double height = default, double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            length -= 5;
            width -= 5;
            height -= 5;
            if (barcode.ToLower().Equals("noread") ||
                weight <= 0 || length <= 0 || width <= 0 ||
                height <= 0 || volume <= 0) {
                return new UploadResponse() {
                    ExceptionMsg = "条码不能为空,并且重量、体积不能为0",
                    ResponseContent = "条码不能为空,并且重量、体积不能为0!",
                    IsSuccess = false
                };
            }
            if (_uid is null) {
                var (key, value) = await LogIn(ApiParameters.UserName, ApiParameters.Password, token);
                if (key && value is not null) {
                    if (value.Status != 0) {
                        return new UploadResponse() {
                            ExceptionMsg = value.Message,
                            RequestContent = value.Message,
                            IsSuccess = false
                        };
                    }
                    else {
                        _uid = value.Uid;
                    }
                }
                else {
                    return new UploadResponse() {
                        ExceptionMsg = "登录连接错误!",
                        RequestContent = "登录连接错误!",
                        IsSuccess = false
                    };
                }
            }
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var method = "/add-entry-big";
            Dictionary<string, object> param = new()
            {
                {"sendcode",barcode},
                {"weight",weight},
                {"length",length},
                {"width",width},
                {"height",height},
                {"machine",ApiParameters.Machine},
                {"uid",_uid},
            };
            var urlJoin = string.Join("&", param.Select(s => $"{s.Key}={s.Value}"));
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(TimeOut);
                    var stringAsync = await httpClient.GetStringAsync($"{ApiParameters.Url}{method}?{urlJoin}", token);

                    resultContent = Regex.Unescape(stringAsync);
                    if (!string.IsNullOrWhiteSpace(resultContent)) {
                        //判断
                        var uploadResultsInfo = JsonConvert.DeserializeObject<UploadResultsInfo>(resultContent);
                        if (uploadResultsInfo is not null && uploadResultsInfo.Result) {
                            isSuccess = true;
                        }
                    }
                }
            }
            catch (HttpRequestException e) {
                isSuccess = false;
                resultContent += e.Message;
            }
            catch (AggregateException) {
                isSuccess = false;
                resultContent += "接口访问异常!";
            }
            catch (JsonException) {
                isSuccess = false;
                exceptionMsg += "报文解析异常!";
            }
            catch (TaskCanceledException) {
                isSuccess = false;
                resultContent += "接口访问返回超时!";
            }
            catch (Exception e) {
                isSuccess = false;
                resultContent += e.Message;
            }
            finally {
                stopwatch.Stop();
                response = new UploadResponse() {
                    ExceptionMsg = exceptionMsg,
                    ApiParameters = JsonConvert.SerializeObject(this),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = $"{ApiParameters.Url}{method}?{urlJoin}",
                    RequestTime = requestTime,
                    RequestUrl = $"{ApiParameters.Url}{method}?{urlJoin}",
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            if (parameters is ApiParameter param) {
                this.ApiParameters = new ApiParameter() {
                    Machine = param.Machine,
                    Password = param.Password,
                    TimeOut = param.TimeOut,
                    Url = param.Url,
                    UserName = param.UserName,
                };
                return Task.FromResult(new KeyValuePair<bool, string>(true, "设置成功!"));
            }
            else {
                return Task.FromResult(new KeyValuePair<bool, string>(false, "参数类型不匹配"));
            }
        }

        public void UploadInBackground(string barcode, double weight, DateTime scanTime, double length = default,
            double width = default, double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
        }

        public void PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="passWord"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, LogInResultsInfo?>> LogIn(string userName, string passWord, CancellationToken token = default) {
            string resultContent;
            var method = "/login";
            var data = new {
                username = userName,
                password = passWord,
            };
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(TimeOut);
                    HttpResponseMessage message;
                    using (Stream dataStream =
                           new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");
                            message = await httpClient.PostAsync($"{ApiParameters.Url}{method}", content, token)
                                .ConfigureAwait(false);
                        }
                    }

                    resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    resultContent = Regex.Unescape(resultContent);

                    var logInResultsInfo = JsonConvert.DeserializeObject<LogInResultsInfo>(resultContent);
                    if (logInResultsInfo is not null) {
                        _uid = logInResultsInfo.Uid;
                    }
                    return new KeyValuePair<bool, LogInResultsInfo?>(true, logInResultsInfo);
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, LogInResultsInfo?>(false, new LogInResultsInfo() {
                    Message = e.Message
                });
            }
        }

        public class ApiParameter {

            /// <summary>
            /// Url
            /// </summary>
            public string Url { get; set; } = string.Empty;

            /// <summary>
            /// 账号
            /// </summary>
            public string UserName { get; set; } = string.Empty;

            /// <summary>
            /// 密码
            /// </summary>
            public string Password { get; set; } = string.Empty;

            /// <summary>
            /// 机器码
            /// </summary>
            public string Machine { get; set; } = "default";

            /// <summary>
            /// 超时
            /// </summary>
            public int TimeOut { get; set; } = 1000;
        }

        public class LogInResultsInfo {

            /// <summary>
            /// 状态
            /// </summary>
            public int Status { get; set; }

            /// <summary>
            /// Uid
            /// </summary>
            public int Uid { get; set; }

            /// <summary>
            /// 名称
            /// </summary>
            public string UserName { get; set; } = string.Empty;

            /// <summary>
            /// 显示名称
            /// </summary>
            public string NickName { get; set; } = string.Empty;

            /// <summary>
            /// 消息
            /// </summary>
            public string Message { get; set; } = string.Empty;
        }

        public class UploadResultsInfo {

            /// <summary>
            /// 结果
            /// </summary>
            public bool Result { get; set; }

            /// <summary>
            /// 信息
            /// </summary>

            public string Message { get; set; } = string.Empty;

            /// <summary>
            /// 频道
            /// </summary>
            public int? ChannelCode { get; set; }
        }
    }
}