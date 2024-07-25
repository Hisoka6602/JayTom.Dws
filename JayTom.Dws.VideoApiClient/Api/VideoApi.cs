using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;

namespace JayTom.Dws.VideoApiClient.Api {

    public class VideoApi : IVideoApi {
        private readonly IHttpClientFactory _httpClientFactory;
        public static string WebDomain { get; set; } = "192.168.31.199";

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut { get; private set; } = 10000;

        //public static string Domain { get; private set; } = "http://192.168.31.199";

        public VideoApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<KeyValuePair<bool, object>> BarcodeInfos(string? barCode, DateTime? nodeStartDateTime,
            DateTime? nodeEndDateTime, string? nodeName, string? cameraSerialNumber,
            string? cameraName, int pageIndex = 0, int pageSize = 1000, CancellationToken cancellationToken = default) {
            try {
                //组包

                var requestJson = JsonConvert.SerializeObject(new {
                    BarCode = barCode,
                    NodeStartDateTime = nodeStartDateTime,
                    NodeEndDateTime = nodeEndDateTime,
                    NodeName = nodeName,
                    CameraSerialNumber = cameraSerialNumber,
                    CameraName = cameraName,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                });

                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromSeconds(20);
                    HttpResponseMessage message;
                    await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");
                            message = await httpClient.PostAsync($"http://{WebDomain}{"/api/BarCode/BarcodeInfos"}", content, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                    string httpResult;
                    switch (message.StatusCode) {
                        case HttpStatusCode.OK: {
                                using (message) {
                                    httpResult = await message.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                                }
                                break;
                            }
                        case HttpStatusCode.NotFound:
                            return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                        default:
                            httpResult = $"{message}";
                            break;
                    }

                    //解码
                    var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                    if (result is { Result: true, Data: not null }) {
                        var apiBarCodesInfos = JsonConvert.DeserializeObject<List<PackageInfoModel>>(result.Data.ToString() ?? string.Empty);
                        return new KeyValuePair<bool, object>(true, new ApiResult() {
                            Data = apiBarCodesInfos,
                            Msg = result.Msg,
                            Result = result.Result,
                            Total = result.Total,
                        });
                    }
                    else {
                        return new KeyValuePair<bool, object>(false, result ?? new ApiResult());
                    }
                }
            }
            catch (HttpRequestException) {
                return new KeyValuePair<bool, object>(false, "Http访问异常!");
            }
            catch (AggregateException) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
            catch (TaskCanceledException) {
                return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
            }
            catch (Exception) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public async Task<KeyValuePair<bool, object>> GroupedNodeNames(CancellationToken cancellationToken = default) {
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    HttpResponseMessage message;
                    message = await httpClient.GetAsync($"http://{WebDomain}{"/api/BarCode/GroupedNodeNames"}", cancellationToken)
                        .ConfigureAwait(false);
                    string httpResult;
                    switch (message.StatusCode) {
                        case HttpStatusCode.OK: {
                                using (message) {
                                    httpResult = await message.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                                }

                                break;
                            }
                        case HttpStatusCode.NotFound:
                            return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                        default:
                            httpResult = $"{message}";
                            break;
                    }
                    //解码
                    var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                    if (result is { Result: true, Data: not null }) {
                        var list = JsonConvert.DeserializeObject<List<string>>(result.Data.ToString() ?? string.Empty);
                        return new KeyValuePair<bool, object>(true, new ApiResult() {
                            Data = list,
                            Msg = result.Msg,
                            Result = result.Result,
                            Total = result.Total,
                        });
                    }
                    else {
                        return new KeyValuePair<bool, object>(false, result ?? new ApiResult());
                    }
                }
            }
            catch (HttpRequestException) {
                return new KeyValuePair<bool, object>(false, "Http访问异常!");
            }
            catch (AggregateException) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
            catch (TaskCanceledException) {
                return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
            }
            catch (Exception) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public async Task<KeyValuePair<bool, object>> BarcodeTotalForDate(DateTime date, CancellationToken cancellationToken = default) {
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    HttpResponseMessage message;
                    message = await httpClient.GetAsync($"http://{WebDomain}{"/api/BarCode/BarcodeTotalForDate"}?date={date}", cancellationToken)
                        .ConfigureAwait(false);
                    string httpResult;
                    switch (message.StatusCode) {
                        case HttpStatusCode.OK: {
                                using (message) {
                                    httpResult = await message.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                                }

                                break;
                            }
                        case HttpStatusCode.NotFound:
                            return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                        default:
                            httpResult = $"{message}";
                            break;
                    }
                    //解码
                    var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                    if (result is { Result: true, Data: not null }) {
                        return new KeyValuePair<bool, object>(true, new ApiResult() {
                            Data = result.Data,
                            Msg = result.Msg,
                            Result = result.Result,
                        });
                    }
                    else {
                        return new KeyValuePair<bool, object>(false, result ?? new ApiResult());
                    }
                }
            }
            catch (HttpRequestException) {
                return new KeyValuePair<bool, object>(false, "Http访问异常!");
            }
            catch (AggregateException) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
            catch (TaskCanceledException) {
                return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
            }
            catch (Exception) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public async Task<KeyValuePair<bool, object>> BarcodeTotalForDateBetween(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) {
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    HttpResponseMessage message;
                    message = await httpClient.GetAsync($"http://{WebDomain}{"/api/BarCode/BarcodeTotalForDateBetween"}?startDate={startDate}&endDate={endDate}", cancellationToken)
                        .ConfigureAwait(false);
                    string httpResult;
                    switch (message.StatusCode) {
                        case HttpStatusCode.OK: {
                                using (message) {
                                    httpResult = await message.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                                }

                                break;
                            }
                        case HttpStatusCode.NotFound:
                            return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                        default:
                            httpResult = $"{message}";
                            break;
                    }
                    //解码
                    var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                    if (result is { Result: true, Data: not null }) {
                        return new KeyValuePair<bool, object>(true, new ApiResult() {
                            Data = result.Data,
                            Msg = result.Msg,
                            Result = result.Result,
                        });
                    }
                    else {
                        return new KeyValuePair<bool, object>(false, result ?? new ApiResult());
                    }
                }
            }
            catch (HttpRequestException) {
                return new KeyValuePair<bool, object>(false, "Http访问异常!");
            }
            catch (AggregateException) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
            catch (TaskCanceledException) {
                return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
            }
            catch (Exception) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public void SetWebDomain(string domain) {
            WebDomain = domain;
        }
    }
}