using Polly;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Net.Http.Headers;
using JayTom.Dws.Integrations.Cloud;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Integrations.Routdata {

    public class RoutDataApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameters Parameters { get; private set; } = new();

        public RoutDataApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UploadResponse> UploadData(string barcode, decimal weight, decimal length = default, decimal width = default, decimal height = default,
            decimal volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            var callApiMethod = await CallApiMethod(ApiMethod.MailInfoQuery, barcode, Parameters.OrgCode, string.Empty,
                Parameters.DeviceCode, string.Empty, true, DateTime.Now, token: token);
            var phyBoxCode = string.Empty;
            var theoryBoxCode = string.Empty;
            var mailInfoQueryResponseContent = callApiMethod.ExceptionMsg;
            try {
                if (callApiMethod.IsSuccess) {
                    //解析
                    var jObject = JObject.Parse(callApiMethod.ResponseContent);
                    //orgCode = jObject?["BODY"]?["201"]?.First?["JGDM"]?.ToString();
                    phyBoxCode = jObject?["BODY"]?["201"]?.First?["WLGK"]?.ToString();
                    theoryBoxCode = jObject?["BODY"]?["201"]?.First?["YLZDONE"]?.ToString();
                }
                else {
                    var jObject = JObject.Parse(callApiMethod.ResponseContent);
                    mailInfoQueryResponseContent = $"{jObject?["HEAD"]?["RET_MSG"]?.ToString() ?? mailInfoQueryResponseContent}";
                    if (callApiMethod.ApiExceptionType == ApiExceptionType.None) {
                        callApiMethod = callApiMethod with {
                            ApiExceptionType = ApiExceptionType.LogicValidationFailed
                        };
                    }
                }
            }
            catch (Exception e) {
                mailInfoQueryResponseContent += $"报文解析异常:{e.Message}";
            }

            PolicyPush(ApiMethod.ScanInfoPush, barcode, Parameters.OrgCode, !string.IsNullOrEmpty(phyBoxCode) ? phyBoxCode : "99",
                Parameters.DeviceCode, !string.IsNullOrEmpty(theoryBoxCode) ? theoryBoxCode : "99",
                callApiMethod.IsSuccess,
                callApiMethod.ResponseTime
                , mailInfoQueryResponseContent, token).ConfigureAwait(false).GetAwaiter();
            PolicyPush(ApiMethod.PickingInfoPush, barcode, Parameters.OrgCode, !string.IsNullOrEmpty(phyBoxCode) ? phyBoxCode : "99",
                Parameters.DeviceCode, !string.IsNullOrEmpty(theoryBoxCode) ? theoryBoxCode : "99",
                callApiMethod.IsSuccess,
                callApiMethod.ResponseTime
                , callApiMethod.IsSuccess ? string.Empty : mailInfoQueryResponseContent, token).ConfigureAwait(false).GetAwaiter();
            return callApiMethod;
        }

        public async Task<UploadResponse> UploadData(string barcode, decimal weight, DateTime scanTime, decimal length = default, decimal width = default,
            decimal height = default, decimal volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            var callApiMethod = await CallApiMethod(ApiMethod.MailInfoQuery, barcode, Parameters.OrgCode, string.Empty,
                Parameters.DeviceCode, string.Empty, true, DateTime.Now, token: token);
            var phyBoxCode = string.Empty;
            var theoryBoxCode = string.Empty;
            var mailInfoQueryResponseContent = callApiMethod.ExceptionMsg;
            try {
                if (callApiMethod.IsSuccess) {
                    //解析
                    var jObject = JObject.Parse(callApiMethod.ResponseContent);
                    //orgCode = jObject?["BODY"]?["201"]?.First?["JGDM"]?.ToString();
                    phyBoxCode = jObject?["BODY"]?["201"]?.First?["WLGK"]?.ToString();
                    theoryBoxCode = jObject?["BODY"]?["201"]?.First?["YLZDONE"]?.ToString();
                }
                else {
                    var jObject = JObject.Parse(callApiMethod.ResponseContent);
                    mailInfoQueryResponseContent = $"{jObject?["HEAD"]?["RET_MSG"]?.ToString() ?? mailInfoQueryResponseContent}";
                    if (callApiMethod.ApiExceptionType == ApiExceptionType.None) {
                        callApiMethod = callApiMethod with {
                            ApiExceptionType = ApiExceptionType.LogicValidationFailed
                        };
                    }
                }
            }
            catch (Exception e) {
                mailInfoQueryResponseContent += $"报文解析异常:{e.Message}";
            }
            PolicyPush(ApiMethod.ScanInfoPush, barcode, Parameters.OrgCode, !string.IsNullOrEmpty(phyBoxCode) ? phyBoxCode : "99",
                Parameters.DeviceCode, !string.IsNullOrEmpty(theoryBoxCode) ? theoryBoxCode : "99",
                callApiMethod.IsSuccess,
                callApiMethod.ResponseTime
                , mailInfoQueryResponseContent, token).ConfigureAwait(false).GetAwaiter();
            PolicyPush(ApiMethod.PickingInfoPush, barcode, Parameters.OrgCode, !string.IsNullOrEmpty(phyBoxCode) ? phyBoxCode : "99",
                Parameters.DeviceCode, !string.IsNullOrEmpty(theoryBoxCode) ? theoryBoxCode : "99",
                callApiMethod.IsSuccess,
                callApiMethod.ResponseTime
                , callApiMethod.IsSuccess ? string.Empty : mailInfoQueryResponseContent, token).ConfigureAwait(false).GetAwaiter();
            return callApiMethod;
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            if (parameters is ApiParameters param) {
                Parameters.DeviceCode = param.DeviceCode;
                Parameters.Url = param.Url;
                Parameters.RetryCount = param.RetryCount;
                Parameters.RetryInterval = param.RetryInterval;
                Parameters.SignKey = param.SignKey;
                Parameters.TimeOut = param.TimeOut;
                Parameters.OrgCode = param.OrgCode;
                return Task.FromResult(new KeyValuePair<bool, string>(true, string.Empty));
            }
            else {
                return Task.FromResult(new KeyValuePair<bool, string>(true, "参数类型不匹配"));
            }
        }

        public Task UploadInBackground(string barcode, decimal weight, DateTime scanTime, decimal length = default,
            decimal width = default, decimal height = default, decimal volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            return Task.CompletedTask;
        }

        public Task PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 调用接口
        /// </summary>
        /// <param name="method"></param>
        /// <param name="barcode"></param>
        /// <param name="deviceCode"></param>
        /// <param name="theoryBoxCode"></param>
        /// <param name="processingResult"></param>
        /// <param name="processingTime"></param>
        /// <param name="mailInfoQueryResponseContent"></param>
        /// <param name="token"></param>
        /// <param name="orgCode"></param>
        /// <param name="phyBoxCode"></param>
        /// <returns></returns>
        public async Task<UploadResponse> CallApiMethod(ApiMethod method,
            string barcode,
            string orgCode,
            string phyBoxCode,
            string deviceCode,
            string theoryBoxCode,
            bool processingResult,
            DateTime processingTime,
            string mailInfoQueryResponseContent = "",
            CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            var requestContent = string.Empty;
            var sign = string.Empty;
            var exceptionType = ApiExceptionType.None;
            stopwatch.Start();
            try {
                //using var httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip });

                using var httpClient = _httpClientFactory.CreateClient(global::JayTom.Dws.Integrations.Contracts.ApiHttpClientNames.ExternalApi);
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                HttpResponseMessage message;

                //判断组网
                requestContent = method switch {
                    ApiMethod.MailInfoQuery => JsonConvert
                        .SerializeObject(new {
                            BODY = new { DatasetIdentifier = new[] { new { JGDM = orgCode, SBDM = deviceCode, YJHM = barcode } } },
                            HEAD = new { FUNC_CODE = "MailInfoQuery" }
                        })
                        .Replace("DatasetIdentifier", "101"),
                    ApiMethod.PickingInfoPush => JsonConvert.SerializeObject(new {
                        BODY = new {
                            DatasetIdentifier = new[]
                                {
                                    new
                                    {
                                        YJHM = barcode,
                                        JGDM = orgCode,
                                        WLGK = phyBoxCode,
                                        CLSJ = processingTime.ToString("yyyyMMddHHmmss"),
                                        CLJG = processingResult?"1":"0",
                                        CWXX = mailInfoQueryResponseContent,
                                        YLZD1 = "",
                                        YLZD2 = "",
                                        YLZD3 = "",
                                        SBDM = deviceCode,
                                        LLGK = theoryBoxCode
                                    }
                                }
                        },
                        HEAD = new { FUNC_CODE = "DespInfoSend" }
                    })
                        .Replace("DatasetIdentifier", "101"),
                    ApiMethod.ScanInfoPush => JsonConvert.SerializeObject(new {
                        BODY = new {
                            DatasetIdentifier = new[]
                                {
                                    new
                                    {
                                        YJHM = barcode,
                                        JGDM = orgCode,
                                        SBDM = deviceCode,
                                        SMFS = "1",
                                        CLSJ = processingTime.ToString("yyyyMMddHHmmss"),
                                        LLGK = theoryBoxCode,
                                        YLZD1 = "",
                                        YLZD2 = "",
                                        YLZD3 = ""
                                    }
                                }
                        },
                        HEAD = new { FUNC_CODE = "ScanInfoSend" }
                    })
                        .Replace("DatasetIdentifier", "101"),
                    _ => string.Empty
                };

                var requestData = Encoding.UTF8.GetBytes(requestContent);
                byte[] compressedData;

                using (var memoryStream = new MemoryStream()) {
                    await using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress)) {
                        await gzipStream.WriteAsync(requestData, 0, requestData.Length, token);
                    }
                    compressedData = memoryStream.ToArray();
                }
                var signBytes = CombineByteArrays(compressedData, Encoding.UTF8.GetBytes(Parameters.SignKey));

                sign = GetSha256Hex(signBytes).ToUpper();

                using (HttpContent content = new ByteArrayContent(compressedData)) {
                    content.Headers.Add("Content-Encoding", "gzip");
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{Parameters.Url}?SIGN={sign}", content, token).ConfigureAwait(false);
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);

                var jObject = JObject.Parse(resultContent);

                isSuccess = jObject?["HEAD"]?["RET_CODE"]?.ToString()?.Equals("0") == true;
            }
            catch (HttpRequestException e) {
                isSuccess = false;
                resultContent += e.Message;
                exceptionType = ApiExceptionType.UnreachableUrl;
            }
            catch (AggregateException) {
                isSuccess = false;
                resultContent += "接口访问异常!";
                exceptionType = ApiExceptionType.UnreachableUrl;
                exceptionMsg += "接口访问异常!";
            }
            catch (JsonException) {
                isSuccess = false;
                exceptionMsg += "报文解析异常!";
                exceptionType = ApiExceptionType.ContentParsingException;
                resultContent += "报文解析异常!";
            }
            catch (TaskCanceledException) {
                isSuccess = false;
                resultContent += "接口访问返回超时!";
                exceptionType = ApiExceptionType.Timeout;
                exceptionMsg += "接口访问返回超时!";
            }
            catch (Exception e) {
                isSuccess = false;
                resultContent += e.Message;
                exceptionType = ApiExceptionType.Other;
            }
            finally {
                stopwatch.Stop();
                response = new UploadResponse() {
                    ExceptionMsg = exceptionMsg,
                    ApiParameters = IntegrationParameterSerializer.Serialize(this),
                    IsSuccess = isSuccess,
                    DurationSeconds = Convert.ToDecimal(stopwatch.Elapsed.TotalSeconds),
                    RequestContent = requestContent,
                    RequestTime = requestTime,
                    RequestUrl = $"{Parameters.Url}?SIGN={sign}",
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now,
                    ApiExceptionType = exceptionType
                };
            }
            return response;
        }

        public async Task PolicyPush(ApiMethod method, string barcode,
            string orgCode,
            string phyBoxCode,
            string deviceCode,
            string theoryBoxCode,
            bool processingResult,
            DateTime processingTime,
            string mailInfoQueryResponseContent = "",
            CancellationToken token = default) {
            var waitAndRetryAsync = Policy.HandleResult<UploadResponse>(result => !result.IsSuccess)
                .Or<Exception>().WaitAndRetryAsync(Parameters.RetryCount, retryCount => TimeSpan.FromSeconds(Parameters.RetryInterval), // 重试间隔时间
                    (ex, timespan, retryCount, context) => {
                        NLog.LogManager.GetCurrentClassLogger().Error($"接口重试次数:{retryCount}");
                    });
            var uploadResponse = await waitAndRetryAsync.ExecuteAsync(async () => {
                return method switch {
                    ApiMethod.PickingInfoPush => await CallApiMethod(ApiMethod.PickingInfoPush, barcode,
                        orgCode ?? string.Empty, phyBoxCode, deviceCode, theoryBoxCode, processingResult,
                        processingTime, mailInfoQueryResponseContent, token),
                    ApiMethod.ScanInfoPush => await CallApiMethod(ApiMethod.ScanInfoPush, barcode,
                        orgCode ?? string.Empty, phyBoxCode, deviceCode, theoryBoxCode,
                        processingResult, processingTime, mailInfoQueryResponseContent, token),

                    _ => new UploadResponse()
                };
            });

            //写出推送信息
            NLog.LogManager.GetCurrentClassLogger().Info(JsonConvert.SerializeObject(uploadResponse));
        }

        public static byte[] CombineByteArrays(byte[] array1, byte[] array2) {
            var newArray = new byte[array1.Length + array2.Length];
            Array.Copy(array1, 0, newArray, 0, array1.Length);
            Array.Copy(array2, 0, newArray, array1.Length, array2.Length);
            return newArray;
        }

        public static string GetSha256Hex(byte[] bytes) {
            // DWS-HEX-COMPACT: 外部接口签名要求使用无分隔符的小写摘要。
            return Convert.ToHexStringLower(SHA256.HashData(bytes));
        }

        public enum ApiMethod {

            /// <summary>
            /// 邮件信息查询接口
            /// </summary>
            MailInfoQuery,

            /// <summary>
            /// 扫描信息推送接口
            /// </summary>
            ScanInfoPush,

            /// <summary>
            /// 分拣信息推送接口
            /// </summary>
            PickingInfoPush
        }

        public class ApiParameters {

            /// <summary>
            /// Url
            /// </summary>
            public string Url { get; set; } = "http://33qhun.natappfree.cc/siss-sorting/service";

            /// <summary>
            /// 超时
            /// </summary>
            public int TimeOut { get; set; } = 1000;

            /// <summary>
            /// SignKey
            /// </summary>
            public string SignKey { get; set; } = "R1O2U3T4D5A6T7A8X9B0S9O8R7T6I5N4G3*2@1";

            /// <summary>
            /// 重试次数
            /// </summary>
            public int RetryCount { get; set; }

            /// <summary>
            /// 重试间隔
            /// </summary>
            public int RetryInterval { get; set; }

            /// <summary>
            /// 设备代码
            /// </summary>
            public string DeviceCode { get; set; } = "51811101007";

            /// <summary>
            /// 机构代码
            /// </summary>
            public string OrgCode { get; set; } = "51811101";
        }
    }
}
