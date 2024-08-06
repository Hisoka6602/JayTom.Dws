using NLog;
using System;
using System.Linq;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using TouchSocket.Core;
using System.Reflection;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Domain.Model;
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using JayTom.Dws.Domain.Interface;
using JayTom.Dws.Domain.Dto.ApiDto;
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.PluginInterface.Utils;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Interface.Attributes;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using PluginType = JayTom.Dws.Client.EventMediators.PluginType;
using InstructionType = JayTom.Dws.Data.Package.InstructionType;
using UploadResponse = JayTom.Dws.Domain.Interface.UploadResponse;
using WindowsAction = JayTom.Dws.Client.EventMediators.WindowsAction;
using ApplicationStatus = JayTom.Dws.Client.EventMediators.ApplicationStatus;
using WindowsActionType = JayTom.Dws.Client.EventMediators.WindowsActionType;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;
using TriggerPositionEvent = JayTom.Dws.Client.EventMediators.TriggerPositionEvent;
using PackageExitUpdateEvent = JayTom.Dws.Client.EventMediators.PackageExitUpdateEvent;
using PluginParamChangedEvent = JayTom.Dws.Client.EventMediators.PluginParamChangedEvent;
using ApplicationStatusChanged = JayTom.Dws.Client.EventMediators.ApplicationStatusChanged;
using PackageAbnormalSortingType = JayTom.Dws.Client.EventMediators.PackageAbnormalSortingType;

namespace JayTom.Dws.Client.Service.BackgroundService {

    /// <summary>
    /// Api提交处理器
    /// </summary>
    public class SubmitApiBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfigRepository _configRepository;
        private readonly IImageStorageService _imageStorageService;
        private readonly IMemoryCache _memoryCache;
        private ConcurrentQueue<SubmitItemInfo> _submitItems = new();
        private ApiSettingsDto? _apiSettingsDto;
        private ConcurrentQueue<SavedImageInfo> _savedImageItems = new();
        private ConcurrentQueue<PackageAggregationInfo> _packageAggregationInfoItems = new();
        private SemaphoreSlim _takePackageSlim = new(1);
        private ConcurrentDictionary<long, PackageSubmissionPushInfo> _packageSubmissionPushItems = new();
        private JtExpressDto _jtExpressDto = new();
        private IApiUploader<BaseApiParameters>? _submissionUploader;

        #region 非通用版本变量(临时)

        private static string _sunnenApiPackage = string.Empty;
        private static bool _isWindowsClose;

        #endregion 非通用版本变量(临时)

