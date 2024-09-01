using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Http;
using System.Diagnostics;
using System.Net.Http.Json;
using SixLabors.ImageSharp;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using Image = System.Drawing.Image;
using JayTom.Dws.Interface.License;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Interface.Cloud.CloudVideo {

    public class CloudVideoUploadApi : ICloud {
        private readonly IHttpClientFactory _httpClientFactory;
        private static CloudVideoApiParameters _parameters = new();

        public CloudVideoUploadApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        /*public async Task<CloudUploadResponse> UploadData(string barcode, DateTime scanTime,
            double weight, string scanNodName, CloudUploadVolumeInfo? volumeInfo = default,
            List<CloudUploadImageInfo>? imageInfos = default, CloudUploadOcrInfo? ocrInfo = default,
            CloudUploadApiInfo? uploadApiInfo = default, CloudUploadSortingInfo? sortingInfo = default,
            List<CloudNvrCameraBindingInfo>? nvrCameraBindingInfos = default,
            object? other = null,
            CancellationToken token = default) {
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            CloudUploadResponse response;
            var requestTime = DateTime.Now;
            var data = string.Empty;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                var formData = new MultipartFormDataContent();
                //组建数据
                data = JsonConvert.SerializeObject(new {
                    Barcode = barcode,
                    ScanNodName = scanNodName,
                    ScanTime = scanTime,
                    NvrCameraBindingInfos = nvrCameraBindingInfos
                });
                var jsonContent = new StringContent(data, Encoding.UTF8, "application/json");
                formData.Add(jsonContent, "jsonData");
                //判断图片
                if (imageInfos?.Any() == true) {
                    //扫码图
                    var imageInfo = imageInfos.LastOrDefault(l => l.Type == 0);
                    if (imageInfo?.Image is not null) {
                        var imageToStreamContent = ImageToStreamContent(imageInfo.Image, "barcodeImage",
                            $"{imageInfo.CameraSerialNumber}_{imageInfo.CustomCameraName}.jpg");
                        if (imageToStreamContent is not null) {
                            formData.Add(imageToStreamContent);
                        }
                    }
                    //全景图
                    var cloudUploadImageInfos = imageInfos.Where(w => w.Type == 1)?.ToList();
                    if (cloudUploadImageInfos?.Any() == true) {
                        foreach (var imageToStreamContent in from cloudUploadImageInfo in cloudUploadImageInfos
                                                             where cloudUploadImageInfo?.Image is not null
                                                             select ImageToStreamContent(cloudUploadImageInfo.Image, "panoramaImages",
                                     $"{cloudUploadImageInfo.CameraSerialNumber}_{cloudUploadImageInfo.CustomCameraName}.jpg") into imageToStreamContent
                                                             where imageToStreamContent is not null
                                                             select imageToStreamContent) {
                            formData.Add(imageToStreamContent);
                        }
                    }
                }
                //提交
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(_parameters.Timeout);
                var message = await httpClient.PostAsync($"http://{_parameters.WebDoMain}/api/BarCode/UploadBarcodeData", formData, token);
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
                response = new CloudUploadResponse() {
                    IsSuccessful = isSuccess,
                    ResponseContent = resultContent,
                    TargetAddress = $"http://{_parameters.WebDoMain}/api/BarCode/UploadBarcodeData",
                    UploadContent = data,
                    UploadDuration = (int?)stopwatch.ElapsedMilliseconds,
                    UploadTime = requestTime,
                    ExceptionMsg = exceptionMsg
                };
            }

            return response;
        }*/

        public async Task<CloudUploadResponse> UploadData([NotNull] PackageCloudInfo packageCloudInfo, object? other = null, CancellationToken token = default) {
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            CloudUploadResponse response;
            var requestTime = DateTime.Now;
            var data = string.Empty;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                var formData = new MultipartFormDataContent();
                //组建数据
                if (packageCloudInfo?.ImageInfos?.Any() == true) {
                    //扫码图
                    var imageInfo = packageCloudInfo.ImageInfos.LastOrDefault(l => l.Type == 0);
                    if (imageInfo?.Image is not null) {
                        var imageToStreamContent = ImageToStreamContent(imageInfo.Image, "barcodeImage",
                            $"{imageInfo.CameraSerialNumber}_{imageInfo.CustomCameraName}.jpg");
                        if (imageToStreamContent is not null) {
                            formData.Add(imageToStreamContent);
                        }
                    }
                    //全景图
                    var cloudUploadImageInfos = packageCloudInfo.ImageInfos.Where(w => w.Type == 1)?.ToList();
                    if (cloudUploadImageInfos?.Any() == true) {
                        foreach (var imageToStreamContent in from cloudUploadImageInfo in cloudUploadImageInfos
                                                             where cloudUploadImageInfo?.Image is not null
                                                             select ImageToStreamContent(cloudUploadImageInfo.Image, "panoramaImages",
                                                                 $"{cloudUploadImageInfo.CameraSerialNumber}_{cloudUploadImageInfo.CustomCameraName}.jpg") into imageToStreamContent
                                                             where imageToStreamContent is not null
                                                             select imageToStreamContent) {
                            formData.Add(imageToStreamContent);
                        }
                    }

                    foreach (var packageCloudImageInfo in packageCloudInfo.ImageInfos) {
                        packageCloudImageInfo.Image = null;
                    }
                }
                data = JsonConvert.SerializeObject(packageCloudInfo);
                var jsonContent = new StringContent(data, Encoding.UTF8, "application/json");
                formData.Add(jsonContent, "packageInfo");
                //提交
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(_parameters.Timeout);
                //$"http://{_parameters.WebDoMain}"
                var message = await httpClient.PostAsync($"{_parameters.WebDoMain}", formData, token);
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
                // TargetAddress = $"http://{_parameters.WebDoMain}/api/BarCode/UploadBarcodeData",
                stopwatch.Stop();
                response = new CloudUploadResponse() {
                    IsSuccessful = isSuccess,
                    ResponseContent = resultContent,
                    TargetAddress = $"{_parameters.WebDoMain}",
                    UploadContent = data,
                    UploadDuration = (int?)stopwatch.ElapsedMilliseconds,
                    UploadTime = requestTime,
                    ExceptionMsg = exceptionMsg
                };
            }
            return response;
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            if (parameters is CloudVideoApiParameters param) {
                _parameters = param;
                return Task.FromResult(new KeyValuePair<bool, string>(true, string.Empty));
            }
            else {
                return Task.FromResult(new KeyValuePair<bool, string>(false, "参数类型错误"));
            }
        }

        public Task<KeyValuePair<bool, string>> SetParameters(Dictionary<string, object> parameters) {
            var any = parameters.Any(a => !a.Key.ToLower().Equals("webdomain") &&
                                          !a.Key.ToLower().Equals("timeout"));

            if (any) {
                return Task.FromResult(new KeyValuePair<bool, string>(false, "键名包含不存在的属性"));
            }
            else {
                foreach (var keyValuePair in parameters) {
                    switch (keyValuePair.Key.ToLower()) {
                        case "webdomain":
                            _parameters.WebDoMain = keyValuePair.Value.ToString() ?? string.Empty;
                            break;

                        case "timeout":
                            _parameters.Timeout = Convert.ToInt32(keyValuePair.Value.ToString() ?? string.Empty);
                            break;
                    }
                }
            }
            return Task.FromResult(new KeyValuePair<bool, string>(true, string.Empty));
        }

        public async Task<KeyValuePair<bool, object>> GetCloudConfiguration(string settingsName, string path = "/api/Config/GetConfig", CancellationToken token = default) {
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                var httpResult = await httpClient.GetStringAsync($"{GetBaseUrl(_parameters.WebDoMain)}{path}?settingsName={settingsName}",
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

        public async Task<KeyValuePair<bool, string>> SubmitCloudConfiguration<T>(string settingsName, T configuration, string path = "/api/Config/SaveConfig", CancellationToken token = default) {
            try {
                var requestJson = JsonConvert.SerializeObject(new {
                    SettingsName = settingsName,
                    ConfigJson = JsonConvert.SerializeObject(configuration)
                });
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromSeconds(2);
                HttpResponseMessage message;
                await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    var s = $"{GetBaseUrl(_parameters.WebDoMain)}{path}";
                    message = await httpClient.PostAsync($"{GetBaseUrl(_parameters.WebDoMain)}{path}", content, token)
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
                        return new KeyValuePair<bool, string>(false, $"该地址不存在!");

                    default:
                        httpResult = $"{message}";
                        break;
                }

                //解码
                var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                return new KeyValuePair<bool, string>(result?.Result ?? false, result?.Msg ?? "上传失败");
            }
            catch (HttpRequestException) {
                return new KeyValuePair<bool, string>(false, "Http访问异常!");
            }
            catch (AggregateException) {
                return new KeyValuePair<bool, string>(false, "接口访问异常!");
            }
            catch (TaskCanceledException) {
                return new KeyValuePair<bool, string>(false, "接口访问返回超时!");
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, "接口访问异常!");
            }
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

        public class CloudVideoApiParameters {
            /*/// <summary>
            /// Url
            /// </summary>
            public string Url { get; set; } = string.Empty;*/

            /// <summary>
            /// ip/域名
            /// </summary>

            public string WebDoMain { get; set; } = string.Empty;

            /// <summary>
            /// 请求超时时间
            /// </summary>
            public int Timeout { get; set; } = 2000;
        }

        public string GetBaseUrl(string url) {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri)) {
                // 获取协议和主机部分
                return uri.GetLeftPart(UriPartial.Authority);
            }

            return string.Empty;
        }
    }
}