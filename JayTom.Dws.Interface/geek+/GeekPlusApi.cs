using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using NPOI.POIFS.Crypt;
using System.Diagnostics;
using TouchSocket.Sockets;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Security.Policy;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Collections.Generic;
using JayTom.Dws.Interface.Cloud;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace JayTom.Dws.Interface.geek_ {

    [ApiClass("Geek+", "GeekPlusApi", "1.0", ExecutionType.UploadInformation | ExecutionType.SendSortingReport)]
    public class GeekPlusApi : IApiUploader<GeekPlusApi.ApiParameters> {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameters Parameters { get; private set; } = new();

        public bool SetParameters(ApiParameters parameters) {
            lock (SettingLock) {
                try {
                    IConfiguration configuration = new ConfigurationBuilder()
                        .SetBasePath($"{AppContext.BaseDirectory}ApiSettingJson")
                        .AddJsonFile("GeekPlusApiSetting.json", optional: false, reloadOnChange: true)
                        .Build();
                    Parameters = new ApiParameters() {
                        Url = configuration["BaseUrl"],
                        TimeOut = Convert.ToInt32(configuration["TimeOut"]),
                        SellerId = Convert.ToInt32(configuration["SellerId"]),
                        Key = configuration["Key"],
                    };
                }
                catch (Exception e) {
                    return false;
                }
            }

            return true;
        }

        public async Task<UploadResponse> UploadInformation([NotNull] string barcode, [NotNull] double weight, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var method = "/scanParcel";
            string hashString;
            var data = new {
                barcode = barcode,
                height = Math.Round(Convert.ToDecimal(height), 3).ToString(CultureInfo.InvariantCulture),
                length = Math.Round(Convert.ToDecimal(length), 3).ToString(CultureInfo.InvariantCulture),
                seller_id = Parameters?.SellerId,
                timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                volume = Math.Round(Convert.ToDecimal(volume), 3).ToString(CultureInfo.InvariantCulture),
                weight = Math.Round(Convert.ToDecimal(weight), 3).ToString(CultureInfo.InvariantCulture),
                width = Math.Round(Convert.ToDecimal(width), 3).ToString(CultureInfo.InvariantCulture),
            };
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Parameters?.Key ?? string.Empty))) {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{Parameters?.Url}{method}|{JsonConvert.SerializeObject(data)}"));

                hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }

            var requestTime = DateTime.Now;

            var stopwatch = new Stopwatch();

            stopwatch.Start();
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters?.TimeOut ?? 3000);
                httpClient.DefaultRequestHeaders.Add("Authorization", hashString);
                HttpResponseMessage message;
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{Parameters?.Url}{method}", content, token)
                        .ConfigureAwait(false);
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //判断
                    var jObject = JObject.Parse(resultContent);
                    if (jObject["code"]?.ToString()?.ToLower()?.Equals("0") == true) {
                        isSuccess = true;
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
                    RequestUrl = $"{Parameters?.Url}{method}",
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

        public async Task<UploadResponse> SendSortingReport([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var method = "/uploadParcelImage";

            var requestTime = DateTime.Now;

            var stopwatch = new Stopwatch();
            var data = new {
                barcode = barcode,
                seller_id = Parameters?.SellerId,
                timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
            };
            stopwatch.Start();
            try {
                var formData = new MultipartFormDataContent();
                //组建数据

                var jsonContent = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                formData.Add(jsonContent, "data");
                //扫码图

                if (imageInfo?.Image is not null) {
                    var clone = (Image)imageInfo.Image?.Clone();
                    if (clone is not null) {
                        //传假的全景图
                        var toStreamContent = ImageToStreamContent(clone, "panoramaImages",
                            $"{imageInfo.CameraSerialNumber}_{data.timestamp}.jpg");
                        if (toStreamContent is not null) {
                            formData.Add(toStreamContent);
                        }
                    }
                    var imageToStreamContent = ImageToStreamContent(imageInfo.Image, "barcodeImage",
                        $"{imageInfo.CameraSerialNumber}_{data.timestamp}.jpg");
                    if (imageToStreamContent is not null) {
                        formData.Add(imageToStreamContent);
                    }
                }
                //全景图
                /*if (panoramaImageInfos?.Any() == true) {
                    foreach (var imageToStreamContent in from cloudUploadImageInfo in panoramaImageInfos
                                                         where cloudUploadImageInfo?.Image is not null
                                                         select ImageToStreamContent(cloudUploadImageInfo.Image, "panoramaImages",
                                                             $"{imageInfo.CameraSerialNumber}_{data.timestamp}.jpg") into imageToStreamContent
                                                         where imageToStreamContent is not null
                                                         select imageToStreamContent) {
                        formData.Add(imageToStreamContent);
                    }
                }*/

                string hashString;
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Parameters?.Key ?? string.Empty))) {
                    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{Parameters?.Url}{method}|{JsonConvert.SerializeObject(data)}"));

                    hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }

                //using var httpClient = new HttpClient();
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters?.TimeOut ?? 3000);
                httpClient.DefaultRequestHeaders.Add("Authorization", hashString);
                var message = await httpClient.PostAsync($"{Parameters?.Url}{method}", formData, token);
                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                isSuccess = resultContent.ToLower().Contains("true");
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
                    RequestUrl = $"{Parameters?.Url}{method}",
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }

            return response;
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

        public object SettingLock { get; private set; } = new();

        public GeekPlusApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public StreamContent? ImageToStreamContent(Image image, string paramName, string fileName) {
            try {
                using var memoryStream = new MemoryStream();
                image.Save(memoryStream, ImageFormat.Jpeg);
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
            catch (Exception e) {
                return null;
            }
            finally {
                image.Dispose();
            }
        }

        public class ApiParameters : BaseApiParameters {
            //public string BaseUrl { get; set; } = "https://erp.lakepoint.io/api/wms";

            public string Key { get; set; } = "12345";

            public int SellerId { get; set; } = 1000;
        }
    }
}