using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NLog;

namespace JayTom.Dws.Interface.Jtexpress {

    /// <summary>
    /// 极兔极昼小件分拣设备数据上传接口。
    /// </summary>
    public sealed class JtPolarDayApi : FixedPointDataUploaderBase {
        /// <summary>
        /// 扫码事件类型。
        /// </summary>
        private const string ScanEventType = "scanInfo";

        /// <summary>
        /// 落格事件类型。
        /// </summary>
        private const string PackageEventType = "packageInfo";

        /// <summary>
        /// 设备信息上传相对地址。
        /// </summary>
        private const string DeviceInfoPath = "/polarDay/upload/deviceInfo";

        /// <summary>
        /// 目标格口查询相对地址。
        /// </summary>
        private const string QueryChutePath = "/polarDay/query/queryChute";

        /// <summary>
        /// JSON 序列化配置。
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// 接口日志记录器。
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// HTTP 客户端工厂。
        /// </summary>
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// 当前参数快照。
        /// </summary>
        private ApiParameter _parameters = new();

        /// <summary>
        /// 初始化极昼接口。
        /// </summary>
        /// <param name="httpClientFactory">HTTP 客户端工厂。</param>
        public JtPolarDayApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory ??
                                 throw new ArgumentNullException(
                                     nameof(httpClientFactory));
        }

        /// <summary>
        /// 查询目标格口。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="volume">体积。</param>
        /// <param name="imageInfo">扫码相机信息。</param>
        /// <param name="panoramaImageInfos">全景相机信息。</param>
        /// <param name="other">极昼扩展上下文。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>目标格口查询响应。</returns>
        protected override Task<UploadResponse> UploadFixedPointDataAsync(
            string barcode,
            decimal weight,
            decimal length,
            decimal width,
            decimal height,
            decimal volume,
            UploadImageInfo? imageInfo,
            List<UploadImageInfo>? panoramaImageInfos,
            object? other,
            CancellationToken token) {
            return QueryChuteAsync(barcode, weight, token);
        }

        /// <summary>
        /// 查询目标格口。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="scanTime">扫码时间。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="volume">体积。</param>
        /// <param name="imageInfo">扫码相机信息。</param>
        /// <param name="panoramaImageInfos">全景相机信息。</param>
        /// <param name="other">极昼扩展上下文。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>目标格口查询响应。</returns>
        protected override Task<UploadResponse> UploadFixedPointDataAsync(
            string barcode,
            decimal weight,
            DateTime scanTime,
            decimal length,
            decimal width,
            decimal height,
            decimal volume,
            UploadImageInfo? imageInfo,
            List<UploadImageInfo>? panoramaImageInfos,
            object? other,
            CancellationToken token) {
            return QueryChuteAsync(barcode, weight, token);
        }

        /// <summary>
        /// 设置极昼接口参数。
        /// </summary>
        /// <typeparam name="T">参数类型。</typeparam>
        /// <param name="parameters">接口参数。</param>
        /// <returns>参数是否设置成功及失败原因。</returns>
        public override Task<KeyValuePair<bool, string>> SetParameters<T>(
            T parameters) {
            if (parameters is not ApiParameter parameter) {
                return Task.FromResult(
                    new KeyValuePair<bool, string>(false, "参数类型不匹配"));
            }

            var validationMessage = ValidateParameters(parameter);
            if (!string.IsNullOrEmpty(validationMessage)) {
                return Task.FromResult(
                    new KeyValuePair<bool, string>(false, validationMessage));
            }

            Interlocked.Exchange(ref _parameters, parameter.Clone());
            return Task.FromResult(
                new KeyValuePair<bool, string>(true, string.Empty));
        }