        public SubmitApiBackgroundService(IHttpClientFactory httpClientFactory,
            IConfigRepository configRepository, IImageStorageService imageStorageService,
            IMemoryCache memoryCache) {
            _httpClientFactory = httpClientFactory;
            _configRepository = configRepository;
            _imageStorageService = imageStorageService;
            _memoryCache = memoryCache;
            //包裹信息完成
            EventAggregator.Instance.Subscribe<PackageInfo>(async item => {
                if (item is { BarCodeInfo: not null } model) {
                    _submitItems.Enqueue(new SubmitItemInfo() {
                        Barcode = model?.BarCodeInfo?.Barcode ?? string.Empty,
                        Height = (float)(model?.VolumeInfo?.FormattedHeight ?? 0),
                        ScanTime = model?.BarCodeInfo?.ScanTime ?? DateTime.Now,
                        Weight = (float)(model?.WeightInfo?.FormattedWeight ?? 0),
                        Length = (float)(model?.VolumeInfo?.FormattedLength ?? 0),
                        Width = (float)(model?.VolumeInfo?.FormattedWidth ?? 0),
                        Volume = (float)(model?.VolumeInfo?.FormattedVolume ?? 0),
                        Guid = model?.Guid ?? 0,
                        IsCreatedByLowerMachine = (bool)model?.IsCreatedByLowerMachine,
                        PackageCreationInstruction = model?.PackageCreationInstruction ?? string.Empty,
                        IsStackedPackage = model?.IsStackedPackage,
                        Timestamp = model?.Timestamp ?? 0,
                        LinkedCarCount = model?.LinkedCarCount ?? 1,
                        Other = model?.Other
                        //图片暂时不写
                    });
                    //添加到推送队列
                    if (model?.IsCreatedByLowerMachine == true && _submissionUploader is not null) {
                        try {
                            await _takePackageSlim.WaitAsync();
                            _packageSubmissionPushItems.TryAdd(
                                new DateTimeOffset(model.CreateTime).ToUnixTimeMilliseconds(),
                                new PackageSubmissionPushInfo() {
                                    PackageInfo = model
                                });
                        }
                        finally {
                            _takePackageSlim.Release();
                        }
                    }
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                await Task.Yield();
                if (item is { } model) {
                    var interfaceType = typeof(IApiUploader<BaseApiParameters>);
                    var types = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(assembly => assembly.GetTypes())
                        .Where(t => interfaceType.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

                    var orDefault = types.FirstOrDefault(f => (bool)f.GetCustomAttribute<ApiClassAttribute>()?.ParametersName.Equals(
                        model.SettingsName,
                        StringComparison.InvariantCultureIgnoreCase));

                    if (model.SettingsName.Equals("ApiSettings", StringComparison.CurrentCultureIgnoreCase) ||
                        orDefault is not null) {
                        //重新实例化
                        var settingsDto = await _configRepository.FirstOrDefaultEntity<ApiSettingsDto>(model.SettingsName);
                        _submissionUploader = CreateInstanceByApiName(settingsDto.ApiName);
                        if (_submissionUploader is not null) {
                            //设置参数
                            var parametersName = GetParametersName(_submissionUploader.GetType());

                            var defaultEntity = await CallConfigRepositoryFirstOrDefaultEntity(_submissionUploader.Parameters.GetType(), parametersName);
                            _submissionUploader.SetParameters(defaultEntity ?? new object());
                        }

                        //CreateInstanceByApiName
                    }
                }
            });
            EventAggregator.Instance.Subscribe<PluginParamChangedEvent>(item => {
                if (item is { } model) {
                    if (model is { Type: PluginType.HomeTool, PluginName: "SunnenPlugin" }) {
                        _sunnenApiPackage = model.Content;
                    }
                }
            });
            _imageStorageService.ImageSaved += delegate (object? sender, ImageSavedEventArgs args) {
                //保存后触发
                _savedImageItems.Enqueue(new SavedImageInfo() {
                    BarCode = args.BarCode,
                    FilePath = args.FilePath,
                    ImageType = args.ImageType,
                    CameraSerialNumber = args.CameraSerialNumber ?? string.Empty,
                    ScanTime = args.ScanTime,
                });
            };
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                if (item is WindowsAction { Type: WindowsActionType.Close }) {
                    _isWindowsClose = true;
                }
            });
            //集包推送
            EventAggregator.Instance.Subscribe<PackageAggregationInfo>(async item => {
                //加入队列
                if (item is { } info) {
                    _packageAggregationInfoItems.Enqueue(info);
                }
            });
            //更新上传状态
            EventAggregator.Instance.Subscribe<ApiResponseReceived>(async item => {
                if (item is { } model && _packageSubmissionPushItems.Any()) {
                    await Task.Yield();
                    try {
                        await _takePackageSlim.WaitAsync();
                        //获取包裹
                        var (key, value) = _packageSubmissionPushItems.FirstOrDefault(f => f.Key.Equals(model.Timestamp));
                        if (value is not null) {
                            //更新格口信息
                            value.ApiResponse = model;
                        }
                    }
                    finally {
                        _takePackageSlim.Release();
                    }
                }
            });
            //系统信息
            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(item => {
                if (item is { Status: ApplicationStatus.Stop }) {
                    _packageSubmissionPushItems.Clear();
                    _packageAggregationInfoItems.Clear();
                }
            });
            //更新格口信息
            EventAggregator.Instance.Subscribe<PackageExitUpdateEvent>(async item => {
                if (item is { } model && _packageSubmissionPushItems.Any()) {
                    try {
                        await _takePackageSlim.WaitAsync();
                        //获取包裹
                        var (key, value) = _packageSubmissionPushItems.FirstOrDefault(f => f.Key.Equals(model.Timestamp));
                        if (value is not null) {
                            //更新格口信息
                            value.PackageExitUpdateItems.Add(model);
                            //推送集包信息
                            /*EventAggregator.Instance.Publish(new PushPackageInfo() {
                                PackageInfo = value.PackageInfo ?? new PackageInfo(),
                                PackageExitUpdateEvent = model
                            });*/
                        }
                        else {
                            NLog.LogManager.GetCurrentClassLogger().Error($"未匹配到包裹:{model.InstructionInfos?.FirstOrDefault()?.InstructionContent} 操作指令");
                        }
                    }
                    finally {
                        _takePackageSlim.Release();
                    }
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //读参数
            await ReadDefaultConfig();
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                await Task.Delay(30, stoppingToken).ContinueWith(_task => {
                    //取出
                    if (_task.IsCompletedSuccessfully) {
                        try {
                            //需要判断用户选择的接口和参数设置
                            var tryDequeue = _submitItems.TryDequeue(out var info);

                            if (tryDequeue && info is not null) {
                                //上传
                                //判断上传接口
                                Task.Factory.StartNew(async () => {
                                    UploadResponse? uploadResponse = null;
                                    if (_submissionUploader is not null) {
                                        var executionType = _submissionUploader.GetType().GetCustomAttribute<ApiClassAttribute>()?.ExecTypes;

                                        if (executionType is not null && executionType.Value.HasFlag(ExecutionType.ScanPackage)) {
                                            //扫描包裹
                                            _submissionUploader.ScanPackage(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                info.Timestamp, null,
                                                null, stoppingToken, stoppingToken);
                                        }

                                        if (executionType is not null && executionType.Value.HasFlag(ExecutionType.UploadInformation)) {
                                            uploadResponse = await _submissionUploader.UploadInformation(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                info.Timestamp, null,
                                                null, stoppingToken, stoppingToken);
                                        }
                                    }
                                    if (_apiSettingsDto?.Type is not null &&
                                        _submissionUploader is not null) {
                                        //临时单线程
                                        EventAggregator.Instance.Publish(new ApiResponseReceived {
                                            Guid = info.Guid,
                                            Barcode = info.Barcode,
                                            ScanTime = info.ScanTime,
                                            UploadResponse = uploadResponse,
                                            PackageCreationInstruction = info.PackageCreationInstruction,
                                            PackageCreationTime = info.PackageCreationTime,
                                            IsCreatedByLowerMachine = info.IsCreatedByLowerMachine,
                                            Timestamp = info.Timestamp,
                                            LinkedCarCount = info.LinkedCarCount
                                        });
                                        EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                            IsSuccess = uploadResponse?.IsSuccess ?? false,
                                            TriggerPosition = TriggerPositionEnum.HttpOutput
                                        });
                                    }
                                }, stoppingToken);
                            }

                            /*
                            //获取包裹
                            var pairs = _packageSubmissionPushItems?.Any(f => f.Value.PackageExitUpdateItems?.Any() == true
                                                                              && f.Value.PackageInfo is not null) == true
                                ? _packageSubmissionPushItems?.Where(f => (f.Value.PackageExitUpdateItems?.Any() == true)
                                                                          && f.Value.PackageInfo is not null)?.ToList()
                                : new List<KeyValuePair<long, PackageSubmissionPushInfo>>();

                            if (pairs?.Any() == true) {
                                if (_submissionUploader is not null) {
                                    /*
                                Parallel.ForEach(pairs, new ParallelOptions() {
                                    MaxDegreeOfParallelism = 10
                                }, (packageValue, _) => {
                                    lock (_packageSubmissionPushLock) {
                                        ReportProgress(packageValue, uploader, stoppingToken);
                                    }
                                });
                                #1#

                                    foreach (var pair in pairs) {
                                        ReportProgress(pair, _submissionUploader, stoppingToken);
                                    }
                                }
                            }
                            //集包
                            var packageAggregationDequeue = _packageAggregationInfoItems.TryDequeue(out var packageAggregationInfo);
                            if (packageAggregationDequeue && packageAggregationInfo is not null) {
                                //集包推送(判断需要使用的Api-Task.Factory.StartNew)

                                Task.Factory.StartNew(async () => {
                                    IDataUploader uploader;
                                    UploadResponse? uploadResponse = null;
                                    switch (_apiSettingsDto?.Type) {
                                        case ApiType.None:
                                            return;

                                        case ApiType.CaiNiaoApi:

                                            uploader = new CaiNiaoApi(_httpClientFactory);
                                            var (key, value) = await uploader.SetParameters(_caiNiaoApiParam);
                                            if (key) {
                                                uploader.PackageAggregation(packageAggregationInfo.PackageExitDefinitionInfo.ExitName,
                                                    packageAggregationInfo.AggregatePackageCode,
                                                    packageAggregationInfo.PackagingTime,
                                                    packageAggregationInfo.PackageItems.Select(s => s.BarCodeInfo?.Barcode ?? string.Empty).ToList(), token: stoppingToken);
                                            }
                                            else {
                                                Console.WriteLine("设置参数失败!");
                                            }
                                            break;
                                    }
                                }, stoppingToken);
                            }*/
                        }
                        catch (Exception e) {
                            LogManager.GetCurrentClassLogger().Error($"{e}");
                        }
                    }
                }, stoppingToken);
            }
        }

