using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Xml.Linq;
using System.Text.Json;
using TouchSocket.Core;
using JayTom.Dws.Plugin;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using JayTom.Dws.Plugin.Excel;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Plugin.Excel.Attributes;
using static System.Net.Mime.MediaTypeNames;
using MD5 = System.Security.Cryptography.MD5;
using static JayTom.Dws.Interface.Szjy188.SzjyApi;
using JsonException = Newtonsoft.Json.JsonException;

namespace JayTom.Dws.Interface.Jtexpress {

    public class JtExpressApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameter Parameters { get; set; } = new();
        public static JtExpressUserInfo UserInfo { get; set; } = new();
        private static List<ExcelDeliveryCode> _excelDeliveryCodes = new();
        private IExcel _excel;

        public JtExpressApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
            if (_excel is null) {
                _excel = new NpoiExport();
            }
            if (_excelDeliveryCodes?.Any() != true) {
                //判断文件是否存在
                var path = $"{AppContext.BaseDirectory}ApiSettingJson\\JtThreeSegmentCodeRout";
                if (Directory.Exists(path)) {
                    var excelFile = Directory.GetFiles(path)?.Select(s => new FileInfo(s))
                        ?.Where(w => w.Extension.Equals(".xlsx"))?.OrderByDescending(o => o.LastWriteTime)
                        ?.Select(s => s.FullName)?.FirstOrDefault();
                    if (!string.IsNullOrEmpty(excelFile)) {
                        //读Excel表格内容到列表
                        //三段码、工号
                        var models = _excel.ReadExcel<ExcelDeliveryCode>(excelFile,
                            p => Task.CompletedTask,
                            e => {
                                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                                return Task.CompletedTask;
                            }
                                ).GetAwaiter().GetResult();
                        if (models?.Any() == true) {
                            _excelDeliveryCodes = models;
                            NLog.LogManager.GetCurrentClassLogger().Error($"{JsonConvert.SerializeObject(_excelDeliveryCodes)}");
                        }
                        else {
                            NLog.LogManager.GetCurrentClassLogger().Error($"读取不到Excel内容");
                        }
                    }
                    else {
                        NLog.LogManager.GetCurrentClassLogger().Error($"查找不到文件");
                    }
                }
            }
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            var deliveryCode = string.Empty;
            var generateSegmentCode = await GenerateSegmentCode(barcode);
            try {
                var jtExpressResponseResult = JsonConvert.DeserializeObject<JtExpressResponseResult>(generateSegmentCode.ResponseContent);
                if (jtExpressResponseResult?.Data is not null) {
                    var segmentCodeInfos = JsonConvert.DeserializeObject<List<SegmentCodeInfo>>(jtExpressResponseResult.Data.ToString() ?? string.Empty);

                    if (segmentCodeInfos?.Any() == true) {
                        var segmentCodeInfo = segmentCodeInfos?.FirstOrDefault();
                        var excelDeliveryCode = _excelDeliveryCodes?.FirstOrDefault(f =>
                            f.ThirdlyDispatchCode.Equals(segmentCodeInfo?.ThirdlyDispatchCode));
                        if (excelDeliveryCode is not null) {
                            deliveryCode = excelDeliveryCode.DeliveryCode;
                        }
                    }
                }
            }
            catch {
                deliveryCode = string.Empty;
            }

            if (!generateSegmentCode.ExceptionMsg.Equals("条码为NoRead")) {
                if (Parameters.BusinessType == BusinessType.ArrivalScan) {
                    ArrivalScan(barcode, weight, DateTime.Now, length, width, height, Parameters.ScanTypeCode
                        , Parameters.TransportTypeCode, Parameters.ScanPda, Parameters.ScanType, Parameters.WeightFlag
                    );
                }
                else if (Parameters.BusinessType == BusinessType.DepartureScan) {
                    DepartureScan(barcode, deliveryCode, Parameters.ScanPda);
                }
                else if (Parameters.BusinessType == BusinessType.ArrivalScanAndDepartureScan) {
                    ArrivalScan(barcode, weight, DateTime.Now, length, width, height, Parameters.ScanTypeCode
                        , Parameters.TransportTypeCode, Parameters.ScanPda, Parameters.ScanType, Parameters.WeightFlag
                    );
                    DepartureScan(barcode, deliveryCode, Parameters.ScanPda);
                }
            }

