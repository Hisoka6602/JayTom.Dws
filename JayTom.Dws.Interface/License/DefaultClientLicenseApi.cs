using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
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

                using (var httpClient = _httpClientFactory.CreateClient(global::JayTom.Dws.Interface.ApiHttpClientNames.ExternalApi)) {
                    httpClient.Timeout = TimeSpan.FromSeconds(20);
                    HttpResponseMessage message;
                    await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");
                            message = await httpClient.PostAsync($"{Domain}{"/api/License/CreateAuthorization"}", content, token)
                                .ConfigureAwait(false);
                        }
                    }
                    using (message) {
                    string httpResult;
                    switch (message.StatusCode) {
                        case HttpStatusCode.OK: {
                                httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
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

                using var httpClient = _httpClientFactory.CreateClient(global::JayTom.Dws.Interface.ApiHttpClientNames.ExternalApi);
                httpClient.Timeout = TimeSpan.FromSeconds(20);
                HttpResponseMessage message;
                await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                    using (HttpContent content = new StreamContent(dataStream)) {
                        content.Headers.Add("Content-Type", "application/json");
                        message = await httpClient.PostAsync($"{Domain}{"/api/License/ActivateAuthorization"}", content, token)
                            .ConfigureAwait(false);
                    }
                }
                using (message) {
                string httpResult;
                switch (message.StatusCode) {
                    case HttpStatusCode.OK: {
                            httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
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

        public async Task<bool> DownloadFileAsync(string fileUrl, string savePath, CancellationToken token = default) {
            string? temporaryPath = null;
            try {
                if (string.IsNullOrEmpty(fileUrl) || string.IsNullOrEmpty(savePath)) {
                    return false;
                }
                using var httpClient = _httpClientFactory.CreateClient(global::JayTom.Dws.Interface.ApiHttpClientNames.ExternalApi);
                using var response = await httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                var directory = Path.GetDirectoryName(Path.GetFullPath(savePath));
                if (string.IsNullOrEmpty(directory)) {
                    return false;
                }
                Directory.CreateDirectory(directory);
                temporaryPath = Path.Combine(directory, $".{Path.GetFileName(savePath)}.{Guid.NewGuid():N}.tmp");

                await using (var contentStream = await response.Content.ReadAsStreamAsync(token)) {
                    await using var fileStream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                        FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.WriteThrough);
                    await contentStream.CopyToAsync(fileStream, token);
                    await fileStream.FlushAsync(token);
                }

                File.Move(temporaryPath, savePath, true);
                temporaryPath = null;

                return true;
            }
            catch (Exception) {
                return false;
            }
            finally {
                if (temporaryPath is not null) {
                    try {
                        File.Delete(temporaryPath);
                    }
                    catch (IOException) {
                        // 下次启动时可由临时文件清理任务处理。
                    }
                }
            }
        }
    }
}
