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

    [ApiClass("旺店通Wms-Api", "WdtWmsApi", "WdtWmsApiParameter", "1.0", ExecutionType.UploadInformation)]
    public class WdtWmsApi : IApiUploader<WdtWmsApi.ApiParameter> {
        private readonly IHttpClientFactory _httpClientFactory;

        public WdtWmsApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public class ApiParameter : BaseApiParameters {
            public string Sid { get; set; } = string.Empty;
            public string AppKey { get; set; } = string.Empty;
            public string AppSecret { get; set; } = string.Empty;
            public string Method { get; set; } = string.Empty;

            /// <summary>
            /// 表示是否必须包含包装条码。
            /// </summary>
            public bool MustIncludeBoxBarcode { get; set; }
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
            var data = new {
                logistics_no = barcode,
                weight = Math.Round(Convert.ToDecimal(weight), 3),
                is_weight = "Y",
                package_barcode = $"{other}"
            };
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var dictionary = new Dictionary<string, object>()
            {
                {"appkey",Parameters.AppKey},
                {"format","json"},
                {"method",Parameters.Method},
                {"sid",Parameters.Sid},
                {"sign_method","md5"},
                {"timestamp",timestamp},
            };

            var pairs = dictionary.OrderBy(o => o.Key);
            var signString = Parameters.AppSecret + string.Join("", pairs?.Select(s => s.Key + s.Value) ?? Array.Empty<string>()) + JsonConvert.SerializeObject(data) + Parameters.AppSecret;

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
                if (Parameters.MustIncludeBoxBarcode && string.IsNullOrEmpty(data.package_barcode)) {
                    //返回
                    resultContent = exceptionMsg = "包装码不能为空!";
                }
                else {
                    using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                    httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                    HttpResponseMessage message;
                    await using (Stream dataStream =
                                 new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using HttpContent content = new StreamContent(dataStream);
                        content.Headers.Add("Content-Type", "text/xmlContent-Length");
                        message = await httpClient.PostAsync($"{Parameters.Url}?{param}", content, token)
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
                    ApiParameters = JsonConvert.SerializeObject(this),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(data),
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