using JayTom.Dws.Application.Configuration;
using NLog;
using JayTom.Dws.Application.Workflows;
using JayTom.Dws.Abstractions.Integrations;
using System;
using ImTools;
using System.Net;
using System.Linq;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using TouchSocket.Core;
using JayTom.Dws.Interface;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Model;
using JayTom.Dws.Interface.Wdt;
using JayTom.Dws.Interface.ttx;
using JayTom.Dws.Data.LocalConf;
using NPOI.SS.Formula.Functions;
using JayTom.Dws.Interface.Post;
using JayTom.Dws.Domain.Manager;
using JayTom.Dws.PluginInterface;
using JayTom.Dws.Interface.geek_;
using System.Collections.Generic;
using NPOI.XSSF.Streaming.Values;
using JayTom.Dws.Interface.Cloud;
using JayTom.Dws.Interface.Sunnen;
using JayTom.Dws.Interface.JdyWms;
using JayTom.Dws.Interface.ZhouYi;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Interface.Szjy188;
using JayTom.Dws.Interface.CaiNiao;
using System.Collections.Concurrent;
using JayTom.Dws.Interface.Routdata;
using JayTom.Dws.Interface.Jtexpress;
using JayTom.Dws.Interface.Jushuitan;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Interface.Eshippingit;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Interface.zhuoyan_scm;
using JayTom.Dws.Client.Service.Sorting;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using static JayTom.Dws.Interface.CaiNiao.CaiNiaoApi;
using static Aliyun.OSS.Model.ListMultipartUploadsResult;
using UploadResponse = JayTom.Dws.Interface.UploadResponse;
using PluginType = JayTom.Dws.Domain.EventMediators.PluginType;
using InstructionType = JayTom.Dws.Data.Package.InstructionType;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.DownstreamProtocols.CommunicationProtocols;
using WindowsAction = JayTom.Dws.Domain.EventMediators.WindowsAction;
using PushPackageInfo = JayTom.Dws.Domain.EventMediators.PushPackageInfo;
using SortingExitType = JayTom.Dws.Domain.EventMediators.SortingExitType;
using ApplicationStatus = JayTom.Dws.Domain.EventMediators.ApplicationStatus;
using WindowsActionType = JayTom.Dws.Domain.EventMediators.WindowsActionType;
using SettingsChangedEvent = JayTom.Dws.Domain.EventMediators.SettingsChangedEvent;
using TriggerPositionEvent = JayTom.Dws.Domain.EventMediators.TriggerPositionEvent;
using PackageExitUpdateEvent = JayTom.Dws.Domain.EventMediators.PackageExitUpdateEvent;
using PluginParamChangedEvent = JayTom.Dws.Domain.EventMediators.PluginParamChangedEvent;
using ApplicationStatusChanged = JayTom.Dws.Domain.EventMediators.ApplicationStatusChanged;
using PackageAbnormalSortingType = JayTom.Dws.Domain.EventMediators.PackageAbnormalSortingType;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    /// <summary>
    /// Api提交处理器
    /// </summary>
    public class SubmitApiBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IProviderRegistry<IDataUploader> _providerRegistry;
        private readonly ISettingsStore _settingsStore;
        private readonly IImageStorageService _imageStorageService;
        private readonly IMemoryCache _memoryCache;
        private readonly BoundedWorkQueue<SubmitItemInfo> _submitItems = new(MaxQueueLength);
        private ApiSettingsDto? _apiSettingsDto;
        private static DefaultApi.DefaultApiParameters _defaultApiParameters = new();
        private static SzjyApi.ApiParameter _szjyApiParam = new();
        private static WdtWmsApi.ApiParameter _wdtWmsApiParameter = new();
        private static WdtFlagshipApi.ApiParameter _wdtFlagshipApiParameter = new();
        private static JtExpressApi.ApiParameter _jtExpressApiParam = new();

        /// <summary>
        /// 极兔极昼接口参数快照。
        /// </summary>
        private static JtPolarDayApi.ApiParameter _jtPolarDayApiParam = new();
        private static RoutDataApi.ApiParameters _rstDataApiParam = new();
        private static CaiNiaoApi.ApiParameters _caiNiaoApiParam = new();
        private static EshippingitApi.ApiParameters _eshippingitApiParam = new();
        private static PostApi.ApiParameters _postApiParam = new();
        private static PostInApi.ApiParameters _postInApiParam = new();
        private static ZhuoYanScmApi.ApiParameters _zhuoYanScmApiParam = new();
        private static JushuitanErpApi.ApiParameters _jushuitanErpParam = new();
        private static ZhouYiApi.ApiParameters _zhouYiApiParam = new();
        private readonly BoundedWorkQueue<SavedImageInfo> _savedImageItems = new(MaxQueueLength);
        /*private ConcurrentQueue<CallBackPackageInfo> _callBackItems = new();
        private ConcurrentDictionary<long, SortingExitReceived> _sortingExitItems = new();*/
        private readonly BoundedWorkQueue<PackageAggregationInfo> _packageAggregationInfoItems = new(MaxQueueLength);
        /// <summary>
        /// 单类接口任务允许排队的最大数量。
        /// </summary>
        private const int MaxQueueLength = 10_000;
        /// <summary>
        /// 用于唤醒接口任务消费者的合并信号。
        /// </summary>
        private readonly SemaphoreSlim _workSignal = new(0, 1);
        /// <summary>统一记录接口工作项的有限重试状态。</summary>
        private readonly RetryAttemptTracker _retryTracker = new(5);
        private readonly SemaphoreSlim _settingsUpdateGate = new(1, 1);
        private readonly ConcurrentDictionary<long, PackageSubmissionPushInfo> _packageSubmissionPushItems = new();
        private readonly ConcurrentDictionary<long, byte> _reportingPackageKeys = new();
        private JtExpressDto _jtExpressDto = new();
        private IDataUploader? _submissionUploader;

        #region 非通用版本变量(临时)

        private static string _sunnenApiPackage = string.Empty;
        private bool _isWindowsClose;

        #endregion 非通用版本变量(临时)

        public SubmitApiBackgroundService(IProviderRegistry<IDataUploader> providerRegistry,
            ISettingsStore settingsStore, IImageStorageService imageStorageService,
            IMemoryCache memoryCache)
        {
            _providerRegistry = providerRegistry;
            _settingsStore = settingsStore;
            _imageStorageService = imageStorageService;
            _memoryCache = memoryCache;
            //包裹信息完成
            EventAggregator.Instance.Subscribe<PackageInfo>(item =>
            {
                if (item is { BarCodeInfo: not null } model)
                {
                    SubmitItemInfo submitItem;
                    bool shouldTrackSubmission;
                    long submissionKey;
                    lock (model.SyncRoot)
                    {
                        submitItem = new SubmitItemInfo
                        {
                            Barcode = model.BarCodeInfo?.Barcode ?? string.Empty,
                            Height = (decimal)(model.VolumeInfo?.FormattedHeight ?? 0),
                            ScanTime = model.BarCodeInfo?.ScanTime ?? DateTime.Now,
                            Weight = (decimal)(model.WeightInfo?.FormattedWeight ?? 0),
                            Length = (decimal)(model.VolumeInfo?.FormattedLength ?? 0),
                            Width = (decimal)(model.VolumeInfo?.FormattedWidth ?? 0),
                            Volume = (decimal)(model.VolumeInfo?.FormattedVolume ?? 0),
                            Guid = model.Guid,
                            IsCreatedByLowerMachine = model.IsCreatedByLowerMachine,
                            PackageCreationInstruction = model.PackageCreationInstruction ?? string.Empty,
                            PackageCreationTime = model.CreateTime,
                            IsStackedPackage = model.IsStackedPackage,
                            Timestamp = model.Timestamp,
                            LinkedCarCount = model.LinkedCarCount,
                            CameraSerialNumber =
                                model.BarCodeInfo?.SerialNumber ??
                                string.Empty,
                            Other = model.Other
                            //图片暂时不写
                        };
                        shouldTrackSubmission = model.IsCreatedByLowerMachine;
                        submissionKey = new DateTimeOffset(model.CreateTime).ToUnixTimeMilliseconds();
                    }

                    EnqueueWork(_submitItems, submitItem);
                    //添加到推送队列
                    if (shouldTrackSubmission)
                    {
                        var added = _packageSubmissionPushItems.TryAdd(
                            submissionKey,
                            new PackageSubmissionPushInfo()
                            {
                                PackageInfo = model
                            });
                        if (added)
                        {
                            LogManager.GetCurrentClassLogger().Info(
                                $"已登记包裹落格回传:Timestamp={submissionKey},WaybillNo={submitItem.Barcode},UploaderReady={_submissionUploader is not null}");
                            SignalWork();
                        }
                        else
                        {
                            LogManager.GetCurrentClassLogger().Warn(
                                $"包裹落格回传记录已存在:Timestamp={submissionKey},WaybillNo={submitItem.Barcode}");
                        }
                    }
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item =>
            {
                if (!await _settingsUpdateGate.WaitAsync(
                        TimeSpan.FromSeconds(30)))
                {
                    LogManager.GetCurrentClassLogger().Warn(
                        $"等待接口配置刷新超时:{item.SettingsName}");
                    return;
                }
                try
                {
                    if (item is { } model)
                    {
                        switch (model.SettingsName)
                        {
                            case "ApiSettings":
                                _apiSettingsDto = await _settingsStore.GetAsync<ApiSettingsDto>(model.SettingsName) ?? new ApiSettingsDto();
                                _submissionUploader = _apiSettingsDto?.Type switch
                                {
                                    ApiType.CaiNiaoApi => ResolveUploader(ApiType.CaiNiaoApi),
                                    ApiType.JtExpressApi => ResolveUploader(ApiType.JtExpressApi),
                                    ApiType.JtPolarDayApi => ResolveUploader(ApiType.JtPolarDayApi),
                                    ApiType.PostInApi => ResolveUploader(ApiType.PostInApi),
                                    ApiType.PostApi => ResolveUploader(ApiType.PostApi),
                                    _ => null
                                };

                                break;

                            case "DefaultApiParameters":
                                {
                                    //默认上传接口改参数
                                    var entity = await _settingsStore.GetAsync<DefaultApiDto>(model.SettingsName) ?? new DefaultApiDto();
                                    _defaultApiParameters = new DefaultApi.DefaultApiParameters()
                                    {
                                        CompleteMatch = entity.CompleteMatch,
                                        IsUseJsonUpload = entity.IsUseJsonUpload,
                                        JsonTemplate = entity.JsonTemplate,
                                        RegularExpression = entity.RegularExpression,
                                        StringContains = entity.StringContains,
                                        Timeout = entity.Timeout,
                                        StringTemplate = entity.StringTemplate,
                                        Url = entity.Url,
                                        ValidationMode = (int)entity.ValidationMode,
                                    };
                                    break;
                                }
                            case "SzjyApiParameters":
                                {
                                    //默认上传接口改参数
                                    var entity = await _settingsStore.GetAsync<SzjyApiDto>(model.SettingsName) ?? new SzjyApiDto();
                                    _szjyApiParam = new SzjyApi.ApiParameter()
                                    {
                                        Machine = entity.Machine,
                                        Password = entity.Password,
                                        TimeOut = entity.TimeOut,
                                        UserName = entity.UserName,
                                        Url = entity.Url,
                                    };
                                    break;
                                }
                            case "WdtWmsApiParameters":
                                {
                                    //默认上传接口改参数
                                    var entity = await _settingsStore.GetAsync<WdtWmsApiDto>(model.SettingsName) ?? new WdtWmsApiDto();
                                    _wdtWmsApiParameter = new WdtWmsApi.ApiParameter
                                    {
                                        AppKey = entity.AppKey,
                                        AppSecret = entity.AppSecret,
                                        TimeOut = entity.TimeOut,
                                        Method = entity.Method,
                                        Url = entity.Url,
                                        Sid = entity.Sid,
                                        MustIncludeBoxBarcode = entity.MustIncludeBoxBarcode
                                    };
                                    break;
                                }
                            case "WdtFlagshipApiParameters":
                                {
                                    //默认上传接口改参数
                                    var entity = await _settingsStore.GetAsync<WdtFlagshipApiDto>(model.SettingsName) ?? new WdtFlagshipApiDto();
                                    _wdtFlagshipApiParameter = new WdtFlagshipApi.ApiParameter
                                    {
                                        TimeOut = entity.TimeOut,
                                        Method = entity.Method,
                                        Url = entity.Url,
                                        Sid = entity.Sid,
                                        Appsecret = entity.Appsecret,
                                        Force = entity.Force,
                                        Key = entity.Key,
                                        OperateTableName = entity.OperateTableName,
                                        PackagerId = entity.PackagerId,
                                        PackagerNo = entity.PackagerNo,
                                        Salt = entity.Salt,
                                        V = entity.V
                                    };
                                    break;
                                }
                            case "JtExpressApiParameters":
                                //默认上传接口改参数
                                _jtExpressDto = await _settingsStore.GetAsync<JtExpressDto>(model.SettingsName) ?? new JtExpressDto();
                                _jtExpressApiParam = new JtExpressApi.ApiParameter
                                {
                                    AppSecret = _jtExpressDto.AppSecret,
                                    AppKey = _jtExpressDto.AppKey,
                                    BusinessType = (JtExpressApi.BusinessType)_jtExpressDto.BusinessType,
                                    Password = _jtExpressDto.Password,
                                    ScanPda = _jtExpressDto.ScanPda,
                                    ScanType = _jtExpressDto.ScanType,
                                    ScanTypeCode = _jtExpressDto.ScanTypeCode,
                                    SegmentCodeTimeOut = _jtExpressDto.SegmentCodeTimeOut,
                                    SegmentCodeUrl = _jtExpressDto.SegmentCodeUrl,
                                    TimeOut = _jtExpressDto.TimeOut,
                                    TransportTypeCode = _jtExpressDto.TransportTypeCode,
                                    Url = _jtExpressDto.Url,
                                    UserName = _jtExpressDto.UserName,
                                    WeightFlag = _jtExpressDto.WeightFlag,
                                    InterceptorEnabled = _jtExpressDto.InterceptorEnabled
                                };
                                break;

                            case "JtPolarDayApiParameters":
                                {
                                    var entity = await _settingsStore
                                        .GetAsync<JtPolarDayDto>(
                                            model.SettingsName) ??
                                                 new JtPolarDayDto();
                                    _jtPolarDayApiParam =
                                        CreateJtPolarDayParameters(entity);
                                    break;
                                }

                            case "RoutDataApiParameters":
                                {
                                    var entity = await _settingsStore.GetAsync<RoutDataApiDto>(model.SettingsName) ?? new RoutDataApiDto();
                                    _rstDataApiParam = new RoutDataApi.ApiParameters()
                                    {
                                        Url = entity.Url,
                                        TimeOut = entity.TimeOut,
                                        DeviceCode = entity.DeviceCode,
                                        RetryCount = entity.RetryCount,
                                        RetryInterval = entity.RetryInterval,
                                        SignKey = entity.SignKey,
                                        OrgCode = entity.OrgCode
                                    };
                                    break;
                                }
                            case "CaiNiaoApiParameters":
                                {
                                    var entity = await _settingsStore.GetAsync<CaiNiaoApiDto>(model.SettingsName) ?? new CaiNiaoApiDto();
                                    _caiNiaoApiParam = new CaiNiaoApi.ApiParameters()
                                    {
                                        BcrName = entity.BcrName,
                                        BcrCode = entity.BcrCode,
                                        Source = entity.Source,
                                        TimeOut = entity.TimeOut,
                                        Url = entity.Url,
                                        Version = entity.Version
                                    };
                                    break;
                                }
                            case "EshippingitApiParameters":
                                {
                                    var entity = await _settingsStore.GetAsync<EshippingitApiDto>(model.SettingsName) ?? new EshippingitApiDto();
                                    _eshippingitApiParam = new EshippingitApi.ApiParameters()
                                    {
                                        Authorization = entity.Authorization,
                                        BucketName = entity.BucketName,
                                        Domain = entity.Domain,
                                        Endpoint = entity.Endpoint,
                                        RetryCount = entity.RetryCount,
                                        RetryInterval = entity.RetryInterval,
                                        TimeOut = entity.TimeOut,
                                        Machine = entity.Machine
                                    };
                                    break;
                                }
                            case "JushuitanErpApiParameters":
                                {
                                    var entity = await _settingsStore.GetAsync<JushuitanErpApiDto>(model.SettingsName) ?? new JushuitanErpApiDto();
                                    _jushuitanErpParam = new JushuitanErpApi.ApiParameters()
                                    {
                                        AppKey = entity.AppKey,
                                        AccessToken = entity.AccessToken,
                                        AppSecret = entity.AppSecret,
                                        IsUnLid = entity.IsUnLid,
                                        IsUploadWeight = entity.IsUploadWeight,
                                        Type = entity.Type,
                                        Channel = entity.Channel,
                                        TimeOut = entity.TimeOut,
                                        Url = entity.Url,
                                        Version = entity.Version,
                                        TokenExpireTime = entity.TokenExpireTime,
                                        LastTokenUpdateTime = entity.LastTokenUpdateTime,
                                    };
                                    break;
                                }
                            case "ZhouYiApiParameters":
                                {
                                    var entity = await _settingsStore.GetAsync<ZhouYiApiDto>(model.SettingsName) ?? new ZhouYiApiDto();
                                    _zhouYiApiParam = new ZhouYiApi.ApiParameters()
                                    {
                                        AppKey = entity.AppKey,
                                        ApplicationCode = entity.ApplicationCode,
                                        NeedUpload = entity.NeedUpload,
                                        IsFstCode = entity.IsFstCode,

                                        TimeOut = entity.TimeOut,
                                        Url = entity.Url,
                                    };
                                    break;
                                }
                        }
                    }
                }
                catch (Exception e)
                {
                    LogManager.GetCurrentClassLogger()
                        .Error(e, $"更新接口配置失败:{item.SettingsName}");
                }
                finally
                {
                    _settingsUpdateGate.Release();
                }
            });
            EventAggregator.Instance.Subscribe<PluginParamChangedEvent>(item =>
            {
                if (item is { } model)
                {
                    if (model is { Type: PluginType.HomeTool, PluginName: "SunnenPlugin" })
                    {
                        _sunnenApiPackage = model.Content;
                    }
                }
            });
            _imageStorageService.ImageSaved += delegate (object? sender, ImageSavedEventArgs args)
            {
                //保存后触发
                var savedImageInfo = new SavedImageInfo()
                {
                    PackageTimestamp = args.PackageTimestamp,
                    BarCode = args.BarCode,
                    FilePath = args.FilePath,
                    ImageType = args.ImageType,
                    CameraSerialNumber = args.CameraSerialNumber ?? string.Empty,
                    ScanTime = args.ScanTime,
                };
                EnqueueWork(_savedImageItems, savedImageInfo);
                if (_apiSettingsDto?.Type == ApiType.JtPolarDayApi &&
                    args.PackageTimestamp > 0 &&
                    args.ImageType == SaveImageType.BarcodeImage)
                {
                    _memoryCache.Set(
                        CreateJtPolarDayImageCacheKey(args.PackageTimestamp),
                        savedImageInfo,
                        new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow =
                                TimeSpan.FromMinutes(10)
                        });
                }
            };
            EventAggregator.Instance.Subscribe<WindowsAction>(item =>
            {
                if (item is WindowsAction { Type: WindowsActionType.Close })
                {
                    _isWindowsClose = true;
                }
            });
            //集包推送
            EventAggregator.Instance.Subscribe<PackageAggregationInfo>(item =>
            {
                //加入队列
                if (item is { } info)
                {
                    EnqueueWork(_packageAggregationInfoItems, info);
                }
            });
            //更新上传状态
            EventAggregator.Instance.Subscribe<ApiResponseReceived>(item =>
            {
                if (item is { } model)
                {
                    if (_packageSubmissionPushItems.TryGetValue(model.Timestamp, out var value))
                    {
                        // 引用以原子方式替换；热回调不等待上传工作器。
                        value.ApiResponse = model;
                        SignalWork();
                    }
                }
            });
            //系统信息
            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(item =>
            {
                if (item is { Status: ApplicationStatus.Stop })
                {
                    _packageSubmissionPushItems.Clear();
                    _packageAggregationInfoItems.Clear();
                }
            });
            //更新格口信息
            EventAggregator.Instance.Subscribe<PackageExitUpdateEvent>(item =>
            {
                if (item is { } model)
                {
                    if (_packageSubmissionPushItems.TryGetValue(model.Timestamp, out var value))
                    {
                        // 并发队列保证上传工作器枚举时不会与热回调发生 List 竞态。
                        if (value.PackageExitUpdateItems.Count < 256)
                        {
                            value.PackageExitUpdateItems.Enqueue(model);
                            if (model.ExitType == SortingExitType.PhysicalExit ||
                                model.InstructionType is
                                    InstructionType.SignalCallback or
                                    InstructionType.PackageExceptionEx)
                            {
                                LogManager.GetCurrentClassLogger().Info(
                                    $"已匹配包裹落格回调，准备接口回传:Timestamp={model.Timestamp},ExitName={model.ExitName},InstructionType={model.InstructionType}");
                            }
                            SignalWork();
                        }
                        else
                        {
                            LogManager.GetCurrentClassLogger().Error(
                                $"单个包裹的格口事件超过上限，拒绝新事件:{model.Timestamp}");
                        }
                    }
                    else
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error($"未匹配到包裹:{model.InstructionInfos?.FirstOrDefault()?.InstructionContent} 操作指令");
                    }
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //读参数
            await ReadDefaultConfig();
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose)
            {
                await _workSignal.WaitAsync(TimeSpan.FromMilliseconds(250), stoppingToken);
                // 所有队列都由这一个工作器消费，避免未跟踪任务重入同一包裹。
                SubmitItemInfo? inFlightSubmit = null;
                SavedImageInfo? inFlightSavedImage = null;
                try
                {
                    //需要判断用户选择的接口和参数设置
                    var tryDequeue = _submitItems.TryDequeue(out var info);

                    if (tryDequeue && info is not null)
                    {
                        inFlightSubmit = info;
                        //上传
                        //判断上传接口
                        {
                            IDataUploader uploader;
                            UploadResponse? uploadResponse = null;
                            switch (_apiSettingsDto?.Type)
                            {
                                case ApiType.None:
                                    break;

                                case ApiType.DefaultApi:
                                    {
                                        //基础接口
                                        uploader = ResolveUploader(ApiType.DefaultApi);
                                        //设置参数
                                        var (key, value) = await uploader.SetParameters(_defaultApiParameters);
                                        if (key)
                                        {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                null, null,
                                                null, stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse = new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                            Console.WriteLine("设置参数失败!");
                                        }

                                        break;
                                    }
                                case ApiType.SunnenApi:
                                    {
                                        uploader = ResolveUploader(ApiType.SunnenApi);
                                        uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                            info.Weight, info.ScanTime,
                                            info.Length, info.Width,
                                            info.Height, info.Volume,
                                            null, null,
                                            _sunnenApiPackage, stoppingToken);
                                        break;
                                    }
                                case ApiType.SzjyApi:
                                    {
                                        //神州集运后台
                                        uploader = ResolveUploader(ApiType.SzjyApi);
                                        var (key, value) = await uploader.SetParameters(_szjyApiParam);
                                        if (key)
                                        {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                null, null,
                                                null, stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse = new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                            Console.WriteLine("设置参数失败!");
                                        }
                                        break;
                                    }
                                case ApiType.WdtWmsApi:
                                    {
                                        uploader = ResolveUploader(ApiType.WdtWmsApi);
                                        var (key, value) = await uploader.SetParameters(_wdtWmsApiParameter);
                                        if (key)
                                        {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                null, null,
                                                info.Other, stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse = new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                            Console.WriteLine("设置参数失败!");
                                        }
                                        break;
                                    }
                                case ApiType.WdtErpFlagShipApi:
                                    {
                                        uploader = ResolveUploader(ApiType.WdtErpFlagShipApi);
                                        var (key, value) = await uploader.SetParameters(_wdtFlagshipApiParameter);
                                        if (key)
                                        {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                null, null,
                                                null, stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse = new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                            Console.WriteLine("设置参数失败!");
                                        }
                                        break;
                                    }
                                case ApiType.JdyWms:
                                    {
                                        uploader = ResolveUploader(ApiType.JdyWms);
                                        uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                            info.Weight, info.ScanTime,
                                            info.Length, info.Width,
                                            info.Height, info.Volume,
                                            null, null,
                                            null, stoppingToken);
                                        break;
                                    }
                                case ApiType.JtExpressApi:
                                    {
                                        uploader = ResolveUploader(ApiType.JtExpressApi);
                                        var (key, value) = await uploader.SetParameters(_jtExpressApiParam);
                                        if (key)
                                        {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                null, null,
                                                null, stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse = new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                            Console.WriteLine("设置参数失败!");
                                        }

                                        break;
                                    }
                                case ApiType.JtPolarDayApi:
                                    {
                                        uploader = ResolveUploader(ApiType.JtPolarDayApi);
                                        var (key, value) =
                                            await uploader.SetParameters(
                                                _jtPolarDayApiParam);
                                        if (key)
                                        {
                                            uploadResponse =
                                                await uploader.UploadData(
                                                    info.Barcode ??
                                                    string.Empty,
                                                    info.Weight,
                                                    info.ScanTime,
                                                    info.Length,
                                                    info.Width,
                                                    info.Height,
                                                    info.Volume,
                                                    new UploadImageInfo
                                                    {
                                                        CameraCustomName =
                                                            info.CameraSerialNumber,
                                                        CameraName =
                                                            info.CameraSerialNumber,
                                                        CameraSerialNumber =
                                                            info.CameraSerialNumber
                                                    },
                                                    null,
                                                    info.Other,
                                                    stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse =
                                                new UploadResponse
                                                {
                                                    ExceptionMsg = value,
                                                    ApiExceptionType =
                                                        JayTom.Dws.Interface.ApiExceptionType
                                                            .LogicValidationFailed
                                                };
                                        }

                                        break;
                                    }
                                case ApiType.RoutDataApi:
                                    {
                                        uploader = ResolveUploader(ApiType.RoutDataApi);
                                        var (key, value) = await uploader.SetParameters(_rstDataApiParam);
                                        if (key)
                                        {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                null, null,
                                                null, stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse = new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                            Console.WriteLine("设置参数失败!");
                                        }
                                        break;
                                    }
                                case ApiType.GeekPlusApi:
                                    {
                                        uploader = ResolveUploader(ApiType.GeekPlusApi);
                                        var (key, value) = await uploader.SetParameters(_rstDataApiParam);
                                        if (key)
                                        {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                null, null,
                                                null, stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse = new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                            Console.WriteLine("设置参数失败!");
                                        }
                                        break;
                                    }
                                case ApiType.CaiNiaoApi:
                                    {
                                        uploader = ResolveUploader(ApiType.CaiNiaoApi);
                                        var (key, value) = await uploader.SetParameters(_caiNiaoApiParam);
                                        if (key)
                                        {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                null, null,
                                                info.IsStackedPackage, stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse = new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                            Console.WriteLine("设置参数失败!");
                                        }
                                        break;
                                    }
                                case ApiType.EshippingitApi:
                                    {
                                        uploader = ResolveUploader(ApiType.EshippingitApi);
                                        var (key, value) = await uploader.SetParameters(_eshippingitApiParam);
                                        if (key)
                                        {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                null, null,
                                                info.IsStackedPackage, stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse = new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                            Console.WriteLine("设置参数失败!");
                                        }
                                        break;
                                    }
                                case ApiType.PostApi:
                                    {
                                        uploader = ResolveUploader(ApiType.PostApi);
                                        var (key, value) = await uploader.SetParameters(_postApiParam);
                                        if (key)
                                        {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                null, null,
                                                info.IsStackedPackage, stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse = new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                            Console.WriteLine("设置参数失败!");
                                        }
                                    }

                                    break;

                                case ApiType.PostInApi:
                                    {
                                        uploader = ResolveUploader(ApiType.PostInApi);
                                        var (key, value) = await uploader.SetParameters(_postInApiParam);
                                        if (key)
                                        {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                null, null,
                                                info.IsStackedPackage, stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse = new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                            Console.WriteLine("设置参数失败!");
                                        }
                                    }

                                    break;

                                case ApiType.ZhuoYanScm:
                                    {
                                        uploader = ResolveUploader(ApiType.ZhuoYanScm);
                                        var (key, value) = await uploader.SetParameters(_zhuoYanScmApiParam);

                                        if (key)
                                        {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                null, null,
                                                info.IsStackedPackage, stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse = new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                            Console.WriteLine("设置参数失败!");
                                        }
                                    }
                                    break;

                                case ApiType.TtxApi:
                                    {
                                        uploader = ResolveUploader(ApiType.TtxApi);
                                        uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                            info.Weight, info.ScanTime,
                                            info.Length, info.Width,
                                            info.Height, info.Volume,
                                            null, null,
                                            info.IsStackedPackage, stoppingToken);
                                    }
                                    break;

                                case ApiType.WdtWmsApiAndTtxApi:
                                    {
                                        using var cancellationTokenSource =
                                            CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                                        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(2));

                                        /// <summary>
                                        /// 提交当前包裹到旺店通接口。
                                        /// </summary>
                                        async Task<UploadResponse> UploadToWdtAsync()
                                        {
                                            var apiUploader = ResolveUploader(ApiType.WdtWmsApi);
                                            var (key, value) = await apiUploader.SetParameters(_wdtWmsApiParameter);
                                            if (key)
                                            {
                                                return await apiUploader.UploadData(info.Barcode ?? string.Empty,
                                                     info.Weight, info.ScanTime,
                                                     info.Length, info.Width,
                                                     info.Height, info.Volume,
                                                     null, null,
                                                     info.Other, cancellationTokenSource.Token);
                                            }

                                            Console.WriteLine("设置参数失败!");
                                            return new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                        }

                                        /// <summary>
                                        /// 提交当前包裹到 TTX 接口。
                                        /// </summary>
                                        async Task<UploadResponse> UploadToTtxAsync()
                                        {
                                            var apiUploader = ResolveUploader(ApiType.TtxApi);
                                            return await apiUploader.UploadData(info.Barcode ?? string.Empty,
                                                 info.Weight, info.ScanTime,
                                                 info.Length, info.Width,
                                                 info.Height, info.Volume,
                                                 null, null,
                                                 info.IsStackedPackage, cancellationTokenSource.Token);
                                        }

                                        var wdtTask = UploadToWdtAsync();
                                        var ttxTask = UploadToTtxAsync();
                                        var completedTask = await Task.WhenAny(wdtTask, ttxTask);
                                        var firstResponse = await completedTask;

                                        if (firstResponse.IsSuccess)
                                        {
                                            uploadResponse = firstResponse;
                                            cancellationTokenSource.Cancel();
                                            var remainingTask =
                                                ReferenceEquals(completedTask, wdtTask) ? ttxTask : wdtTask;
                                            _ = remainingTask.ContinueWith(
                                                static task => _ = task.Exception,
                                                CancellationToken.None,
                                                TaskContinuationOptions.OnlyOnFaulted |
                                                TaskContinuationOptions.ExecuteSynchronously,
                                                TaskScheduler.Default);
                                        }
                                        else
                                        {
                                            var remainingTask =
                                                ReferenceEquals(completedTask, wdtTask) ? ttxTask : wdtTask;
                                            var secondResponse = await remainingTask;
                                            // 任一接口成功都视为成功；两者均失败时保持原有的 WDT 结果优先规则。
                                            uploadResponse = secondResponse.IsSuccess
                                                ? secondResponse
                                                : await wdtTask;
                                        }
                                    }
                                    break;

                                case ApiType.Jushuitan:
                                    {
                                        uploader = ResolveUploader(ApiType.Jushuitan);
                                        var (key, value) = await uploader.SetParameters(_jushuitanErpParam);
                                        if (key)
                                        {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                null, null,
                                                null, stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse = new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                            Console.WriteLine("设置参数失败!");
                                        }
                                    }
                                    break;

                                case ApiType.ZhouYi:
                                    {
                                        uploader = ResolveUploader(ApiType.ZhouYi);
                                        var (key, value) = await uploader.SetParameters(_zhouYiApiParam);
                                        if (key)
                                        {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                null, null,
                                                null, stoppingToken);
                                        }
                                        else
                                        {
                                            uploadResponse = new UploadResponse()
                                            {
                                                ExceptionMsg = value
                                            };
                                            Console.WriteLine("设置参数失败!");
                                        }
                                    }
                                    break;
                            }
                            if (_apiSettingsDto?.Type is not null &&
                                _apiSettingsDto.Type != ApiType.None)
                            {
                                //临时单线程
                                EventAggregator.Instance.Publish(new ApiResponseReceived
                                {
                                    Guid = info.Guid,
                                    Barcode = info.Barcode,
                                    ScanTime = info.ScanTime,
                                    UploadResponse = uploadResponse,
                                    PackageCreationInstruction = info.PackageCreationInstruction,
                                    PackageCreationTime = info.PackageCreationTime,
                                    IsCreatedByLowerMachine = info.IsCreatedByLowerMachine,
                                    IsStackedPackage = info.IsStackedPackage ?? false,
                                    Timestamp = info.Timestamp,
                                    LinkedCarCount = info.LinkedCarCount
                                });
                                EventAggregator.Instance.Publish(new TriggerPositionEvent()
                                {
                                    IsSuccess = uploadResponse?.IsSuccess ?? false,
                                    TriggerPosition = TriggerPositionEnum.HttpOutput
                                });
                            }
                        }
                        inFlightSubmit = null;
                    }

                    //取出图片
                    var dequeue = _savedImageItems.TryDequeue(out var model);
                    if (dequeue && model is not null && !string.IsNullOrEmpty(model.FilePath) &&
                        model.ImageType == SaveImageType.BarcodeImage)
                    {
                        inFlightSavedImage = model;
                        {
                            //后续上传
                            IDataUploader uploader;
                            switch (_apiSettingsDto?.Type)
                            {
                                case ApiType.None:
                                    break;

                                case ApiType.GeekPlusApi:

                                    uploader = ResolveUploader(ApiType.GeekPlusApi);
                                    using (var uploadImage = LoadImageSnapshot(model.FilePath))
                                    {
                                        await uploader.UploadInBackground(model.BarCode ?? string.Empty, 0,
                                            model.ScanTime, imageInfo: new UploadImageInfo()
                                            {
                                                CameraCustomName = model.CameraSerialNumber,
                                                CameraName = model.CameraSerialNumber,
                                                CameraSerialNumber = model.CameraSerialNumber,
                                                Image = uploadImage is null
                                                    ? null
                                                    : ImageHandle.TakeOwnership(
                                                        (Image)uploadImage.Clone())
                                            }, token: stoppingToken);
                                    }
                                    break;

                                case ApiType.EshippingitApi:
                                    uploader = ResolveUploader(ApiType.EshippingitApi);
                                    var (key, value) = await uploader.SetParameters(_eshippingitApiParam);
                                    if (key)
                                    {
                                        using var uploadImage = LoadImageSnapshot(model.FilePath);
                                        await uploader.UploadInBackground(model.BarCode ?? string.Empty, 0,
                                            model.ScanTime, imageInfo: new UploadImageInfo()
                                            {
                                                CameraCustomName = model.CameraSerialNumber,
                                                CameraName = model.CameraSerialNumber,
                                                CameraSerialNumber = model.CameraSerialNumber,
                                                Image = uploadImage is null
                                                    ? null
                                                    : ImageHandle.TakeOwnership(
                                                        (Image)uploadImage.Clone())
                                            }, token: stoppingToken);
                                    }
                                    else
                                    {
                                        LogManager.GetCurrentClassLogger().Error("设置参数失败!");
                                    }

                                    break;
                            }
                        }
                        inFlightSavedImage = null;
                    }

                    //获取需要提交到备用格口的数据

                    //获取包裹
                    var pairs = _packageSubmissionPushItems
                        .Where(pair =>
                            pair.Value.PackageExitUpdateItems.Count > 0 &&
                            pair.Value.PackageInfo is not null &&
                            pair.Value.WaitSubmitTime <= DateTime.Now)
                        .ToArray();

                    if (pairs.Length > 0)
                    {
                        // 同一次循环使用一致的接口类型和上传器，避免配置热切换造成类型错配。
                        var submissionUploader = _submissionUploader;
                        var submissionApiType = _apiSettingsDto?.Type;
                        if (submissionUploader is not null &&
                            submissionApiType.HasValue)
                        {
                            if (submissionApiType == ApiType.JtPolarDayApi)
                            {
                                await Parallel.ForEachAsync(
                                    pairs,
                                    new ParallelOptions
                                    {
                                        MaxDegreeOfParallelism = 4,
                                        CancellationToken = stoppingToken
                                    },
                                    (pair, cancellationToken) =>
                                        new ValueTask(ReportProgressAsync(
                                            pair,
                                            submissionUploader,
                                            submissionApiType.Value,
                                            cancellationToken)));
                            }
                            else
                            {
                                foreach (var pair in pairs)
                                {
                                    await ReportProgressAsync(
                                        pair,
                                        submissionUploader,
                                        submissionApiType.Value,
                                        stoppingToken);
                                }
                            }
                        }
                    }
                    //集包
                    var packageAggregationDequeue = _packageAggregationInfoItems.TryDequeue(out var packageAggregationInfo);
                    if (packageAggregationDequeue && packageAggregationInfo is not null)
                    {
                        //集包推送（判断需要使用的 API）
                        switch (_apiSettingsDto?.Type)
                        {
                            case ApiType.None:
                                return;

                            case ApiType.CaiNiaoApi:
                                IDataUploader uploader =
                                    ResolveUploader(ApiType.CaiNiaoApi);
                                var (key, _) = await uploader
                                    .SetParameters(_caiNiaoApiParam);
                                if (key)
                                {
                                    await uploader.PackageAggregation(
                                        packageAggregationInfo
                                            .PackageExitDefinitionInfo
                                            .ExitName,
                                        packageAggregationInfo
                                            .AggregatePackageCode,
                                        packageAggregationInfo.PackagingTime,
                                        [.. packageAggregationInfo
                                            .PackageItems
                                            .Select(item =>
                                                item.BarCodeInfo?.Barcode ??
                                                string.Empty)],
                                        token: stoppingToken);
                                }
                                else
                                {
                                    Console.WriteLine("设置参数失败!");
                                }
                                break;
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    if (inFlightSubmit is not null)
                    {
                        RequeueWork(_submitItems, inFlightSubmit);
                    }
                    if (inFlightSavedImage is not null)
                    {
                        RequeueWork(_savedImageItems, inFlightSavedImage);
                    }
                    LogManager.GetCurrentClassLogger().Error($"{e}");
                }
                finally
                {
                    if (!_submitItems.IsEmpty || !_savedImageItems.IsEmpty ||
                        !_packageAggregationInfoItems.IsEmpty)
                    {
                        SignalWork();
                    }
                }
            }
        }

        /// <summary>
        /// 将接口工作加入有界并发队列并唤醒消费者。
        /// </summary>
        private void EnqueueWork<T>(BoundedWorkQueue<T> queue, T item) where T : class
        {
            if (!queue.TryEnqueue(item))
            {
                LogManager.GetCurrentClassLogger().Error(
                    $"接口提交队列已达到上限 {MaxQueueLength}，拒绝新任务:{typeof(T).Name}");
                return;
            }

            SignalWork();
        }

        /// <summary>
        /// 对失败的接口工作执行有限次数重试。
        /// </summary>
        private void RequeueWork<T>(BoundedWorkQueue<T> queue, T item) where T : class
        {
            if (!_retryTracker.TryRegisterFailure(item, out _))
            {
                LogManager.GetCurrentClassLogger().Error(
                    $"接口任务超过最大重试次数 {_retryTracker.MaxAttempts}，停止重试:{typeof(T).Name}");
                return;
            }

            EnqueueWork(queue, item);
        }

        /// <summary>
        /// 唤醒接口工作消费者且不累积无意义的通知。
        /// </summary>
        private void SignalWork()
        {
            if (_workSignal.CurrentCount == 0)
            {
                try
                {
                    _workSignal.Release();
                }
                catch (SemaphoreFullException)
                {
                    // 其他生产者已经完成通知。
                }
            }
        }

        /// <summary>
        /// 通过集中注册表创建指定类型的上传提供商。
        /// </summary>
        private IDataUploader ResolveUploader(ApiType apiType)
        {
            if (_providerRegistry.TryResolve(apiType.ToString(), out var uploader) &&
                uploader is not null)
            {
                return uploader;
            }

            throw new InvalidOperationException($"未注册上传提供商: {apiType}");
        }

        private async Task ReadDefaultConfig()
        {
            //上传类型
            _apiSettingsDto = await _settingsStore.GetAsync<ApiSettingsDto>("ApiSettings") ?? new ApiSettingsDto();

            //默认接口
            var defaultentity = await _settingsStore.GetAsync<DefaultApiDto>("DefaultApiParameters") ?? new DefaultApiDto();
            _defaultApiParameters = new DefaultApi.DefaultApiParameters()
            {
                CompleteMatch = defaultentity.CompleteMatch,
                IsUseJsonUpload = defaultentity.IsUseJsonUpload,
                JsonTemplate = defaultentity.JsonTemplate,
                RegularExpression = defaultentity.RegularExpression,
                StringContains = defaultentity.StringContains,
                Timeout = defaultentity.Timeout,
                StringTemplate = defaultentity.StringTemplate,
                Url = defaultentity.Url,
                ValidationMode = (int)defaultentity.ValidationMode,
            };
            //神州
            var szjyEntity = await _settingsStore.GetAsync<SzjyApiDto>("SzjyApiParameters") ?? new SzjyApiDto();
            _szjyApiParam = new SzjyApi.ApiParameter()
            {
                Machine = szjyEntity.Machine,
                Password = szjyEntity.Password,
                TimeOut = szjyEntity.TimeOut,
                UserName = szjyEntity.UserName,
                Url = szjyEntity.Url,
            };

            //旺店通Wms
            var wdtWmsApiDto = await _settingsStore.GetAsync<WdtWmsApiDto>("WdtWmsApiParameters") ?? new WdtWmsApiDto();

            _wdtWmsApiParameter = new WdtWmsApi.ApiParameter
            {
                AppKey = wdtWmsApiDto.AppKey,
                AppSecret = wdtWmsApiDto.AppSecret,
                TimeOut = wdtWmsApiDto.TimeOut,
                Method = wdtWmsApiDto.Method,
                Url = wdtWmsApiDto.Url,
                Sid = wdtWmsApiDto.Sid,
                MustIncludeBoxBarcode = wdtWmsApiDto.MustIncludeBoxBarcode
            };
            //旺店通旗舰版
            var wdtFlagshipApiDto = await _settingsStore.GetAsync<WdtFlagshipApiDto>("WdtFlagshipApiParameters") ?? new WdtFlagshipApiDto();

            _wdtFlagshipApiParameter = new WdtFlagshipApi.ApiParameter
            {
                TimeOut = wdtFlagshipApiDto.TimeOut,
                Method = wdtFlagshipApiDto.Method,
                Url = wdtFlagshipApiDto.Url,
                Sid = wdtFlagshipApiDto.Sid,
                Appsecret = wdtFlagshipApiDto.Appsecret,
                Force = wdtFlagshipApiDto.Force,
                Key = wdtFlagshipApiDto.Key,
                OperateTableName = wdtFlagshipApiDto.OperateTableName,
                PackagerId = wdtFlagshipApiDto.PackagerId,
                PackagerNo = wdtFlagshipApiDto.PackagerNo,
                Salt = wdtFlagshipApiDto.Salt,
                V = wdtFlagshipApiDto.V
            };
            //极兔
            _jtExpressDto = await _settingsStore.GetAsync<JtExpressDto>("JtExpressApiParameters") ?? new JtExpressDto();
            _jtExpressApiParam = new JtExpressApi.ApiParameter
            {
                AppSecret = _jtExpressDto.AppSecret,
                AppKey = _jtExpressDto.AppKey,
                BusinessType = (JtExpressApi.BusinessType)_jtExpressDto.BusinessType,
                Password = _jtExpressDto.Password,
                ScanPda = _jtExpressDto.ScanPda,
                ScanType = _jtExpressDto.ScanType,
                ScanTypeCode = _jtExpressDto.ScanTypeCode,
                SegmentCodeTimeOut = _jtExpressDto.SegmentCodeTimeOut,
                SegmentCodeUrl = _jtExpressDto.SegmentCodeUrl,
                TimeOut = _jtExpressDto.TimeOut,
                TransportTypeCode = _jtExpressDto.TransportTypeCode,
                Url = _jtExpressDto.Url,
                UserName = _jtExpressDto.UserName,
                WeightFlag = _jtExpressDto.WeightFlag,
                InterceptorEnabled = _jtExpressDto.InterceptorEnabled
            };
            var jtPolarDayDto = await _settingsStore
                .GetAsync<JtPolarDayDto>(
                    "JtPolarDayApiParameters") ??
                                    new JtPolarDayDto();
            _jtPolarDayApiParam =
                CreateJtPolarDayParameters(jtPolarDayDto);
            //络道科技Api
            var routDataApiDto = await _settingsStore.GetAsync<RoutDataApiDto>("RoutDataApiParameters") ?? new RoutDataApiDto();
            _rstDataApiParam = new RoutDataApi.ApiParameters()
            {
                Url = routDataApiDto.Url,
                TimeOut = routDataApiDto.TimeOut,
                DeviceCode = routDataApiDto.DeviceCode,
                RetryCount = routDataApiDto.RetryCount,
                RetryInterval = routDataApiDto.RetryInterval,
                SignKey = routDataApiDto.SignKey,
                OrgCode = routDataApiDto.OrgCode
            };
            //菜鸟Api
            var caiNiaoApiDto = await _settingsStore.GetAsync<CaiNiaoApiDto>("CaiNiaoApiParameters") ?? new CaiNiaoApiDto();

            _caiNiaoApiParam = new CaiNiaoApi.ApiParameters()
            {
                BcrName = caiNiaoApiDto.BcrName,
                BcrCode = caiNiaoApiDto.BcrCode,
                Source = caiNiaoApiDto.Source,
                TimeOut = caiNiaoApiDto.TimeOut,
                Url = caiNiaoApiDto.Url,
                Version = caiNiaoApiDto.Version
            };
            //海通智运Api
            var eshippingitApiDto = await _settingsStore.GetAsync<EshippingitApiDto>("EshippingitApiParameters") ?? new EshippingitApiDto();
            _eshippingitApiParam = new EshippingitApi.ApiParameters()
            {
                Authorization = eshippingitApiDto.Authorization,
                BucketName = eshippingitApiDto.BucketName,
                Domain = eshippingitApiDto.Domain,
                Endpoint = eshippingitApiDto.Endpoint,
                RetryCount = eshippingitApiDto.RetryCount,
                RetryInterval = eshippingitApiDto.RetryInterval,
                TimeOut = eshippingitApiDto.TimeOut,
                Machine = eshippingitApiDto.Machine
            };
            var jushuitanErpApiDto = await _settingsStore.GetAsync<JushuitanErpApiDto>("JushuitanErpApiParameters") ?? new JushuitanErpApiDto();
            _jushuitanErpParam = new JushuitanErpApi.ApiParameters()
            {
                AppKey = jushuitanErpApiDto.AppKey,
                AccessToken = jushuitanErpApiDto.AccessToken,
                AppSecret = jushuitanErpApiDto.AppSecret,
                IsUnLid = jushuitanErpApiDto.IsUnLid,
                IsUploadWeight = jushuitanErpApiDto.IsUploadWeight,
                Type = jushuitanErpApiDto.Type,
                Channel = jushuitanErpApiDto.Channel,
                TimeOut = jushuitanErpApiDto.TimeOut,
                Url = jushuitanErpApiDto.Url,
                Version = jushuitanErpApiDto.Version,
                TokenExpireTime = jushuitanErpApiDto.TokenExpireTime,
                LastTokenUpdateTime = jushuitanErpApiDto.LastTokenUpdateTime,
            };
            var zhouYiApiDto = await _settingsStore.GetAsync<ZhouYiApiDto>("ZhouYiApiParameters") ?? new ZhouYiApiDto();
            _zhouYiApiParam = new ZhouYiApi.ApiParameters()
            {
                AppKey = zhouYiApiDto.AppKey,
                ApplicationCode = zhouYiApiDto.ApplicationCode,
                NeedUpload = zhouYiApiDto.NeedUpload,
                IsFstCode = zhouYiApiDto.IsFstCode,

                TimeOut = zhouYiApiDto.TimeOut,
                Url = zhouYiApiDto.Url,
            };
            _submissionUploader = _apiSettingsDto?.Type switch
            {
                ApiType.CaiNiaoApi => ResolveUploader(ApiType.CaiNiaoApi),
                ApiType.JtExpressApi => ResolveUploader(ApiType.JtExpressApi),
                ApiType.JtPolarDayApi => ResolveUploader(ApiType.JtPolarDayApi),
                ApiType.PostInApi => ResolveUploader(ApiType.PostInApi),
                ApiType.PostApi => ResolveUploader(ApiType.PostApi),
                _ => null
            };
            LogManager.GetCurrentClassLogger().Info(
                $"接口回传初始化完成:ApiType={_apiSettingsDto?.Type},UploaderReady={_submissionUploader is not null},JtPolarDayBaseUrl={_jtPolarDayApiParam.BaseUrl}");
        }

        /// <summary>
        /// 将持久化配置转换为极昼接口参数快照。
        /// </summary>
        /// <param name="settings">持久化配置。</param>
        /// <returns>极昼接口参数。</returns>
        private static JtPolarDayApi.ApiParameter CreateJtPolarDayParameters(
            JtPolarDayDto settings)
        {
            return new JtPolarDayApi.ApiParameter
            {
                BaseUrl = JtPolarDayApi.NormalizeProductionBaseUrl(
                    settings.BaseUrl),
                AppKey = settings.AppKey,
                AppSecret = settings.AppSecret,
                ImageServiceBaseUrl = DefaultIfBlank(
                    settings.ImageServiceBaseUrl,
                    JtPolarDayApi.DefaultImageServiceBaseUrl),
                ImageAccount = settings.ImageAccount,
                ImagePassword = settings.ImagePassword,
                ImageAppKey = settings.ImageAppKey,
                ImageAppSecret = settings.ImageAppSecret,
                ImageScanType = settings.ImageScanType,
                ImageUploadTimeoutMilliseconds =
                    settings.ImageUploadTimeoutMilliseconds,
                SiteCode = DefaultIfBlank(
                    settings.SiteCode,
                    JtPolarDayApi.DefaultSiteCode),
                EquipmentCode = DefaultIfBlank(
                    settings.EquipmentCode,
                    JtPolarDayApi.DefaultEquipmentCode),
                SortingPlanCode = DefaultIfBlank(
                    settings.SortingPlanCode,
                    JtPolarDayApi.DefaultSortingPlanCode),
                OperateType = settings.OperateType,
                Operator = DefaultIfBlank(
                    settings.Operator,
                    JtPolarDayApi.DefaultOperator),
                MainLineCode = settings.MainLineCode,
                EquipmentLayer = settings.EquipmentLayer,
                AreaNum = settings.AreaNum,
                MaxCircleNum = settings.MaxCircleNum,
                SupplyDeskCode = settings.SupplyDeskCode,
                SupplyDeskSerialNo = settings.SupplyDeskSerialNo,
                SupplyDeskMethod = settings.SupplyDeskMethod,
                SupplyDeskArea = settings.SupplyDeskArea,
                LayerNum = settings.LayerNum,
                ChuteModel = settings.ChuteModel,
                FallArea = settings.FallArea,
                WeightSource = settings.WeightSource,
                QueryTimeoutMilliseconds =
                    settings.QueryTimeoutMilliseconds,
                TimeoutMilliseconds = settings.TimeoutMilliseconds,
                RetryCount = settings.RetryCount,
                RetryIntervalMilliseconds =
                    settings.RetryIntervalMilliseconds
            };
        }

        /// <summary>
        /// 空配置使用指定默认值，非空配置保持不变。
        /// </summary>
        private static string DefaultIfBlank(
            string? value,
            string defaultValue)
        {
            return string.IsNullOrWhiteSpace(value)
                ? defaultValue
                : value;
        }

        /// <summary>
        /// 将本地分拣异常转换为极昼格口分类码。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="packageExitItems">落格事件。</param>
        /// <returns>极昼格口分类码。</returns>
        private static string ConvertToPolarDayGridCode(
            string barcode,
            IEnumerable<PackageExitUpdateEvent> packageExitItems)
        {
            if (string.Equals(
                    barcode,
                    "noread",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "993";
            }

            var abnormalTypes = packageExitItems
                .Select(item => item.PackageAbnormalSortingType)
                .ToHashSet();
            if (abnormalTypes.Contains(
                    PackageAbnormalSortingType.MultipleBarCode))
            {
                return "994";
            }

            if (abnormalTypes.Contains(
                    PackageAbnormalSortingType.NoSortingInstruction))
            {
                return "996";
            }

            if (abnormalTypes.Contains(
                    PackageAbnormalSortingType.NetworkTimeout) ||
                abnormalTypes.Contains(
                    PackageAbnormalSortingType.ApiAccessError))
            {
                return "985";
            }

            return abnormalTypes.All(
                type => type == PackageAbnormalSortingType.None)
                ? "111"
                : "992";
        }

        private KeyValuePair<int, CaiNiaoExitInfo> CaiNiaoStatusConvert(
            string barcode,
            IEnumerable<PackageExitUpdateEvent> packageExitItems)
        {
            if (packageExitItems.Any(a => (int)a.PackageAbnormalSortingType == (int)PackageAbnormalSortingType.LockExit) == true)
            {
                return new KeyValuePair<int, CaiNiaoExitInfo>(3, new CaiNiaoExitInfo()
                {
                    ErrorReson = "锁格",
                    ChuteCode = packageExitItems?.FirstOrDefault(f =>
                        f.PackageAbnormalSortingType ==
                        PackageAbnormalSortingType.LockExit)?.ExitName ?? string.Empty
                });
            }

            var exitUpdateEvent = packageExitItems?.FirstOrDefault(f => f.InstructionType == InstructionType.PackageException);
            if (exitUpdateEvent is not null)
            {
                return new KeyValuePair<int, CaiNiaoExitInfo>(6, new CaiNiaoExitInfo()
                {
                    ChuteCode = exitUpdateEvent.ExitName,
                    ErrorReson = exitUpdateEvent.PackageAbnormalSortingType.GetDescription()
                });
            }

            if (packageExitItems?.Any(a => a.PackageAbnormalSortingType
                                          == PackageAbnormalSortingType.LockExit) != true &&
                packageExitItems?.Any(a => a.InstructionType ==
                                          InstructionType.SignalCallback) != true)
            {
                return new KeyValuePair<int, CaiNiaoExitInfo>(6, new CaiNiaoExitInfo()
                {
                    ChuteCode = "格口100",
                    ErrorReson = "未获取到落格信息"
                });
            }
            return barcode.ToLower().Equals("noread") ? new KeyValuePair<int, CaiNiaoExitInfo>(6, new CaiNiaoExitInfo()
            {
                ErrorReson = "无条码",
                ChuteCode = packageExitItems?.LastOrDefault()?.ExitName ?? string.Empty
            }) : new KeyValuePair<int, CaiNiaoExitInfo>(0, new CaiNiaoExitInfo()
            {
                ErrorReson = "分拣成功",
                ChuteCode = packageExitItems?.LastOrDefault()?.ExitName ?? string.Empty
            });
        }

        private async Task ReportProgressAsync(KeyValuePair<long, PackageSubmissionPushInfo> packageValue,
            IDataUploader uploader, ApiType apiType, CancellationToken token)
        {
            if (!_reportingPackageKeys.TryAdd(packageValue.Key, 0))
            {
                return;
            }

            try
            {
                //提交
                if (packageValue.Value is { PackageInfo: not null } && packageValue.Value.PackageExitUpdateItems?.Any() == true)
                {
                    switch (apiType)
                    {
                        case ApiType.CaiNiaoApi:
                            //实时方案
                            /*if (DateTime.Now.Subtract(packageValue.Value.PackageInfo.CreateTime).TotalSeconds >= 60) {
                                //实时-超时删除直接不匹配
                                _packageSubmissionPushItems?.TryRemove(packageValue);
                                NLog.LogManager.GetCurrentClassLogger().Error($"待提交的单号:{packageValue.Value.PackageInfo.BarCodeInfo?.Barcode},格口:[{packageValue.Value.PackageExitUpdateItems?.FirstOrDefault(f => f.InstructionType == InstructionType.CreatePackage)?.ExitName}],超过等待回调时间");
                                return;
                            }
                            //判断状态有完成再提交
                            if (packageValue.Value.PackageExitUpdateItems?.Any(a => a.InstructionType == InstructionType.SignalCallback &&
                                    a.InstructionType == InstructionType.PackageExceptionEx) != true) {
                                return;
                            }
                            NLog.LogManager.GetCurrentClassLogger().Error($"准备发送");
                            var (key, value) = await uploader.SetParameters(_caiNiaoApiParam);
                            if (key) {
                                var caiNiaoStatusConvert = CaiNiaoStatusConvert(
                                    packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty,
                                    packageValue.Value.PackageExitUpdateItems);
                                await uploader.UploadInBackground(packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty, packageValue.Value.PackageInfo.WeightInfo?.FormattedWeight ?? 0,
                                    packageValue.Value.PackageInfo.BarCodeInfo?.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo() {
                                        CameraCustomName = packageValue.Value.PackageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                                        CameraName = packageValue.Value.PackageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                                        CameraSerialNumber = packageValue.Value.PackageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                                    }, other: new ReportChuteInfo {
                                        ChuteCode = packageValue.Value.PackageExitUpdateItems?.LastOrDefault()?.ExitName ?? string.Empty,
                                        ChuteCodePhysical = packageValue.Value.PackageExitUpdateItems?.LastOrDefault(l => l.ExitType == SortingExitType.TheoreticalExit)?.ExitName ?? string.Empty,
                                        ErrorReson = caiNiaoStatusConvert.Value,
                                        Status = caiNiaoStatusConvert.Key,
                                    }, token: stoppingToken);
                            }
                            else {
                                NLog.LogManager.GetCurrentClassLogger().Error("设置Api参数失败");
                            }*/
                            //延迟方案
                            if (DateTime.Now.Subtract(packageValue.Value.PackageInfo.CreateTime).TotalSeconds >= 60)
                            {
                                //超时删除直接不匹配
                                /*do {
                                    if (_packageSubmissionPushItems?.ContainsKey(packageValue.Key) == true) {
                                        isRemove = _packageSubmissionPushItems?.TryRemove(packageValue.Key, out _) ?? false;
                                    }
                                    else {
                                        break;
                                    }
                                    await Task.Delay(5, token);
                                } while (isRemove);*/
                                _packageSubmissionPushItems?.TryRemove(packageValue.Key, out _);
                                return;
                            }
                            //创建时间大于23s再提交
                            if (DateTime.Now.Subtract(packageValue.Value.PackageInfo.CreateTime).TotalSeconds < 23 ||
                                packageValue.Value.PackageExitUpdateItems?.Any(a =>
                                    a.InstructionType == InstructionType.SendSorting) != true)
                            {
                                return;
                            }

                            NLog.LogManager.GetCurrentClassLogger().Error($"准备发送");

                            var (key, value) = await uploader.SetParameters(_caiNiaoApiParam);

                            if (key)
                            {
                                if (!_memoryCache.TryGetValue(packageValue.Key, out _))
                                {
                                    _memoryCache.Set(packageValue.Key, packageValue.Value, new MemoryCacheEntryOptions
                                    {
                                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                                    });
                                    var caiNiaoStatusConvert = CaiNiaoStatusConvert(
                                        packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty,
                                        packageValue.Value.PackageExitUpdateItems);
                                    await uploader.UploadInBackground(packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty, packageValue.Value.PackageInfo.WeightInfo?.FormattedWeight ?? 0,
                                        packageValue.Value.PackageInfo.BarCodeInfo?.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo()
                                        {
                                            CameraCustomName = packageValue.Value.PackageInfo.BarCodeInfo?.SerialNumber ?? string.Empty,
                                            CameraName = packageValue.Value.PackageInfo.BarCodeInfo?.SerialNumber ?? string.Empty,
                                            CameraSerialNumber = packageValue.Value.PackageInfo.BarCodeInfo?.SerialNumber ?? string.Empty,
                                        }, other: new ReportChuteInfo
                                        {
                                            ChuteCode = caiNiaoStatusConvert.Value.ChuteCode,
                                            ChuteCodePhysical = packageValue.Value.PackageExitUpdateItems?.LastOrDefault(l => l.ExitType == SortingExitType.TheoreticalExit)?.ExitName ?? string.Empty,
                                            ErrorReson = caiNiaoStatusConvert.Value.ErrorReson,
                                            Status = caiNiaoStatusConvert.Key,
                                        }, token: token);
                                }
                            }
                            else
                            {
                                NLog.LogManager.GetCurrentClassLogger().Error("设置Api参数失败");
                            }

                            break;

                        case ApiType.JtExpressApi:
                            if (_jtExpressDto.IsUploadAfterReturn && packageValue.Value.PackageExitUpdateItems.Any(a => a.Type == ExitType.AbnormalExit))
                            {
                                //删除这条
                                _packageSubmissionPushItems?.TryRemove(packageValue.Key, out _);
                                return;
                            }
                            if (packageValue.Value.ApiResponse.UploadResponse is null || DateTime.Now.Subtract(packageValue.Value.ApiResponse.UploadResponse.ResponseTime).TotalSeconds < 2)
                            {
                                return;
                            }

                            var keyValuePair = await uploader.SetParameters(_jtExpressApiParam);
                            if (keyValuePair.Key)
                            {
                                if (!_memoryCache.TryGetValue(packageValue.Key, out _))
                                {
                                    var cameraSerialNumber =
                                        packageValue.Value.PackageInfo
                                            .BarCodeInfo?.SerialNumber ??
                                        string.Empty;
                                    await uploader.UploadInBackground(
                                        packageValue.Value.PackageInfo
                                            .BarCodeInfo?.Barcode ?? string.Empty,
                                        packageValue.Value.PackageInfo.WeightInfo?
                                            .FormattedWeight ?? 0,
                                        packageValue.Value.PackageInfo
                                            .BarCodeInfo?.ScanTime ?? DateTime.Now,
                                        packageValue.Value.PackageInfo.VolumeInfo?
                                            .FormattedLength ?? 0,
                                        packageValue.Value.PackageInfo.VolumeInfo?
                                            .FormattedWidth ?? 0,
                                        packageValue.Value.PackageInfo.VolumeInfo?
                                            .FormattedHeight ?? 0,
                                        packageValue.Value.PackageInfo.VolumeInfo?
                                            .FormattedVolume ?? 0,
                                        new UploadImageInfo
                                        {
                                            CameraCustomName = cameraSerialNumber,
                                            CameraName = cameraSerialNumber,
                                            CameraSerialNumber = cameraSerialNumber
                                        },
                                        null,
                                        packageValue.Value.ApiResponse.UploadResponse,
                                        token);
                                    // 仅在扫描上报成功后标记完成。
                                    _memoryCache.Set(
                                        packageValue.Key,
                                        packageValue.Value,
                                        new MemoryCacheEntryOptions
                                        {
                                            AbsoluteExpirationRelativeToNow =
                                                TimeSpan.FromMinutes(1)
                                        });
                                    NLog.LogManager.GetCurrentClassLogger()
                                        .Info("极兔扫描数据提交成功");
                                }
                            }
                            else
                            {
                                packageValue.Value.WaitSubmitTime =
                                    DateTime.Now.AddSeconds(1);
                                NLog.LogManager.GetCurrentClassLogger()
                                    .Error($"设置极兔 Api 参数失败:{keyValuePair.Value}");
                                return;
                            }

                            break;

                        case ApiType.JtPolarDayApi:
                            {
                                var parameterResult =
                                    await uploader.SetParameters(
                                        _jtPolarDayApiParam);
                                if (!parameterResult.Key)
                                {
                                    LogManager.GetCurrentClassLogger()
                                        .Error(
                                            $"设置极昼接口参数失败:{parameterResult.Value}");
                                    packageValue.Value.WaitSubmitTime =
                                        DateTime.Now.AddSeconds(1);
                                    return;
                                }

                                var submissionCacheKey =
                                    CreateJtPolarDaySubmissionCacheKey(
                                        packageValue.Key);
                                if (_memoryCache.TryGetValue(
                                        submissionCacheKey,
                                        out _))
                                {
                                    LogManager.GetCurrentClassLogger().Info(
                                        $"极昼设备信息已回传，跳过重复提交:Timestamp={packageValue.Key},WaybillNo={packageValue.Value.PackageInfo.BarCodeInfo?.Barcode}");
                                    break;
                                }

                                var packageInfo =
                                    packageValue.Value.PackageInfo;
                                var exitItems = packageValue.Value
                                    .PackageExitUpdateItems
                                    .ToArray();
                                var fallEvent = exitItems.LastOrDefault(item =>
                                    item.ExitType ==
                                    SortingExitType.PhysicalExit ||
                                    item.InstructionType is
                                        InstructionType.SignalCallback or
                                        InstructionType.PackageExceptionEx);
                                if (fallEvent is null)
                                {
                                    // 理论格口等事件可能先到，保留记录等待真实落格回调。
                                    return;
                                }
                                var barcode = packageInfo.BarCodeInfo?.Barcode ??
                                              string.Empty;
                                var imageCacheKey =
                                    CreateJtPolarDayImageCacheKey(
                                        packageValue.Key);
                                _memoryCache.TryGetValue(
                                    imageCacheKey,
                                    out SavedImageInfo? savedImageInfo);
                                if (savedImageInfo is null &&
                                    _imageStorageService.ImageSettingsDto?
                                        .IsSaveBarcodeImage == true &&
                                    DateTime.Now.Subtract(packageInfo.CreateTime)
                                        .TotalSeconds < 8)
                                {
                                    packageValue.Value.WaitSubmitTime =
                                        DateTime.Now.AddMilliseconds(200);
                                    return;
                                }

                                using var image = LoadImageSnapshot(
                                    savedImageInfo?.FilePath);
                                var cameraSerialNumber =
                                    savedImageInfo?.CameraSerialNumber ??
                                    packageInfo.BarCodeInfo?.SerialNumber ??
                                    string.Empty;
                                var fallTime = fallEvent.InstructionInfos?
                                                   .FirstOrDefault()?
                                                   .InstructionGeneratedTime ??
                                               fallEvent.CreateTime;
                                await uploader.UploadInBackground(
                                    barcode,
                                    packageInfo.WeightInfo?.FormattedWeight ??
                                    0,
                                    packageInfo.BarCodeInfo?.ScanTime ??
                                    DateTime.Now,
                                    packageInfo.VolumeInfo?.FormattedLength ??
                                    0,
                                    packageInfo.VolumeInfo?.FormattedWidth ??
                                    0,
                                    packageInfo.VolumeInfo?.FormattedHeight ??
                                    0,
                                    packageInfo.VolumeInfo?.FormattedVolume ??
                                    0,
                                    new UploadImageInfo
                                    {
                                        Image = image is null
                                            ? null
                                            : ImageHandle.TakeOwnership(
                                                (Image)image.Clone()),
                                        CameraCustomName = cameraSerialNumber,
                                        CameraName = cameraSerialNumber,
                                        CameraSerialNumber =
                                            cameraSerialNumber
                                    },
                                    null,
                                    new JtPolarDayApi.UploadContext
                                    {
                                        LandOnCarTime =
                                            packageInfo.BarCodeInfo?.ScanTime ??
                                            packageInfo.CreateTime,
                                        CarNum =
                                            packageInfo.GrayscaleResultInfo is
                                            { } grayscaleResult
                                                ? grayscaleResult.CarNumber
                                                    .ToString()
                                                : packageInfo.Guid.ToString(),
                                        GridNo = fallEvent.ExitName,
                                        GridCode =
                                            ConvertToPolarDayGridCode(
                                                barcode,
                                                exitItems),
                                        FallTime = fallTime
                                    },
                                    token);
                                // 仅在上传任务明确完成后标记，异常时保留记录供下一轮重试。
                                _memoryCache.Set(
                                    submissionCacheKey,
                                    true,
                                    new MemoryCacheEntryOptions
                                    {
                                        AbsoluteExpirationRelativeToNow =
                                            TimeSpan.FromMinutes(1)
                                    });
                                LogManager.GetCurrentClassLogger().Info(
                                    $"极昼设备信息回传完成:Timestamp={packageValue.Key},WaybillNo={barcode},GridNo={fallEvent.ExitName}");
                                _memoryCache.Remove(imageCacheKey);
                                break;
                            }

                        case ApiType.PostInApi:
                            if (DateTime.Now.Subtract(packageValue.Value.PackageInfo.CreateTime).TotalSeconds >= 100)
                            {
                                _packageSubmissionPushItems?.TryRemove(packageValue.Key, out _);
                                return;
                            }
                            if (DateTime.Now.Subtract(packageValue.Value.PackageInfo.CreateTime).TotalSeconds < 80 &&
                                packageValue.Value.PackageExitUpdateItems?.Any(a =>
                                    a.InstructionType == InstructionType.SignalCallback) != true)
                            {
                                return;
                            }
                            var (b, s) = await uploader.SetParameters(_postInApiParam);
                            if (b)
                            {
                                var exitName = packageValue.Value.PackageExitUpdateItems?.FirstOrDefault(f =>
                                        f.InstructionType == InstructionType.SignalCallback)
                                    ?.ExitName ?? string.Empty;
                                var uploadResponse = packageValue.Value.ApiResponse.UploadResponse;
                                if (!string.IsNullOrEmpty(uploadResponse?.RequestContent) &&
                                    !uploadResponse.IsSuccess)
                                {
                                    uploadResponse = uploadResponse with
                                    {
                                        RequestContent = uploadResponse.RequestContent + $"落格:[{exitName}]"
                                    };
                                    packageValue.Value.ApiResponse.UploadResponse = uploadResponse;
                                }

                                await uploader.UploadInBackground(packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty, packageValue.Value.PackageInfo?.WeightInfo?.FormattedWeight ?? 0,
                                    packageValue.Value.PackageInfo?.BarCodeInfo?.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo(), other:
                                    uploadResponse, token: token);
                                _packageSubmissionPushItems?.TryRemove(packageValue.Key, out _);
                            }
                            break;

                        case ApiType.PostApi:
                            if (DateTime.Now.Subtract(packageValue.Value.PackageInfo.CreateTime).TotalSeconds >= 60)
                            {
                                _packageSubmissionPushItems?.TryRemove(packageValue.Key, out _);
                                return;
                            }
                            if (DateTime.Now.Subtract(packageValue.Value.PackageInfo.CreateTime).TotalSeconds < 35 ||
                                packageValue.Value.PackageExitUpdateItems?.Any(a =>
                                    a.InstructionType == InstructionType.SendSorting) != true)
                            {
                                return;
                            }

                            var valuePair = await uploader.SetParameters(_postApiParam);
                            if (valuePair.Key)
                            {
                                if (!_memoryCache.TryGetValue(packageValue.Key, out _))
                                {
                                    _memoryCache.Set(packageValue.Key, packageValue.Value, new MemoryCacheEntryOptions
                                    {
                                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                                    });
                                    await uploader.UploadInBackground(packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty, packageValue.Value.PackageInfo?.WeightInfo?.FormattedWeight ?? 0,
                                        packageValue.Value.PackageInfo?.BarCodeInfo?.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo(), other:
                                        packageValue.Value.ApiResponse.UploadResponse, token: token);
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交");
                                }
                            }
                            break;
                    }
                    //判断推送锁格(条码、原格口、包裹信息)
                    //推送集包信息
                    EventAggregator.Instance.Publish(new PushPackageInfo()
                    {
                        PackageInfo = packageValue.Value.PackageInfo ?? new PackageInfo(),
                        PackageExitUpdateEvent = packageValue.Value.PackageExitUpdateItems?.LastOrDefault() ?? new PackageExitUpdateEvent(),
                        SignalCallbackTime = packageValue.Value.PackageExitUpdateItems?.LastOrDefault(l => l.InstructionType is InstructionType.SignalCallback or InstructionType.PackageExceptionEx)?.InstructionInfos?.FirstOrDefault()?.InstructionGeneratedTime
                    });
                    //删除这条

                    /*do {
                        if (_packageSubmissionPushItems?.ContainsKey(packageValue.Key) == true) {
                            isRemove = _packageSubmissionPushItems?.TryRemove(packageValue.Key, out _) ?? false;
                        }
                        else {
                            break;
                        }
                        await Task.Delay(5, token);
                    } while (isRemove);*/
                    _packageSubmissionPushItems?.TryRemove(packageValue.Key, out _);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                packageValue.Value.WaitSubmitTime = DateTime.Now.AddSeconds(1);
                LogManager.GetCurrentClassLogger().Error(
                    exception,
                    $"包裹落格上报失败，稍后重试:{packageValue.Value.PackageInfo?.BarCodeInfo?.Barcode}");
            }
            finally
            {
                _reportingPackageKeys.TryRemove(packageValue.Key, out _);
            }
        }

        private static Image? LoadImageSnapshot(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                using var source = Image.FromFile(path);
                return new Bitmap(source);
            }
            catch (Exception e) when (e is System.IO.IOException or
                                      UnauthorizedAccessException or
                                      ArgumentException)
            {
                LogManager.GetCurrentClassLogger().Warn(e, $"读取待上传图片失败:{path}");
                return null;
            }
        }

        /// <summary>
        /// 创建极昼扫描图片与包裹时间戳的缓存键。
        /// </summary>
        /// <param name="packageTimestamp">包裹时间戳。</param>
        /// <returns>不会与上报完成缓存冲突的字符串键。</returns>
        private static string CreateJtPolarDayImageCacheKey(
            long packageTimestamp)
        {
            return $"jt-polar-day-image:{packageTimestamp}";
        }

        /// <summary>
        /// 创建极昼设备信息回传去重缓存键。
        /// </summary>
        /// <param name="packageTimestamp">包裹时间戳。</param>
        /// <returns>不会与数据仓储裸时间戳缓存冲突的字符串键。</returns>
        private static string CreateJtPolarDaySubmissionCacheKey(
            long packageTimestamp)
        {
            return $"jt-polar-day-submission:{packageTimestamp}";
        }

        public class SubmitItemInfo
        {
            public long Guid { get; set; }

            /// <summary>
            /// 条码
            /// </summary>
            public string? Barcode { get; set; }

            /// <summary>
            /// 重量
            /// </summary>
            public decimal Weight { get; set; }

            /// <summary>
            /// 扫码时间
            /// </summary>
            public DateTime ScanTime { get; set; }

            /// <summary>
            /// 长度
            /// </summary>
            public decimal Length { get; set; }

            /// <summary>
            /// 宽度
            /// </summary>
            public decimal Width { get; set; }

            /// <summary>
            /// 高度
            /// </summary>
            public decimal Height { get; set; }

            /// <summary>
            /// 体积
            /// </summary>
            public decimal Volume { get; set; }

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
            /// 扫码相机序列号
            /// </summary>
            public string CameraSerialNumber { get; set; } = string.Empty;

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
        public class ApiResponseReceived
        {
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
        public class PackageSubmissionPushInfo
        {

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
            public ConcurrentQueue<PackageExitUpdateEvent> PackageExitUpdateItems { get; } = new();

            /// <summary>
            /// 回传信息
            /// </summary>
            private ApiResponseReceived _apiResponse = new();

            public ApiResponseReceived ApiResponse
            {
                get => Volatile.Read(ref _apiResponse);
                set => Volatile.Write(ref _apiResponse, value);
            }

            /// <summary>
            /// 是否已提交过备用格口
            /// </summary>
            public bool WasPushedAlternateExitSorter { get; set; }
        }

        public class CaiNiaoExitInfo
        {
            public string ChuteCode { get; set; } = string.Empty;
            public string ErrorReson { get; set; } = string.Empty;
        }
    }
}
