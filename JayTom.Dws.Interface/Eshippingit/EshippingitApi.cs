using Polly;
using System;
using Aliyun.OSS;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net.Http;
using Newtonsoft.Json;
using System.Diagnostics;
using SixLabors.ImageSharp;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Image = System.Drawing.Image;
using JayTom.Dws.Interface.Routdata;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Interface.Eshippingit {

    public class EshippingitApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameters Parameters { get; private set; } = new();
        public static OssParameters? OssParam { get; private set; }
        private static OssClient? _ossClient;

        public EshippingitApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var requestTime = DateTime.Now;
            var data = new {
                orderNo = Regex.Replace(barcode, @"[\u0000-\u001f\b]", ""),
                inboundWeight = Math.Round(Convert.ToDecimal(weight), 3),
                inboundLength = Math.Round(Convert.ToDecimal(length / 10), 3),
                inboundWidth = Math.Round(Convert.ToDecimal(width / 10), 3),
                inboundHeight = Math.Round(Convert.ToDecimal(height / 10), 3),
            };

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                    httpClient.DefaultRequestHeaders.Add("Authorization", Parameters.Authorization);
                    HttpResponseMessage message;
                    using (Stream dataStream =
                           new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json; charset=UTF-8");
                            message = await httpClient.PostAsync($"{Parameters.Domain}/api/ilw-service/ilw/parcel/asyncInbound", content, token)
                                .ConfigureAwait(false);
                        }
                    }

                    resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    resultContent = Regex.Unescape(resultContent);
                    if (!string.IsNullOrWhiteSpace(resultContent)) {
                        //判断
                        var jObject = JObject.Parse(resultContent);

                        if (jObject["ok"] is not null) {
                            isSuccess = Convert.ToBoolean(jObject["ok"]?.ToString() ?? "false");
                        }
                    }
                    //判断是否成功条件
                }
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
                    RequestUrl = $"{Parameters.Domain}/api/ilw-service/ilw/parcel/asyncInbound",
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
            var requestTime = DateTime.Now;
            var data = new {
                orderNo = Regex.Replace(barcode, @"[\u0000-\u001f\b]", ""),
                inboundWeight = Math.Round(Convert.ToDecimal(weight), 3),
                inboundLength = Math.Round(Convert.ToDecimal(length / 10), 3),
                inboundWidth = Math.Round(Convert.ToDecimal(width / 10), 3),
                inboundHeight = Math.Round(Convert.ToDecimal(height / 10), 3),
            };

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                    httpClient.DefaultRequestHeaders.Add("Authorization", Parameters.Authorization);
                    HttpResponseMessage message;
                    using (Stream dataStream =
                           new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json; charset=UTF-8");
                            message = await httpClient.PostAsync($"{Parameters.Domain}/api/ilw-service/ilw/parcel/asyncInbound", content, token)
                                .ConfigureAwait(false);
                        }
                    }

                    resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    resultContent = Regex.Unescape(resultContent);
                    if (!string.IsNullOrWhiteSpace(resultContent)) {
                        //判断
                        var jObject = JObject.Parse(resultContent);

                        if (jObject["ok"] is not null) {
                            isSuccess = Convert.ToBoolean(jObject["ok"]?.ToString() ?? "false");
                        }
                    }
                    //判断是否成功条件
                }
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
                    RequestUrl = $"{Parameters.Domain}/api/ilw-service/ilw/parcel/asyncInbound",
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            if (parameters is ApiParameters param) {
                Parameters = new ApiParameters() {
                    Authorization = param.Authorization,
                    BucketName = param.BucketName,
                    Domain = param.Domain,
                    Endpoint = param.Endpoint,
                    RetryCount = param.RetryCount,
                    RetryInterval = param.RetryInterval,
                    TimeOut = param.TimeOut
                };
                return Task.FromResult(new KeyValuePair<bool, string>(true, "设置成功"));
            }
            return Task.FromResult(new KeyValuePair<bool, string>(false, "参数类型不匹配"));
        }

        public async void UploadInBackground(string barcode, double weight, DateTime scanTime, double length = default,
            double width = default, double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            if (_ossClient is not null &&
                imageInfo?.Image is not null) {
                await PolicyPush(barcode, scanTime, imageInfo.Image, token);
            }
        }

        public void PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
        }

        public async Task PolicyPush(string barcode, DateTime scanTime, Image image, CancellationToken token = default) {
            var waitAndRetryAsync = Policy.HandleResult<bool>(result => !result)
                .Or<Exception>().WaitAndRetryAsync(Parameters.RetryCount, retryCount => TimeSpan.FromSeconds(Parameters.RetryInterval), // 重试间隔时间
                    (ex, timespan, retryCount, context) => {
                        NLog.LogManager.GetCurrentClassLogger().Error($"Oss接口重试次数:{retryCount}");
                    });
            var uploadResponse = await waitAndRetryAsync.ExecuteAsync(async () => {
                try {
                    if (OssParam is null || DateTime.Now.CompareTo(OssParam.Expiration.ToLocalTime()) >= 0) {
                        //重新申请
                        OssParam = await GetOssParameters();
                        if (OssParam is null) {
                            return false;
                        }
                    }
                    _ossClient ??= new OssClient(Parameters.Endpoint, OssParam.AccessKeyId, OssParam.AccessKeySecret,
                        OssParam.SecurityToken);

                    using MemoryStream memoryStream = new MemoryStream();
                    image.Save(memoryStream, image.RawFormat);

                    memoryStream.Seek(0, SeekOrigin.Begin);

                    var putObjectResult = _ossClient.PutObject(Parameters.BucketName, $"ilwParcelImages/{scanTime:yyyy-MM-dd}/{barcode}.png",
                        memoryStream);
                    if (putObjectResult.HttpStatusCode == HttpStatusCode.OK) {
                        return true;
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }

                return false;
            });
        }

        public async Task<OssParameters?> GetOssParameters() {
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                    httpClient.DefaultRequestHeaders.Add("Authorization", Parameters.Authorization);

                    var stringAsync = await httpClient.GetStringAsync($"{Parameters.Domain}/api/mdm-service/oss/openSts");

                    var resultContent = Regex.Unescape(stringAsync);

                    var jObject = JObject.Parse(resultContent);
                    if (jObject["content"] is not null) {
                        return JsonConvert.DeserializeObject<OssParameters>(jObject["content"]?.ToString() ?? string.Empty);
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"获取Oss参数错误:{e}");
            }

            return null;
        }

        public class ApiParameters {

            /// <summary>
            /// 域名
            /// </summary>
            public string Domain { get; set; } = "https://qa.gateway.eshippingit.com";

            /// <summary>
            /// 超时时间
            /// </summary>
            public int TimeOut { get; set; } = 1500;

            public string Authorization { get; set; } = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJwYXlsb2FkIjoie1wiaWRcIjpcIjE0MTM2ODQwMzE0NTQ4NDI5MTlcIixcIm5hbWVcIjpcIuiUoeWuuOabplwiLFwidElkXCI6MSxcIm1JZFwiOjEsXCJtTmFtZVwiOlwi5rex5Zyz5LiA5rW36YCa5YWo55CD5L6b5bqU6ZO-566h55CG5pyJ6ZmQ5YWs5Y-4XCIsXCJhSWRcIjoxfSIsImlzcyI6IlNFUlZJQ0UiLCJleHAiOjE3MTM2MDIwMzQsImlhdCI6MTcxMjczODAzNH0.Zee9jgBJdouBAR3R3G1utcFLOt98UZAeaWbMy0VeViw";
            public string Endpoint { get; set; } = "oss-cn-shanghai.aliyuncs.com";
            public string BucketName { get; set; } = "esit-open-qa";
            public int RetryCount { get; set; } = 2;
            public int RetryInterval { get; set; } = 1;
        }

        public class OssParameters {
            public string SecurityToken { get; set; } = string.Empty;
            public string AccessKeySecret { get; set; } = string.Empty;
            public string AccessKeyId { get; set; } = string.Empty;
            public DateTime Expiration { get; set; }
        }
    }
}