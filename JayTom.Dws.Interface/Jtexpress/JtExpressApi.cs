using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Xml.Linq;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using static JayTom.Dws.Interface.Szjy188.SzjyApi;

namespace JayTom.Dws.Interface.Jtexpress {

    public class JtExpressApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameter Parameters { get; set; } = new();
        public JtExpressUserInfo UserInfo { get; set; } = new();

        public JtExpressApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            if (Parameters.BusinessType == BusinessType.ArrivalScan) {
                ArrivalScan(barcode, weight, DateTime.Now, length, width, height, Parameters.ScanTypeCode
                , Parameters.TransportTypeCode, Parameters.ScanPda, Parameters.ScanType, Parameters.WeightFlag
                    ).Start();
            }
            else if (Parameters.BusinessType == BusinessType.DepartureScan) {
                DepartureScan(barcode, Parameters.ScanPda).Start();
            }

            return await GenerateSegmentCode(barcode);
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, DateTime scanTime, double length = default, double width = default,
            double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            if (Parameters.BusinessType == BusinessType.ArrivalScan) {
                ArrivalScan(barcode, weight, scanTime, length, width, height, Parameters.ScanTypeCode
                    , Parameters.TransportTypeCode, Parameters.ScanPda, Parameters.ScanType, Parameters.WeightFlag
                ).Start();
            }
            else if (Parameters.BusinessType == BusinessType.DepartureScan) {
                DepartureScan(barcode, Parameters.ScanPda).Start();
            }

