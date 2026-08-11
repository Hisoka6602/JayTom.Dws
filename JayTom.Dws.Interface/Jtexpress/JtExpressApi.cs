using System.ComponentModel;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using JayTom.Dws.Plugin.Excel;
using JayTom.Dws.Plugin.Excel.Attributes;
using Newtonsoft.Json;
using NLog;

namespace JayTom.Dws.Interface.Jtexpress {

    /// <summary>
    /// 极兔旧版 OPA 接口。
    /// </summary>
    public sealed class JtExpressApi : IDataUploader {
        /// <summary>
        /// 接口日志记录器。
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 三段码与员工工号映射的异步缓存。
        /// </summary>
        private static readonly Lazy<Task<FrozenDictionary<string, string>>> DeliveryCodeCache =
            new(LoadDeliveryCodesAsync, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// HTTP 客户端工厂。
        /// </summary>
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// 登录互斥锁，防止并发刷新令牌。
        /// </summary>
        private readonly SemaphoreSlim _loginGate = new(1, 1);

        /// <summary>
        /// 当前接口参数快照。
        /// </summary>
        private ApiParameter _parameters = new();

        /// <summary>
        /// 当前登录信息快照。
        /// </summary>
        private JtExpressUserInfo _userInfo = new();

        /// <summary>
        /// 初始化极兔旧版 OPA 接口。
        /// </summary>
        /// <param name="httpClientFactory">HTTP 客户端工厂。</param>
        public JtExpressApi(IHttpClientFactory httpClientFactory) {
            ArgumentNullException.ThrowIfNull(httpClientFactory);
            _httpClientFactory = httpClientFactory;
            _ = DeliveryCodeCache.Value;
        }

        /// <summary>
        /// 查询三段码。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="volume">体积。</param>
        /// <param name="imageInfo">扫码图片。</param>
        /// <param name="panoramaImageInfos">全景图片。</param>
        /// <param name="other">扩展信息。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>三段码查询结果。</returns>
        public Task<UploadResponse> UploadData(
            string barcode,
            decimal weight,
            decimal length = default,
            decimal width = default,
            decimal height = default,
            decimal volume = default,
            UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null,
            CancellationToken token = default) {
            return GenerateSegmentCode(barcode, token);
        }

        /// <summary>
        /// 查询三段码。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="scanTime">扫码时间。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="volume">体积。</param>
        /// <param name="imageInfo">扫码图片。</param>
        /// <param name="panoramaImageInfos">全景图片。</param>
        /// <param name="other">扩展信息。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>三段码查询结果。</returns>
        public Task<UploadResponse> UploadData(
            string barcode,
            decimal weight,
            DateTime scanTime,
            decimal length = default,
            decimal width = default,
            decimal height = default,
            decimal volume = default,
            UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null,
            CancellationToken token = default) {
            return GenerateSegmentCode(barcode, token);
        }

        /// <summary>
        /// 设置接口参数并替换当前不可变参数快照。
        /// </summary>
        /// <typeparam name="T">参数类型。</typeparam>
        /// <param name="parameters">接口参数。</param>
        /// <returns>参数是否设置成功及失败原因。</returns>
        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            if (parameters is not ApiParameter parameter) {
                return Task.FromResult(
                    new KeyValuePair<bool, string>(false, "参数类型不匹配"));
            }

            if (string.IsNullOrWhiteSpace(parameter.Url) ||
                string.IsNullOrWhiteSpace(parameter.SegmentCodeUrl)) {
                return Task.FromResult(
                    new KeyValuePair<bool, string>(false, "接口地址不能为空"));
            }

            if (parameter.TimeOut <= 0 ||
                parameter.SegmentCodeTimeOut <= 0) {
                return Task.FromResult(
                    new KeyValuePair<bool, string>(false, "接口超时必须大于零"));
            }

            var snapshot = parameter.Clone();
            Interlocked.Exchange(ref _parameters, snapshot);
            Interlocked.Exchange(ref _userInfo, new JtExpressUserInfo());
            return Task.FromResult(
                new KeyValuePair<bool, string>(true, string.Empty));
        }

