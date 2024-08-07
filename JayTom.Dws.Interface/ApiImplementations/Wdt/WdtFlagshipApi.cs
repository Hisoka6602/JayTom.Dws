using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using JayTom.Dws.Domain.Interface;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Domain.Interface.Attributes;

namespace JayTom.Dws.Interface.ApiImplementations.Wdt {

    [ApiClass("旺店通旗舰版Api", "WdtFlagshipApi", "WdtFlagshipApiParameter", "1.0", ExecutionType.UploadInformation)]
    public class WdtFlagshipApi : IApiUploader<WdtFlagshipApi.ApiParameter> {
        private readonly IHttpClientFactory _httpClientFactory;

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
            var objects = Parameters.Method.Equals("wms.stockout.Sales.weighingExt") ?
                new object[] { barcode, string.Empty, roundedWeight, Parameters.PackagerId, Parameters.Force }
                : new object[] { barcode, string.Empty, roundedWeight, Parameters.PackagerId, Parameters.OperateTableName, Parameters.Force };

            var dictionary = new Dictionary<string, object>()
            {
                {"body",JsonConvert.SerializeObject(objects)},
                {"key",Parameters.Key},
                {"sid",Parameters.Sid},
                {"method",Parameters.Method},
                {"v",Parameters.V},
                {"salt",Parameters.Salt},
                {"timestamp",DateTimeOffset.Now.ToUnixTimeSeconds()- 1325347200},
            };
            var pairs = dictionary.OrderBy(o => o.Key);
            var signString = Parameters.Appsecret + string.Join("", pairs?.Select(s => s.Key + s.Value) ?? Array.Empty<string>()) + Parameters.Appsecret;

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
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                HttpResponseMessage message;
                using (Stream dataStream =
                       new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objects)))) {
                    using (HttpContent content = new StreamContent(dataStream)) {
                        content.Headers.Add("Content-Type", "application/json");
                        message = await httpClient.PostAsync($"{Parameters.Url}?{param}", content, token)
                            .ConfigureAwait(false);
                    }
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
                    ApiParameters = JsonConvert.SerializeObject(this),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(objects),
                    RequestTime = requestTime,
                    RequestUrl = Parameters.Url,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public class ApiParameter : BaseApiParameters {

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
            /// 打包台名称
            /// </summary>
            public string OperateTableName { get; set; } = string.Empty;

            /// <summary>
            /// 是否强制称重
            /// </summary>
            public bool Force { get; set; }
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
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;

            var roundedWeight = Math.Round(Convert.ToDecimal(weight), 3);
            var objects = Parameters.Method.Equals("wms.stockout.Sales.weighingExt") ?
                new object[] { barcode, string.Empty, roundedWeight, Parameters.PackagerId, Parameters.Force }
                : new object[] { barcode, string.Empty, roundedWeight, Parameters.PackagerId, Parameters.OperateTableName, Parameters.Force };

            var dictionary = new Dictionary<string, object>()
            {
                {"body",JsonConvert.SerializeObject(objects)},
                {"key",Parameters.Key},
                {"sid",Parameters.Sid},
                {"method",Parameters.Method},
                {"v",Parameters.V},
                {"salt",Parameters.Salt},
                {"timestamp",DateTimeOffset.Now.ToUnixTimeSeconds()- 1325347200},
            };
            var pairs = dictionary.OrderBy(o => o.Key);
            var signString = Parameters.Appsecret + string.Join("", pairs?.Select(s => s.Key + s.Value) ?? Array.Empty<string>()) + Parameters.Appsecret;

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
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                HttpResponseMessage message;
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objects)))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{Parameters.Url}?{param}", content, token)
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
                    ApiParameters = JsonConvert.SerializeObject(this.Parameters),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(objects),
                    RequestTime = requestTime,
                    RequestUrl = Parameters.Url,
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