            return generateSegmentCode;
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, DateTime scanTime, double length = default, double width = default,
            double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            var deliveryCode = string.Empty;
            var generateSegmentCode = await GenerateSegmentCode(barcode);
            try {
                var jtExpressResponseResult = JsonConvert.DeserializeObject<JtExpressResponseResult>(generateSegmentCode.ResponseContent);
                if (jtExpressResponseResult?.Data is not null) {
                    var segmentCodeInfos = JsonConvert.DeserializeObject<List<SegmentCodeInfo>>(jtExpressResponseResult.Data.ToString() ?? string.Empty);

                    if (segmentCodeInfos?.Any() == true) {
                        var segmentCodeInfo = segmentCodeInfos?.FirstOrDefault();
                        var excelDeliveryCode = _excelDeliveryCodes?.FirstOrDefault(f =>
                            f.ThirdlyDispatchCode.Equals(segmentCodeInfo?.ThirdlyDispatchCode));
                        if (excelDeliveryCode is not null) {
                            deliveryCode = excelDeliveryCode.DeliveryCode;
                        }
                    }
                }
            }
            catch {
                deliveryCode = string.Empty;
            }

            if (!generateSegmentCode.ExceptionMsg.Equals("条码为NoRead")) {
                if (Parameters.BusinessType == BusinessType.ArrivalScan) {
                    ArrivalScan(barcode, weight, scanTime, length, width, height, Parameters.ScanTypeCode
                        , Parameters.TransportTypeCode, Parameters.ScanPda, Parameters.ScanType, Parameters.WeightFlag
                    );
                }
                else if (Parameters.BusinessType == BusinessType.DepartureScan) {
                    DepartureScan(barcode, deliveryCode, Parameters.ScanPda);
                }
                else if (Parameters.BusinessType == BusinessType.ArrivalScanAndDepartureScan) {
                    ArrivalScan(barcode, weight, scanTime, length, width, height, Parameters.ScanTypeCode
                        , Parameters.TransportTypeCode, Parameters.ScanPda, Parameters.ScanType, Parameters.WeightFlag
                    );
                    DepartureScan(barcode, deliveryCode, Parameters.ScanPda);
                }
            }

            return generateSegmentCode;
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            if (parameters is ApiParameter param) {
                Parameters = new ApiParameter() {
                    AppSecret = param.AppSecret,
                    AppKey = param.AppKey,
                    BusinessType = param.BusinessType,
                    Password = param.Password,
                    ScanPda = param.ScanPda,
                    ScanType = param.ScanType,
                    ScanTypeCode = param.ScanTypeCode,
                    SegmentCodeTimeOut = param.SegmentCodeTimeOut,
                    SegmentCodeUrl = param.SegmentCodeUrl,
                    TimeOut = param.TimeOut,
                    TransportTypeCode = param.TransportTypeCode,
                    Url = param.Url,
                    UserName = param.UserName,
                    WeightFlag = param.WeightFlag,
                };
                return Task.FromResult(new KeyValuePair<bool, string>(true, string.Empty));
            }
            else {
                return Task.FromResult(new KeyValuePair<bool, string>(true, "参数类型不匹配"));
            }
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

                var method = "/opa/smartLogin";
                var data = new {
                    account = userName,
                    password = sign.ToLower(),
                    appKey = appKey,
                    appSecret = appSecret,
                };
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
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

