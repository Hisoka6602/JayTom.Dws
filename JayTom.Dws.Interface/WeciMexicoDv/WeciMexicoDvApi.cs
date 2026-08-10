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
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Interface.WeciMexicoDv {

    /// <summary>
    /// 卫慈-墨西哥dv60
    /// </summary>
    public class WeciMexicoDvApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// 机器码
        /// </summary>
        public string MachineNo { get; private set; } = "no123";

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; private set; } = "https://dwsinvenova.azurewebsites.net/api/v1/SendPackageInfo";

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut { get; private set; } = 10000;

        public WeciMexicoDvApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            UploadResponse response;
            var effectiveScanTime = other is DateTime specifiedScanTime ? specifiedScanTime : DateTime.Now;
            using var watermarkedImage = imageInfo?.Image is null ? null : new Bitmap(imageInfo.Image);
            watermarkedImage?.AddTextWatermark(
                $"bc_no:{barcode}\nsize_width:{width}\nsize_long:{length}\nsize_heigth:{height}\nweigth_kg:{weight}\ndate_tran:{effectiveScanTime:yyyy-MM-dd HH:mm:ss}",
                Color.Red, 30);
            var imageBase64 = watermarkedImage?.ConvertImageToBase64() ?? string.Empty;
            //image?.Save($"{AppDomain.CurrentDomain.BaseDirectory}watermark.jpg", ImageFormat.Jpeg);
            //var base64String = Convert.ToBase64String(Encoding.Default.GetBytes(imageBase64));
            var data = new {
                bc_no = barcode,
                size_width = width,
                size_long = length,
                size_heigth = height,
                weigth_kg = Math.Round(Convert.ToDecimal(weight), 3),
                date_tran = $"{effectiveScanTime:yyyy-MM-dd HH:mm:ss}",
                time_tran = $"{effectiveScanTime:yyyy-MM-dd HH:mm:ss}",
                machine_no = MachineNo,
                imagebase64 = imageBase64
            };
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using (var httpClient = _httpClientFactory.CreateClient(global::JayTom.Dws.Interface.ApiHttpClientNames.ExternalApi)) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(TimeOut);
                    using var content = new StringContent(
                        JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                    using var message = await httpClient.PostAsync(Url, content, token)
                        .ConfigureAwait(false);

                    resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
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
                    RequestUrl = Url,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public Task<UploadResponse> UploadData(string barcode, double weight, DateTime scanTime, double length = default, double width = default,
            double height = default, double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            return UploadData(barcode, weight, length, width, height, volume, imageInfo, panoramaImageInfos,
                scanTime, token);
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            if (parameters is WeciMexicoDvApiParam param) {
                this.Url = param.Url;
                this.MachineNo = param.MachineNo;
                this.TimeOut = param.TimeOut;
                return Task.FromResult(new KeyValuePair<bool, string>(true, "设置成功!"));
            }
            else {
                return Task.FromResult(new KeyValuePair<bool, string>(false, "参数类型不匹配"));
            }
        }

        public async Task UploadInBackground(string barcode, double weight, DateTime scanTime, double length = default,
            double width = default, double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            await UploadData(barcode, weight, scanTime, length, width, height, volume, imageInfo,
                panoramaImageInfos, other, token).ConfigureAwait(false);
        }

        public Task PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
            return Task.CompletedTask;
        }
    }

    public class WeciMexicoDvApiParam {

        /// <summary>
        /// 机器码
        /// </summary>
        public string MachineNo { get; set; } = "no123";

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; } = "https://dwsinvenova.azurewebsites.net/api/v1/SendPackageInfo";

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut { get; set; } = 10000;
    }
}
