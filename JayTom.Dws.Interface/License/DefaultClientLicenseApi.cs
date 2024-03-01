using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using TouchSocket.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Interface.License {
    public class DefaultClientLicenseApi : IClientLicenseApi {
        private readonly IHttpClientFactory _httpClientFactory;

        public static string Domain { get; private set; } = "http://api.wxck.top";

        public DefaultClientLicenseApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<KeyValuePair<bool, object>> CreateAuthorization(string licenseCode, string machineCode, string remarks, CancellationToken token = default) {
            try {
                //组包
                var requestJson = JsonConvert.SerializeObject(new {
                    licenseCode = licenseCode,
                    machineCode = machineCode,
                    remarks = remarks,
                });

                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromSeconds(20);
                    HttpResponseMessage message;
                    await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");
                            message = await httpClient.PostAsync($"{Domain}{"/api/License/CreateAuthorization"}", content, token)
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
                    return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
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

        public async Task<KeyValuePair<bool, object>> ActivateAuthorization(string licenseCode, string machineCode, string remarks, CancellationToken token = default) {
            try {
                //组包
                var requestJson = JsonConvert.SerializeObject(new {
                    licenseCode = licenseCode,
                    machineCode = machineCode,
                    remarks = remarks
                });

                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromSeconds(20);
                HttpResponseMessage message;
                await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                    using (HttpContent content = new StreamContent(dataStream)) {
                        content.Headers.Add("Content-Type", "application/json");
                        message = await httpClient.PostAsync($"{Domain}{"/api/License/ActivateAuthorization"}", content, token)
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
            catch (Exception) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public async Task<bool> DownloadFileAsync(string fileUrl, string savePath) {
            try {
                if (string.IsNullOrEmpty(fileUrl) || string.IsNullOrEmpty(savePath)) {
                    return false;
                }
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                using var response = await httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using var contentStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await contentStream.CopyToAsync(fileStream);

                return true;
            }
            catch (Exception) {
                return false;
            }
        }
    }
}