using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using JayTom.Dws.Domain.Interface;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.Domain.Interface.Attributes;

namespace JayTom.Dws.Interface.ApiImplementations.zhuoyan_scm {

    [ApiClass("拙燕仓Api", "ZhuoYanScmApi", "ZhuoYanScmApiParameters", "1.0", ExecutionType.UploadInformation)]
    public class ZhuoYanScmApi : IApiUploader<ZhuoYanScmApi.ApiParameters> {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameters Parameters { get; private set; } = new();

        public bool SetParameters(object parameters) {
            if (parameters is not ApiParameters param) return false;
            lock (Parameters) {
                try {
                    IConfiguration configuration = new ConfigurationBuilder()
                        .SetBasePath($"{AppContext.BaseDirectory}ApiSettingJson")
                        .AddJsonFile("ZhuoYanScmSettings.json", optional: false, reloadOnChange: true)
                        .Build();
                    Parameters = new ApiParameters() {
                        Url = configuration["Url"] ?? string.Empty,
                        TimeOut = Convert.ToInt32(configuration["TimeOut"]),
                    };
                }
                catch (Exception e) {
                    Parameters = new();
                    NLog.LogManager.GetCurrentClassLogger().Error($"读取接口配置错误:{e}");
                    return false;
                }
            }

            return true;
        }

        public async Task<UploadResponse> UploadInformation(string barcode, double weight, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            //请求格口
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var requestTime = DateTime.Now;
            var data = new object[]
            {
                new {
                    codeInfo = barcode,
                    weight = Math.Round(Convert.ToDecimal(weight), 3),
                    packageVolume = 0,
                    packageLength = 0,
                    packageWidth = 0,
                    packageHeight = 0,
                }
            };
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters?.TimeOut ?? 1000);
                HttpResponseMessage message;
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync(Parameters?.Url, content, token)
                        .ConfigureAwait(false);
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    //判断
                    var jObject = JObject.Parse(resultContent);

                    if (jObject["result"] is not null && jObject["result"]?.ToString()?.Equals("true", StringComparison.CurrentCultureIgnoreCase) == true) {
                        isSuccess = true;
                    }
                }
                //判断是否成功条件
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
                response = new UploadResponse() {
                    ExceptionMsg = exceptionMsg,
                    ApiParameters = JsonConvert.SerializeObject(this),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(data),
                    RequestTime = requestTime,
                    RequestUrl = Parameters?.Url ?? string.Empty,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now,
                    ExecutionType = ExecutionType.UploadInformation
                };
            }
            return response;
        }

        public void ScanPackage(string barcode, double weight = default, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
        }

        public Task<UploadResponse> SendSortingReport(string barcode, double weight = default, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            return Task.FromResult(new UploadResponse());
        }

        public Task<UploadResponse> SendPickupReport(string barcode, double weight = default, DateTime scanTime = default, double length = default,
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

        public object SettingLock { get; private set; } = new();

        public ZhuoYanScmApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public class ApiParameters : BaseApiParameters {
        }
    }
}