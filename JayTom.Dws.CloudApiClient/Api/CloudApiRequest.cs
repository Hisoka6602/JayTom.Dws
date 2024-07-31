using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.CloudApiClient.Data.Models;

namespace JayTom.Dws.CloudApiClient.Api {

    public class CloudApiRequest : ICloudApiRequest {
        private readonly IHttpClientFactory _httpClientFactory;
        public static string Domain { get; private set; } = "http://192.168.31.199";

        public CloudApiRequest(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public void SetBaseUrl(string url) {
            Domain = url;
        }

        public async Task<KeyValuePair<bool, object>> GetStatistics(DateTime? startDateTime, DateTime? endDateTime, string? deviceName,
            CancellationToken token = default) {
            try {
                //组包

                var requestJson = JsonConvert.SerializeObject(new {
                    StartDateTime = startDateTime,
                    EndDateTime = endDateTime,
                    DeviceName = deviceName,
                });

                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromMinutes(1);
                    HttpResponseMessage message;
                    await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");
                            message = await httpClient.PostAsync($"{Domain}{"/api/Package/Statistics"}", content, token)
                                .ConfigureAwait(false);
                        }
                    }
                    string httpResult;
                    switch (message.StatusCode) {
                        case HttpStatusCode.OK: {
                                using (message) {
                                    httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
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
                        var statisticsInfoModel = JsonConvert.DeserializeObject<StatisticsInfoModel>(result.Data.ToString() ?? string.Empty);
                        return statisticsInfoModel is not null ? new KeyValuePair<bool, object>(true, statisticsInfoModel) : new KeyValuePair<bool, object>(false, result ?? new ApiResult());
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

        public async Task<KeyValuePair<bool, object>> GetPackages(string? barcode, DateTime? startScanTime, DateTime? endScanTime, string? cameraSerialNumber,
            double? minWeight, double? maxWeight, int? requestStatus, string? physicalExit, string? sentInstruction,
            string? logisticsName, string? threeSegmentCode, string? nodeName, string? deviceName, int pageIndex = 0,
            int pageSize = 1000, CancellationToken token = default) {
            //组包
            try {
                var requestJson = JsonConvert.SerializeObject(new {
                    pageIndex = pageIndex,
                    pageSize = pageSize,
                    barcode = barcode,
                    startScanTime = startScanTime,
                    endScanTime = endScanTime,
                    cameraSerialNumber = cameraSerialNumber,
                    minWeight = minWeight,
                    maxWeight = maxWeight,
                    requestStatus = requestStatus,
                    physicalExit = physicalExit,
                    sentInstruction = sentInstruction,
                    logisticsName = logisticsName,
                    threeSegmentCode = threeSegmentCode,
                    nodeName = nodeName,
                    deviceName = deviceName,
                });

                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                HttpResponseMessage message;
                await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{Domain}{"/api/Package/Packages"}", content, token)
                        .ConfigureAwait(false);
                }
                string httpResult;
                switch (message.StatusCode) {
                    case HttpStatusCode.OK: {
                            using (message) {
                                httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
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
                return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
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
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public async Task<KeyValuePair<bool, object>> AddExceptionType(string exceptionName, string exceptionColor, CancellationToken token = default) {
            try {
                var requestJson = JsonConvert.SerializeObject(new {
                    ExceptionName = exceptionName,
                    ExceptionColor = exceptionColor,
                });
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromSeconds(20);
                HttpResponseMessage message;
                await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{Domain}{"/api/Conf/AddExceptionType"}", content, token)
                        .ConfigureAwait(false);
                }
                string httpResult;
                switch (message.StatusCode) {
                    case HttpStatusCode.OK: {
                            using (message) {
                                httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
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
                return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
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
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public async Task<KeyValuePair<bool, object>> UpdateExceptionType(long exceptionCategoryId, string exceptionName, string exceptionColor,
            CancellationToken token = default) {
            try {
                var requestJson = JsonConvert.SerializeObject(new {
                    ExceptionTypeId = exceptionCategoryId,
                    ExceptionName = exceptionName,
                    ExceptionColor = exceptionColor,
                });
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromSeconds(20);
                HttpResponseMessage message;
                await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{Domain}{"/api/Conf/UpdateExceptionType"}", content, token)
                        .ConfigureAwait(false);
                }
                string httpResult;
                switch (message.StatusCode) {
                    case HttpStatusCode.OK: {
                            using (message) {
                                httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
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
                return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
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
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public async Task<KeyValuePair<bool, object>> DeleteExceptionType(long exceptionCategoryId, CancellationToken token = default) {
            try {
                var requestJson = JsonConvert.SerializeObject(new {
                    ExceptionTypeId = exceptionCategoryId,
                });
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromSeconds(20);
                HttpResponseMessage message;
                await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{Domain}{"/api/Conf/DeleteExceptionType"}", content, token)
                        .ConfigureAwait(false);
                }
                string httpResult;
                switch (message.StatusCode) {
                    case HttpStatusCode.OK: {
                            using (message) {
                                httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
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
                return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
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
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public async Task<KeyValuePair<bool, object>> AddExceptionRule(string keywords, string customRegex, int dataSource, string exceptionTypeName,
            long exceptionTypeId, int priority, CancellationToken token = default) {
            try {
                var requestJson = JsonConvert.SerializeObject(new {
                    Keywords = keywords,
                    CustomRegex = customRegex,
                    DataSource = dataSource,
                    ExceptionTypeName = exceptionTypeName,
                    ExceptionTypeId = exceptionTypeId,
                    Priority = priority,
                });
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromSeconds(20);
                HttpResponseMessage message;
                await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{Domain}{"/api/Conf/AddExceptionRule"}", content, token)
                        .ConfigureAwait(false);
                }
                string httpResult;
                switch (message.StatusCode) {
                    case HttpStatusCode.OK: {
                            using (message) {
                                httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
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
                return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
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
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public async Task<KeyValuePair<bool, object>> UpdateExceptionRule(long exceptionRuleId, string keywords, string customRegex, int dataSource,
            string exceptionTypeName, long exceptionTypeId, int priority, CancellationToken token = default) {
            try {
                var requestJson = JsonConvert.SerializeObject(new {
                    Keywords = keywords,
                    CustomRegex = customRegex,
                    DataSource = dataSource,
                    ExceptionTypeName = exceptionTypeName,
                    ExceptionTypeId = exceptionTypeId,
                    Priority = priority,
                    ExceptionRuleId = exceptionRuleId,
                });
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromSeconds(20);
                HttpResponseMessage message;
                await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{Domain}{"/api/Conf/UpdateExceptionRule"}", content, token)
                        .ConfigureAwait(false);
                }
                string httpResult;
                switch (message.StatusCode) {
                    case HttpStatusCode.OK: {
                            using (message) {
                                httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
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
                return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
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
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public async Task<KeyValuePair<bool, object>> DeleteExceptionRule(long exceptionRuleId, CancellationToken token = default) {
            try {
                var requestJson = JsonConvert.SerializeObject(new {
                    ExceptionRuleId = exceptionRuleId,
                });
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromSeconds(20);
                HttpResponseMessage message;
                await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{Domain}{"/api/Conf/DeleteExceptionRule"}", content, token)
                        .ConfigureAwait(false);
                }
                string httpResult;
                switch (message.StatusCode) {
                    case HttpStatusCode.OK: {
                            using (message) {
                                httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
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
                return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
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
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public async Task<KeyValuePair<bool, object>> ExceptionTypes(CancellationToken token = default) {
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromSeconds(20);
                var httpResult = await httpClient.GetStringAsync($"{Domain}{"/api/Conf/ExceptionTypes"}",
                    token);
                //解码
                var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
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
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public async Task<KeyValuePair<bool, object>> ExceptionRule(CancellationToken token = default) {
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromSeconds(20);
                var httpResult = await httpClient.GetStringAsync($"{Domain}{"/api/Conf/ExceptionRules"}",
                    token);
                //解码
                var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
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
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }
    }
}