using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using TouchSocket.Core;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Security.Policy;
using System.Collections.Generic;
using Org.BouncyCastle.Asn1.Ocsp;
using JayTom.Dws.Domain.Interface;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Interface.Attributes;
using static JayTom.Dws.Interface.ApiImplementations.CaiNiao.CaiNiaoApi;

namespace JayTom.Dws.Interface.ApiImplementations.CaiNiao {

    [ApiClass("菜鸟Api", "CaiNiaoApi", "CaiNiaoApiParameters", "1.0", ExecutionType.UploadInformation | ExecutionType.SendSortingReport | ExecutionType.SendConsolidationReport)]
    public class CaiNiaoApi : IApiUploader<ApiParameters> {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameters Parameters { get; private set; } = new();

        public bool SetParameters(object parameters) {
            if (parameters is not ApiParameters param) return false;
            Parameters = param;
            return true;
        }

        public void OpenJsonConfigFile() {
        }

        public async Task<UploadResponse> UploadInformation([NotNull] string barcode, [NotNull] double weight, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            //请求格口
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var requestTime = DateTime.Now;
            var data = new {
                source = Parameters.Source,
                version = Parameters.Version,
                requestId = new DateTimeOffset(requestTime).ToUnixTimeSeconds(),
                data = new object[]
                {
                    new
                    {
                        command="sorter.dest_request",
                        @params=new
                        {
                            barCode=barcode,
                            weight=0,
                            length=0,
                            width=0,
                            height=0,
                            bcrCode= Parameters.BcrCode,
                            bcrName=Parameters.BcrName,
                            foldFlag=other is true?0:1
                        }
                    }
                },
            };

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
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

                    if (jObject["result"] is not null) {
                        var jArray = JArray.Parse(jObject["result"]?.ToString() ?? string.Empty);
                        isSuccess = jArray.FirstOrDefault()?["code"]?.ToString() == "0";
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

        public async Task<UploadResponse> SendSortingReport([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            UploadResponse response = new();
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var requestTime = DateTime.Now;
            if (other is ReportChuteInfo reportChuteInfo) {
                if (reportChuteInfo.ErrorReson.Contains("车号不匹配")) {
                    reportChuteInfo.ChuteCode = "100";
                }
                NLog.LogManager.GetCurrentClassLogger().Error($"提交分拣报告:barCode:{barcode},chuteCode:{reportChuteInfo.ChuteCode},reportChuteInfo.ErrorReson:{reportChuteInfo.ErrorReson}");

                var data = new {
                    source = Parameters.Source,
                    version = Parameters.Version,
                    requestId = new DateTimeOffset(requestTime).ToUnixTimeSeconds(),
                    data = new object[]
                    {
                    new
                    {
                        command="sorter.sort_report",
                        @params=new
                        {
                            barCode=barcode,
                            chuteCode=new string(reportChuteInfo.ChuteCode.Where(char.IsDigit).ToArray()),
                            chuteCodePhysical=new string(reportChuteInfo.ChuteCodePhysical.Where(char.IsDigit).ToArray()),
                            status=reportChuteInfo.Status,
                            errorReson=reportChuteInfo.ErrorReson,
                            bcrCode= Parameters.BcrCode,
                            bcrName=Parameters.BcrName,
                        }
                    }
                    },
                };
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                try {
                    using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                    httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut * 5);
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
                    NLog.LogManager.GetCurrentClassLogger().Error($"分拣报告返回:{resultContent}");
                }
                catch (TaskCanceledException) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"分拣报告请求超时:{Parameters.TimeOut * 5}ms");
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"分拣报告异常:{e}");
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
                        ExecutionType = ExecutionType.SendSortingReport
                    };
                }
            }

            return response;
        }

        public Task<UploadResponse> SendPickupReport([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            return Task.FromResult(new UploadResponse() {
                ExecutionType = ExecutionType.SendPickupReport
            });
        }

        public async Task<UploadResponse> SendConsolidationReport(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
            UploadResponse response = new();
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var requestTime = DateTime.Now;
            var data = new {
                source = Parameters.Source,
                version = Parameters.Version,
                requestId = new DateTimeOffset(requestTime).ToUnixTimeSeconds(),
                data = new object[]
                {
                    new
                    {
                        command="sorter.batch_report",
                        @params=new
                        {
                            barCodeList=packageItems,
                            chuteCode=new string(packageExit.Where(char.IsDigit).ToArray()) ,
                            bcrCode= Parameters.BcrCode,
                            bcrName=Parameters.BcrName,
                        }
                    }
                },
            };
            NLog.LogManager.GetCurrentClassLogger().Error($"提交集包报告:格口:{new string(packageExit.Where(char.IsDigit).ToArray())},包裹数:{packageItems.Count},具体单号:{string.Join(",", packageItems)}");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut * 5);
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
                NLog.LogManager.GetCurrentClassLogger().Error($"集包返回:{resultContent}");
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"集包报告异常:{e}");
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
                    ExecutionType = ExecutionType.SendConsolidationReport
                };
            }

            return response;
        }

        public Task<UploadResponse> SendImage(string barcode, List<UploadImageInfo> uploadImagesInfos, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse() {
                ExecutionType = ExecutionType.SendImage
            });
        }

        public Task<UploadResponse> SendLockCommand(string lockIdentifier, object? other = null, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse() {
                ExecutionType = ExecutionType.SendLockCommand
            });
        }

        public Task<UploadResponse> SendUnlockCommand(string lockIdentifier, object? other = null, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse() {
                ExecutionType = ExecutionType.SendUnlockCommand
            });
        }

        public Task<UploadResponse> SendDeviceReport(string deviceIdentifier, string deviceStatus, object? other = null,
            CancellationToken token = default) {
            return Task.FromResult(new UploadResponse() {
                ExecutionType = ExecutionType.SendDeviceReport
            });
        }

        public CaiNiaoApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public class ApiParameters : BaseApiParameters {

            /// <summary>
            /// SignKey
            /// </summary>
            public string Source { get; set; } = "test";

            /// <summary>
            /// 版本
            /// </summary>
            public int Version { get; set; } = 1;

            /// <summary>
            /// 设备代码
            /// </summary>
            public string BcrCode { get; set; } = "BCR02";

            /// <summary>
            /// 设备名称
            /// </summary>
            public string BcrName { get; set; } = "sorter";
        }

        public class ReportChuteInfo {
            public string ChuteCode { get; set; } = string.Empty;
            public string ChuteCodePhysical { get; set; } = string.Empty;
            public string ErrorReson { get; set; } = string.Empty;
            public int Status { get; set; }
        }
    }
}