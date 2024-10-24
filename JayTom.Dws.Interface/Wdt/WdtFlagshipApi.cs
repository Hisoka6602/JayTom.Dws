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
using static JayTom.Dws.Interface.Szjy188.SzjyApi;

namespace JayTom.Dws.Interface.Wdt {

    public class WdtFlagshipApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameter ApiParameters { get; set; } = new();

        public WdtFlagshipApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var roundedWeight = Math.Round(Convert.ToDecimal(weight), 3);
            var objects = ApiParameters.Method switch {
                "wms.stockout.Sales.weighingExt" => new object[]
                {
                    barcode, string.Empty, roundedWeight, ApiParameters.PackagerId, ApiParameters.Force
                },
                "wms.stockout.Sales.onceWeighing" => new object[]
                {
                    barcode, string.Empty, roundedWeight, ApiParameters.PackagerId, ApiParameters.OperateTableName,
                    ApiParameters.Force
                },
                "wms.stockout.Sales.onceWeighingByNo" => new object[]
                {
                    barcode, string.Empty, roundedWeight, ApiParameters.PackagerNo, ApiParameters.OperateTableName,
                    ApiParameters.Force
                },
                _ => new object[] { }
            };

            var dictionary = new Dictionary<string, object>()
            {
                {"body",JsonConvert.SerializeObject(objects)},
                {"key",ApiParameters.Key},
                {"sid",ApiParameters.Sid},
                {"method",ApiParameters.Method},
                {"v",ApiParameters.V},
                {"salt",ApiParameters.Salt},
                {"timestamp",DateTimeOffset.Now.ToUnixTimeSeconds()- 1325347200},
            };
            var pairs = dictionary.OrderBy(o => o.Key);
            var signString = ApiParameters.Appsecret + string.Join("", pairs?.Select(s => s.Key + s.Value) ?? Array.Empty<string>()) + ApiParameters.Appsecret;

            //转MD5
            string sign;
            using (var md5 = MD5.Create()) {
                var result = md5.ComputeHash(Encoding.UTF8.GetBytes(signString));
                var strResult = BitConverter.ToString(result);
                sign = strResult.Replace("-", "");
            }
            dictionary.Add("sign", sign.ToLower());
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
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objects)))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{ApiParameters.Url}?{param}", content, token)
                        .ConfigureAwait(false);
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //判断
                    var jObject = JObject.Parse(resultContent);
                    if (jObject["status"]?.ToString()?.ToUpper()?.Equals("0") == true) {
                        isSuccess = true;
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
                    ApiParameters = JsonConvert.SerializeObject(ApiParameters),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(objects),
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
            var roundedWeight = Math.Round(Convert.ToDecimal(weight), 3);
            var objects = ApiParameters.Method switch {
                "wms.stockout.Sales.weighingExt" => new object[]
                {
                    barcode, string.Empty, roundedWeight, ApiParameters.PackagerId, ApiParameters.Force
                },
                "wms.stockout.Sales.onceWeighing" => new object[]
                {
                    barcode, string.Empty, roundedWeight, ApiParameters.PackagerId, ApiParameters.OperateTableName,
                    ApiParameters.Force
                },
                "wms.stockout.Sales.onceWeighingByNo" => new object[]
                {
                    barcode, string.Empty, roundedWeight, ApiParameters.PackagerNo, ApiParameters.OperateTableName,
                    ApiParameters.Force
                },
                _ => new object[] { }
            };

            var dictionary = new Dictionary<string, object>()
            {
                {"body",JsonConvert.SerializeObject(objects)},
                {"key",ApiParameters.Key},
                {"sid",ApiParameters.Sid},
                {"method",ApiParameters.Method},
                {"v",ApiParameters.V},
                {"salt",ApiParameters.Salt},
                {"timestamp",DateTimeOffset.Now.ToUnixTimeSeconds()- 1325347200},
            };
            var pairs = dictionary.OrderBy(o => o.Key);
            var signString = ApiParameters.Appsecret + string.Join("", pairs?.Select(s => s.Key + s.Value) ?? Array.Empty<string>()) + ApiParameters.Appsecret;

            //转MD5
            string sign;
            using (var md5 = MD5.Create()) {
                var result = md5.ComputeHash(Encoding.UTF8.GetBytes(signString));
                var strResult = BitConverter.ToString(result);
                sign = strResult.Replace("-", "");
            }
            dictionary.Add("sign", sign.ToLower());
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
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objects)))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{ApiParameters.Url}?{param}", content, token)
                        .ConfigureAwait(false);
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //判断
                    var jObject = JObject.Parse(resultContent);
                    if (jObject["status"]?.ToString()?.ToUpper()?.Equals("0") == true) {
                        isSuccess = true;
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
                    ApiParameters = JsonConvert.SerializeObject(ApiParameters),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(objects),
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
                    Appsecret = param.Appsecret,
                    Force = param.Force,
                    Key = param.Key,
                    Method = param.Method,
                    OperateTableName = param.OperateTableName,
                    PackagerId = param.PackagerId,
                    PackagerNo = param.PackagerNo,
                    Salt = param.Salt,
                    Sid = param.Sid,
                    TimeOut = param.TimeOut,
                    Url = param.Url,
                    V = param.V,
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

            /// <summary>
            /// Url
            /// </summary>
            public string Url { get; set; } = string.Empty;

            /// <summary>
            /// Key
            /// </summary>
            public string Key { get; set; } = string.Empty;

            /// <summary>
            /// appsecret
            /// </summary>
            public string Appsecret { get; set; } = string.Empty;

            /// <summary>
            /// sid
            /// </summary>
            public string Sid { get; set; } = string.Empty;

            /// <summary>
            /// method
            /// </summary>
            public string Method { get; set; } = string.Empty;

            /// <summary>
            /// v版本号
            /// </summary>
            public string V { get; set; } = string.Empty;

            /// <summary>
            /// salt(加密)
            /// </summary>
            public string Salt { get; set; } = string.Empty;

            /// <summary>
            /// 打包员Id
            /// </summary>
            public int PackagerId { get; set; }

            /// <summary>
            /// 打包员编号
            /// </summary>
            public string PackagerNo { get; set; } = string.Empty;

            /// <summary>
            /// 打包台名称
            /// </summary>
            public string OperateTableName { get; set; } = string.Empty;

            /// <summary>
            /// 是否强制称重
            /// </summary>
            public bool Force { get; set; }

            /// <summary>
            /// 超时
            /// </summary>
            public int TimeOut { get; set; } = 1000;
        }
    }
}