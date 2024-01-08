using Polly;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Net.Http.Headers;
using JayTom.Dws.Interface.Cloud;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Interface.Routdata {

    public class RoutdataApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public string BaseUrl { get; set; } = "http://33qhun.natappfree.cc/siss-sorting/service";
        public string DeviceCode { get; set; } = "51811101007";

        public RoutdataApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            var callApiMethod = await CallApiMethod(ApiMethod.MailInfoQuery, barcode, string.Empty, string.Empty,
                DeviceCode, string.Empty, token: token);
            var orgCode = string.Empty;
            var phyBoxCode = string.Empty;
            var theoryBoxCode = string.Empty;
            var mailInfoQueryResponseContent = callApiMethod.ResponseContent;
            if (callApiMethod.IsSuccess) {
                //解析
                try {
                    var jObject = JObject.Parse(callApiMethod.ResponseContent);
                    orgCode = jObject?["BODY"]?["201"]?.First?["JGDM"]?.ToString();
                    phyBoxCode = jObject?["BODY"]?["201"]?.First?["WLGK"]?.ToString();
                    theoryBoxCode = jObject?["BODY"]?["201"]?.First?["YLZDONE"]?.ToString();
                }
                catch (Exception e) {
                    mailInfoQueryResponseContent += $"报文解析异常:{e.Message}";
                }
            }

            PolicyPush(ApiMethod.ScanInfoPush, barcode, orgCode ?? string.Empty, phyBoxCode ?? string.Empty,
                DeviceCode, theoryBoxCode ?? string.Empty,
                callApiMethod.IsSuccess,
                callApiMethod.ResponseTime
                , mailInfoQueryResponseContent, token).ConfigureAwait(false).GetAwaiter();
            PolicyPush(ApiMethod.PickingInfoPush, barcode, orgCode ?? string.Empty, phyBoxCode ?? string.Empty,
                DeviceCode, theoryBoxCode ?? string.Empty,
                callApiMethod.IsSuccess,
                callApiMethod.ResponseTime
                , callApiMethod.IsSuccess ? string.Empty : mailInfoQueryResponseContent, token).ConfigureAwait(false).GetAwaiter();
            return callApiMethod;
        }

        public Task<UploadResponse> UploadData(string barcode, double weight, DateTime scanTime, double length = default, double width = default,
            double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            throw new NotImplementedException();
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
            bool processingResult = default,
            DateTime processingTime = default,
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
            stopwatch.Start();
            try {
                using var httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip });
                //using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(5000);
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
                var signBytes = CombineByteArrays(compressedData, Encoding.UTF8.GetBytes("R1O2U3T4D5A6T7A8X9B0S9O8R7T6I5N4G3*2@1"));

                sign = GetSha256Hex(signBytes).ToUpper();

                using (HttpContent content = new ByteArrayContent(compressedData)) {
                    content.Headers.Add("Content-Encoding", "gzip");
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{BaseUrl}?SIGN={sign}", content, token).ConfigureAwait(false);
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);

                var jObject = JObject.Parse(requestContent);

                isSuccess = jObject?["HEAD"]?["RET_CODE"]?.ToString()?.Equals("0") == true;
            }
            catch (HttpRequestException e) {
                isSuccess = false;
                resultContent += e.Message;
            }
            catch (AggregateException) {
                isSuccess = false;
                resultContent += "接口访问异常!";
            }
            catch (JsonException) {
                isSuccess = false;
                exceptionMsg += "报文解析异常!";
            }
            catch (TaskCanceledException) {
                isSuccess = false;
                resultContent += "接口访问返回超时!";
            }
            catch (Exception e) {
                isSuccess = false;
                resultContent += e.Message;
            }
            finally {
                stopwatch.Stop();
                response = new UploadResponse() {
                    ExceptionMsg = exceptionMsg,
                    ApiParameters = JsonConvert.SerializeObject(this),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = requestContent,
                    RequestTime = requestTime,
                    RequestUrl = $"{BaseUrl}?SIGN={sign}",
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public async Task PolicyPush(ApiMethod method, string barcode,
            string orgCode,
            string phyBoxCode,
            string deviceCode,
            string theoryBoxCode,
            bool processingResult = default,
            DateTime processingTime = default,
            string mailInfoQueryResponseContent = "",
            CancellationToken token = default) {
            var waitAndRetryAsync = Policy.HandleResult<UploadResponse>(result => !result.IsSuccess)
                .Or<Exception>().WaitAndRetryAsync(5, retryCount => TimeSpan.FromSeconds(3), // 重试间隔时间
                    (ex, timespan, retryCount, context) => {
                        NLog.LogManager.GetCurrentClassLogger().Error($"接口重试次数:{retryCount}");
                    });
            var uploadResponse = await waitAndRetryAsync.ExecuteAsync(async () => {
                return method switch {
                    ApiMethod.PickingInfoPush => await CallApiMethod(ApiMethod.PickingInfoPush, barcode,
                        orgCode ?? string.Empty, phyBoxCode, DeviceCode, theoryBoxCode, processingResult,
                        processingTime, mailInfoQueryResponseContent, token),
                    ApiMethod.ScanInfoPush => await CallApiMethod(ApiMethod.ScanInfoPush, barcode,
                        orgCode ?? string.Empty, phyBoxCode, DeviceCode, theoryBoxCode, token: token),
                    _ => new UploadResponse()
                };
            });
        }

        public static byte[] CombineByteArrays(byte[] array1, byte[] array2) {
            var newArray = new byte[array1.Length + array2.Length];
            Array.Copy(array1, 0, newArray, 0, array1.Length);
            Array.Copy(array2, 0, newArray, array1.Length, array2.Length);
            return newArray;
        }

        public static string GetSha256Hex(byte[] bytes) {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(bytes);
            var builder = new StringBuilder();
            foreach (byte b in hashBytes) {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
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
    }
}