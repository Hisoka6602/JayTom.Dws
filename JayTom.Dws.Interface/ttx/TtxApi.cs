using System;
using HidSharp;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace JayTom.Dws.Interface.ttx {

    public class TtxApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameter? ApiParameters { get; set; }
        public object SettingLock { get; private set; } = new();

        public TtxApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
            lock (SettingLock) {
                try {
                    if (ApiParameters is null) {
                        IConfiguration configuration = new ConfigurationBuilder()
                            .SetBasePath($"{AppContext.BaseDirectory}ApiSettingJson")
                            .AddJsonFile("TtxApiSettings.json", optional: false, reloadOnChange: true)
                            .Build();
                        ApiParameters = new ApiParameter() {
                            Url = configuration["Url"] ?? string.Empty,
                            TimeOut = Convert.ToInt32(configuration["TimeOut"]),
                            WarehouseCode = configuration["WarehouseCode"] ?? string.Empty,
                            DeviceId = configuration["DeviceId"] ?? string.Empty,
                            Api = configuration["Api"] ?? string.Empty,
                        };
                    }
                }
                catch (Exception e) {
                    ApiParameters = new();
                    NLog.LogManager.GetCurrentClassLogger().Error($"读取接口配置错误:{e}");
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
            var data = new {
                warehouseCode = ApiParameters?.WarehouseCode,
                deviceId = ApiParameters?.DeviceId,
                waybillCode = barcode,
                weight = Math.Round(Convert.ToDecimal(weight), 3),
            };
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("api", ApiParameters?.Api??string.Empty),
                new KeyValuePair<string, string>("data", JsonConvert.SerializeObject(data))
            });
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(ApiParameters?.TimeOut ?? 1000);
                var message = await httpClient.PostAsync($"{ApiParameters?.Url}", content, token)
                    .ConfigureAwait(false);

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //判断
                    var jObject = JObject.Parse(resultContent);
                    if (jObject["msg"]?.ToString()?.ToLower()?.Equals("success") == true) {
                        isSuccess = true;
                    }
                    else {
                        exceptionMsg = jObject["msg"]?.ToString();
                    }
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
                    RequestUrl = ApiParameters?.Url ?? string.Empty,
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
            var data = new {
                warehouseCode = ApiParameters?.WarehouseCode,
                deviceId = ApiParameters?.DeviceId,
                waybillCode = barcode,
                weight = Math.Round(Convert.ToDecimal(weight), 3),
            };
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("api", ApiParameters?.Api??string.Empty),
                new KeyValuePair<string, string>("data", JsonConvert.SerializeObject(data))
            });
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(ApiParameters?.TimeOut ?? 1000);
                var message = await httpClient.PostAsync($"{ApiParameters?.Url}", content, token)
                    .ConfigureAwait(false);

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //判断
                    var jObject = JObject.Parse(resultContent);
                    if (jObject["msg"]?.ToString()?.ToLower()?.Equals("success") == true) {
                        isSuccess = true;
                    }
                    else {
                        exceptionMsg = jObject["msg"]?.ToString();
                    }
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
                    RequestUrl = ApiParameters?.Url ?? string.Empty,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            if (parameters is ApiParameter param) {
                this.ApiParameters = new ApiParameter() {
                    Api = param.Api,
                    WarehouseCode = param.WarehouseCode,
                    DeviceId = param.DeviceId,
                    TimeOut = param.TimeOut,
                    Url = param.Url,
                };
                return Task.FromResult(new KeyValuePair<bool, string>(true, "设置成功!"));
            }
            else {
                return Task.FromResult(new KeyValuePair<bool, string>(false, "参数类型不匹配"));
            }
        }

        public void UploadInBackground(string barcode, double weight, DateTime scanTime, double length = default,
            double width = default, double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
        }

        public void PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
        }

        public class ApiParameter {
            public string Url { get; set; } = "https://ttx.yskc.cn/api/wms/sync/in";
            public string Api { get; set; } = "wms.weight.update";

            /// <summary>
            /// 仓库代码
            /// </summary>
            public string WarehouseCode { get; set; } = "YC001";

            /// <summary>
            /// 设备Id
            /// </summary>
            public string DeviceId { get; set; } = "abc";

            /// <summary>
            /// 超时
            /// </summary>
            public int TimeOut { get; set; } = 1000;
        }
    }
}