        /// <summary>
        /// 根据旧版业务类型执行到件或出仓扫描。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="scanTime">扫码时间。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="volume">体积。</param>
        /// <param name="imageInfo">扫码图片。</param>
        /// <param name="panoramaImageInfos">全景图片。</param>
        /// <param name="other">三段码查询结果。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>后台上传任务。</returns>
        public async Task UploadInBackground(
            string barcode,
            decimal weight,
            DateTime scanTime,
            decimal length = default,
            decimal width = default,
            decimal height = default,
            decimal volume = default,
            UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null,
            CancellationToken token = default) {
            if (string.Equals(barcode, "noread", StringComparison.OrdinalIgnoreCase) ||
                other is not UploadResponse uploadResponse) {
                return;
            }

            var parameters = Volatile.Read(ref _parameters);
            var deliveryCode = await ResolveDeliveryCodeAsync(uploadResponse)
                .ConfigureAwait(false);
            var decimalWeight = Convert.ToDecimal(weight);

            switch (parameters.BusinessType) {
                case BusinessType.ArrivalScan:
                    var arrivalResponse = await ArrivalScanAsync(
                            barcode,
                            decimalWeight,
                            scanTime,
                            Convert.ToDecimal(length),
                            Convert.ToDecimal(width),
                            Convert.ToDecimal(height),
                            token)
                        .ConfigureAwait(false);
                    EnsureScanSucceeded(arrivalResponse);
                    break;

                case BusinessType.DepartureScan:
                    var departureResponse = await DepartureScanAsync(
                            barcode,
                            deliveryCode,
                            scanTime,
                            token)
                        .ConfigureAwait(false);
                    EnsureScanSucceeded(departureResponse);
                    break;

                case BusinessType.ArrivalScanAndDepartureScan:
                    var combinedArrivalResponse = await ArrivalScanAsync(
                            barcode,
                            decimalWeight,
                            scanTime,
                            Convert.ToDecimal(length),
                            Convert.ToDecimal(width),
                            Convert.ToDecimal(height),
                            token)
                        .ConfigureAwait(false);
                    EnsureScanSucceeded(combinedArrivalResponse);
                    await Task.Delay(TimeSpan.FromSeconds(10), token)
                        .ConfigureAwait(false);
                    var combinedDepartureResponse = await DepartureScanAsync(
                            barcode,
                            deliveryCode,
                            scanTime,
                            token)
                        .ConfigureAwait(false);
                    EnsureScanSucceeded(combinedDepartureResponse);
                    break;
            }
        }

