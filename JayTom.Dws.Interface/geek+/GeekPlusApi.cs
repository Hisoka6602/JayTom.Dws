using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using NPOI.POIFS.Crypt;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Security.Policy;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Collections.Generic;
using JayTom.Dws.Interface.Cloud;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.Domain.Service;

namespace JayTom.Dws.Interface.geek_
{

    public class GeekPlusApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameters? Parameters { get; private set; }
        public object SettingLock { get; private set; } = new();

        public GeekPlusApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
            lock (SettingLock) {
                try {
                    if (Parameters is null) {
                        IConfiguration configuration = new ConfigurationBuilder()
                            .SetBasePath($"{AppContext.BaseDirectory}ApiSettingJson")
                            .AddJsonFile("GeekPlusApiSetting.json", optional: false, reloadOnChange: true)
                            .Build();
                        Parameters = new ApiParameters() {
                            BaseUrl = configuration["BaseUrl"],
                            TimeOut = Convert.ToInt32(configuration["TimeOut"]),
                            SellerId = Convert.ToInt32(configuration["SellerId"]),
                            Key = configuration["Key"],
                        };
                    }
                }
                catch (Exception e) {
                }
                _httpClientFactory = httpClientFactory;
            }
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var method = "/scanParcel";
            string hashString;
            var data = new {
                barcode = barcode,
                height = Math.Round(Convert.ToDecimal(height), 3).ToString(),
                length = Math.Round(Convert.ToDecimal(length), 3).ToString(),
                seller_id = Parameters?.SellerId,
                timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                volume = Math.Round(Convert.ToDecimal(volume), 3).ToString(),
                weight = Math.Round(Convert.ToDecimal(weight), 3).ToString(),
                width = Math.Round(Convert.ToDecimal(width), 3).ToString(),
            };
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Parameters?.Key ?? string.Empty))) {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{Parameters.BaseUrl}{method}|{JsonConvert.SerializeObject(data)}"));

                hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }

            var requestTime = DateTime.Now;

            var stopwatch = new Stopwatch();

            stopwatch.Start();
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                httpClient.DefaultRequestHeaders.Add("Authorization", hashString);
                HttpResponseMessage message;
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{Parameters.BaseUrl}{method}", content, token)
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
                    RequestUrl = $"{Parameters.BaseUrl}{method}",
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, DateTime scanTime, double length = default, double width = default,
            double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var method = "/scanParcel";
            string hashString;
            var data = new {
                barcode = barcode,
                height = Math.Round(Convert.ToDecimal(height), 3).ToString(),
                length = Math.Round(Convert.ToDecimal(length), 3).ToString(),
                seller_id = Parameters?.SellerId,
                timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                volume = Math.Round(Convert.ToDecimal(volume), 3).ToString(),
                weight = Math.Round(Convert.ToDecimal(weight), 3).ToString(),
                width = Math.Round(Convert.ToDecimal(width), 3).ToString(),
            };
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Parameters?.Key ?? string.Empty))) {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{Parameters?.BaseUrl}{method}|{JsonConvert.SerializeObject(data)}"));

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
                    message = await httpClient.PostAsync($"{Parameters?.BaseUrl}{method}", content, token)
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
                    RequestUrl = $"{Parameters?.BaseUrl}{method}",
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            return Task.FromResult(new KeyValuePair<bool, string>(true, "无可设置参数"));
        }

        public async void UploadInBackground(string barcode, double weight, DateTime scanTime, double length = default,
            double width = default, double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var method = "/uploadParcelImage";
            string hashString;

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

                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Parameters?.Key ?? string.Empty))) {
                    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{Parameters?.BaseUrl}{method}|{JsonConvert.SerializeObject(data)}"));

                    hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }

                //using var httpClient = new HttpClient();
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters?.TimeOut ?? 3000);
                httpClient.DefaultRequestHeaders.Add("Authorization", hashString);
                var message = await httpClient.PostAsync($"{Parameters?.BaseUrl}{method}", formData, token);
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
                var response = new UploadResponse() {
                    ExceptionMsg = exceptionMsg,
                    ApiParameters = JsonConvert.SerializeObject(this),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(data),
                    RequestTime = requestTime,
                    RequestUrl = $"{Parameters?.BaseUrl}{method}",
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
        }

        public void PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
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

        public class ApiParameters {
            public string BaseUrl { get; set; } = "https://erp.lakepoint.io/api/wms";

            public string Key { get; set; } = "12345";

            public int SellerId { get; set; } = 1000;
            public int TimeOut { get; set; } = 10000;
        }
    }
}