        private async Task ReadDefaultConfig() {
            //上传类型
            _apiSettingsDto = await _configRepository.FirstOrDefaultEntity<ApiSettingsDto>("ApiSettings") ?? new ApiSettingsDto();

            //重新实例化
            _submissionUploader = CreateInstanceByApiName(_apiSettingsDto.ApiName);
            if (_submissionUploader is not null) {
                //设置参数
                var parametersName = GetParametersName(_submissionUploader.GetType());

                var defaultEntity = await CallConfigRepositoryFirstOrDefaultEntity(_submissionUploader.Parameters.GetType(), parametersName);
                _submissionUploader.SetParameters(defaultEntity ?? new object());
            }
        }

        private KeyValuePair<int, CaiNiaoExitInfo> CaiNiaoStatusConvert(string barcode, List<PackageExitUpdateEvent> packageExitItems) {
            if (packageExitItems.Any(a => (int)a.PackageAbnormalSortingType == (int)PackageAbnormalSortingType.LockExit) == true) {
                return new KeyValuePair<int, CaiNiaoExitInfo>(3, new CaiNiaoExitInfo() {
                    ErrorReson = "锁格",
                    ChuteCode = packageExitItems?.FirstOrDefault(f =>
                        f.PackageAbnormalSortingType ==
                        PackageAbnormalSortingType.LockExit)?.ExitName ?? string.Empty
                });
            }

            var exitUpdateEvent = packageExitItems?.FirstOrDefault(f => f.InstructionType == InstructionType.PackageException);
            if (exitUpdateEvent is not null) {
                return new KeyValuePair<int, CaiNiaoExitInfo>(6, new CaiNiaoExitInfo() {
                    ChuteCode = exitUpdateEvent.ExitName,
                    ErrorReson = exitUpdateEvent.PackageAbnormalSortingType.GetDescription()
                });
            }

            if (packageExitItems?.Any(a => a.PackageAbnormalSortingType
                                          == PackageAbnormalSortingType.LockExit) != true &&
                packageExitItems?.Any(a => a.InstructionType ==
                                          InstructionType.SignalCallback) != true) {
                return new KeyValuePair<int, CaiNiaoExitInfo>(6, new CaiNiaoExitInfo() {
                    ChuteCode = "格口100",
                    ErrorReson = "未获取到落格信息"
                });
            }
            return barcode.ToLower().Equals("noread") ? new KeyValuePair<int, CaiNiaoExitInfo>(6, new CaiNiaoExitInfo() {
                ErrorReson = "无条码",
                ChuteCode = packageExitItems?.LastOrDefault()?.ExitName ?? string.Empty
            }) : new KeyValuePair<int, CaiNiaoExitInfo>(0, new CaiNiaoExitInfo() {
                ErrorReson = "分拣成功",
                ChuteCode = packageExitItems?.LastOrDefault()?.ExitName ?? string.Empty
            });
        }