        /// <summary>
        /// 旧版极兔接口不支持集包上报。
        /// </summary>
        /// <param name="packageExit">格口。</param>
        /// <param name="aggregatePackageCode">集包码。</param>
        /// <param name="packagingTime">集包时间。</param>
        /// <param name="packageItems">包裹列表。</param>
        /// <param name="other">扩展信息。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>已完成任务。</returns>
        public Task PackageAggregation(
            string packageExit,
            string aggregatePackageCode,
            DateTime packagingTime,
            List<string> packageItems,
            object? other = null,
            CancellationToken token = default) {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 登录旧版极兔 OPA。
        /// </summary>
        /// <param name="userName">用户名。</param>
        /// <param name="passWord">密码。</param>
        /// <param name="appKey">应用标识。</param>
        /// <param name="appSecret">应用密钥。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>登录状态与用户信息。</returns>
        public async Task<KeyValuePair<bool, JtExpressUserInfo>> LogIn(
            string userName,
            string passWord,
            string appKey,
            string appSecret,
            CancellationToken token = default) {
            await _loginGate.WaitAsync(token).ConfigureAwait(false);
            try {
                var result = await LogInCoreAsync(
                        userName,
                        passWord,
                        appKey,
                        appSecret,
                        token)
                    .ConfigureAwait(false);
                if (result.Key) {
                    Interlocked.Exchange(ref _userInfo, result.Value);
                }

                return result;
            }
            finally {
                _loginGate.Release();
            }
        }

        /// <summary>
        /// 兼容旧调用方的到件扫描入口。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="scanTime">扫码时间。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="scanTypeCode">条码类型。</param>
        /// <param name="transportTypeCode">运输方式。</param>
        /// <param name="scanPda">设备编号。</param>
        /// <param name="scanType">扫描类型。</param>
        /// <param name="weightFlag">重量标识。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>到件扫描任务。</returns>
        public async Task ArrivalScan(
            string barcode,
            decimal weight,
            DateTime scanTime,
            decimal length = default,
            decimal width = default,
            decimal height = default,
            string? scanTypeCode = default,
            string? transportTypeCode = default,
            string? scanPda = default,
            int scanType = 1,
            string? weightFlag = default,
            CancellationToken token = default) {
            await ArrivalScanAsync(
                    barcode,
                    Convert.ToDecimal(weight),
                    scanTime,
                    Convert.ToDecimal(length),
                    Convert.ToDecimal(width),
                    Convert.ToDecimal(height),
                    token,
                    scanTypeCode,
                    transportTypeCode,
                    scanPda,
                    scanType,
                    weightFlag)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// 兼容旧调用方的出仓扫描入口。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="deliveryCode">员工工号。</param>
        /// <param name="scanPda">设备编号。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>出仓扫描任务。</returns>
        public async Task DepartureScan(
            string barcode,
            string deliveryCode,
            string? scanPda = default,
            CancellationToken token = default) {
            await DepartureScanAsync(
                    barcode,
                    deliveryCode,
                    DateTime.Now,
                    token,
                    scanPda)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// 查询旧版三段码。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>三段码查询结果。</returns>
        public async Task<UploadResponse> GenerateSegmentCode(
            string barcode,
            CancellationToken token = default) {
            var parameters = Volatile.Read(ref _parameters);
            var method = "/assSortingSegmented/listByWaybillNo";
            var requestUrl = CombineUrl(parameters.SegmentCodeUrl, method);
            var requestTime = DateTime.Now;
            var request = new {
                waybillNo = barcode
            };
            var requestContent = JsonConvert.SerializeObject(request);
            var stopwatch = Stopwatch.StartNew();
            var resultContent = string.Empty;
            var exceptionMessage = string.Empty;
            var exceptionType = ApiExceptionType.None;
            var isSuccess = false;

            try {
                if (string.Equals(
                        barcode,
                        "noread",
                        StringComparison.OrdinalIgnoreCase)) {
                    exceptionMessage = "条码为NoRead";
                    exceptionType = ApiExceptionType.LogicValidationFailed;
                    resultContent = JsonConvert.SerializeObject(
                        new JtExpressResponseResult {
                            Code = 500,
                            Fail = true,
                            Msg = "noread",
                            Succ = false,
                            Data = "noread"
                        });
                }
                else {
                    resultContent = await PostJsonAsync(
                            requestUrl,
                            requestContent,
                            parameters.SegmentCodeTimeOut,
                            null,
                            token)
                        .ConfigureAwait(false);
                    var result =
                        JsonConvert.DeserializeObject<JtExpressResponseResult>(
                            resultContent);
                    isSuccess = result?.Succ == true;
                    if (!isSuccess) {
                        exceptionMessage = result?.Msg ?? "三段码查询失败";
                        exceptionType = ApiExceptionType.LogicValidationFailed;
                    }
                    else {
                        var infos =
                            JsonConvert.DeserializeObject<List<SegmentCodeInfo>>(
                                result?.Data?.ToString() ?? string.Empty);
                        var info = infos?.FirstOrDefault();
                        if (parameters.InterceptorEnabled &&
                            info?.Interceptor == 1) {
                            isSuccess = false;
                            exceptionMessage = "拦截件";
                            exceptionType =
                                ApiExceptionType.LogicValidationFailed;
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested) {
                exceptionMessage = "接口访问返回超时";
                exceptionType = ApiExceptionType.Timeout;
            }
            catch (HttpRequestException exception) {
                exceptionMessage = exception.Message;
                exceptionType = ApiExceptionType.UnreachableUrl;
            }
            catch (JsonException exception) {
                exceptionMessage = exception.Message;
                exceptionType = ApiExceptionType.ContentParsingException;
            }
            catch (Exception exception) when (exception is not OperationCanceledException) {
                exceptionMessage = exception.Message;
                exceptionType = ApiExceptionType.Other;
            }
            finally {
                stopwatch.Stop();
            }

            return new UploadResponse {
                ExceptionMsg = exceptionMessage,
                ApiExceptionType = exceptionType,
                ApiParameters = CreateRedactedParameterJson(parameters),
                IsSuccess = isSuccess,
                DurationSeconds = Convert.ToDecimal(stopwatch.Elapsed.TotalSeconds),
                RequestUrl = requestUrl,
                RequestTime = requestTime,
                RequestContent = requestContent,
                ResponseContent = resultContent,
                ResponseTime = DateTime.Now
            };
        }

        /// <summary>
        /// 执行到件扫描。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="scanTime">扫码时间。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>上传结果。</returns>
        private async Task<UploadResponse> ArrivalScanAsync(
            string barcode,
            decimal weight,
            DateTime scanTime,
            decimal length,
            decimal width,
            decimal height,
            CancellationToken token,
            string? scanTypeCode = null,
            string? transportTypeCode = null,
            string? scanPda = null,
            int? scanType = null,
            string? weightFlag = null) {
            var parameters = Volatile.Read(ref _parameters);
            var userInfo = await EnsureLoggedInAsync(token).ConfigureAwait(false);
            if (userInfo is null) {
                return CreateLoginFailureResponse(
                    "/opa/smart/scan/uploadUnloadingArrivalData");
            }

            var requestTime = DateTime.Now;
            var data = new object[] {
                new {
                    listId =
                        $"{userInfo.NetworkCode}{new DateTimeOffset(requestTime).ToUnixTimeMilliseconds()}",
                    waybillId = barcode,
                    scanTime = scanTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    scanTypeCode = scanTypeCode ?? parameters.ScanTypeCode,
                    weight,
                    length,
                    wide = width,
                    high = height,
                    transportTypeCode =
                        transportTypeCode ?? parameters.TransportTypeCode,
                    scanPda = scanPda ?? parameters.ScanPda,
                    scanType = scanType ?? parameters.ScanType,
                    weightFlag = weightFlag ?? parameters.WeightFlag
                }
            };
            return await SendScanAsync(
                    "/opa/smart/scan/uploadUnloadingArrivalData",
                    data,
                    userInfo.Token,
                    token)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// 执行出仓扫描。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="deliveryCode">员工工号。</param>
        /// <param name="scanTime">扫码时间。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>上传结果。</returns>
        private async Task<UploadResponse> DepartureScanAsync(
            string barcode,
            string deliveryCode,
            DateTime scanTime,
            CancellationToken token,
            string? scanPda = null) {
            var parameters = Volatile.Read(ref _parameters);
            var userInfo = await EnsureLoggedInAsync(token).ConfigureAwait(false);
            if (userInfo is null) {
                return CreateLoginFailureResponse(
                    "/opa/smart/scan/uploadDeliveryOutStockData");
            }

            var requestTime = DateTime.Now;
            var data = new object[] {
                new {
                    listId =
                        $"{userInfo.NetworkCode}{new DateTimeOffset(requestTime).ToUnixTimeMilliseconds()}",
                    waybillId = barcode,
                    scanTime = scanTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    deliveryCode = string.IsNullOrWhiteSpace(deliveryCode)
                        ? parameters.UserName
                        : deliveryCode,
                    scanPda = scanPda ?? parameters.ScanPda
                }
            };
            return await SendScanAsync(
                    "/opa/smart/scan/uploadDeliveryOutStockData",
                    data,
                    userInfo.Token,
                    token)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// 确保扫描上报成功，失败时交由后台队列按策略重试。
        /// </summary>
        /// <param name="response">扫描上报响应。</param>
        /// <exception cref="InvalidOperationException">扫描上报失败。</exception>
        private static void EnsureScanSucceeded(UploadResponse response) {
            if (!response.IsSuccess) {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(response.ExceptionMsg)
                        ? "极兔扫描上报失败"
                        : response.ExceptionMsg);
            }
        }

        /// <summary>
        /// 发送旧版扫描数据。
        /// </summary>
        /// <param name="method">相对接口地址。</param>
        /// <param name="data">请求数据。</param>
        /// <param name="authToken">登录令牌。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>上传结果。</returns>
        private async Task<UploadResponse> SendScanAsync(
            string method,
            object data,
            string authToken,
            CancellationToken token) {
            var parameters = Volatile.Read(ref _parameters);
            var requestUrl = CombineUrl(parameters.Url, method);
            var requestTime = DateTime.Now;
            var requestContent = JsonConvert.SerializeObject(data);
            var stopwatch = Stopwatch.StartNew();
            var responseContent = string.Empty;
            var exceptionMessage = string.Empty;
            var exceptionType = ApiExceptionType.None;
            var isSuccess = false;

            try {
                responseContent = await PostJsonAsync(
                        requestUrl,
                        requestContent,
                        parameters.TimeOut,
                        authToken,
                        token)
                    .ConfigureAwait(false);
                var result =
                    JsonConvert.DeserializeObject<JtExpressResponseResult>(
                        responseContent);
                isSuccess = result?.Succ == true;
                if (!isSuccess) {
                    exceptionMessage = result?.Msg ?? "扫描数据上传失败";
                    exceptionType = ApiExceptionType.LogicValidationFailed;
                }
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested) {
                exceptionMessage = "接口访问返回超时";
                exceptionType = ApiExceptionType.Timeout;
            }
            catch (HttpRequestException exception) {
                exceptionMessage = exception.Message;
                exceptionType = ApiExceptionType.UnreachableUrl;
            }
            catch (JsonException exception) {
                exceptionMessage = exception.Message;
                exceptionType = ApiExceptionType.ContentParsingException;
            }
            catch (Exception exception) when (exception is not OperationCanceledException) {
                exceptionMessage = exception.Message;
                exceptionType = ApiExceptionType.Other;
            }
            finally {
                stopwatch.Stop();
            }

            var response = new UploadResponse {
                ExceptionMsg = exceptionMessage,
                ApiExceptionType = exceptionType,
                ApiParameters = CreateRedactedParameterJson(parameters),
                IsSuccess = isSuccess,
                DurationSeconds = Convert.ToDecimal(stopwatch.Elapsed.TotalSeconds),
                RequestContent = requestContent,
                RequestTime = requestTime,
                RequestUrl = requestUrl,
                ResponseContent = responseContent,
                ResponseTime = DateTime.Now
            };
            if (isSuccess) {
                Logger.Info($"极兔旧版扫描上传成功:{method}");
            }
            else {
                Logger.Error($"极兔旧版扫描上传失败:{method},{exceptionMessage}");
            }

            return response;
        }

        /// <summary>
        /// 确保旧版登录令牌有效。
        /// </summary>
        /// <param name="token">取消令牌。</param>
        /// <returns>有效登录信息；登录失败时返回空。</returns>
        private async Task<JtExpressUserInfo?> EnsureLoggedInAsync(
            CancellationToken token) {
            var current = Volatile.Read(ref _userInfo);
            if (IsLoginValid(current)) {
                return current;
            }

            await _loginGate.WaitAsync(token).ConfigureAwait(false);
            try {
                current = Volatile.Read(ref _userInfo);
                if (IsLoginValid(current)) {
                    return current;
                }

                var parameters = Volatile.Read(ref _parameters);
                var loginResult = await LogInCoreAsync(
                        parameters.UserName,
                        parameters.Password,
                        parameters.AppKey,
                        parameters.AppSecret,
                        token)
                    .ConfigureAwait(false);
                if (!loginResult.Key) {
                    Logger.Error($"极兔旧版登录失败:{loginResult.Value.ExceptionMsg}");
                    return null;
                }

                Interlocked.Exchange(ref _userInfo, loginResult.Value);
                return loginResult.Value;
            }
            finally {
                _loginGate.Release();
            }
        }

        /// <summary>
        /// 执行登录请求。
        /// </summary>
        /// <param name="userName">用户名。</param>
        /// <param name="passWord">密码。</param>
        /// <param name="appKey">应用标识。</param>
        /// <param name="appSecret">应用密钥。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>登录状态和用户信息。</returns>
        private async Task<KeyValuePair<bool, JtExpressUserInfo>> LogInCoreAsync(
            string userName,
            string passWord,
            string appKey,
            string appSecret,
            CancellationToken token) {
            try {
                var parameters = Volatile.Read(ref _parameters);
                // DWS-HEX-COMPACT: 外部接口密码摘要要求使用无分隔符格式。
                var passwordHash = Convert.ToHexStringLower(
                    MD5.HashData(Encoding.UTF8.GetBytes(passWord)));
                var request = new {
                    account = userName,
                    password = passwordHash,
                    appKey,
                    appSecret
                };
                var responseContent = await PostJsonAsync(
                        CombineUrl(parameters.Url, "/opa/smartLogin"),
                        JsonConvert.SerializeObject(request),
                        parameters.TimeOut,
                        null,
                        token)
                    .ConfigureAwait(false);
                var result =
                    JsonConvert.DeserializeObject<JtExpressResponseResult>(
                        responseContent);
                if (result?.Succ != true) {
                    return LoginFailure(result?.Msg ?? "登录失败");
                }

                var userInfo =
                    JsonConvert.DeserializeObject<JtExpressUserInfo>(
                        result.Data?.ToString() ?? string.Empty);
                if (userInfo is null) {
                    return LoginFailure("登录内容解析失败");
                }

                userInfo.LoginTime = DateTime.Now;
                return new KeyValuePair<bool, JtExpressUserInfo>(true, userInfo);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested) {
                return LoginFailure("登录请求超时");
            }
            catch (Exception exception) when (exception is not OperationCanceledException) {
                return LoginFailure(exception.Message);
            }
        }

        /// <summary>
        /// 发送 JSON 请求。
        /// </summary>
        /// <param name="requestUrl">完整请求地址。</param>
        /// <param name="requestContent">请求内容。</param>
        /// <param name="timeoutMilliseconds">超时毫秒数。</param>
        /// <param name="authToken">可选登录令牌。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>响应正文。</returns>
        private async Task<string> PostJsonAsync(
            string requestUrl,
            string requestContent,
            int timeoutMilliseconds,
            string? authToken,
            CancellationToken token) {
            using var timeoutSource =
                CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutSource.CancelAfter(timeoutMilliseconds);
            using var content =
                new StringContent(requestContent, Encoding.UTF8, "application/json");
            if (!string.IsNullOrWhiteSpace(authToken)) {
                content.Headers.Add("authToken", authToken);
            }

            using var client = _httpClientFactory.CreateClient(global::JayTom.Dws.Interface.ApiHttpClientNames.ExternalApi);
            using var message = await client.PostAsync(
                    requestUrl,
                    content,
                    timeoutSource.Token)
                .ConfigureAwait(false);
            var responseContent = await message.Content
                .ReadAsStringAsync(timeoutSource.Token)
                .ConfigureAwait(false);
            if (message.StatusCode is < HttpStatusCode.OK or
                >= HttpStatusCode.MultipleChoices) {
                throw new HttpRequestException(
                    $"HTTP {(int)message.StatusCode}:{responseContent}",
                    null,
                    message.StatusCode);
            }

            return responseContent;
        }

        /// <summary>
        /// 从三段码结果解析员工工号。
        /// </summary>
        /// <param name="uploadResponse">三段码响应。</param>
        /// <returns>员工工号。</returns>
        private static async Task<string> ResolveDeliveryCodeAsync(
            UploadResponse uploadResponse) {
            try {
                var result =
                    JsonConvert.DeserializeObject<JtExpressResponseResult>(
                        uploadResponse.ResponseContent);
                var infos =
                    JsonConvert.DeserializeObject<List<SegmentCodeInfo>>(
                        result?.Data?.ToString() ?? string.Empty);
                var thirdCode = infos?.FirstOrDefault()?.ThirdlyDispatchCode;
                if (string.IsNullOrWhiteSpace(thirdCode)) {
                    return string.Empty;
                }

                var mappings = await DeliveryCodeCache.Value.ConfigureAwait(false);
                return mappings.TryGetValue(thirdCode, out var deliveryCode)
                    ? deliveryCode
                    : string.Empty;
            }
            catch (JsonException exception) {
                Logger.Warn(exception, "解析极兔三段码员工工号失败");
                return string.Empty;
            }
        }

        /// <summary>
        /// 在后台读取三段码与员工工号映射。
        /// </summary>
        /// <returns>只读映射。</returns>
        private static Task<FrozenDictionary<string, string>>
            LoadDeliveryCodesAsync() {
            return Task.Run<FrozenDictionary<string, string>>(async () => {
                try {
                    var directory = Path.Combine(
                        AppContext.BaseDirectory,
                        "ApiSettingJson",
                        "JtThreeSegmentCodeRout");
                    if (!Directory.Exists(directory)) {
                        return FrozenDictionary<string, string>.Empty;
                    }

                    var excelFile = Directory
                        .EnumerateFiles(directory, "*.xlsx")
                        .Select(path => new FileInfo(path))
                        .OrderByDescending(file => file.LastWriteTime)
                        .FirstOrDefault();
                    if (excelFile is null) {
                        return FrozenDictionary<string, string>.Empty;
                    }

                    var excel = new NpoiExport();
                    var models = await excel.ReadExcel<ExcelDeliveryCode>(
                            excelFile.FullName,
                            _ => Task.CompletedTask,
                            exception => {
                                Logger.Error(exception, "读取极兔三段码路由表失败");
                                return Task.CompletedTask;
                            })
                        .ConfigureAwait(false);
                    return models
                        .Where(item =>
                            !string.IsNullOrWhiteSpace(item.ThirdlyDispatchCode))
                        .GroupBy(
                            item => item.ThirdlyDispatchCode,
                            StringComparer.OrdinalIgnoreCase)
                        .ToFrozenDictionary(
                            group => group.Key,
                            group => group.First().DeliveryCode,
                            StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception exception) {
                    Logger.Error(exception, "加载极兔三段码路由表失败");
                    return FrozenDictionary<string, string>.Empty;
                }
            });
        }

        /// <summary>
        /// 判断登录信息是否仍然有效。
        /// </summary>
        /// <param name="userInfo">登录信息。</param>
        /// <returns>是否有效。</returns>
        private static bool IsLoginValid(JtExpressUserInfo userInfo) {
            return userInfo.LoginTime is { } loginTime &&
                   DateTime.Now.Subtract(loginTime) < TimeSpan.FromHours(20) &&
                   !string.IsNullOrWhiteSpace(userInfo.Token);
        }

        /// <summary>
        /// 合并基础地址与相对地址。
        /// </summary>
        /// <param name="baseUrl">基础地址。</param>
        /// <param name="relativeUrl">相对地址。</param>
        /// <returns>完整地址。</returns>
        private static string CombineUrl(string baseUrl, string relativeUrl) {
            return $"{baseUrl.TrimEnd('/')}/{relativeUrl.TrimStart('/')}";
        }

        /// <summary>
        /// 创建已脱敏的参数日志。
        /// </summary>
        /// <param name="parameters">接口参数。</param>
        /// <returns>脱敏后的 JSON。</returns>
        private static string CreateRedactedParameterJson(
            ApiParameter parameters) {
            return JsonConvert.SerializeObject(new {
                parameters.Url,
                parameters.UserName,
                parameters.AppKey,
                parameters.TimeOut,
                parameters.ScanTypeCode,
                parameters.TransportTypeCode,
                parameters.ScanPda,
                parameters.ScanType,
                parameters.WeightFlag,
                parameters.SegmentCodeUrl,
                parameters.SegmentCodeTimeOut,
                parameters.BusinessType,
                parameters.InterceptorEnabled
            });
        }

        /// <summary>
        /// 创建登录失败响应。
        /// </summary>
        /// <param name="method">接口相对地址。</param>
        /// <returns>失败响应。</returns>
        private UploadResponse CreateLoginFailureResponse(string method) {
            var parameters = Volatile.Read(ref _parameters);
            var now = DateTime.Now;
            return new UploadResponse {
                IsSuccess = false,
                ApiExceptionType = ApiExceptionType.LogicValidationFailed,
                ExceptionMsg = "极兔登录失败",
                ApiParameters = CreateRedactedParameterJson(parameters),
                RequestUrl = CombineUrl(parameters.Url, method),
                RequestTime = now,
                ResponseTime = now
            };
        }

        /// <summary>
        /// 创建登录失败结果。
        /// </summary>
        /// <param name="message">失败原因。</param>
        /// <returns>登录失败结果。</returns>
        private static KeyValuePair<bool, JtExpressUserInfo> LoginFailure(
            string message) {
            return new KeyValuePair<bool, JtExpressUserInfo>(
                false,
                new JtExpressUserInfo {
                    ExceptionMsg = message
                });
        }

        /// <summary>
        /// 极兔旧版业务类型。
        /// </summary>
        public enum BusinessType {
            /// <summary>
            /// 到件扫描。
            /// </summary>
            ArrivalScan = 0,

            /// <summary>
            /// 出仓扫描。
            /// </summary>
            DepartureScan = 1,

            /// <summary>
            /// 到件后再执行出仓扫描。
            /// </summary>
            ArrivalScanAndDepartureScan = 2
        }

        /// <summary>
        /// 极兔旧版登录信息。
        /// </summary>
        public sealed class JtExpressUserInfo {
            /// <summary>
            /// 登录人的网点编码。
            /// </summary>
            [JsonProperty("networkId")]
            public string NetworkIdentifier { get; set; } = string.Empty;

            /// <summary>
            /// 网点代码。
            /// </summary>
            public string NetworkCode { get; set; } = string.Empty;

            /// <summary>
            /// 登录人的网点名称。
            /// </summary>
            public string NetworkName { get; set; } = string.Empty;

            /// <summary>
            /// 用户名。
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// 登录时间。
            /// </summary>
            public DateTime? LoginTime { get; set; }

            /// <summary>
            /// 登录令牌。
            /// </summary>
            public string Token { get; set; } = string.Empty;

            /// <summary>
            /// 错误信息。
            /// </summary>
            public string ExceptionMsg { get; set; } = string.Empty;
        }

        /// <summary>
        /// 极兔旧版标准响应。
        /// </summary>
        public sealed class JtExpressResponseResult {
            /// <summary>
            /// 响应代码。
            /// </summary>
            public int Code { get; set; }

            /// <summary>
            /// 响应消息。
            /// </summary>
            public string Msg { get; set; } = string.Empty;

            /// <summary>
            /// 响应数据。
            /// </summary>
            public object? Data { get; set; }

            /// <summary>
            /// 是否成功。
            /// </summary>
            public bool Succ { get; set; }

            /// <summary>
            /// 是否失败。
            /// </summary>
            public bool Fail { get; set; }
        }

        /// <summary>
        /// 三段码信息。
        /// </summary>
        public sealed class SegmentCodeInfo {
            /// <summary>
            /// 运单号。
            /// </summary>
            public string? WaybillNo { get; set; }

            /// <summary>
            /// 末端派送码。
            /// </summary>
            public string? TerminalDispatchCode { get; set; }

            /// <summary>
            /// 一段码。
            /// </summary>
            public string? FirstDispatchCode { get; set; }

            /// <summary>
            /// 二段码。
            /// </summary>
            public string? SecondDispatchCode { get; set; }

            /// <summary>
            /// 三段码。
            /// </summary>
            public string? ThirdlyDispatchCode { get; set; }

            /// <summary>
            /// 客户编码。
            /// </summary>
            public string? CustomerCode { get; set; }

            /// <summary>
            /// 是否为拦截件。
            /// </summary>
            public int? Interceptor { get; set; }
        }

        /// <summary>
        /// 极兔旧版接口参数。
        /// </summary>
        public sealed class ApiParameter {
            /// <summary>
            /// OPA 基础地址。
            /// </summary>
            public string Url { get; set; } = "https://opa.jtexpress.com.cn";

            /// <summary>
            /// 账号。
            /// </summary>
            public string UserName { get; set; } = string.Empty;

            /// <summary>
            /// 密码。
            /// </summary>
            public string Password { get; set; } = string.Empty;

            /// <summary>
            /// 应用标识。
            /// </summary>
            public string AppKey { get; set; } = "default";

            /// <summary>
            /// 应用密钥。
            /// </summary>
            public string AppSecret { get; set; } = "default";

            /// <summary>
            /// 扫描接口超时毫秒数。
            /// </summary>
            public int TimeOut { get; set; } = 1000;

            /// <summary>
            /// 条码类型。
            /// </summary>
            public string ScanTypeCode { get; set; } = string.Empty;

            /// <summary>
            /// 运输方式标识。
            /// </summary>
            public string TransportTypeCode { get; set; } = string.Empty;

            /// <summary>
            /// 设备编号。
            /// </summary>
            public string ScanPda { get; set; } = string.Empty;

            /// <summary>
            /// 扫描类型。
            /// </summary>
            public int ScanType { get; set; }

            /// <summary>
            /// 重量标识。
            /// </summary>
            public string WeightFlag { get; set; } = string.Empty;

            /// <summary>
            /// 三段码基础地址。
            /// </summary>
            public string SegmentCodeUrl { get; set; } =
                "https://opa.jtexpress.com.cn";

            /// <summary>
            /// 三段码接口超时毫秒数。
            /// </summary>
            public int SegmentCodeTimeOut { get; set; } = 1000;

            /// <summary>
            /// 业务类型。
            /// </summary>
            public BusinessType BusinessType { get; set; }

            /// <summary>
            /// 是否启用拦截件判断。
            /// </summary>
            public bool InterceptorEnabled { get; set; }

            /// <summary>
            /// 创建独立参数快照。
            /// </summary>
            /// <returns>参数副本。</returns>
            public ApiParameter Clone() {
                return new ApiParameter {
                    Url = Url,
                    UserName = UserName,
                    Password = Password,
                    AppKey = AppKey,
                    AppSecret = AppSecret,
                    TimeOut = TimeOut,
                    ScanTypeCode = ScanTypeCode,
                    TransportTypeCode = TransportTypeCode,
                    ScanPda = ScanPda,
                    ScanType = ScanType,
                    WeightFlag = WeightFlag,
                    SegmentCodeUrl = SegmentCodeUrl,
                    SegmentCodeTimeOut = SegmentCodeTimeOut,
                    BusinessType = BusinessType,
                    InterceptorEnabled = InterceptorEnabled
                };
            }
        }

        /// <summary>
        /// Excel 三段码与员工工号映射。
        /// </summary>
        public sealed class ExcelDeliveryCode {
            /// <summary>
            /// 员工工号。
            /// </summary>
            [DisplayName("员工工号"), MemberNotNull, ExcelInfo(Width = 4000)]
            public string DeliveryCode { get; set; } = string.Empty;

            /// <summary>
            /// 三段码。
            /// </summary>
            [DisplayName("三段码"), MemberNotNull, ExcelInfo(Width = 4000)]
            public string ThirdlyDispatchCode { get; set; } = string.Empty;
        }
    }
}
