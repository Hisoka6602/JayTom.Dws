using System;
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
using System.Collections.Generic;
using Image = System.Drawing.Image;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Domain.Interface.Cloud;

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
    }
}