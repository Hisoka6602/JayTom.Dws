using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net.Http;
using Newtonsoft.Json;
using JayTom.Dws.Utils;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
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
        public string MachineNo { get; set; } = "no123";

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; } = "https://dwsinvenova.azurewebsites.net/api/v1/SendPackageInfo";

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut { get; set; } = 10000;

        public WeciMexicoDvApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UploadResponse> UploadData([NotNull] string barcode, [NotNull] double weight, double length = default, double width = default, double height = default,
            double volume = default, Bitmap? image = default, Bitmap? panoramaImage = default,
            CancellationToken token = default) {
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            UploadResponse response;
            var data = new {
                bc_no = barcode,
                size_width = width,
                size_long = length,
                size_heigth = height,
                weigth_kg = weight,
                date_tran = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                time_tran = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                machine_no = MachineNo,
                imagebase64 = image?.ConvertBitmapToBase64() ?? string.Empty
            };
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(TimeOut);
                    httpClient.DefaultRequestHeaders.Add("Access-Control-Allow-Origin", "www.invenova.mx");
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
                    //判断是否成功条件
                }
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

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            throw new NotImplementedException();
        }
    }
}