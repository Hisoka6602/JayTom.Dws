using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using static Org.BouncyCastle.Bcpg.Attr.ImageAttrib;

namespace JayTom.Dws.Interface.Jushuitan {

    public class JushuitanErpApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiParameters _parameters = new();

        public JushuitanErpApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UploadResponse> UploadData([NotNull] string barcode, [NotNull] double weight, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            //请求格口
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var requestTime = DateTime.Now;
            var biz = new object[]
            {
               new {
                    f_volume = Math.Round(volume, 3) % 1 == 0
                        ? Math.Truncate(volume)
                        : Math.Round(volume, 3),
                    channel = _parameters.Channel,
                    weight = _parameters.IsUploadWeight ? Math.Round(weight, 3) : -1,
                    l_id = barcode,
                    type = _parameters.Type,
                    is_un_lid = _parameters.IsUnLid
                }
            };

            var parameters = new Dictionary<string, string> {
                ["app_key"] = _parameters.AppKey,
                ["access_token"] = _parameters.AccessToken,
                ["biz"] = JsonConvert.SerializeObject(biz),
                ["timestamp"] = DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                ["charset"] = "utf-8",
                ["version"] = _parameters.Version.ToString()
            };

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                //加密
                var sign = GenerateSign(parameters, _parameters.AppSecret);
                parameters["sign"] = sign;
                // 发请求
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(_parameters.TimeOut);

                using var content = new FormUrlEncodedContent(parameters);

                var message = await httpClient.PostAsync(_parameters.Url, content, token).ConfigureAwait(false);
                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //判断
                    var jObject = JObject.Parse(resultContent);
                    if (jObject["data"] is not null) {
                        var jArray = JArray.Parse(jObject["datas"]?.ToString() ?? string.Empty);
                        isSuccess = Convert.ToBoolean(jArray.FirstOrDefault()?["is_success"]);
                    }
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
                    RequestContent = JsonConvert.SerializeObject(parameters),
                    RequestTime = requestTime,
                    RequestUrl = _parameters.Url,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now,
                };
            }
            return response;
        }

        public async Task<UploadResponse> UploadData([NotNull] string barcode, [NotNull] double weight, DateTime scanTime, double length = default, double width = default,
            double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var requestTime = DateTime.Now;
            var biz = new object[]
            {
                new {
                    f_volume = Math.Round(volume, 3) % 1 == 0
                        ? Math.Truncate(volume)
                        : Math.Round(volume, 3),
                    channel = _parameters.Channel,
                    weight = _parameters.IsUploadWeight ? Math.Round(weight, 3) : -1,
                    l_id = barcode,
                    type = _parameters.Type,
                    is_un_lid = _parameters.IsUnLid
                }
            };

            var parameters = new Dictionary<string, string> {
                ["app_key"] = _parameters.AppKey,
                ["access_token"] = _parameters.AccessToken,
                ["biz"] = JsonConvert.SerializeObject(biz),
                ["timestamp"] = DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                ["charset"] = "utf-8",
                ["version"] = _parameters.Version.ToString()
            };

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                //加密
                var sign = GenerateSign(parameters, _parameters.AppSecret);
                parameters["sign"] = sign;
                // 发请求
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(_parameters.TimeOut);

                using var content = new FormUrlEncodedContent(parameters);

                var message = await httpClient.PostAsync(_parameters.Url, content, token).ConfigureAwait(false);
                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //判断
                    var jObject = JObject.Parse(resultContent);
                    if (jObject["data"] is not null) {
                        var jArray = JArray.Parse(jObject["datas"]?.ToString() ?? string.Empty);
                        isSuccess = Convert.ToBoolean(jArray.FirstOrDefault()?["is_success"]);
                    }
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
                    RequestContent = JsonConvert.SerializeObject(parameters),
                    RequestTime = requestTime,
                    RequestUrl = _parameters.Url,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now,
                };
            }
            return response;
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            if (parameters is ApiParameters param) {
                _parameters.Url = param.Url;
                _parameters.TimeOut = param.TimeOut;
                _parameters.AppKey = param.AppKey;
                _parameters.Version = param.Version;
                _parameters.AppSecret = param.AppSecret;
                _parameters.AccessToken = param.AccessToken;
                _parameters.IsUnLid = param.IsUnLid;
                _parameters.IsUploadWeight = param.IsUploadWeight;
                _parameters.Type = param.Type;
                _parameters.Channel = param.Channel;
                _parameters.LastTokenUpdateTime = param.LastTokenUpdateTime;
                _parameters.TokenExpireTime = param.TokenExpireTime;
                return Task.FromResult(new KeyValuePair<bool, string>(true, string.Empty));
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
        /// 签名
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="appSecret"></param>
        /// <returns></returns>
        private static string GenerateSign(Dictionary<string, string> parameters, string appSecret) {
            // 1. 按键名字典序排序
            var sortedKeys = parameters.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

            // 2. 按 key+value 拼接
            var paramStr = new StringBuilder();
            foreach (var key in sortedKeys) {
                paramStr.Append(key).Append(parameters[key]);
            }

            // 3. 拼接 appSecret 在前
            var signStr = appSecret + paramStr;

            // 4. 计算 MD5 并转小写
            using var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(signStr));
            var sb = new StringBuilder();
            foreach (var b in hashBytes)
                sb.Append(b.ToString("x2")); // x2 = 小写
            return sb.ToString();
        }

        /// <summary>
        /// 刷新 AccessToken
        /// </summary>
        /// <param name="token">取消令牌</param>
        public async Task<KeyValuePair<bool, string>> RefreshAccessTokenAsync(CancellationToken token = default) {
            bool isSuccess = false;
            var refreshUrl = "https://openapi.jushuitan.com/open/refresh/token";

            // 刷新 Token 请求参数
            var parameters = new Dictionary<string, string> {
                ["app_key"] = _parameters.AppKey,
                ["timestamp"] = DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                ["grant_type"] = "authorization_code",
                ["charset"] = "utf-8",
                ["code"] = new Random().Next(100000, 1000000).ToString(),
            };

            try {
                var sign = GenerateSign(parameters, _parameters.AppSecret);
                parameters["sign"] = sign;
                // 发请求
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(5000);

                using var content = new FormUrlEncodedContent(parameters);

                var message = await httpClient.PostAsync(refreshUrl, content, token).ConfigureAwait(false);
                var resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //判断
                    var jObject = JObject.Parse(resultContent);
                    if (jObject["code"] is not null) {
                        isSuccess = (bool)jObject["code"]?.ToString().Equals("0");
                    }
                }
                return new KeyValuePair<bool, string>(isSuccess, resultContent);
            }
            catch (Exception ex) {
                return new KeyValuePair<bool, string>(false, $"{ex}");
            }
        }

        public class ApiParameters {

            /// <summary>
            /// Url
            /// </summary>
            public string Url { get; set; } = "https://openapi.jushuitan.com/open/orders/weight/send/upload";

            /// <summary>
            /// 超时
            /// </summary>
            public int TimeOut { get; set; } = 1000;

            /// <summary>
            /// AppKey
            /// </summary>
            public string AppKey { get; set; } = string.Empty;

            /// <summary>
            /// AppSecret
            /// </summary>
            public string AppSecret { get; set; } = string.Empty;

            /// <summary>
            /// AccessToken
            /// </summary>
            public string AccessToken { get; set; } = string.Empty;

            /// <summary>
            /// 版本
            /// </summary>
            public int Version { get; set; } = 2;

            /// <summary>
            /// 是否上传重量（默认值 true）
            /// </summary>
            public bool IsUploadWeight { get; set; } = true;

            /// <summary>
            /// 称重类型（默认值为 1）
            /// 0: 验货后称重
            /// 1: 验货后称重并发货
            /// 2: 无须验货称重
            /// 3: 无须验货称重并发货
            /// 4: 发货后称重
            /// 5: 自动判断称重并发货
            /// </summary>
            public int Type { get; set; } = 1;

            /// <summary>
            /// 是否为国际运单号（默认值 false，表示国内快递）
            /// </summary>
            public bool IsUnLid { get; set; } = false;

            /// <summary>
            /// 称重来源备注（会显示在订单操作日志中）
            /// </summary>
            public string Channel { get; set; } = string.Empty;

            /// <summary>
            /// 上次更新 Token 的时间
            /// </summary>
            public DateTime LastTokenUpdateTime { get; set; } = DateTime.MinValue;

            /// <summary>
            /// Token 到期时间
            /// </summary>
            public DateTime TokenExpireTime { get; set; } = DateTime.MinValue;
        }
    }
}