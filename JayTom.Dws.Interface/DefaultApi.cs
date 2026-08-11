using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Http;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Interface {

    public class DefaultApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        private DefaultApiParameters _parameters = new();

        public DefaultApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public Task<UploadResponse> UploadData(string barcode, decimal weight, decimal length = default, decimal width = default, decimal height = default,
            decimal volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            return UploadData(barcode, weight, DateTime.Now, length, width, height, volume, imageInfo,
                panoramaImageInfos, other, token);
        }

        public async Task<UploadResponse> UploadData(string barcode, decimal weight, DateTime scanTime, decimal length = default, decimal width = default,
            decimal height = default, decimal volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            UploadResponse response;
            //创建数据
            string data;
            if (!_parameters.IsUploadScanImage) {
                if (_parameters.IsUseJsonUpload) {
                    data = ParseJsonTemplate(_parameters.JsonTemplate, barcode, (decimal)weight, scanTime,
                        (decimal)length, (decimal)width, (decimal)height,
                        (decimal)volume, "");
                }
                else {
                    var list = _parameters.StringTemplate.Split(",").Select(s =>
                        ParseTemplate(s, barcode, (decimal)weight, scanTime,
                            (decimal)length, (decimal)width, (decimal)height,
                            (decimal)volume, "")).ToList();
                    data = string.Join(",", list);
                }
            }
            else {
                data = ParseJsonTemplate(_parameters.JsonTemplate, barcode, (decimal)weight, scanTime,
                    (decimal)length, (decimal)width, (decimal)height,
                    (decimal)volume, "");
            }

            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using var httpClient = _httpClientFactory.CreateClient(global::JayTom.Dws.Interface.ApiHttpClientNames.ExternalApi);
                httpClient.Timeout = _parameters.Timeout;
                using HttpResponseMessage message = await CreateRequestAsync(httpClient, data, imageInfo,
                    panoramaImageInfos, token).ConfigureAwait(false);
                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //临时判断
                    try {
                        isSuccess = _parameters.ValidationMode switch {
                            0 => resultContent.Equals(_parameters.CompleteMatch),
                            1 => resultContent.Contains(_parameters.StringContains),
                            2 => Regex.IsMatch(resultContent, _parameters.RegularExpression,
                                RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250)),
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
                    ApiParameters = JsonConvert.SerializeObject(_parameters),
                    IsSuccess = isSuccess,
                    DurationSeconds = Convert.ToDecimal(stopwatch.Elapsed.TotalSeconds),
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

        public Task UploadInBackground(string barcode, decimal weight, DateTime scanTime, decimal length = default,
            decimal width = default, decimal height = default, decimal volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            return UploadData(barcode, weight, scanTime, length, width, height, volume, imageInfo,
                panoramaImageInfos, other, token);
        }

        /// <summary>
        /// 根据当前参数创建并发送上传请求，统一管理请求内容的释放。
        /// </summary>
        private async Task<HttpResponseMessage> CreateRequestAsync(HttpClient httpClient, string data,
            UploadImageInfo? imageInfo, List<UploadImageInfo>? panoramaImageInfos, CancellationToken token) {
            if (!_parameters.IsUseUploadImage) {
                using var content = new StringContent(data, Encoding.UTF8, "application/json");
                return await httpClient.PostAsync(_parameters.Url, content, token).ConfigureAwait(false);
            }

            using var formData = new MultipartFormDataContent();
            if (imageInfo?.Image is not null) {
                formData.Add(ImageToStreamContent(imageInfo.Image.As<Image>(), "barcodeImage",
                    $"{imageInfo.CameraSerialNumber}_{imageInfo.CameraCustomName}.jpg"));
            }

            foreach (var info in panoramaImageInfos ?? []) {
                if (info?.Image is not null) {
                    formData.Add(ImageToStreamContent(info.Image.As<Image>(), "panoramaImages",
                        $"{info.CameraSerialNumber}_{info.CameraCustomName}.jpg"));
                }
            }

            formData.Add(new StringContent(data, Encoding.UTF8, "application/json"), "jsonData");
            return await httpClient.PostAsync(_parameters.Url, formData, token).ConfigureAwait(false);
        }

        public Task PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
            return Task.CompletedTask;
        }

        public string ParseTemplate(string source, string barCode, decimal weight, DateTime scanTime, decimal length,
            decimal width, decimal height, decimal volume, string cameraSerialNumber, bool isWatermark = false) {
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

        public string ParseJsonTemplate(string jsonTemplate, string barCode, decimal weight, DateTime scanTime, decimal length,
            decimal width, decimal height, decimal volume, string cameraSerialNumber, bool isWatermark = false) {
            var result = jsonTemplate.Replace("BarCodeValue", EscapeJsonString(barCode), StringComparison.Ordinal)
                  .Replace("WeightValue", weight.ToString(CultureInfo.InvariantCulture))
                  .Replace("ScanTimeValue", scanTime.ToString("yyyy-MM-dd HH:mm:ss"))
                  .Replace("LengthValue", length.ToString(CultureInfo.InvariantCulture))
                  .Replace("WidthValue", width.ToString(CultureInfo.InvariantCulture))
                  .Replace("HeightValue", height.ToString(CultureInfo.InvariantCulture))
                  .Replace("VolumeValue", volume.ToString(CultureInfo.InvariantCulture))
                  .Replace("CameraSerialNumberValue", EscapeJsonString(cameraSerialNumber), StringComparison.Ordinal);

            _ = JsonConvert.DeserializeObject(result)
                ?? throw new JsonException("JSON 模板替换后不是有效的 JSON。");
            return result;
        }

        /// <summary>
        /// 将模板中的字符串值转义为安全的 JSON 字符串内容。
        /// </summary>
        private static string EscapeJsonString(string value) {
            var serialized = JsonConvert.ToString(value);
            return serialized.Length >= 2 ? serialized[1..^1] : string.Empty;
        }

        public StreamContent ImageToStreamContent(Image image, string paramName, string fileName) {
            using var memoryStream = new MemoryStream();
            image.Save(memoryStream, ImageFormat.Jpeg); // 假设保存为JPEG格式，根据实际情况修改
            memoryStream.Seek(0, SeekOrigin.Begin);

            var clonedStream = new MemoryStream();
            memoryStream.CopyTo(clonedStream);
            clonedStream.Seek(0, SeekOrigin.Begin);

            var streamContent = new StreamContent(clonedStream);
            streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
                Name = paramName,
                FileName = fileName
            };

            return streamContent;
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

            /// <summary>
            /// 是否上传图片
            /// </summary>
            public bool IsUseUploadImage { get; set; }

            /// <summary>
            /// 是否上传扫码图
            /// </summary>
            public bool IsUploadScanImage { get; set; }

            /// <summary>
            /// 是否上传全景图
            /// </summary>
            public bool IsUploadPanoramaImage { get; set; }
        }
    }
}
