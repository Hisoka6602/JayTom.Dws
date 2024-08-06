using System;
using System.Web;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net.Http;
using Newtonsoft.Json;
using JayTom.Dws.Utils;
using System.Threading;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using JayTom.Dws.Domain.Interface;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Domain.Interface.Attributes;

namespace JayTom.Dws.Interface.ApiImplementations.WeciMexicoDv {

    /// <summary>
    /// 卫慈-墨西哥dv60
    /// </summary>
    [ApiClass("卫慈-墨西哥-Api", "WeciMexicoDvApi", "WeciMexicoDvApiParameter", "1.0", ExecutionType.UploadInformation)]
    public class WeciMexicoDvApi : IApiUploader<WeciMexicoDvApi.ApiParameter> {
        private readonly IHttpClientFactory _httpClientFactory;

        public WeciMexicoDvApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public class ApiParameter : BaseApiParameters {

            /// <summary>
            /// 机器码
            /// </summary>
            public string MachineNo { get; set; } = "no123";
        }

        public ApiParameter Parameters { get; private set; } = new();

        public bool SetParameters(object parameters) {
            if (parameters is not ApiParameter param) return false;
            Parameters = param;
            return true;
        }

        public async Task<UploadResponse> UploadInformation([NotNull] string barcode, [NotNull] double weight, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            UploadResponse response;
            imageInfo.Image = imageInfo.Image?.AddTextWatermark(
                $"bc_no:{barcode}\nsize_width:{width}\nsize_long:{length}\nsize_heigth:{height}\nweigth_kg:{weight}\ndate_tran:{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                Color.Red, 30);
            var imageBase64 = imageInfo.Image?.ConvertImageToBase64() ?? string.Empty;
            //image?.Save($"{AppDomain.CurrentDomain.BaseDirectory}watermark.jpg", ImageFormat.Jpeg);
            //var base64String = Convert.ToBase64String(Encoding.Default.GetBytes(imageBase64));
            var data = new {
                bc_no = barcode,
                size_width = width,
                size_long = length,
                size_heigth = height,
                weigth_kg = Math.Round(Convert.ToDecimal(weight), 3),
                date_tran = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                time_tran = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                machine_no = Parameters.MachineNo,
                imagebase64 = imageBase64
            };
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                httpClient.DefaultRequestHeaders.Add("Access-Control-Allow-Origin", "www.invenova.mx");
                HttpResponseMessage message;
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync(Parameters.Url, content, token)
                        .ConfigureAwait(false);
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //判断
                    var jObject = JObject.Parse(resultContent);
                    if (jObject["code"]?.ToString()?.ToUpper()?.Equals("0") == true &&
                        jObject["message"]?.ToString()?.ToUpper()?.Equals("OK") == true) {
                        isSuccess = true;
                    }
                }
                //判断是否成功条件
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
                    ApiParameters = JsonConvert.SerializeObject(this),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(data),
                    RequestTime = requestTime,
                    RequestUrl = Parameters.Url,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now,
                    ExecutionType = ExecutionType.UploadInformation
                };
            }
            return response;
        }

        public void ScanPackage([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
        }

        public Task<UploadResponse> SendSortingReport([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }

        public Task<UploadResponse> SendPickupReport([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }

        public Task<UploadResponse> SendConsolidationReport(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }

        public Task<UploadResponse> SendImage(string barcode, List<UploadImageInfo> uploadImagesInfos, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }

        public Task<UploadResponse> SendLockCommand(string lockIdentifier, object? other = null, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }

        public Task<UploadResponse> SendUnlockCommand(string lockIdentifier, object? other = null, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }

        public Task<UploadResponse> SendDeviceReport(string deviceIdentifier, string deviceStatus, object? other = null,
            CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }
    }
}