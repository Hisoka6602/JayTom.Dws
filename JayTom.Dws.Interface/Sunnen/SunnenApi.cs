using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net.Http;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Integrations.Sunnen {

    public class SunnenApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; private set; } = "https://portal.syspex.com/api/dws-alcon";

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut { get; private set; } = 10000;

        public SunnenApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UploadResponse> UploadData(string barcode, decimal weight, decimal length = default, decimal width = default, decimal height = default,
            decimal volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var data = new {
                handlingunit = barcode,
                length = length / (decimal)10,
                width = width / (decimal)10,
                height = height / (decimal)10,
                weight = weight,
                barcode = barcode,
                handlingunitgroup = other?.ToString()
            };
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using (var httpClient = _httpClientFactory.CreateClient(global::JayTom.Dws.Integrations.Contracts.ApiHttpClientNames.ExternalApi)) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(TimeOut);
                    HttpResponseMessage message;
                    using (Stream dataStream =
                           new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");
                            message = await httpClient.PostAsync(Url, content, token)
                                .ConfigureAwait(false);
                        }
                    }

                    resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    resultContent = Regex.Unescape(resultContent);
                    if (!string.IsNullOrWhiteSpace(resultContent)) {
                        //判断
                        var jObject = JObject.Parse(resultContent);
                        if (jObject["code"]?.ToString()?.ToUpper()?.Equals("1") == true) {
                            isSuccess = true;
                        }
                    }
                    //判断是否成功条件
                }
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
                    ApiParameters = IntegrationParameterSerializer.Serialize(this),
                    IsSuccess = isSuccess,
                    DurationSeconds = Convert.ToDecimal(stopwatch.Elapsed.TotalSeconds),
                    RequestContent = JsonConvert.SerializeObject(data),
                    RequestTime = requestTime,
                    RequestUrl = Url,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public async Task<UploadResponse> UploadData(string barcode, decimal weight, DateTime scanTime, decimal length = default, decimal width = default,
            decimal height = default, decimal volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var data = new {
                handlingunit = barcode,
                length = length / (decimal)10,
                width = width / (decimal)10,
                height = height / (decimal)10,
                weight = weight,
                barcode = barcode,
                handlingunitgroup = other?.ToString()
            };
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using (var httpClient = _httpClientFactory.CreateClient(global::JayTom.Dws.Integrations.Contracts.ApiHttpClientNames.ExternalApi)) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(TimeOut);
                    HttpResponseMessage message;
                    using (Stream dataStream =
                           new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");
                            message = await httpClient.PostAsync(Url, content, token)
                                .ConfigureAwait(false);
                        }
                    }

                    resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    resultContent = Regex.Unescape(resultContent);
                    if (!string.IsNullOrWhiteSpace(resultContent)) {
                        //判断
                        var jObject = JObject.Parse(resultContent);
                        if (jObject["code"]?.ToString()?.ToUpper()?.Equals("1") == true) {
                            isSuccess = true;
                        }
                    }
                    //判断是否成功条件
                }
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
                    ApiParameters = IntegrationParameterSerializer.Serialize(this),
                    IsSuccess = isSuccess,
                    DurationSeconds = Convert.ToDecimal(stopwatch.Elapsed.TotalSeconds),
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

        public Task UploadInBackground(string barcode, decimal weight, DateTime scanTime, decimal length = default,
            decimal width = default, decimal height = default, decimal volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            return Task.CompletedTask;
        }

        public Task PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
            return Task.CompletedTask;
        }
    }
}
