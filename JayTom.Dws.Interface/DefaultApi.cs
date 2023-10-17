using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Http;
using JayTom.Dws.Utils;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;

namespace JayTom.Dws.Interface {

    public class DefaultApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        private DefaultApiParameters _parameters = new();

        public DefaultApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UploadResponse> UploadData([NotNull] string barcode, [NotNull] double weight, double length = default, double width = default, double height = default,
            double volume = default, Image? image = default, Image? panoramaImage = default, object? other = null, CancellationToken token = default) {
            return new UploadResponse();
        }

        public async Task<UploadResponse> UploadData([NotNull] string barcode, [NotNull] double weight, DateTime scanTime, double length = default, double width = default,
            double height = default, double volume = default, Image? image = default, Image? panoramaImage = default, object? other = null,
            CancellationToken token = default) {
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            UploadResponse response;
            //创建数据
            string data;
            if (_parameters.IsUseJsonUpload) {
                data = _parameters.JsonTemplate;
            }
            else {
                var list = _parameters.StringTemplate.Split(",").Select(s =>
                    ParseTemplate(s, barcode, (float)weight, scanTime,
                        (float)length, (float)width, (float)height,
                        (float)volume, "")).ToList();
                data = string.Join(",", list);
            }
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = _parameters.Timeout;
                HttpResponseMessage message;
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(data))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync(_parameters.Url, content, token)
                        .ConfigureAwait(false);
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //临时判断
                    try {
                        isSuccess = _parameters.ValidationMode switch {
                            0 => resultContent.Equals(_parameters.CompleteMatch),
                            1 => resultContent.Contains(_parameters.StringContains),
                            2 => Regex.IsMatch(resultContent, _parameters.RegularExpression),
                            _ => false
                        };
                    }
                    catch (Exception e) {
                        Console.WriteLine(e);
                    }
                }
                //判断是否成功条件
            }
            catch (HttpRequestException e) {
                isSuccess = false;
                exceptionMsg = e.Message;
            }
            catch (AggregateException) {
                isSuccess = false;
                exceptionMsg = "接口访问异常!";
            }
            catch (JsonException) {
                isSuccess = false;
                exceptionMsg = "报文解析异常!";
            }
            catch (TaskCanceledException) {
                isSuccess = false;
                exceptionMsg = "接口访问返回超时!";
            }
            catch (Exception e) {
                isSuccess = false;
                exceptionMsg = e.Message;
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
                    RequestUrl = _parameters.Url,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            if (parameters is not DefaultApiParameters param)
                return Task.FromResult(new KeyValuePair<bool, string>(false, "参数类型错误!"));
            _parameters = param;
            return Task.FromResult(new KeyValuePair<bool, string>(true, string.Empty));
        }

        public string ParseTemplate(string source, string barCode, float weight, DateTime scanTime, float length,
            float width, float height, float volume, string cameraSerialNumber, bool isWatermark = false) {
            return source switch {
                "{BarCode}" => $"{(isWatermark ? "BarCode:" : string.Empty)}{barCode}",
                "{Weight}" => $"{(isWatermark ? "Weight:" : string.Empty)}{weight.ToString(CultureInfo.InvariantCulture)}",
                "{Volume}" => $"{(isWatermark ? "Volume:" : string.Empty)}{volume.ToString(CultureInfo.InvariantCulture)}",
                "{Length}" => $"{(isWatermark ? "Length:" : string.Empty)}{length.ToString(CultureInfo.InvariantCulture)}",
                "{Width}" => $"{(isWatermark ? "Width:" : string.Empty)}{width.ToString(CultureInfo.InvariantCulture)}",
                "{Height}" => $"{(isWatermark ? "Height:" : string.Empty)}{height.ToString(CultureInfo.InvariantCulture)}",
                "{ScanTime}" => $"{(isWatermark ? "ScanTime:" : string.Empty)}{(isWatermark ? $"{scanTime:yyyy-MM-dd HH:mm:ss.fff}" : $"{scanTime:yyyyMMddHHmmssfff}")}",
                "{TimestampedGuid}" => $"{(isWatermark ? "TimestampedGuid:" : string.Empty)}{new DateTimeOffset(scanTime).ToUnixTimeMilliseconds().ToString()}",
                "{CameraSerialNumber}" => $"{(isWatermark ? "CameraSerialNumber:" : string.Empty)}{cameraSerialNumber}",
                "{Year}" => $"{(isWatermark ? "Year:" : string.Empty)}{scanTime:yyyy}",
                "{Month}" => $"{(isWatermark ? "Month:" : string.Empty)}{scanTime:MM}",
                "{Day}" => $"{(isWatermark ? "Day:" : string.Empty)}{scanTime:dd}",
                "{Hour}" => $"{(isWatermark ? "Hour:" : string.Empty)}{scanTime:HH}",
                _ => source
            };
        }

        public class DefaultApiParameters {

            /// <summary>
            /// 是否使用Json上传
            /// </summary>
            public bool IsUseJsonUpload { get; set; }

            /// <summary>
            /// Url
            /// </summary>
            public string Url { get; set; } = string.Empty;

            /// <summary>
            /// 请求超时时间
            /// </summary>
            public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

            /// <summary>
            /// 字符串模板
            /// </summary>
            public string StringTemplate { get; set; } = string.Empty;

            /// <summary>
            /// Json模板
            /// </summary>
            public string JsonTemplate { get; set; } = string.Empty;

            /// <summary>
            /// 验证模式(0=完全匹配、1=包含字符串、2=正则表达式)
            /// </summary>
            public int ValidationMode { get; set; } = 1;

            /// <summary>
            /// 完全匹配的内容
            /// </summary>
            public string CompleteMatch { get; set; } = string.Empty;

            /// <summary>
            /// 包含字符串的内容
            /// </summary>
            public string StringContains { get; set; } = string.Empty;

            /// <summary>
            /// 正则表达式
            /// </summary>
            public string RegularExpression { get; set; } = string.Empty;
        }
    }
}