using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace JayTom.Dws.Interface.JdyWms {

    /// <summary>
    /// 筋斗云Wms
    /// </summary>
    public class JdyWmsApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Url
        /// </summary>
        public string? Url { get; private set; }

        /// <summary>
        /// 超时
        /// </summary>
        public int? TimeOut { get; private set; }

        public JdyWmsApi(IHttpClientFactory httpClientFactory) {
            //读取、设置
            lock (httpClientFactory) {
                if (Url is null || TimeOut is null) {
                    try {
                        IConfiguration configuration = new ConfigurationBuilder()
                            .SetBasePath($"{AppContext.BaseDirectory}ApiSettingJson")
                            .AddJsonFile("JdyApiSetting.json", optional: false, reloadOnChange: true)
                            .Build();
                        Url = configuration["Url"];
                        TimeOut = Convert.ToInt32(configuration["TimeOut"]);
                    }
                    catch (Exception e) {
                        Url = string.Empty;
                        TimeOut = 3000;
                    }
                }
                _httpClientFactory = httpClientFactory;
            }
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var data = new {
                ticketsNum = barcode,
                length = length,
                width = Math.Round(Convert.ToDecimal(weight), 3),
                height = height,
                weight = weight,
                workConsole = "分拣机",
                destination = string.Empty,
            };
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(TimeOut ?? 3000);
                    HttpResponseMessage message;
                    using (Stream dataStream =
                           new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");
                            message = await httpClient.PostAsync(Url ?? string.Empty, content, token)
                                .ConfigureAwait(false);
                        }
                    }

                    resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    resultContent = Regex.Unescape(resultContent);
                    if (!string.IsNullOrWhiteSpace(resultContent)) {
                        //判断
                        var jObject = JObject.Parse(resultContent);
                        if (jObject["result"]?.ToString()?.ToLower()?.Equals("true") == true) {
                            isSuccess = true;
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
                    RequestUrl = Url,
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
            var data = new {
                ticketsNum = barcode,
                length = length,
                width = width,
                height = height,
                weight = weight,
                workConsole = "分拣机",
                destination = string.Empty,
            };
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(TimeOut ?? 3000);
                    HttpResponseMessage message;
                    using (Stream dataStream =
                           new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");
                            message = await httpClient.PostAsync(Url ?? string.Empty, content, token)
                                .ConfigureAwait(false);
                        }
                    }

                    resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    resultContent = Regex.Unescape(resultContent);
                    if (!string.IsNullOrWhiteSpace(resultContent)) {
                        //判断
                        var jObject = JObject.Parse(resultContent);
                        if (jObject["result"]?.ToString()?.ToLower()?.Equals("true") == true) {
                            isSuccess = true;
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
                    RequestUrl = Url,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            return Task.FromResult(new KeyValuePair<bool, string>(true, "无可设置参数"));
        }

        public void UploadInBackground(string barcode, double weight, DateTime scanTime, double length = default,
            double width = default, double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
        }
    }
}