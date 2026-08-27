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

namespace JayTom.Dws.Integrations.Wdt {

    public class WdtWmsApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameter ApiParameters { get; set; } = new();

        public WdtWmsApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UploadResponse> UploadData(string barcode, decimal weight, decimal length = default, decimal width = default, decimal height = default,
            decimal volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var data = new {
                logistics_no = barcode,
                weight = Math.Round(Convert.ToDecimal(weight), 3),
                is_weight = "Y",
                package_barcode = $"{other}"
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
                             string.Join("", pairs?.Select(s => s.Key + s.Value) ?? [])
                             + JsonConvert.SerializeObject(data) + ApiParameters.AppSecret;

            //转MD5
            // DWS-HEX-COMPACT: 外部接口签名要求使用无分隔符摘要。
            var sign = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(signString)));
            dictionary.Add("sign", sign);
            dictionary.Remove("body");
            //拼接url
            var param = string.Join("&", dictionary?.OrderBy(o => o.Key)?.Select(s => s.Key + "=" + s.Value) ?? []);

            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            //判断是否必须包含包装条码

            try {
                if (ApiParameters.MustIncludeBoxBarcode && string.IsNullOrEmpty(data.package_barcode)) {
                    //返回
                    resultContent = exceptionMsg = "包装码不能为空!";
                }
                else {
                    using var httpClient = _httpClientFactory.CreateClient(global::JayTom.Dws.Integrations.Contracts.ApiHttpClientNames.ExternalApi);
                    httpClient.Timeout = TimeSpan.FromMilliseconds(ApiParameters.TimeOut);
                    HttpResponseMessage message;
                    await using (Stream dataStream =
                                 new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using HttpContent content = new StreamContent(dataStream);
                        content.Headers.Add("Content-Type", "text/xmlContent-Length");
                        message = await httpClient.PostAsync($"{ApiParameters.Url}?{param}", content, token)
                            .ConfigureAwait(false);
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
                    ExceptionMsg = exceptionMsg ?? string.Empty,
                    ApiParameters = IntegrationParameterSerializer.Serialize(this),
                    IsSuccess = isSuccess,
                    DurationSeconds = Convert.ToDecimal(stopwatch.Elapsed.TotalSeconds),
                    RequestContent = JsonConvert.SerializeObject(data),
                    RequestTime = requestTime,
                    RequestUrl = ApiParameters.Url,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public async Task<UploadResponse> UploadData(string barcode, decimal weight, DateTime scanTime, decimal length = default, decimal width = default,
            decimal height = default, decimal volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var data = new {
                logistics_no = barcode,
                weight = Math.Round(Convert.ToDecimal(weight), 3),
                is_weight = "Y",
                package_barcode = $"{other}"
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
            var signString = ApiParameters.AppSecret + string.Join("", pairs?.Select(s => s.Key + s.Value) ?? []) + JsonConvert.SerializeObject(data) + ApiParameters.AppSecret;

            //转MD5
            // DWS-HEX-COMPACT: 外部接口签名要求使用无分隔符摘要。
            var sign = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(signString)));
            dictionary.Add("sign", sign);
            dictionary.Remove("body");
            //拼接url
            var param = string.Join("&", dictionary?.OrderBy(o => o.Key)?.Select(s => s.Key + "=" + s.Value) ?? []);

            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                if (ApiParameters.MustIncludeBoxBarcode && string.IsNullOrEmpty(data.package_barcode)) {
                    //返回
                    resultContent = exceptionMsg = "包装码不能为空!";
                }
                else {
                    using var httpClient = _httpClientFactory.CreateClient(global::JayTom.Dws.Integrations.Contracts.ApiHttpClientNames.ExternalApi);
                    httpClient.Timeout = TimeSpan.FromMilliseconds(ApiParameters.TimeOut);
                    HttpResponseMessage message;
                    await using (Stream dataStream =
                                 new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using HttpContent content = new StreamContent(dataStream);
                        content.Headers.Add("Content-Type", "text/xmlContent-Length");
                        message = await httpClient.PostAsync($"{ApiParameters.Url}?{param}", content, token)
                            .ConfigureAwait(false);
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
                    ExceptionMsg = exceptionMsg ?? string.Empty,
                    ApiParameters = IntegrationParameterSerializer.Serialize(this),
                    IsSuccess = isSuccess,
                    DurationSeconds = Convert.ToDecimal(stopwatch.Elapsed.TotalSeconds),
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
                    MustIncludeBoxBarcode = param.MustIncludeBoxBarcode
                };
                return Task.FromResult(new KeyValuePair<bool, string>(true, "设置成功!"));
            }
            else {
                return Task.FromResult(new KeyValuePair<bool, string>(false, "参数类型不匹配"));
            }
        }

        public Task UploadInBackground(string barcode, decimal weight, DateTime scanTime, decimal length = default,
            decimal width = default, decimal height = default, decimal volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            return Task.CompletedTask;
        }

        public Task PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
            return Task.CompletedTask;
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

            /// <summary>
            /// 表示是否必须包含包装条码。
            /// </summary>
            public bool MustIncludeBoxBarcode { get; set; }

            /*/// <summary>
            /// 是否重量不能为0
            /// </summary>
            public bool IsWeightNonZero { get;  set; }*/
        }
    }
}