            return await GenerateSegmentCode(barcode);
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="passWord"></param>
        /// <param name="appSecret"></param>
        /// <param name="token"></param>
        /// <param name="appKey"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, JtExpressUserInfo>> LogIn(string userName, string passWord,
            string appKey, string appSecret,
            CancellationToken token = default) {
            try {
                //密码加密
                //转MD5
                string sign;
                using (var md5 = MD5.Create()) {
                    var result = md5.ComputeHash(Encoding.UTF8.GetBytes(passWord));
                    var strResult = BitConverter.ToString(result);
                    sign = strResult.Replace("-", "");
                }
                string resultContent;
                var method = "/opa/smartLogin";
                var data = new {
                    account = userName,
                    password = sign.ToLower(),
                    appKey = appKey,
                    appSecret = appSecret,
                };
                //using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                HttpResponseMessage message;
                using (Stream dataStream =
                       new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                    using (HttpContent content = new StreamContent(dataStream)) {
                        content.Headers.Add("Content-Type", "application/json");
                        message = await httpClient.PostAsync($"{Parameters.Url}{method}", content, token)
                            .ConfigureAwait(false);
                    }
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrEmpty(resultContent)) {
                    //解析登录返回内容

                    var result = JsonConvert.DeserializeObject<JtExpressResponseResult>(resultContent);
                    if (result?.Succ == true) {
                        var jtExpressUserInfo = JsonConvert.DeserializeObject<JtExpressUserInfo>(result?.Data?.ToString() ?? string.Empty);

                        if (jtExpressUserInfo is not null) {
                            return new KeyValuePair<bool, JtExpressUserInfo>(true, jtExpressUserInfo);
                        }
                    }
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, JtExpressUserInfo>(false, new JtExpressUserInfo() {
                    ExceptionMsg = e.Message
                });
            }
            return new KeyValuePair<bool, JtExpressUserInfo>(false, new JtExpressUserInfo() {
                ExceptionMsg = "未知错误"
            });
        }

        /// <summary>
        /// 三段码
        /// </summary>
        /// <returns></returns>
        public async Task<UploadResponse> GenerateSegmentCode(string barcode) {
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            string resultContent = string.Empty;
            var method = "assSortingSegmented/listByWaybillNo";
            UploadResponse response;
            var requestTime = DateTime.Now;
            var data = new {
                waybillNo = barcode,
            };
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try {
                //using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                HttpResponseMessage message;
                using (Stream dataStream =
                       new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                    using (HttpContent content = new StreamContent(dataStream)) {
                        content.Headers.Add("Content-Type", "application/json");

                        message = await httpClient.PostAsync($"{Parameters.Url}{method}", content)
                            .ConfigureAwait(false);
                    }
                }

                resultContent = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrEmpty(resultContent)) {
                    //解析登录返回内容
                    var result = JsonConvert.DeserializeObject<JtExpressResponseResult>(resultContent);
                    isSuccess = result?.Succ ?? false;
                }
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
                    RequestContent = $"{Parameters.SegmentCodeUrl}{method}",
                    RequestTime = requestTime,
                    RequestUrl = JsonConvert.SerializeObject(data),
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        /// <summary>
        /// 进仓扫描
        /// </summary>
        public async Task ArrivalScan(string barcode, double weight, DateTime scanTime, double length = default, double width = default,
            double height = default, string? scanTypeCode = default, string? transportTypeCode = default, string? scanPda = default,
            int scanType = 1, string? weightFlag = default) {
            //如果没登录或登录时间超20小时则先登录,

            try {
                //密码加密
                //转MD5
                var nowDate = DateTime.Now;
                string resultContent;
                var method = "/opa/smart/scan/uploadUnloadingArrivalData";
                var data = new {
                    listld = $"{UserInfo.NetworkCode}{new DateTimeOffset(nowDate).ToUnixTimeMilliseconds()}",
                    waybillld = barcode,
                    scanTime = $"{nowDate:yyyy-MM-dd HH:mm:ss}",
                    scanTypeCode = scanTypeCode,
                    weight = weight,
                    length = length,
                    wide = width,
                    high = height,
                    transportTypeCode = transportTypeCode,
                    scanPda = scanPda,
                    scanType = scanType,
                    weightFlag = weightFlag
                };
                //using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                HttpResponseMessage message;
                using (Stream dataStream =
                       new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                    using (HttpContent content = new StreamContent(dataStream)) {
                        content.Headers.Add("Content-Type", "application/json");
                        content.Headers.Add("authToken", UserInfo.Token);
                        message = await httpClient.PostAsync($"{Parameters.Url}{method}", content)
                            .ConfigureAwait(false);
                    }
                }

                resultContent = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrEmpty(resultContent)) {
                    //解析登录返回内容

                    var result = JsonConvert.DeserializeObject<JtExpressResponseResult>(resultContent);
                    if (result?.Succ != true) {
                        NLog.LogManager.GetCurrentClassLogger().Error(resultContent);
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error(e.Message);
            }
        }

        /// <summary>
        /// 出仓扫描
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="scanPda"></param>
        /// <returns></returns>
        public async Task DepartureScan(string barcode, string? scanPda = default) {
            //如果没登录或登录时间超20小时则先登录,
            try {
                //密码加密
                //转MD5
                var nowDate = DateTime.Now;
                string resultContent;
                var method = "/opa/smart/scan/uploadDeliveryOutStockData";
                var data = new {
                    listld = $"{UserInfo.NetworkCode}{new DateTimeOffset(nowDate).ToUnixTimeMilliseconds()}",
                    waybillld = barcode,
                    scanTime = $"{nowDate:yyyy-MM-dd HH:mm:ss}",
                    deliveryCode = string.Empty,
                    scanPda = scanPda,
                };
                //using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                HttpResponseMessage message;
                using (Stream dataStream =
                       new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                    using (HttpContent content = new StreamContent(dataStream)) {
                        content.Headers.Add("Content-Type", "application/json");
                        content.Headers.Add("authToken", UserInfo.Token);
                        message = await httpClient.PostAsync($"{Parameters.Url}{method}", content)
                            .ConfigureAwait(false);
                    }
                }

                resultContent = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrEmpty(resultContent)) {
                    //解析登录返回内容

                    var result = JsonConvert.DeserializeObject<JtExpressResponseResult>(resultContent);
                    if (result?.Succ != true) {
                        NLog.LogManager.GetCurrentClassLogger().Error(resultContent);
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error(e.Message);
            }
        }

        /// <summary>
        /// 业务类型
        /// </summary>
        public enum BusinessType {

            /// <summary>
            /// 到件扫描
            /// </summary>
            ArrivalScan = 0,

            /// <summary>
            /// 出仓扫描
            /// </summary>
            DepartureScan = 1
        }

        public class JtExpressUserInfo {

            /// <summary>
            /// 登录人的网点编码
            /// </summary>
            public string NetworkId { get; set; } = string.Empty;

            /// <summary>
            /// 网点代码
            /// </summary>
            public string NetworkCode { get; set; } = string.Empty;

            /// <summary>
            /// 登录人的网点名称
            /// </summary>
            public string NetworkName { get; set; } = string.Empty;

            /// <summary>
            /// 用户名
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// 登录时间
            /// </summary>
            public DateTime? LoginTime { get; set; }

            /// <summary>
            /// Token
            /// </summary>
            public string Token { get; set; } = string.Empty;

            /// <summary>
            /// 错误信息
            /// </summary>
            public string ExceptionMsg { get; set; } = string.Empty;
        }

        public class JtExpressResponseResult {
            public int Code { get; set; }
            public string Msg { get; set; } = string.Empty;
            public object? Data { get; set; }
            public bool Succ { get; set; }
            public bool Fail { get; set; }
        }

        public class SegmentCodeInfo {
            public string? WaybillNo { get; set; }
            public string? TerminalDispatchCode { get; set; }
            public string? FirstDispatchCode { get; set; }
            public string? SecondDispatchCode { get; set; }
            public string? ThirdlyDispatchCode { get; set; }
            public string? CustomerCode { get; set; }
            public int? Interceptor { get; set; }
        }

        public class ApiParameter {

            /// <summary>
            /// Url
            /// </summary>
            public string Url { get; set; } = "https://opa.jtexpress.com.cn";

            /// <summary>
            /// 账号
            /// </summary>
            public string UserName { get; set; } = string.Empty;

            /// <summary>
            /// 密码
            /// </summary>
            public string Password { get; set; } = string.Empty;

            /// <summary>
            /// AppKey
            /// </summary>
            public string AppKey { get; set; } = "default";

            /// <summary>
            /// AppSecret
            /// </summary>
            public string AppSecret { get; set; } = "default";

            /// <summary>
            /// 超时
            /// </summary>
            public int TimeOut { get; set; } = 1000;

            /// <summary>
            /// 条码类型
            /// </summary>
            public string ScanTypeCode { get; set; } = string.Empty;

            /// <summary>
            /// 运输方式id
            /// </summary>
            public string TransportTypeCode { get; set; } = string.Empty;

            /// <summary>
            /// 设备编号
            /// </summary>
            public string ScanPda { get; set; } = string.Empty;

            /// <summary>
            /// 扫描类型
            /// </summary>
            public int ScanType { get; set; }

            /// <summary>
            /// 重量标识
            /// </summary>
            public string WeightFlag { get; set; } = string.Empty;

            //------三段码的上传参数-----------
            /// <summary>
            /// Url
            /// </summary>
            public string SegmentCodeUrl { get; set; } = "https://opa.jtexpress.com.cn";

            /// <summary>
            /// 超时
            /// </summary>
            public int SegmentCodeTimeOut { get; set; } = 1000;

            /// <summary>
            /// 业务类型
            /// </summary>
            public BusinessType BusinessType { get; set; }
        }
    }
}