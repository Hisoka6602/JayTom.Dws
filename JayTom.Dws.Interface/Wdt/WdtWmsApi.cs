using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Http;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Interface.Wdt {

    public class WdtWmsApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameter ApiParameters { get; set; } = new();

        public WdtWmsApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var data = new {
                logistics_no = barcode,
                weight = Math.Round(Convert.ToDecimal(weight), 3),
                is_weight = "Y"
            };
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var dictionary = new Dictionary<string, object>()
            {
                {"appkey",ApiParameters.AppKey},
                {"format","json"},
                {"method",ApiParameters.Method},
                {"sid",ApiParameters.Sid},
                {"sign_method","md5"},
                {"timestamp",timestamp},
            };

            var pairs = dictionary.OrderBy(o => o.Key);
            var signString = ApiParameters.AppSecret +
                             string.Join("", pairs?.Select(s => s.Key + s.Value) ?? Array.Empty<string>())
                             + JsonConvert.SerializeObject(data) + ApiParameters.AppSecret;

            //转MD5
            string sign;
            using (var md5 = MD5.Create()) {
                var result = md5.ComputeHash(Encoding.UTF8.GetBytes(signString));
                var strResult = BitConverter.ToString(result);
                sign = strResult.Replace("-", "");
            }
            dictionary.Add("sign", sign);
            dictionary.Remove("body");
            //拼接url
            var param = string.Join("&", dictionary?.OrderBy(o => o.Key)?.Select(s => s.Key + "=" + s.Value) ?? Array.Empty<string>());

            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(ApiParameters.TimeOut);
                HttpResponseMessage message;
                using (Stream dataStream =
                       new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                    using (HttpContent content = new StreamContent(dataStream)) {
                        content.Headers.Add("Content-Type", "text/xmlContent-Length");
                        message = await httpClient.PostAsync($"{ApiParameters.Url}?{param}", content, token)
                            .ConfigureAwait(false);
                    }
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //判断
                    var jObject = JObject.Parse(resultContent);
                    if (jObject["flag"]?.ToString()?.ToLower()?.Equals("success") == true) {
                        isSuccess = true;
                    }
                    else {
                        exceptionMsg = jObject["message"]?.ToString();
                    }
                }
                //判断是否成功条件
            }
            catch (HttpRequestException e) {
                isSuccess = false;
                resultContent = exceptionMsg = e.Message;
            }
            catch (AggregateException) {
                isSuccess = false;
                resultContent = exceptionMsg = "接口访问异常!";
            }
            catch (JsonException) {
                isSuccess = false;
                resultContent = exceptionMsg = "报文解析异常!";
            }
            catch (TaskCanceledException) {
                isSuccess = false;
                resultContent = exceptionMsg = "接口访问返回超时!";
            }
            catch (Exception e) {
                isSuccess = false;
                resultContent = exceptionMsg = e.Message;
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
                    RequestUrl = ApiParameters.Url,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, DateTime scanTime, double length = default, double width = default,
            double height = default, double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var data = new {
                logistics_no = barcode,
                weight = Math.Round(Convert.ToDecimal(weight), 3),
                is_weight = "Y"
            };
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var dictionary = new Dictionary<string, object>()
            {
                {"appkey",ApiParameters.AppKey},
                {"format","json"},
                {"method",ApiParameters.Method},
                {"sid",ApiParameters.Sid},
                {"sign_method","md5"},
                {"timestamp",timestamp},
            };

            var pairs = dictionary.OrderBy(o => o.Key);
            var signString = ApiParameters.AppSecret + string.Join("", pairs?.Select(s => s.Key + s.Value) ?? Array.Empty<string>()) + JsonConvert.SerializeObject(data) + ApiParameters.AppSecret;

            //转MD5
            string sign;
            using (var md5 = MD5.Create()) {
                var result = md5.ComputeHash(Encoding.UTF8.GetBytes(signString));
                var strResult = BitConverter.ToString(result);
                sign = strResult.Replace("-", "");
            }
            dictionary.Add("sign", sign);
            dictionary.Remove("body");
            //拼接url
            var param = string.Join("&", dictionary?.OrderBy(o => o.Key)?.Select(s => s.Key + "=" + s.Value) ?? Array.Empty<string>());

            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(ApiParameters.TimeOut);
                HttpResponseMessage message;
                using (Stream dataStream =
                       new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                    using (HttpContent content = new StreamContent(dataStream)) {
                        content.Headers.Add("Content-Type", "text/xmlContent-Length");
                        message = await httpClient.PostAsync($"{ApiParameters.Url}?{param}", content, token)
                            .ConfigureAwait(false);
                    }
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //判断
                    var jObject = JObject.Parse(resultContent);
                    if (jObject["flag"]?.ToString()?.ToLower()?.Equals("success") == true) {
                        isSuccess = true;
                    }
                    else {
                        exceptionMsg = jObject["message"]?.ToString();
                    }
                }
                //判断是否成功条件
            }
            catch (HttpRequestException e) {
                isSuccess = false;
                resultContent = exceptionMsg = e.Message;
            }
            catch (AggregateException) {
                isSuccess = false;
                resultContent = exceptionMsg = "接口访问异常!";
            }
            catch (JsonException) {
                isSuccess = false;
                resultContent = exceptionMsg = "报文解析异常!";
            }
            catch (TaskCanceledException) {
                isSuccess = false;
                resultContent = exceptionMsg = "接口访问返回超时!";
            }
            catch (Exception e) {
                isSuccess = false;
                resultContent = exceptionMsg = e.Message;
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
                    RequestUrl = ApiParameters.Url,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            if (parameters is ApiParameter param) {
                this.ApiParameters = new ApiParameter() {
                    AppSecret = param.AppSecret,
                    AppKey = param.AppKey,
                    Method = param.Method,
                    Sid = param.Sid,
                    TimeOut = param.TimeOut,
                    Url = param.Url,
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

        public class ApiParameter {
            public string Url { get; set; } = string.Empty;
            public string Sid { get; set; } = string.Empty;
            public string AppKey { get; set; } = string.Empty;
            public string AppSecret { get; set; } = string.Empty;
            public string Method { get; set; } = string.Empty;

            /// <summary>
            /// 超时
            /// </summary>
            public int TimeOut { get; set; } = 1000;
        }
    }
}