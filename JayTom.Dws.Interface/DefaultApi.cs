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
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;

namespace JayTom.Dws.Interface {

    [ApiClass("默认Api", "DefaultApi")]
    public class DefaultApi : IApiUploader<DefaultApi.ApiParameters> {
        private readonly IHttpClientFactory _httpClientFactory;

        public DefaultApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
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

        public string ParseJsonTemplate(string jsonTemplate, string barCode, float weight, DateTime scanTime, float length,
            float width, float height, float volume, string cameraSerialNumber, bool isWatermark = false) {
            return jsonTemplate.Replace("BarCodeValue", barCode)
                  .Replace("WeightValue", weight.ToString(CultureInfo.InvariantCulture))
                  .Replace("ScanTimeValue", scanTime.ToString("yyyy-MM-dd HH:mm:ss"))
                  .Replace("LengthValue", length.ToString(CultureInfo.InvariantCulture))
                  .Replace("WidthValue", width.ToString(CultureInfo.InvariantCulture))
                  .Replace("HeightValue", height.ToString(CultureInfo.InvariantCulture))
                  .Replace("VolumeValue", volume.ToString(CultureInfo.InvariantCulture))
                  .Replace("CameraSerialNumberValue", cameraSerialNumber);
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

        public class ApiParameters : BaseApiParameters {

            /// <summary>
            /// 是否使用Json上传
            /// </summary>
            public bool IsUseJsonUpload { get; set; }

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

        public ApiParameters Parameters { get; private set; } = new();

        public bool SetParameters(ApiParameters parameters) {
            Parameters = parameters;
            return true;
        }

        public async Task<UploadResponse> UploadInformation([NotNull] string barcode, [NotNull] double weight, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            UploadResponse response;
            string data;
            //创建数据
            if (!Parameters.IsUploadScanImage) {
                if (Parameters.IsUseJsonUpload) {
                    data = ParseJsonTemplate(Parameters.JsonTemplate, barcode, (float)weight, scanTime,
                        (float)length, (float)width, (float)height,
                        (float)volume, "");
                }
                else {
                    var list = Parameters.StringTemplate.Split(",").Select(s =>
                        ParseTemplate(s, barcode, (float)weight, scanTime,
                            (float)length, (float)width, (float)height,
                            (float)volume, "")).ToList();
                    data = string.Join(",", list);
                }
            }
            else {
                data = ParseJsonTemplate(Parameters.JsonTemplate, barcode, (float)weight, scanTime,
                    (float)length, (float)width, (float)height,
                    (float)volume, "");
            }
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                HttpResponseMessage message;
                if (!Parameters.IsUseUploadImage) {
                    await using Stream dataStream =
                        new MemoryStream(Encoding.UTF8.GetBytes(data));
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync(Parameters.Url, content, token)
                        .ConfigureAwait(false);
                }
                else {
                    //上传图片
                    var formData = new MultipartFormDataContent();
                    if (imageInfo?.Image is not null) {
                        var imageToStreamContent = ImageToStreamContent(imageInfo.Image, "barcodeImage",
                            $"{imageInfo.CameraSerialNumber}_{imageInfo.CameraCustomName}.jpg");
                        formData.Add(imageToStreamContent);
                    }

                    foreach (var imageToStreamContent in from info in panoramaImageInfos ?? new List<UploadImageInfo>()
                                                         where info?.Image is not null
                                                         select ImageToStreamContent(info.Image, "panoramaImages",
                                 $"{info.CameraSerialNumber}_{info.CameraCustomName}.jpg")) {
                        formData.Add(imageToStreamContent);
                    }
                    var jsonContent = new StringContent(data, Encoding.UTF8, "application/json");
                    formData.Add(jsonContent, "jsonData");
                    message = await httpClient.PostAsync(Parameters.Url, formData, token);
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //临时判断
                    try {
                        isSuccess = Parameters.ValidationMode switch {
                            0 => resultContent.Equals(Parameters.CompleteMatch),
                            1 => resultContent.Contains(Parameters.StringContains),
                            2 => Regex.IsMatch(resultContent, Parameters.RegularExpression),
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
                    ApiParameters = JsonConvert.SerializeObject(this),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(data),
                    RequestTime = requestTime,
                    RequestUrl = Parameters.Url,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
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
    }
}