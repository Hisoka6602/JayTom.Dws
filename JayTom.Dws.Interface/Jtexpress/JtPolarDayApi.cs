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
        /// 新版极昼服务正式地址。
        /// </summary>
        public const string ProductionBaseUrl =
            "https://sdsonline.jtexpress.com.cn/sdsOnlineApi";

        /// <summary>
        /// 新版极昼服务测试地址。
        /// </summary>
        public const string TestBaseUrl =
            "https://uat-sdsonline.jtexpress.com.cn/sdsOnlineApi";

        /// <summary>
        /// 旧版小件回传正式地址。
        /// </summary>
        public const string LegacySmallItemProductionUrl =
            "https://assscan.jtexpress.com.cn/assscanface/face/" +
            "assScanSmallUpper/smallUpperDataUpload";

        /// <summary>
        /// 旧版小件回传测试地址。
        /// </summary>
        public const string LegacySmallItemTestUrl =
            "https://uat-assscan.jtexpress.com.cn/assscanface/face/" +
            "assScanSmallUpper/smallUpperDataUpload";

        /// <summary>
        /// 默认场地编码。
        /// </summary>
        public const string DefaultSiteCode = "6398155";

        /// <summary>
        /// 默认设备编码。
        /// </summary>
        public const string DefaultEquipmentCode = "ZXJCD6398155001";

        /// <summary>
        /// 默认分拣方案编码。
        /// </summary>
        public const string DefaultSortingPlanCode = "6398155-001";

        /// <summary>
        /// 默认登录人账号。
        /// </summary>
        public const string DefaultOperator = "LS6398155001";

        /// <summary>
        /// JSON 序列化配置。
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
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
            ArgumentNullException.ThrowIfNull(httpClientFactory);
            _httpClientFactory = httpClientFactory;
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
        /// 使用当前旧版配置回传一条测试运单。
        /// </summary>
        /// <param name="barcode">测试运单号。</param>
        /// <param name="weight">测试重量，单位千克。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>包含请求和响应详情的测试结果。</returns>
        public Task<UploadResponse> TestLegacySmallItemUploadAsync(
            string barcode,
            decimal weight,
            CancellationToken token = default) {
            ArgumentException.ThrowIfNullOrWhiteSpace(barcode);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weight);
            var parameters = Volatile.Read(ref _parameters);
            var now = DateTime.Now;
            var context = new UploadContext {
                CarNum = "1",
                GridNo = "1",
                GridCode = "111",
                CyclesNum = 0,
                FallTime = now
            };
            return SendLegacySmallItemAsync(
                barcode.Trim(),
                weight,
                0,
                0,
                0,
                now,
                context,
                parameters,
                token);
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
            var parameters = Volatile.Read(ref _parameters);
            if (parameters.UseLegacyUpload) {
                await UploadLegacySmallItemAsync(
                        barcode,
                        weight,
                        length,
                        width,
                        height,
                        scanTime,
                        context,
                        parameters,
                        token)
                    .ConfigureAwait(false);
                return;
            }

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
        /// 按旧版小件协议回传一条完整分拣记录。
        /// </summary>
        private async Task UploadLegacySmallItemAsync(
            string barcode,
            decimal weight,
            decimal length,
            decimal width,
            decimal height,
            DateTime scanTime,
            UploadContext context,
            ApiParameter parameters,
            CancellationToken token) {
            if (string.IsNullOrWhiteSpace(context.CarNum) ||
                string.IsNullOrWhiteSpace(context.GridNo) ||
                string.IsNullOrWhiteSpace(context.GridCode)) {
                Logger.Error("极昼旧版小件回传缺少小车号、格口或格口分类码");
                return;
            }

            if (context.CyclesNum < 0) {
                Logger.Error("极昼旧版小件回传的循环圈数不能小于零");
                return;
            }

            var response = await SendLegacySmallItemAsync(
                    barcode,
                    weight,
                    length,
                    width,
                    height,
                    scanTime,
                    context,
                    parameters,
                    token)
                .ConfigureAwait(false);
            if (response.IsSuccess) {
                Logger.Info(
                    $"极昼旧版小件回传成功:{barcode},{context.GridNo}");
            }
            else {
                Logger.Error(
                    $"极昼旧版小件回传失败:{barcode},{context.GridNo}," +
                    response.ExceptionMsg);
            }
        }

        /// <summary>
        /// 发送一条旧版小件回传请求并返回完整结果。
        /// </summary>
        private Task<UploadResponse> SendLegacySmallItemAsync(
            string barcode,
            decimal weight,
            decimal length,
            decimal width,
            decimal height,
            DateTime scanTime,
            UploadContext context,
            ApiParameter parameters,
            CancellationToken token) {
            var request = CreateLegacySmallItemRequest(
                barcode,
                weight,
                length,
                width,
                height,
                scanTime,
                context,
                parameters);
            return ExecuteRequestAsync(
                string.Empty,
                new[] { request },
                parameters,
                parameters.TimeoutMilliseconds,
                EvaluateLegacyUploadResponse,
                token,
                parameters.LegacyUploadUrl,
                true);
        }

        /// <summary>
        /// 创建旧版小件回传报文。
        /// </summary>
        private static LegacySmallItemRequest CreateLegacySmallItemRequest(
            string barcode,
            decimal weight,
            decimal length,
            decimal width,
            decimal height,
            DateTime scanTime,
            UploadContext context,
            ApiParameter parameters) {
            var uploadTime = DateTime.Now;
            return new LegacySmallItemRequest {
                WaybillNo = NormalizeBarcode(barcode),
                // 旧协议字段名为 networkCode，业务含义实际是场地编码。
                NetworkCode = parameters.SiteCode,
                ScanTime = scanTime.ToString("yyyy-MM-dd HH:mm:ss"),
                UserNum = parameters.Operator,
                Weight = PositiveMeasurementOrNull(weight),
                Length = PositiveMeasurementOrNull(length),
                Wide = PositiveMeasurementOrNull(width),
                High = PositiveMeasurementOrNull(height),
                UploadResult = IsNoReadBarcode(barcode) ? 2 : 1,
                CrossBeltMac = parameters.CrossBeltMac,
                SupplyDeskCode = parameters.SupplyDeskCode,
                SupplyDeskMac = parameters.SupplyDeskMac,
                UploadTime = uploadTime.ToString("yyyy-MM-dd HH:mm:ss"),
                SortingPlanCode = parameters.SortingPlanCode,
                OperateType = parameters.OperateType,
                EquipmentCode = parameters.EquipmentCode,
                EquipmentLayer = parameters.EquipmentLayer,
                GridNo = context.GridNo,
                PackageNo = EmptyToNull(context.PackageNo),
                FallTime = (context.FallTime ?? uploadTime)
                    .ToString("yyyy-MM-dd HH:mm:ss"),
                NextStation = EmptyToNull(context.NextStation),
                CyclesNum = RoundMeasurement(context.CyclesNum),
                CarNum = context.CarNum,
                GridCode = context.GridCode,
                Rfid = EmptyToNull(context.Rfid),
                ThirdCode = EmptyToNull(context.ThirdCode),
                BagUserCode = EmptyToNull(context.BagUserCode)
            };
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
                SiteCode = parameters.SiteCode,
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
            CancellationToken token,
            string? absoluteUrl = null,
            bool useLegacyCredentials = false) {
            var requestUrl = string.IsNullOrWhiteSpace(absoluteUrl)
                ? CombineUrl(parameters.BaseUrl, relativePath)
                : absoluteUrl;
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
                            useLegacyCredentials,
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
        /// 判定旧版小件回传响应。
        /// </summary>
        private static (bool IsSuccess, string ExceptionMessage,
            ApiExceptionType ExceptionType) EvaluateLegacyUploadResponse(
            string responseContent) {
            if (string.IsNullOrWhiteSpace(responseContent)) {
                return (
                    false,
                    "极昼旧版小件回传响应为空",
                    ApiExceptionType.ContentParsingException);
            }

            try {
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.String) {
                    var text = root.GetString() ?? string.Empty;
                    return IsLegacySuccessText(text)
                        ? (true, string.Empty, ApiExceptionType.None)
                        : (
                            false,
                            text,
                            ApiExceptionType.LogicValidationFailed);
                }

                if (root.ValueKind != JsonValueKind.Object) {
                    return (
                        false,
                        "极昼旧版小件回传响应格式无效",
                        ApiExceptionType.ContentParsingException);
                }

                if (TryGetJsonProperty(root, "fail", out var fail) &&
                    fail.ValueKind == JsonValueKind.True) {
                    return (
                        false,
                        GetLegacyResponseMessage(root),
                        ApiExceptionType.LogicValidationFailed);
                }

                if (TryGetJsonProperty(root, "succ", out var succeeded)) {
                    var isSuccess = IsLegacySuccessValue(succeeded);
                    return isSuccess
                        ? (true, string.Empty, ApiExceptionType.None)
                        : (
                            false,
                            GetLegacyResponseMessage(root),
                            ApiExceptionType.LogicValidationFailed);
                }

                if (TryGetJsonProperty(root, "success", out var success) ||
                    TryGetJsonProperty(root, "result", out success)) {
                    var isSuccess = IsLegacySuccessValue(success);
                    return isSuccess
                        ? (true, string.Empty, ApiExceptionType.None)
                        : (
                            false,
                            GetLegacyResponseMessage(root),
                            ApiExceptionType.LogicValidationFailed);
                }

                if (TryGetJsonProperty(root, "code", out var code)) {
                    var isSuccess = IsLegacySuccessValue(code);
                    return isSuccess
                        ? (true, string.Empty, ApiExceptionType.None)
                        : (
                            false,
                            GetLegacyResponseMessage(root),
                            ApiExceptionType.LogicValidationFailed);
                }

                var message = GetLegacyResponseMessage(root);
                if (!string.IsNullOrWhiteSpace(message)) {
                    return IsLegacySuccessText(message)
                        ? (true, string.Empty, ApiExceptionType.None)
                        : (
                            false,
                            message,
                            ApiExceptionType.LogicValidationFailed);
                }

                return (true, string.Empty, ApiExceptionType.None);
            }
            catch (JsonException) {
                var text = responseContent.Trim();
                return IsLegacySuccessText(text)
                    ? (true, string.Empty, ApiExceptionType.None)
                    : (
                        false,
                        text,
                        ApiExceptionType.ContentParsingException);
            }
        }

        /// <summary>
        /// 判断旧版响应值是否表示成功。
        /// </summary>
        private static bool IsLegacySuccessValue(JsonElement value) {
            if (value.ValueKind is JsonValueKind.True) {
                return true;
            }

            if (value.ValueKind is JsonValueKind.Number &&
                value.TryGetInt32(out var number)) {
                return number is 0 or 1 or 200;
            }

            return value.ValueKind == JsonValueKind.String &&
                   IsLegacySuccessText(value.GetString() ?? string.Empty);
        }

        /// <summary>
        /// 判断旧版响应文本是否表示成功。
        /// </summary>
        private static bool IsLegacySuccessText(string value) {
            var normalized = value.Trim();
            return normalized is "0" or "1" or "200" ||
                   normalized.Equals(
                       "success",
                       StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals(
                       "ok",
                       StringComparison.OrdinalIgnoreCase) ||
                   normalized.Contains(
                       "成功",
                       StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 读取旧版响应中的提示信息。
        /// </summary>
        private static string GetLegacyResponseMessage(JsonElement root) {
            if (TryGetJsonProperty(root, "msg", out var message) ||
                TryGetJsonProperty(root, "message", out message) ||
                TryGetJsonProperty(root, "errorMsg", out message)) {
                return message.ValueKind == JsonValueKind.String
                    ? message.GetString() ?? string.Empty
                    : message.ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// 不区分大小写读取 JSON 字段。
        /// </summary>
        private static bool TryGetJsonProperty(
            JsonElement element,
            string name,
            out JsonElement value) {
            foreach (var property in element.EnumerateObject()) {
                if (property.Name.Equals(
                        name,
                        StringComparison.OrdinalIgnoreCase)) {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
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
        /// <param name="useLegacyCredentials">是否使用旧版回传凭证。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>响应正文。</returns>
        private async Task<string> SendAsync(
            string requestUrl,
            byte[] requestBytes,
            ApiParameter parameters,
            int timeoutMilliseconds,
            bool useLegacyCredentials,
            CancellationToken token) {
            var appKey = useLegacyCredentials
                ? parameters.LegacyAppKey
                : parameters.AppKey;
            var appSecret = useLegacyCredentials
                ? parameters.LegacyAppSecret
                : parameters.AppSecret;
            var timestamp =
                DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            var signature = CreateSignature(
                appSecret,
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
                appKey);
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
            // DWS-HEX-COMPACT: 外部接口签名要求使用无分隔符摘要。
            var md5Hex = Convert.ToHexStringLower(MD5.HashData(source));
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

            if (parameters.UseLegacyUpload) {
                if (string.IsNullOrWhiteSpace(parameters.LegacyAppKey) ||
                    string.IsNullOrWhiteSpace(parameters.LegacyAppSecret)) {
                    return "极昼旧版回传 AppKey 和 AppSecret 不能为空";
                }

                if (parameters.OperateType > 2) {
                    return "极昼旧版回传的操作类型只能为 1 或 2";
                }

                if (!Uri.TryCreate(
                        parameters.LegacyUploadUrl,
                        UriKind.Absolute,
                        out var legacyUploadUri) ||
                    legacyUploadUri.Scheme is not "http" and not "https") {
                    return "极昼旧版回传地址无效";
                }

                if (string.IsNullOrWhiteSpace(parameters.SiteCode) ||
                    string.IsNullOrWhiteSpace(parameters.CrossBeltMac) ||
                    string.IsNullOrWhiteSpace(parameters.SupplyDeskMac)) {
                    return "极昼旧版回传的场地编码、交叉带 MAC 和供件台 MAC 不能为空";
                }

                if (!IsMacAddress(parameters.CrossBeltMac) ||
                    !IsMacAddress(parameters.SupplyDeskMac)) {
                    return "极昼旧版回传 MAC 格式无效，应填写 12 位十六进制 MAC 地址";
                }

                if (parameters.EquipmentLayer <= 0) {
                    return "极昼旧版回传的设备层数必须大于零";
                }

                if (string.IsNullOrWhiteSpace(parameters.SupplyDeskCode)) {
                    return "极昼旧版回传的供件台编号不能为空";
                }

                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(parameters.SiteCode)) {
                return "极昼场地编码不能为空";
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
        /// 判断文本是否为常见的 12 位十六进制 MAC 地址。
        /// </summary>
        private static bool IsMacAddress(string value) {
            var hexCount = 0;
            foreach (var character in value.Trim()) {
                if (character is ':' or '-' or '.') {
                    continue;
                }

                if (!Uri.IsHexDigit(character)) {
                    return false;
                }

                hexCount++;
            }

            return hexCount == 12;
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
            return IsNoReadBarcode(barcode)
                ? "NoRead"
                : barcode;
        }

        /// <summary>
        /// 判断是否为未识别条码。
        /// </summary>
        private static bool IsNoReadBarcode(string barcode) {
            return string.IsNullOrWhiteSpace(barcode) ||
                   string.Equals(
                       barcode,
                       "noread",
                       StringComparison.OrdinalIgnoreCase);
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
        /// 将有效测量值保留两位小数，否则省略该可选字段。
        /// </summary>
        private static decimal? PositiveMeasurementOrNull(decimal value) {
            return value > 0 ? RoundMeasurement(value) : null;
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
        /// 根据新版服务环境取得默认旧版小件回传地址。
        /// </summary>
        public static string GetDefaultLegacyUploadUrl(string baseUrl) {
            return baseUrl.Contains(
                "uat",
                StringComparison.OrdinalIgnoreCase)
                ? LegacySmallItemTestUrl
                : LegacySmallItemProductionUrl;
        }

        /// <summary>
        /// 将空地址或内置测试地址切换为新版正式环境地址。
        /// </summary>
        /// <param name="baseUrl">当前保存的新版服务地址。</param>
        /// <returns>可继续使用的正式或自定义服务地址。</returns>
        public static string NormalizeProductionBaseUrl(string? baseUrl) {
            return string.IsNullOrWhiteSpace(baseUrl) ||
                   string.Equals(
                       baseUrl.TrimEnd('/'),
                       TestBaseUrl,
                       StringComparison.OrdinalIgnoreCase)
                ? ProductionBaseUrl
                : baseUrl;
        }

        /// <summary>
        /// 将空地址或内置测试地址切换为旧版正式回传地址。
        /// </summary>
        /// <param name="legacyUploadUrl">当前保存的旧版回传地址。</param>
        /// <returns>可继续使用的正式或自定义回传地址。</returns>
        public static string NormalizeLegacyProductionUrl(
            string? legacyUploadUrl) {
            return string.IsNullOrWhiteSpace(legacyUploadUrl) ||
                   string.Equals(
                       legacyUploadUrl,
                       LegacySmallItemTestUrl,
                       StringComparison.OrdinalIgnoreCase)
                ? LegacySmallItemProductionUrl
                : legacyUploadUrl;
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
                    parameters.UseLegacyUpload,
                    parameters.LegacyUploadUrl,
                    parameters.LegacyAppKey,
                    parameters.SiteCode,
                    parameters.CrossBeltMac,
                    parameters.SupplyDeskMac,
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
                ProductionBaseUrl;

            /// <summary>
            /// 应用标识。
            /// </summary>
            public string AppKey { get; set; } = string.Empty;

            /// <summary>
            /// 应用密钥。
            /// </summary>
            public string AppSecret { get; set; } = string.Empty;

            /// <summary>
            /// 是否使用旧版小件回传；默认使用新版回传。
            /// </summary>
            public bool UseLegacyUpload { get; set; }

            /// <summary>
            /// 旧版小件回传地址。
            /// </summary>
            public string LegacyUploadUrl { get; set; } =
                LegacySmallItemProductionUrl;

            /// <summary>
            /// 旧版小件回传应用标识。
            /// </summary>
            public string LegacyAppKey { get; set; } = string.Empty;

            /// <summary>
            /// 旧版小件回传应用密钥。
            /// </summary>
            public string LegacyAppSecret { get; set; } = string.Empty;

            /// <summary>
            /// 新版回传场地编码。
            /// </summary>
            public string SiteCode { get; set; } = DefaultSiteCode;

            /// <summary>
            /// 旧版小件回传交叉带 MAC 地址。
            /// </summary>
            public string CrossBeltMac { get; set; } = string.Empty;

            /// <summary>
            /// 旧版小件回传供件台 MAC 地址。
            /// </summary>
            public string SupplyDeskMac { get; set; } = string.Empty;

            /// <summary>
            /// 设备编号。
            /// </summary>
            public string EquipmentCode { get; set; } =
                DefaultEquipmentCode;

            /// <summary>
            /// 分拣计划编码。
            /// </summary>
            public string SortingPlanCode { get; set; } =
                DefaultSortingPlanCode;

            /// <summary>
            /// 操作类型，1 出港、2 进港、3 进出港。
            /// </summary>
            public int OperateType { get; set; } = 1;

            /// <summary>
            /// 操作员 JMS 账号。
            /// </summary>
            public string Operator { get; set; } = DefaultOperator;

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
                    UseLegacyUpload = UseLegacyUpload,
                    LegacyUploadUrl = LegacyUploadUrl,
                    LegacyAppKey = LegacyAppKey,
                    LegacyAppSecret = LegacyAppSecret,
                    SiteCode = SiteCode,
                    CrossBeltMac = CrossBeltMac,
                    SupplyDeskMac = SupplyDeskMac,
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
            /// 旧版小件回传使用的格口下一站或目的地。
            /// </summary>
            public string NextStation { get; set; } = string.Empty;

            /// <summary>
            /// 旧版小件回传使用的三段码。
            /// </summary>
            public string ThirdCode { get; set; } = string.Empty;

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
            /// 场地编码。
            /// </summary>
            public string SiteCode { get; set; } = string.Empty;

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
