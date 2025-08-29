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
using JayTom.Dws.Domain.Interface;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Plugin.Excel.Attributes;
using static System.Net.Mime.MediaTypeNames;
using JayTom.Dws.Domain.Interface.Attributes;
using MD5 = System.Security.Cryptography.MD5;
using JsonException = Newtonsoft.Json.JsonException;
using static JayTom.Dws.Interface.ApiImplementations.Szjy188.SzjyApi;

namespace JayTom.Dws.Interface.ApiImplementations.Jtexpress {

    [ApiClass("极兔Api", "JtExpressApi", "JtExpressApiParameters", "1.0", ExecutionType.UploadInformation | ExecutionType.SendSortingReport)]
    public class JtExpressApi : IApiUploader<JtExpressApi.ApiParameter> {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameter Parameters { get; set; } = new();

        public bool SetParameters(object parameters) {
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
            if (parameters is not ApiParameter param) return false;

            Parameters = param;
            return true;
        }

        public void OpenJsonConfigFile() {
        }

        public async Task<UploadResponse> UploadInformation([NotNull] string barcode, [NotNull] double weight, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            var response = await GenerateSegmentCode(barcode);
            response.ExecutionType = ExecutionType.UploadInformation;
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
            var deliveryCode = string.Empty;
            if (other is UploadResponse uploadResponse) {
                try {
                    var jtExpressResponseResult = JsonConvert.DeserializeObject<JtExpressResponseResult>(uploadResponse.ResponseContent);
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
                if (!barcode.ToLower().Equals("noread")) {
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
                        await Task.Delay(TimeSpan.FromSeconds(10), token);
                        DepartureScan(barcode, deliveryCode, Parameters.ScanPda);
                    }
                }
            }

            return new UploadResponse() {
                ExecutionType = ExecutionType.SendSortingReport
            };
        }

        public Task<UploadResponse> SendPickupReport([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default,
         double width = default, double height = default, double volume = default, long packageId = default,
         UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
         CancellationToken token = default) {
            return Task.FromResult(new UploadResponse() {
                ExecutionType = ExecutionType.SendPickupReport
            });
        }

        public Task<UploadResponse> SendConsolidationReport(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse() {
                ExecutionType = ExecutionType.SendConsolidationReport
            });
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

        public static JtExpressUserInfo UserInfo { get; set; } = new();
        private static List<ExcelDeliveryCode> _excelDeliveryCodes = new();
        private readonly IExcel _excel;

        public JtExpressApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
            _excel ??= new NpoiExport();
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            return await GenerateSegmentCode(barcode);
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
                    appKey,
                    appSecret,
                };
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.TimeOut);
                HttpResponseMessage message;
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    message = await httpClient.PostAsync($"{Parameters.Url}{method}", content, token)
                        .ConfigureAwait(false);
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
            var resultContent = string.Empty;
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
                    httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.SegmentCodeTimeOut);
                    HttpResponseMessage message;
                    await using (Stream dataStream =
                                 new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                        using HttpContent content = new StreamContent(dataStream);
                        content.Headers.Add("Content-Type", "application/json");

                        message = await httpClient.PostAsync($"{Parameters.SegmentCodeUrl}{method}", content)
                            .ConfigureAwait(false);
                    }

                    resultContent = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
                    resultContent = Regex.Unescape(resultContent);
                    var pattern = "\"extendJson\":\"({(?:[^{}]|(?<open>\\{)|(?<-open>\\}))+(?(open)(?!))})\"";
                    var match = Regex.Match(resultContent, pattern);

                    if (match.Success) {
                        var nestedJsonStr = match.Groups[1].Value;

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
                            /*if (isSuccess && _excelDeliveryCodes?.Any(a => a.ThirdlyDispatchCode.Equals(segmentCodeInfo?.FirstOrDefault()?.ThirdlyDispatchCode ?? string.Empty)) != true &&
                                Parameters.BusinessType == BusinessType.ArrivalScanAndDepartureScan) {
                                isSuccess = false;
                                exceptionMsg = "服务器返回的三段码不在对应分拣路由表里";
                            }*/
                            var info = segmentCodeInfo?.FirstOrDefault();
                            if (info is not null && info.Interceptor == 1 && Parameters.InterceptorEnabled) {
                                isSuccess = false;
                                exceptionMsg = "拦截件";
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
                    ApiParameters = JsonConvert.SerializeObject(Parameters),
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
                    return;
                }
            }

            if (string.IsNullOrEmpty(UserInfo.Token)) {
                NLog.LogManager.GetCurrentClassLogger().Error("Token为空!");
                return;
            }
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var resultContent = string.Empty;
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
                    scanTypeCode,
                    weight,
                    length,
                    wide = width,
                    high = height,
                    transportTypeCode,
                    scanPda,
                    scanType,
                    weightFlag
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
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    content.Headers.Add("authToken", UserInfo.Token);
                    message = await httpClient.PostAsync($"{Parameters.Url}{method}", content)
                        .ConfigureAwait(false);
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
                    ApiParameters = JsonConvert.SerializeObject(Parameters),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(data),
                    RequestTime = requestTime,
                    RequestUrl = $"{Parameters.Url}{method}",
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
                NLog.LogManager.GetCurrentClassLogger().Error($"进仓扫描:{JsonConvert.SerializeObject(response)}");
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
            var resultContent = string.Empty;
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
                    scanPda,
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
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "application/json");
                    content.Headers.Add("authToken", UserInfo.Token);
                    message = await httpClient.PostAsync($"{Parameters.Url}{method}", content)
                        .ConfigureAwait(false);
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
                    ApiParameters = JsonConvert.SerializeObject(Parameters),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(data),
                    RequestTime = requestTime,
                    RequestUrl = $"{Parameters.Url}{method}",
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now
                };
                NLog.LogManager.GetCurrentClassLogger().Error($"出仓扫描:{JsonConvert.SerializeObject(response)}");
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

        public class ApiParameter : BaseApiParameters {
            /*/// <summary>
            /// Url
            /// </summary>
            public string Url { get; set; } = "https://opa.jtexpress.com.cn";*/

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

            /// <summary>
            /// 是否启用拦截件
            /// </summary>
            public bool InterceptorEnabled { get; set; }
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