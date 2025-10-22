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

namespace JayTom.Dws.Interface.ZhouYi {

    public class ZhouYiApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        private ApiParameters _parameters = new();

        public ZhouYiApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        // Infrastructure/Http/InsuranceClient.cs 内部
        public async Task<UploadResponse> UploadData(
            string barcode, double weight, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            var requestTime = DateTime.Now;
            var weightValue = weight.ToString("0.###");
            var jobj = new JObject {
                ["sheetNo"] = barcode,
                ["segmentCode"] = string.Empty,
                ["needUpload"] = _parameters.NeedUpload,
                ["isFstCode"] = _parameters.IsFstCode,
                ["weight"] = new JRaw(weightValue)
            };

            var bodyStr = jobj.ToString(Formatting.None);

            var bodyStrNoWs = Regex.Replace(bodyStr, "\\s+", string.Empty);

            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            var signPlain = $"{_parameters.AppId}{bodyStrNoWs}{timestamp}{_parameters.AppKey}";

            string sign;
            using (var md5 = MD5.Create()) {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(signPlain));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                sign = sb.ToString();
            }

            using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
            httpClient.Timeout = TimeSpan.FromMilliseconds(_parameters.TimeOut);

            httpClient.DefaultRequestHeaders.Add("appid", _parameters.AppId);
            httpClient.DefaultRequestHeaders.Add("timestamp", timestamp.ToString());
            httpClient.DefaultRequestHeaders.Add("sign", sign);

            using var content = new StringContent(bodyStrNoWs, Encoding.UTF8, "application/json");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var isSuccess = false;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;

            try {
                var message = await httpClient.PostAsync(_parameters.Url, content, token).ConfigureAwait(false);
                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    var jObject = JObject.Parse(resultContent);
                    if (jObject["code"]?.ToString() == "0") isSuccess = true;
                }
            }
            catch (HttpRequestException e) {
                exceptionMsg = e.Message;
            }
            catch (TaskCanceledException) {
                exceptionMsg = "接口访问返回超时!";
            }
            catch (JsonException) {
                exceptionMsg = "报文解析异常!";
            }
            catch (Exception e) {
                exceptionMsg = e.Message;
            }
            finally {
                stopwatch.Stop();
            }

            return new UploadResponse {
                ExceptionMsg = exceptionMsg,
                ApiParameters = JsonConvert.SerializeObject(this),
                IsSuccess = isSuccess,
                Duration = stopwatch.Elapsed.TotalSeconds,
                RequestContent = bodyStrNoWs,
                RequestTime = requestTime,
                RequestUrl = _parameters?.Url ?? string.Empty,
                ResponseContent = resultContent,
                ResponseTime = DateTime.Now
            };
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, DateTime scanTime, double length = default, double width = default,
            double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            //请求格口
            var requestTime = DateTime.Now;
            var weightValue = weight.ToString("0.###");
            var jobj = new JObject {
                ["sheetNo"] = barcode,
                ["segmentCode"] = string.Empty,
                ["needUpload"] = _parameters.NeedUpload,
                ["isFstCode"] = _parameters.IsFstCode,
                ["weight"] = new JRaw(weightValue)
            };

            var bodyStr = jobj.ToString(Formatting.None);

            var bodyStrNoWs = Regex.Replace(bodyStr, "\\s+", string.Empty);

            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            var signPlain = $"{_parameters.AppId}{bodyStrNoWs}{timestamp}{_parameters.AppKey}";

            string sign;
            using (var md5 = MD5.Create()) {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(signPlain));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                sign = sb.ToString();
            }

            using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
            httpClient.Timeout = TimeSpan.FromMilliseconds(_parameters.TimeOut);

            httpClient.DefaultRequestHeaders.Add("appid", _parameters.AppId);
            httpClient.DefaultRequestHeaders.Add("timestamp", timestamp.ToString());
            httpClient.DefaultRequestHeaders.Add("sign", sign);

            using var content = new StringContent(bodyStrNoWs, Encoding.UTF8, "application/json");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var isSuccess = false;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;

            try {
                var message = await httpClient.PostAsync(_parameters.Url, content, token).ConfigureAwait(false);
                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    var jObject = JObject.Parse(resultContent);
                    if (jObject["code"]?.ToString() == "0") isSuccess = true;
                }
            }
            catch (HttpRequestException e) {
                exceptionMsg = e.Message;
            }
            catch (TaskCanceledException) {
                exceptionMsg = "接口访问返回超时!";
            }
            catch (JsonException) {
                exceptionMsg = "报文解析异常!";
            }
            catch (Exception e) {
                exceptionMsg = e.Message;
            }
            finally {
                stopwatch.Stop();
            }

            return new UploadResponse {
                ExceptionMsg = exceptionMsg,
                ApiParameters = JsonConvert.SerializeObject(this),
                IsSuccess = isSuccess,
                Duration = stopwatch.Elapsed.TotalSeconds,
                RequestContent = bodyStrNoWs,
                RequestTime = requestTime,
                RequestUrl = _parameters?.Url ?? string.Empty,
                ResponseContent = resultContent,
                ResponseTime = DateTime.Now
            };
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            if (parameters is ApiParameters param) {
                _parameters = param;
                return Task.FromResult(new KeyValuePair<bool, string>(true, "设置成功!"));
            }
            else {
                return Task.FromResult(new KeyValuePair<bool, string>(true, "参数类型不匹配"));
            }
        }

        public void UploadInBackground(string barcode, double weight, DateTime scanTime, double length = default,
            double width = default, double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
        }

        public void PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
        }

        public class ApiParameters {

            /// <summary>
            /// Url
            /// </summary>
            public string Url { get; set; } = "http://api.zygp.site/openapi/express/fjUpload";

            /// <summary>
            /// 超时
            /// </summary>
            public int TimeOut { get; set; } = 10000;

            public string AppId { get; set; } = string.Empty;
            public string AppKey { get; set; } = string.Empty;
            public bool NeedUpload { get; set; }
            public bool IsFstCode { get; set; }
        }
    }
}