        public IApiUploader<BaseApiParameters>? CreateInstanceByApiName(string apiName) {
            try {
                var interfaceType = typeof(IApiUploader<>);
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => t is { IsClass: true, IsAbstract: false })
                    .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType))
                    .ToList();

                foreach (var type in types) {
                    var apiNameAttribute = type.GetCustomAttribute<ApiClassAttribute>();
                    if (apiNameAttribute != null && apiNameAttribute.Name == apiName) {
                        var constructor = type.GetConstructor(new[] { typeof(IHttpClientFactory) });
                        if (constructor != null) {
                            var instance = (IApiUploader<BaseApiParameters>)constructor.Invoke(new object[] { _httpClientFactory });
                            return instance;
                        }
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return null;
        }

        private async Task<object?> CallConfigRepositoryFirstOrDefaultEntity(Type apiParametersType, string settingsName, CancellationToken token = default) {
            try {
                // 获取方法信息
                var method = typeof(ConfigRepository).GetMethod("FirstOrDefaultEntity");

                // 创建泛型方法
                var genericMethod = method.MakeGenericMethod(apiParametersType);

                // 调用泛型方法
                var resultTask = (Task)genericMethod.Invoke(_configRepository, new object[] { settingsName, token });

                // 等待任务完成
                if (resultTask != null) {
                    await resultTask;

                    // 获取任务结果
                    var resultProperty = resultTask.GetType().GetProperty("Result");
                    var result = resultProperty?.GetValue(resultTask);

                    return result;
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return null;
        }

        private string? GetParametersName(Type type) {
            var attribute = type.GetCustomAttribute<ApiClassAttribute>();
            return attribute?.ParametersName;
        }

        public class SubmitItemInfo {
            public long Guid { get; set; }

            /// <summary>
            /// 条码
            /// </summary>
            public string? Barcode { get; set; }

            /// <summary>
            /// 重量
            /// </summary>
            public float Weight { get; set; }

            /// <summary>
            /// 扫码时间
            /// </summary>
            public DateTime ScanTime { get; set; }

            /// <summary>
            /// 长度
            /// </summary>
            public float Length { get; set; }

            /// <summary>
            /// 宽度
            /// </summary>
            public float Width { get; set; }

            /// <summary>
            /// 高度
            /// </summary>
            public float Height { get; set; }

            /// <summary>
            /// 体积
            /// </summary>
            public float Volume { get; set; }

            /// <summary>
            /// 条码图片
            /// </summary>
            public Bitmap? Image { get; set; }

            /// <summary>
            /// 全景图
            /// </summary>
            public Bitmap? PanoramaImage { get; set; }

            /// <summary>
            /// 创建包裹时间
            /// </summary>
            public DateTime PackageCreationTime { get; set; }

            /// <summary>
            /// 创建包裹指令
            /// </summary>
            public string PackageCreationInstruction { get; set; } = string.Empty;

            /// <summary>
            /// 是否由下位机创建
            /// </summary>
            public bool IsCreatedByLowerMachine { get; set; }

            /// <summary>
            /// 是否叠包
            /// </summary>
            public bool? IsStackedPackage { get; set; }

            /// <summary>
            /// 包裹时间戳
            /// </summary>
            public long Timestamp { get; set; }

            /// <summary>
            /// 联动车辆
            /// </summary>
            public int LinkedCarCount { get; set; } = 0;

            /// <summary>
            /// 其他
            /// </summary>
            public object? Other { get; set; }
        }

        /// <summary>
        /// Api回传类
        /// </summary>
        public class ApiResponseReceived {
            public long Guid { get; set; }

            /// <summary>
            /// 创建包裹时间
            /// </summary>
            public DateTime PackageCreationTime { get; set; }

            /// <summary>
            /// 创建包裹指令
            /// </summary>
            public string PackageCreationInstruction { get; set; } = string.Empty;

            /// <summary>
            /// 是否由下位机创建
            /// </summary>
            public bool IsCreatedByLowerMachine { get; set; }

            /// <summary>
            /// 条码
            /// </summary>
            public string? Barcode { get; set; }

            /// <summary>
            /// 扫码时间
            /// </summary>
            public DateTime ScanTime { get; set; }

            /// <summary>
            /// 响应内容
            /// </summary>
            public UploadResponse? UploadResponse { get; set; }

            /// <summary>
            /// 是否叠包
            /// </summary>
            public bool IsStackedPackage { get; set; }

            /// <summary>
            /// 包裹时间戳
            /// </summary>
            public long Timestamp { get; set; }

            /// <summary>
            /// 联动车辆
            /// </summary>
            public int LinkedCarCount { get; set; } = 0;
        }

        /// <summary>
        /// 包裹推送
        /// </summary>
        public class PackageSubmissionPushInfo {

            /// <summary>
            /// 包裹信息
            /// </summary>
            public PackageInfo? PackageInfo { get; set; }

            /// <summary>
            /// 等待提交时间
            /// </summary>
            public DateTime WaitSubmitTime { get; set; } = DateTime.Now;

            /// <summary>
            /// 格口更新
            /// </summary>
            public List<PackageExitUpdateEvent> PackageExitUpdateItems { get; set; } = new();

            /// <summary>
            /// 回传信息
            /// </summary>
            public ApiResponseReceived ApiResponse { get; set; } = new();

            /// <summary>
            /// 是否已提交过备用格口
            /// </summary>
            public bool WasPushedAlternateExitSorter { get; set; }
        }

        public class CaiNiaoExitInfo {
            public string ChuteCode { get; set; } = string.Empty;
            public string ErrorReson { get; set; } = string.Empty;
        }
    }
}