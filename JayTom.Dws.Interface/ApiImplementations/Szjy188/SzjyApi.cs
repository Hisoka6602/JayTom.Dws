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
using System.Collections.Generic;
using JayTom.Dws.Domain.Interface;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;
using JayTom.Dws.Domain.Interface.Attributes;
using static JayTom.Dws.Interface.ApiImplementations.Szjy188.SzjyApi;

namespace JayTom.Dws.Interface.ApiImplementations.Szjy188 {

    [ApiClass("神州集运Api", "SzjyApi", "SzjyApiParameter", "1.0", ExecutionType.UploadInformation)]
    public class SzjyApi : IApiUploader<ApiParameter> {
        private readonly IHttpClientFactory _httpClientFactory;
        public static int? _uid = null;

        public SzjyApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
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
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                HttpResponseMessage message;
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{Parameters.Url}{method}", content, token)
                        .ConfigureAwait(false);
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);

                var logInResultsInfo = JsonConvert.DeserializeObject<LogInResultsInfo>(resultContent);
                if (logInResultsInfo is not null) {
                    _uid = logInResultsInfo.Uid;
                }
                return new KeyValuePair<bool, LogInResultsInfo?>(true, logInResultsInfo);
            }
            catch (Exception e) {
                return new KeyValuePair<bool, LogInResultsInfo?>(false, new LogInResultsInfo() {
                    Message = e.Message
                });
            }
        }

        public class ApiParameter : BaseApiParameters {

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

        public ApiParameter Parameters { get; private set; } = new();

        public bool SetParameters(object parameters) {
            if (parameters is not ApiParameter param) return false;
            Parameters = param;
            return true;
        }

        public void OpenJsonConfigFile() {
        }

        public async Task<UploadResponse> UploadInformation([NotNull] string barcode, [NotNull] double weight, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
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
                var (key, value) = await LogIn(Parameters.UserName, Parameters.Password, token);
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
                {"machine",Parameters.Machine},
                {"uid",_uid},
            };
            var urlJoin = string.Join("&", param.Select(s => $"{s.Key}={s.Value}"));
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                var stringAsync = await httpClient.GetStringAsync($"{Parameters.Url}{method}?{urlJoin}", token);

                resultContent = Regex.Unescape(stringAsync);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //判断
                    var uploadResultsInfo = JsonConvert.DeserializeObject<UploadResultsInfo>(resultContent);
                    if (uploadResultsInfo is not null && uploadResultsInfo.Result) {
                        isSuccess = true;
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
                    RequestContent = $"{Parameters.Url}{method}?{urlJoin}",
                    RequestTime = requestTime,
                    RequestUrl = $"{Parameters.Url}{method}?{urlJoin}",
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now,
                    ExecutionType = ExecutionType.UploadInformation
                };
            }
            return response;
        }

        public void ScanPackage([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
        }

        public Task<UploadResponse> SendSortingReport([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }

        public Task<UploadResponse> SendPickupReport([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }

        public Task<UploadResponse> SendConsolidationReport(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }

        public Task<UploadResponse> SendImage(string barcode, List<UploadImageInfo> uploadImagesInfos, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }

        public Task<UploadResponse> SendLockCommand(string lockIdentifier, object? other = null, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }

        public Task<UploadResponse> SendUnlockCommand(string lockIdentifier, object? other = null, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }

        public Task<UploadResponse> SendDeviceReport(string deviceIdentifier, string deviceStatus, object? other = null,
            CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }
    }
}