        /// <summary>
        /// 上传落格事件。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="scanTime">扫码时间。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="volume">体积。</param>
        /// <param name="imageInfo">扫码相机信息。</param>
        /// <param name="panoramaImageInfos">全景相机信息。</param>
        /// <param name="other">包含实际格口和落格时间的极昼上下文。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>后台上传任务。</returns>
        protected override async Task UploadFixedPointDataInBackgroundAsync(
            string barcode,
            decimal weight,
            DateTime scanTime,
            decimal length,
            decimal width,
            decimal height,
            decimal volume,
            UploadImageInfo? imageInfo,
            List<UploadImageInfo>? panoramaImageInfos,
            object? other,
            CancellationToken token) {
            var context = other as UploadContext ?? new UploadContext();
            var scanRequest = CreateRequest(
                ScanEventType,
                barcode,
                weight,
                scanTime,
                imageInfo,
                context);
            var scanResponse = await UploadDeviceInfoAsync(scanRequest, token)
                .ConfigureAwait(false);
            if (scanResponse.IsSuccess) {
                Logger.Info($"极昼小件扫码上报成功:{barcode}");
            }
            else {
                Logger.Error(
                    $"极昼小件扫码上报失败:{barcode},{scanResponse.ExceptionMsg}");
            }

            if (string.IsNullOrWhiteSpace(context.CarNum) ||
                string.IsNullOrWhiteSpace(context.GridNo) ||
                string.IsNullOrWhiteSpace(context.GridCode)) {
                Logger.Error("极昼小件落格上报缺少小车号、格口或格口分类码");
                return;
            }

            if (context.CyclesNum < 0 ||
                context.OverAreaNum < 0 ||
                context.FallArea is <= 0 ||
                !IsValidChuteModel(context.ChuteModel)) {
                Logger.Error("极昼小件落格上报的圈数、超区数、落格区域或落格模式无效");
                return;
            }

            var request = CreateRequest(
                PackageEventType,
                barcode,
                weight,
                scanTime,
                imageInfo,
                context);
            var response = await UploadDeviceInfoAsync(request, token)
                .ConfigureAwait(false);
            if (response.IsSuccess) {
                Logger.Info($"极昼落格上报成功:{barcode},{context.GridNo}");
            }
            else {
                Logger.Error(
                    $"极昼落格上报失败:{barcode},{context.GridNo},{response.ExceptionMsg}");
            }
        }