                var resultContent = message.Content.ReadAsStringAsync(token).ConfigureAwait(false).GetAwaiter().GetResult();
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrEmpty(resultContent)) {
                    //解析登录返回内容

                    var result = JsonConvert.DeserializeObject<JtExpressResponseResult>(resultContent);
                    if (result?.Succ == true) {
                        var jtExpressUserInfo =
                            JsonConvert.DeserializeObject<JtExpressUserInfo>(result?.Data?.ToString() ?? string.Empty);

                        if (jtExpressUserInfo is not null) {
                            return new KeyValuePair<bool, JtExpressUserInfo>(true, jtExpressUserInfo);
                        }
                        else {
                            return new KeyValuePair<bool, JtExpressUserInfo>(false, new JtExpressUserInfo() {
                                ExceptionMsg = "内容解析失败"
                            });
                        }
                    }
                    else {
                        return new KeyValuePair<bool, JtExpressUserInfo>(false, new JtExpressUserInfo() {
                            ExceptionMsg = "登录失败"
                        });
                    }
                }
                else {
                    return new KeyValuePair<bool, JtExpressUserInfo>(false, new JtExpressUserInfo() {
                        ExceptionMsg = "返回内容为空"
                    });
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, JtExpressUserInfo>(false, new JtExpressUserInfo() {
                    ExceptionMsg = e.Message
                });
            }
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
                if (barcode.ToLower().Equals("noread")) {
                    isSuccess = false;
                    exceptionMsg = "条码为NoRead";
                    resultContent = JsonConvert.SerializeObject(new JtExpressResponseResult {
                        Code = 500,
                        Fail = true,
                        Msg = "noread",
                        Succ = false,
                        Data = "noread"
                    });
                }
                else {
                    using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                    httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                    HttpResponseMessage message;
                    using (Stream dataStream =
                           new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");

                            message = await httpClient.PostAsync($"{Parameters.SegmentCodeUrl}{method}", content)
                                .ConfigureAwait(false);
                        }
                    }

                    resultContent = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
                    resultContent = Regex.Unescape(resultContent);
                    string pattern = "\"extendJson\":\"({(?:[^{}]|(?<open>\\{)|(?<-open>\\}))+(?(open)(?!))})\"";
                    Match match = Regex.Match(resultContent, pattern);

                    if (match.Success) {
                        string nestedJsonStr = match.Groups[1].Value;

                        // 判断嵌套 JSON 字符串是否为非空
                        if (!string.IsNullOrEmpty(nestedJsonStr)) {
                            // 将双引号进行转义
                            var extendJson = nestedJsonStr.Replace("\"", "\\\"");

                            // 执行其他操作...

                            // 输出结果
                            resultContent = resultContent.Replace(nestedJsonStr, extendJson);
                        }
                    }
                    if (!string.IsNullOrEmpty(resultContent)) {
                        //解析登录返回内容
                        var result = JsonConvert.DeserializeObject<JtExpressResponseResult>(resultContent, new JsonSerializerSettings {
                            StringEscapeHandling = StringEscapeHandling.EscapeHtml
                        });
                        isSuccess = result?.Succ ?? false;
                        if (isSuccess) {
                            var segmentCodeInfo = JsonConvert.DeserializeObject<List<SegmentCodeInfo>>(result?.Data?.ToString() ?? string.Empty, new JsonSerializerSettings {
                                StringEscapeHandling = StringEscapeHandling.EscapeHtml
                            });
                            if (string.IsNullOrEmpty(segmentCodeInfo?.FirstOrDefault()?.ThirdlyDispatchCode)) {
                                isSuccess = false;
                                exceptionMsg = "三段码为空";
                            }
                            if (isSuccess && _excelDeliveryCodes?.Any(a => a.ThirdlyDispatchCode.Equals(segmentCodeInfo?.FirstOrDefault()?.ThirdlyDispatchCode ?? string.Empty)) != true) {
                                isSuccess = false;
                                exceptionMsg = "服务器返回的三段码不在对应分拣路由表里";
                            }
                        }
                    }
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
                    ApiParameters = JsonConvert.SerializeObject(this.Parameters),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestUrl = $"{Parameters.SegmentCodeUrl}{method}",
                    RequestTime = requestTime,
                    RequestContent = JsonConvert.SerializeObject(data),
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        /// <summary>
        /// 进仓扫描
        /// </summary>
        public async void ArrivalScan(string barcode, double weight, DateTime scanTime, double length = default, double width = default,
            double height = default, string? scanTypeCode = default, string? transportTypeCode = default, string? scanPda = default,
            int scanType = 1, string? weightFlag = default) {
            //如果没登录或登录时间超20小时则先登录,
            if (UserInfo.LoginTime is null ||
                DateTime.Now.Subtract(UserInfo.LoginTime.Value).TotalHours >= 20 ||
                string.IsNullOrEmpty(UserInfo.Token)) {
                var (key, value) = LogIn(Parameters.UserName, Parameters.Password,
                    Parameters.AppKey, Parameters.AppSecret).GetAwaiter().GetResult(); ; ;
                if (key) {
                    UserInfo = value;
                }
                else {
                    NLog.LogManager.GetCurrentClassLogger().Error(JsonConvert.SerializeObject(value));
                    return;
                }
            }

            if (string.IsNullOrEmpty(UserInfo.Token)) {
                NLog.LogManager.GetCurrentClassLogger().Error("Token为空!");
                return;
            }
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            string resultContent = string.Empty;
            UploadResponse response;
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            var method = "/opa/smart/scan/uploadUnloadingArrivalData";
            var data = new object[]
            {
                new
                {
                    listId = $"{UserInfo.NetworkCode}{new DateTimeOffset(requestTime).ToUnixTimeMilliseconds()}",
                    waybillId = barcode,
                    scanTime = $"{requestTime:yyyy-MM-dd HH:mm:ss}",
                    scanTypeCode = scanTypeCode,
                    weight = weight,
                    length = length,
                    wide = width,
                    high = height,
                    transportTypeCode = transportTypeCode,
                    scanPda = scanPda,
                    scanType = scanType,
                    weightFlag = weightFlag
                }
            };
            stopwatch.Start();
            try {
                //密码加密
                //转MD5

                /*var data = new {
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
                };*/
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
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
            finally {
                stopwatch.Stop();
                response = new UploadResponse() {
                    ExceptionMsg = exceptionMsg,
                    ApiParameters = JsonConvert.SerializeObject(this.Parameters),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(data),
                    RequestTime = requestTime,
                    RequestUrl = $"{Parameters.Url}{method}",
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
            }
        }

        /// <summary>
        /// 出仓扫描
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="deliveryCode"></param>
        /// <param name="scanPda"></param>
        /// <returns></returns>
        public async void DepartureScan(string barcode, string deliveryCode, string? scanPda = default) {
            //如果没登录或登录时间超20小时则先登录,
            if (UserInfo.LoginTime is null ||
                DateTime.Now.Subtract(UserInfo.LoginTime.Value).TotalHours >= 20 ||
                string.IsNullOrEmpty(UserInfo.Token)) {
                var (key, value) = LogIn(Parameters.UserName, Parameters.Password,
                    Parameters.AppKey, Parameters.AppSecret).GetAwaiter().GetResult();
                if (key) {
                    UserInfo = value;
                }
                else {
                    NLog.LogManager.GetCurrentClassLogger().Error(JsonConvert.SerializeObject(value));
                    return;
                }
            }
            if (string.IsNullOrEmpty(UserInfo.Token)) {
                NLog.LogManager.GetCurrentClassLogger().Error("Token为空!");
                return;
            }
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            string resultContent = string.Empty;
            var method = "/opa/smart/scan/uploadDeliveryOutStockData";
            UploadResponse response;
            var requestTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            var data = new object[]
            {
                new
                {
                    listId = $"{UserInfo.NetworkCode}{new DateTimeOffset(requestTime).ToUnixTimeMilliseconds()}",
                    waybillId = barcode,
                    scanTime = $"{requestTime:yyyy-MM-dd HH:mm:ss}",
                    deliveryCode = string.IsNullOrEmpty(deliveryCode)?Parameters.UserName:deliveryCode,
                    scanPda = scanPda,
                }
            };
            try {
                //密码加密
                //转MD5

                /*var data = new {
                    listld = $"{UserInfo.NetworkCode}{new DateTimeOffset(nowDate).ToUnixTimeMilliseconds()}",
                    waybillld = barcode,
                    scanTime = $"{nowDate:yyyy-MM-dd HH:mm:ss}",
                    deliveryCode = string.Empty,
                    scanPda = scanPda,
                };*/
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
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
            finally {
                stopwatch.Stop();
                response = new UploadResponse() {
                    ExceptionMsg = exceptionMsg,
                    ApiParameters = JsonConvert.SerializeObject(this.Parameters),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(data),
                    RequestTime = requestTime,
                    RequestUrl = $"{Parameters.Url}{method}",
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
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
            DepartureScan = 1,

            /// <summary>
            /// 到派一体
            /// </summary>
            ArrivalScanAndDepartureScan = 2,
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

        public class ExcelDeliveryCode {

            /// <summary>
            /// 员工工号
            /// </summary>
            [DisplayName("员工工号"), MemberNotNull, ExcelInfo(Width = 4000)]
            public string DeliveryCode { get; set; } = string.Empty;

            /// <summary>
            /// 三段码
            /// </summary>
            [DisplayName("三段码"), MemberNotNull, ExcelInfo(Width = 4000)]
            public string ThirdlyDispatchCode { get; set; } = string.Empty;
        }
    }
}