        /// <summary>
        /// 极昼 IDataUploader 接入不使用集包上报。
        /// </summary>
        /// <param name="packageExit">格口。</param>
        /// <param name="aggregatePackageCode">集包码。</param>
        /// <param name="packagingTime">集包时间。</param>
        /// <param name="packageItems">包裹列表。</param>
        /// <param name="other">扩展信息。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>已完成任务。</returns>
        public override Task PackageAggregation(
            string packageExit,
            string aggregatePackageCode,
            DateTime packagingTime,
            List<string> packageItems,
            object? other = null,
            CancellationToken token = default) {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 构造扫码或落格请求。
        /// </summary>
        /// <param name="eventType">事件类型。</param>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="scanTime">扫码时间。</param>
        /// <param name="imageInfo">相机信息。</param>
        /// <param name="context">极昼扩展上下文。</param>
        /// <returns>设备信息请求。</returns>
        private DeviceInfoRequest CreateRequest(
            string eventType,
            string barcode,
            decimal weight,
            DateTime scanTime,
            UploadImageInfo? imageInfo,
            UploadContext context) {
            var parameters = Volatile.Read(ref _parameters);
            var isPackageEvent = eventType == PackageEventType;
            return new DeviceInfoRequest {
                EquipmentCode = parameters.EquipmentCode,
                EventType = eventType,
                WaybillNo = NormalizeBarcode(barcode),
                OperateType = parameters.OperateType,
                Operator = parameters.Operator,
                IsOfflineData = context.IsOfflineData ? 1 : 0,
                EquipmentLayer = parameters.EquipmentLayer,
                AreaNum = parameters.AreaNum,
                MaxCircleNum = parameters.MaxCircleNum,
                SupplyDeskCode = parameters.SupplyDeskCode,
                SupplyDeskSerialNo = parameters.SupplyDeskSerialNo,
                SupplyDeskMethod = parameters.SupplyDeskMethod,
                SupplyDeskArea = parameters.SupplyDeskArea,
                LayerNum = parameters.LayerNum,
                ScanTime = scanTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                ScanCameraName = EmptyToNull(
                    imageInfo?.CameraCustomName ??
                    imageInfo?.CameraName),
                ScanCameraSn = EmptyToNull(imageInfo?.CameraSerialNumber),
                Weight = RoundMeasurement(weight),
                WeightSource = parameters.WeightSource,
                SortingPlanCode = parameters.SortingPlanCode,
                HalfPath = EmptyToNull(context.HalfPath),
                LandOnCarTime = isPackageEvent
                    ? (context.LandOnCarTime ?? scanTime)
                    .ToString("yyyy-MM-dd HH:mm:ss.fff")
                    : null,
                CarNum = isPackageEvent
                    ? EmptyToNull(context.CarNum)
                    : null,
                PackageNo = isPackageEvent
                    ? EmptyToNull(context.PackageNo)
                    : null,
                BagUserCode = isPackageEvent
                    ? EmptyToNull(context.BagUserCode)
                    : null,
                BindBagTime = isPackageEvent && context.BindBagTime.HasValue
                    ? context.BindBagTime.Value
                    .ToString("yyyy-MM-dd HH:mm:ss.fff")
                    : null,
                Rfid = isPackageEvent
                    ? EmptyToNull(context.Rfid)
                    : null,
                ChuteModel = isPackageEvent
                    ? (string.IsNullOrWhiteSpace(context.ChuteModel)
                        ? parameters.ChuteModel
                        : context.ChuteModel)
                    : null,
                CyclesNum = isPackageEvent
                    ? RoundMeasurement(context.CyclesNum)
                    : null,
                GridNo = isPackageEvent
                    ? EmptyToNull(context.GridNo)
                    : null,
                GridCode = isPackageEvent
                    ? EmptyToNull(context.GridCode)
                    : null,
                FallTime = isPackageEvent
                    ? (context.FallTime ?? DateTime.Now)
                    .ToString("yyyy-MM-dd HH:mm:ss.fff")
                    : null,
                FallArea = isPackageEvent
                    ? context.FallArea ?? parameters.FallArea
                    : null,
                OverAreaNum = isPackageEvent
                    ? context.OverAreaNum
                    : null,
                OverAreaReason = isPackageEvent &&
                                 context.OverAreaNum > 0
                    ? EmptyToNull(context.OverAreaReason)
                    : null
            };
        }

        /// <summary>
        /// 上传单条设备信息；协议请求体仍使用列表格式。
        /// </summary>
        /// <param name="request">设备信息。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>上传响应。</returns>
        private async Task<UploadResponse> UploadDeviceInfoAsync(
            DeviceInfoRequest request,
            CancellationToken token) {
            var parameters = Volatile.Read(ref _parameters);
            return await ExecuteRequestAsync(
                    DeviceInfoPath,
                    new[] { request },
                    parameters,
                    parameters.TimeoutMilliseconds,
                    EvaluateDeviceInfoResponse,
                    token)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// 查询运单对应的目标格口。
        /// </summary>
        /// <param name="barcode">运单号。</param>
        /// <param name="weight">重量，单位千克。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>包含目标格口原始响应的上传响应。</returns>
        private async Task<UploadResponse> QueryChuteAsync(
            string barcode,
            decimal weight,
            CancellationToken token) {
            var parameters = Volatile.Read(ref _parameters);
            var request = new QueryChuteRequest {
                WaybillNo = NormalizeBarcode(barcode),
                SortingPlanCode = parameters.SortingPlanCode,
                EquipmentCode = parameters.EquipmentCode,
                MainLineCode = EmptyToNull(parameters.MainLineCode),
                Weight = RoundMeasurement(weight)
            };
            return await ExecuteRequestAsync(
                    QueryChutePath,
                    request,
                    parameters,
                    parameters.QueryTimeoutMilliseconds,
                    EvaluateQueryChuteResponse,
                    token)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// 执行带重试、计时和标准日志信息的极昼请求。
        /// </summary>
        /// <typeparam name="TRequest">请求类型。</typeparam>
        /// <param name="relativePath">接口相对地址。</param>
        /// <param name="request">请求对象。</param>
        /// <param name="parameters">不可变参数快照。</param>
        /// <param name="timeoutMilliseconds">本次请求超时毫秒数。</param>
        /// <param name="evaluateResponse">业务响应判定方法。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>标准上传响应。</returns>
        private async Task<UploadResponse> ExecuteRequestAsync<TRequest>(
            string relativePath,
            TRequest request,
            ApiParameter parameters,
            int timeoutMilliseconds,
            Func<string, (bool IsSuccess, string ExceptionMessage,
                ApiExceptionType ExceptionType)> evaluateResponse,
            CancellationToken token) {
            var requestUrl = CombineUrl(parameters.BaseUrl, relativePath);
            var requestTime = DateTime.Now;
            var requestBytes = JsonSerializer.SerializeToUtf8Bytes(
                request,
                JsonOptions);
            var requestContent = Encoding.UTF8.GetString(requestBytes);
            var stopwatch = Stopwatch.StartNew();
            var responseContent = string.Empty;
            var exceptionMessage = string.Empty;
            var exceptionType = ApiExceptionType.None;
            var isSuccess = false;

            for (var attempt = 1;
                 attempt <= parameters.RetryCount && !isSuccess;
                 attempt++) {
                var shouldRetry = false;
                try {
                    responseContent = await SendAsync(
                            requestUrl,
                            requestBytes,
                            parameters,
                            timeoutMilliseconds,
                            token)
                        .ConfigureAwait(false);
                    var evaluation = evaluateResponse(responseContent);
                    isSuccess = evaluation.IsSuccess;
                    exceptionMessage = evaluation.ExceptionMessage;
                    exceptionType = evaluation.ExceptionType;
                }
                catch (OperationCanceledException)
                    when (!token.IsCancellationRequested) {
                    exceptionMessage = "极昼接口访问超时";
                    exceptionType = ApiExceptionType.Timeout;
                    shouldRetry = true;
                }
                catch (HttpRequestException exception) {
                    exceptionMessage = exception.Message;
                    exceptionType = ApiExceptionType.UnreachableUrl;
                    shouldRetry = true;
                }
                catch (JsonException exception) {
                    exceptionMessage = exception.Message;
                    exceptionType =
                        ApiExceptionType.ContentParsingException;
                }
                catch (Exception exception)
                    when (exception is not OperationCanceledException) {
                    exceptionMessage = exception.Message;
                    exceptionType = ApiExceptionType.Other;
                }

                if (!isSuccess &&
                    shouldRetry &&
                    attempt < parameters.RetryCount) {
                    await Task.Delay(
                            parameters.RetryIntervalMilliseconds,
                            token)
                        .ConfigureAwait(false);
                }
                else if (!isSuccess) {
                    break;
                }
            }

            stopwatch.Stop();
            return new UploadResponse {
                RequestContent = requestContent,
                ResponseContent = responseContent,
                IsSuccess = isSuccess,
                RequestTime = requestTime,
                ResponseTime = DateTime.Now,
                Duration = stopwatch.Elapsed.TotalSeconds,
                ApiParameters = CreateRedactedParameterJson(parameters),
                RequestUrl = requestUrl,
                ExceptionMsg = exceptionMessage,
                ApiExceptionType = exceptionType
            };
        }

        /// <summary>
        /// 判定设备报文上传响应。
        /// </summary>
        /// <param name="responseContent">响应正文。</param>
        /// <returns>业务判定结果。</returns>
        private static (bool IsSuccess, string ExceptionMessage,
            ApiExceptionType ExceptionType) EvaluateDeviceInfoResponse(
            string responseContent) {
            var response = JsonSerializer.Deserialize<PolarDayResponse>(
                responseContent,
                JsonOptions);
            if (response is null) {
                return (
                    false,
                    "极昼设备报文响应为空",
                    ApiExceptionType.ContentParsingException);
            }

            return response.Code == 1
                ? (true, string.Empty, ApiExceptionType.None)
                : (
                    false,
                    response.Msg,
                    ApiExceptionType.LogicValidationFailed);
        }

        /// <summary>
        /// 判定目标格口查询响应。
        /// </summary>
        /// <param name="responseContent">响应正文。</param>
        /// <returns>接口调用判定结果；目标格口由响应内容分拣规则解析。</returns>
        private static (bool IsSuccess, string ExceptionMessage,
            ApiExceptionType ExceptionType) EvaluateQueryChuteResponse(
            string responseContent) {
            var response = JsonSerializer.Deserialize<PolarDayResponse>(
                responseContent,
                JsonOptions);
            if (response is null) {
                return (
                    false,
                    "极昼格口查询响应为空",
                    ApiExceptionType.ContentParsingException);
            }

            if (response.Code != 1) {
                return (
                    false,
                    response.Msg,
                    ApiExceptionType.LogicValidationFailed);
            }

            return (true, string.Empty, ApiExceptionType.None);
        }

        /// <summary>
        /// 发送带极昼签名的请求。
        /// </summary>
        /// <param name="requestUrl">完整请求地址。</param>
        /// <param name="requestBytes">请求正文。</param>
        /// <param name="parameters">参数快照。</param>
        /// <param name="timeoutMilliseconds">请求超时毫秒数。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>响应正文。</returns>
        private async Task<string> SendAsync(
            string requestUrl,
            byte[] requestBytes,
            ApiParameter parameters,
            int timeoutMilliseconds,
            CancellationToken token) {
            var timestamp =
                DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            var signature = CreateSignature(
                parameters.AppSecret,
                timestamp,
                requestBytes);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                requestUrl);
            request.Headers.TryAddWithoutValidation(
                "timestamp",
                timestamp);
            request.Headers.TryAddWithoutValidation(
                "token",
                signature);
            request.Headers.TryAddWithoutValidation(
                "appKey",
                parameters.AppKey);
            request.Headers.TryAddWithoutValidation(
                "X-Trace-Id",
                Guid.NewGuid().ToString());
            request.Content = new ByteArrayContent(requestBytes);
            request.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(
                    "application/json");

            using var timeoutSource =
                CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutSource.CancelAfter(timeoutMilliseconds);
            using var client =
                _httpClientFactory.CreateClient("INSURANCE");
            using var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutSource.Token)
                .ConfigureAwait(false);
            var responseContent = await response.Content
                .ReadAsStringAsync(timeoutSource.Token)
                .ConfigureAwait(false);
            if (response.StatusCode is < HttpStatusCode.OK or
                >= HttpStatusCode.MultipleChoices) {
                throw new HttpRequestException(
                    $"HTTP {(int)response.StatusCode}:{responseContent}",
                    null,
                    response.StatusCode);
            }

            return responseContent;
        }

        /// <summary>
        /// 计算极昼请求签名。
        /// </summary>
        /// <param name="appSecret">应用密钥。</param>
        /// <param name="timestamp">Unix 毫秒时间戳。</param>
        /// <param name="bodyBytes">请求正文。</param>
        /// <returns>Base64 签名。</returns>
        private static string CreateSignature(
            string appSecret,
            string timestamp,
            byte[] bodyBytes) {
            var body = Encoding.UTF8.GetString(bodyBytes);
            var source = Encoding.UTF8.GetBytes(
                string.Concat(appSecret, timestamp, body));
            var md5Hex = Convert.ToHexString(MD5.HashData(source))
                .ToLowerInvariant();
            return Convert.ToBase64String(
                Encoding.UTF8.GetBytes(md5Hex));
        }

        /// <summary>
        /// 验证极昼参数。
        /// </summary>
        /// <param name="parameters">接口参数。</param>
        /// <returns>失败原因；成功时为空。</returns>
        private static string ValidateParameters(ApiParameter parameters) {
            if (string.IsNullOrWhiteSpace(parameters.BaseUrl)) {
                return "极昼接口地址不能为空";
            }

            if (string.IsNullOrWhiteSpace(parameters.AppKey) ||
                string.IsNullOrWhiteSpace(parameters.AppSecret)) {
                return "极昼 AppKey 和 AppSecret 不能为空";
            }

            if (string.IsNullOrWhiteSpace(parameters.EquipmentCode) ||
                string.IsNullOrWhiteSpace(parameters.SortingPlanCode) ||
                string.IsNullOrWhiteSpace(parameters.Operator)) {
                return "极昼设备编号、分拣计划和操作员不能为空";
            }

            if (parameters.QueryTimeoutMilliseconds <= 0 ||
                parameters.TimeoutMilliseconds <= 0 ||
                parameters.RetryCount <= 0 ||
                parameters.RetryIntervalMilliseconds < 0) {
                return "极昼超时和重试参数无效";
            }

            if (parameters.OperateType is < 1 or > 3) {
                return "极昼操作类型只能为 1、2 或 3";
            }

            if (parameters.EquipmentLayer <= 0 ||
                parameters.AreaNum <= 0 ||
                parameters.MaxCircleNum <= 0 ||
                parameters.LayerNum <= 0) {
                return "极昼设备层数、供件区数、最大圈数和供件台层数必须大于零";
            }

            if (string.IsNullOrWhiteSpace(parameters.SupplyDeskCode) ||
                string.IsNullOrWhiteSpace(parameters.SupplyDeskSerialNo) ||
                string.IsNullOrWhiteSpace(parameters.SupplyDeskArea)) {
                return "极昼供件台编号、供件台序号和供件区不能为空";
            }

            if (!int.TryParse(
                    parameters.SupplyDeskSerialNo,
                    out var supplyDeskSerialNo) ||
                supplyDeskSerialNo <= 0) {
                return "极昼供件台序号必须是从 1 开始的正整数";
            }

            if (parameters.SupplyDeskMethod is not "1" and not "2" and
                not "3" and not "4" and not "5") {
                return "极昼供件方式只能为 1、2、3、4 或 5";
            }

            if (parameters.ChuteModel is not "1" and not "2" and
                not "3" and not "4") {
                return "极昼落格模式只能为 1、2、3 或 4";
            }

            if (parameters.FallArea <= 0) {
                return "极昼落格供件区编号必须大于零";
            }

            if (parameters.WeightSource is not "0" and not "1") {
                return "极昼重量来源只能为 0 或 1";
            }

            return string.Empty;
        }

        /// <summary>
        /// 检查单票落格模式覆盖值。
        /// </summary>
        /// <param name="chuteModel">单票落格模式；空值表示使用接口配置。</param>
        /// <returns>是否可以用于请求。</returns>
        private static bool IsValidChuteModel(string chuteModel) {
            return string.IsNullOrWhiteSpace(chuteModel) ||
                   chuteModel is "1" or "2" or "3" or "4";
        }

        /// <summary>
        /// 标准化协议条码。
        /// </summary>
        /// <param name="barcode">原始条码。</param>
        /// <returns>协议条码。</returns>
        private static string NormalizeBarcode(string barcode) {
            return string.IsNullOrWhiteSpace(barcode) ||
                   string.Equals(
                       barcode,
                       "noread",
                       StringComparison.OrdinalIgnoreCase)
                ? "NoRead"
                : barcode;
        }

        /// <summary>
        /// 将测量值保留两位小数。
        /// </summary>
        /// <param name="value">原始测量值。</param>
        /// <returns>定点测量值。</returns>
        private static decimal RoundMeasurement(decimal value) {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 将空字符串转换为空值。
        /// </summary>
        /// <param name="value">原始字符串。</param>
        /// <returns>非空字符串或空值。</returns>
        private static string? EmptyToNull(string? value) {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>
        /// 合并基础地址和相对地址。
        /// </summary>
        /// <param name="baseUrl">基础地址。</param>
        /// <param name="relativeUrl">相对地址。</param>
        /// <returns>完整地址。</returns>
        private static string CombineUrl(
            string baseUrl,
            string relativeUrl) {
            return $"{baseUrl.TrimEnd('/')}/{relativeUrl.TrimStart('/')}";
        }

        /// <summary>
        /// 创建脱敏参数日志。
        /// </summary>
        /// <param name="parameters">接口参数。</param>
        /// <returns>脱敏 JSON。</returns>
        private static string CreateRedactedParameterJson(
            ApiParameter parameters) {
            return JsonSerializer.Serialize(
                new {
                    parameters.BaseUrl,
                    parameters.AppKey,
                    parameters.EquipmentCode,
                    parameters.SortingPlanCode,
                    parameters.OperateType,
                    parameters.Operator,
                    parameters.MainLineCode,
                    parameters.EquipmentLayer,
                    parameters.AreaNum,
                    parameters.MaxCircleNum,
                    parameters.SupplyDeskCode,
                    parameters.SupplyDeskSerialNo,
                    parameters.SupplyDeskMethod,
                    parameters.SupplyDeskArea,
                    parameters.LayerNum,
                    parameters.ChuteModel,
                    parameters.FallArea,
                    parameters.WeightSource,
                    parameters.QueryTimeoutMilliseconds,
                    parameters.TimeoutMilliseconds,
                    parameters.RetryCount,
                    parameters.RetryIntervalMilliseconds
                },
                JsonOptions);
        }

        /// <summary>
        /// 极昼接口参数。
        /// </summary>
        public sealed class ApiParameter {
            /// <summary>
            /// 极昼服务基础地址。
            /// </summary>
            public string BaseUrl { get; set; } =
                "https://uat-sdsonline.jtexpress.com.cn/sdsOnlineApi";

            /// <summary>
            /// 应用标识。
            /// </summary>
            public string AppKey { get; set; } = string.Empty;

            /// <summary>
            /// 应用密钥。
            /// </summary>
            public string AppSecret { get; set; } = string.Empty;

            /// <summary>
            /// 设备编号。
            /// </summary>
            public string EquipmentCode { get; set; } = string.Empty;

            /// <summary>
            /// 分拣计划编码。
            /// </summary>
            public string SortingPlanCode { get; set; } = string.Empty;

            /// <summary>
            /// 操作类型，1 出港、2 进港、3 进出港。
            /// </summary>
            public int OperateType { get; set; } = 1;

            /// <summary>
            /// 操作员 JMS 账号。
            /// </summary>
            public string Operator { get; set; } = string.Empty;

            /// <summary>
            /// 格口查询使用的可选主线编码。
            /// </summary>
            public string MainLineCode { get; set; } = string.Empty;

            /// <summary>
            /// 设备实际层数。
            /// </summary>
            public int EquipmentLayer { get; set; } = 1;

            /// <summary>
            /// 设备实际供件区数量。
            /// </summary>
            public int AreaNum { get; set; } = 1;

            /// <summary>
            /// 设备允许的最大循环圈数。
            /// </summary>
            public int MaxCircleNum { get; set; } = 1;

            /// <summary>
            /// 供件台编号；无供件台时填写供件区编号。
            /// </summary>
            public string SupplyDeskCode { get; set; } = string.Empty;

            /// <summary>
            /// 供件台在当前供件区内的连续序号。
            /// </summary>
            public string SupplyDeskSerialNo { get; set; } = "1";

            /// <summary>
            /// 供件方式，1 供包台、2 补码台、3 自动供包、4 人工供包、5 快手供件。
            /// </summary>
            public string SupplyDeskMethod { get; set; } = "1";

            /// <summary>
            /// 供件台所属供件区。
            /// </summary>
            public string SupplyDeskArea { get; set; } = string.Empty;

            /// <summary>
            /// 供件台所在层数。
            /// </summary>
            public int LayerNum { get; set; } = 1;

            /// <summary>
            /// 落格模式，1 就近、2 循环、3 瀑布、4 随机。
            /// </summary>
            public string ChuteModel { get; set; } = "1";

            /// <summary>
            /// 默认实际落格供件区编号。
            /// </summary>
            public int FallArea { get; set; } = 1;

            /// <summary>
            /// 重量来源，0 秤、1 系统或默认值。
            /// </summary>
            public string WeightSource { get; set; } = "0";

            /// <summary>
            /// 格口查询超时毫秒数。
            /// </summary>
            public int QueryTimeoutMilliseconds { get; set; } = 800;

            /// <summary>
            /// 数据上报超时毫秒数。
            /// </summary>
            public int TimeoutMilliseconds { get; set; } = 1000;

            /// <summary>
            /// 最大请求次数。
            /// </summary>
            public int RetryCount { get; set; } = 3;

            /// <summary>
            /// 重试间隔毫秒数。
            /// </summary>
            public int RetryIntervalMilliseconds { get; set; } = 100;

            /// <summary>
            /// 创建独立参数快照。
            /// </summary>
            /// <returns>参数副本。</returns>
            public ApiParameter Clone() {
                return new ApiParameter {
                    BaseUrl = BaseUrl,
                    AppKey = AppKey,
                    AppSecret = AppSecret,
                    EquipmentCode = EquipmentCode,
                    SortingPlanCode = SortingPlanCode,
                    OperateType = OperateType,
                    Operator = Operator,
                    MainLineCode = MainLineCode,
                    EquipmentLayer = EquipmentLayer,
                    AreaNum = AreaNum,
                    MaxCircleNum = MaxCircleNum,
                    SupplyDeskCode = SupplyDeskCode,
                    SupplyDeskSerialNo = SupplyDeskSerialNo,
                    SupplyDeskMethod = SupplyDeskMethod,
                    SupplyDeskArea = SupplyDeskArea,
                    LayerNum = LayerNum,
                    ChuteModel = ChuteModel,
                    FallArea = FallArea,
                    WeightSource = WeightSource,
                    QueryTimeoutMilliseconds = QueryTimeoutMilliseconds,
                    TimeoutMilliseconds = TimeoutMilliseconds,
                    RetryCount = RetryCount,
                    RetryIntervalMilliseconds = RetryIntervalMilliseconds
                };
            }
        }

        /// <summary>
        /// 极昼单票扩展上下文。
        /// </summary>
        public sealed class UploadContext {
            /// <summary>
            /// 是否为离线补传数据。
            /// </summary>
            public bool IsOfflineData { get; set; }

            /// <summary>
            /// 图片相对路径。
            /// </summary>
            public string HalfPath { get; set; } = string.Empty;

            /// <summary>
            /// 包裹上车时间。
            /// </summary>
            public DateTime? LandOnCarTime { get; set; }

            /// <summary>
            /// 小车号。
            /// </summary>
            public string CarNum { get; set; } = string.Empty;

            /// <summary>
            /// 落格包牌号。
            /// </summary>
            public string PackageNo { get; set; } = string.Empty;

            /// <summary>
            /// 建包员编号。
            /// </summary>
            public string BagUserCode { get; set; } = string.Empty;

            /// <summary>
            /// 绑包时间。
            /// </summary>
            public DateTime? BindBagTime { get; set; }

            /// <summary>
            /// RFID 标签。
            /// </summary>
            public string Rfid { get; set; } = string.Empty;

            /// <summary>
            /// 落格模式；为空时使用接口配置值。
            /// </summary>
            public string ChuteModel { get; set; } = string.Empty;

            /// <summary>
            /// 包裹已循环圈数。
            /// </summary>
            public decimal CyclesNum { get; set; }

            /// <summary>
            /// 实际格口号。
            /// </summary>
            public string GridNo { get; set; } = string.Empty;

            /// <summary>
            /// 格口分类码。
            /// </summary>
            public string GridCode { get; set; } = string.Empty;

            /// <summary>
            /// 落格时间。
            /// </summary>
            public DateTime? FallTime { get; set; }

            /// <summary>
            /// 实际落格所在供件区；为空时使用接口配置值。
            /// </summary>
            public int? FallArea { get; set; }

            /// <summary>
            /// 超区次数。
            /// </summary>
            public int OverAreaNum { get; set; }

            /// <summary>
            /// 首次超区原因。
            /// </summary>
            public string OverAreaReason { get; set; } = string.Empty;
        }

        /// <summary>
        /// 极昼目标格口查询请求。
        /// </summary>
        private sealed class QueryChuteRequest {
            /// <summary>
            /// 运单号或包牌号。
            /// </summary>
            public string WaybillNo { get; set; } = string.Empty;

            /// <summary>
            /// 分拣计划编码。
            /// </summary>
            public string SortingPlanCode { get; set; } = string.Empty;

            /// <summary>
            /// 设备编号。
            /// </summary>
            public string EquipmentCode { get; set; } = string.Empty;

            /// <summary>
            /// 可选主线编码。
            /// </summary>
            public string? MainLineCode { get; set; }

            /// <summary>
            /// 重量，单位千克。
            /// </summary>
            public decimal Weight { get; set; }
        }

        /// <summary>
        /// 极昼设备信息请求。
        /// </summary>
        private sealed class DeviceInfoRequest {
            /// <summary>
            /// 设备编号。
            /// </summary>
            public string EquipmentCode { get; set; } = string.Empty;

            /// <summary>
            /// 事件类型。
            /// </summary>
            public string EventType { get; set; } = string.Empty;

            /// <summary>
            /// 运单号。
            /// </summary>
            public string WaybillNo { get; set; } = string.Empty;

            /// <summary>
            /// 操作类型。
            /// </summary>
            public int OperateType { get; set; }

            /// <summary>
            /// 操作员。
            /// </summary>
            public string Operator { get; set; } = string.Empty;

            /// <summary>
            /// 是否为离线数据。
            /// </summary>
            public int IsOfflineData { get; set; }

            /// <summary>
            /// 设备实际层数。
            /// </summary>
            public int EquipmentLayer { get; set; }

            /// <summary>
            /// 设备实际供件区数量。
            /// </summary>
            public int AreaNum { get; set; }

            /// <summary>
            /// 设备允许的最大循环圈数。
            /// </summary>
            public int MaxCircleNum { get; set; }

            /// <summary>
            /// 供件台编号。
            /// </summary>
            public string SupplyDeskCode { get; set; } = string.Empty;

            /// <summary>
            /// 供件台在当前供件区内的连续序号。
            /// </summary>
            public string SupplyDeskSerialNo { get; set; } = string.Empty;

            /// <summary>
            /// 供件方式。
            /// </summary>
            public string SupplyDeskMethod { get; set; } = string.Empty;

            /// <summary>
            /// 供件台所属供件区。
            /// </summary>
            public string SupplyDeskArea { get; set; } = string.Empty;

            /// <summary>
            /// 供件台所在层数。
            /// </summary>
            public int LayerNum { get; set; }

            /// <summary>
            /// 扫码时间。
            /// </summary>
            public string ScanTime { get; set; } = string.Empty;

            /// <summary>
            /// 扫码相机名称。
            /// </summary>
            public string? ScanCameraName { get; set; }

            /// <summary>
            /// 扫码相机序列号。
            /// </summary>
            public string? ScanCameraSn { get; set; }

            /// <summary>
            /// 重量，单位千克。
            /// </summary>
            public decimal Weight { get; set; }

            /// <summary>
            /// 重量来源。
            /// </summary>
            public string WeightSource { get; set; } = string.Empty;

            /// <summary>
            /// 包裹上车时间。
            /// </summary>
            public string? LandOnCarTime { get; set; }

            /// <summary>
            /// 小车号。
            /// </summary>
            public string? CarNum { get; set; }

            /// <summary>
            /// 落格包牌号。
            /// </summary>
            public string? PackageNo { get; set; }

            /// <summary>
            /// 建包员编号。
            /// </summary>
            public string? BagUserCode { get; set; }

            /// <summary>
            /// 绑包时间。
            /// </summary>
            public string? BindBagTime { get; set; }

            /// <summary>
            /// 分拣计划编码。
            /// </summary>
            public string SortingPlanCode { get; set; } = string.Empty;

            /// <summary>
            /// 图片相对路径。
            /// </summary>
            public string? HalfPath { get; set; }

            /// <summary>
            /// RFID 标签。
            /// </summary>
            public string? Rfid { get; set; }

            /// <summary>
            /// 落格模式。
            /// </summary>
            public string? ChuteModel { get; set; }

            /// <summary>
            /// 包裹已循环圈数。
            /// </summary>
            public decimal? CyclesNum { get; set; }

            /// <summary>
            /// 实际格口号。
            /// </summary>
            public string? GridNo { get; set; }

            /// <summary>
            /// 格口分类码。
            /// </summary>
            public string? GridCode { get; set; }

            /// <summary>
            /// 落格时间。
            /// </summary>
            public string? FallTime { get; set; }

            /// <summary>
            /// 实际落格所在供件区。
            /// </summary>
            public int? FallArea { get; set; }

            /// <summary>
            /// 超区次数。
            /// </summary>
            public int? OverAreaNum { get; set; }

            /// <summary>
            /// 首次超区原因。
            /// </summary>
            public string? OverAreaReason { get; set; }
        }

        /// <summary>
        /// 极昼标准响应。
        /// </summary>
        private sealed class PolarDayResponse {
            /// <summary>
            /// 响应代码，1 表示成功。
            /// </summary>
            public int Code { get; set; }

            /// <summary>
            /// 响应消息。
            /// </summary>
            public string Msg { get; set; } = string.Empty;

            /// <summary>
            /// 响应数据。
            /// </summary>
            public JsonElement? Data { get; set; }
        }
    }
}
