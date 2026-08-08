using NLog;
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
using PluginType = JayTom.Dws.Client.EventMediators.PluginType;
using InstructionType = JayTom.Dws.Data.Package.InstructionType;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.DownstreamProtocols.CommunicationProtocols;
using WindowsAction = JayTom.Dws.Client.EventMediators.WindowsAction;
using PushPackageInfo = JayTom.Dws.Client.EventMediators.PushPackageInfo;
using SortingExitType = JayTom.Dws.Client.EventMediators.SortingExitType;
using ApplicationStatus = JayTom.Dws.Client.EventMediators.ApplicationStatus;
using WindowsActionType = JayTom.Dws.Client.EventMediators.WindowsActionType;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;
using TriggerPositionEvent = JayTom.Dws.Client.EventMediators.TriggerPositionEvent;
using PackageExitUpdateEvent = JayTom.Dws.Client.EventMediators.PackageExitUpdateEvent;
using PluginParamChangedEvent = JayTom.Dws.Client.EventMediators.PluginParamChangedEvent;
using ApplicationStatusChanged = JayTom.Dws.Client.EventMediators.ApplicationStatusChanged;
using PackageAbnormalSortingType = JayTom.Dws.Client.EventMediators.PackageAbnormalSortingType;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    /// <summary>
    /// Api提交处理器
    /// </summary>
    public class SubmitApiBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfigRepository _configRepository;
        private readonly IImageStorageService _imageStorageService;
        private readonly IMemoryCache _memoryCache;
        private readonly ConcurrentQueue<SubmitItemInfo> _submitItems = new();
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
        private readonly ConcurrentQueue<SavedImageInfo> _savedImageItems = new();
        /*private ConcurrentQueue<CallBackPackageInfo> _callBackItems = new();
        private ConcurrentDictionary<long, SortingExitReceived> _sortingExitItems = new();*/
        private readonly ConcurrentQueue<PackageAggregationInfo> _packageAggregationInfoItems = new();
        private readonly SemaphoreSlim _settingsUpdateGate = new(1, 1);
        private readonly ConcurrentDictionary<long, PackageSubmissionPushInfo> _packageSubmissionPushItems = new();
        private readonly ConcurrentDictionary<long, byte> _reportingPackageKeys = new();
        private JtExpressDto _jtExpressDto = new();
        private IDataUploader? _submissionUploader;

        #region 非通用版本变量(临时)

        private static string _sunnenApiPackage = string.Empty;
        private bool _isWindowsClose;

        #endregion 非通用版本变量(临时)

        public SubmitApiBackgroundService(IHttpClientFactory httpClientFactory,
            IConfigRepository configRepository, IImageStorageService imageStorageService,
            IMemoryCache memoryCache)
        {
            _httpClientFactory = httpClientFactory;
            _configRepository = configRepository;
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
                            Height = (float)(model.VolumeInfo?.FormattedHeight ?? 0),
                            ScanTime = model.BarCodeInfo?.ScanTime ?? DateTime.Now,
                            Weight = (float)(model.WeightInfo?.FormattedWeight ?? 0),
                            Length = (float)(model.VolumeInfo?.FormattedLength ?? 0),
                            Width = (float)(model.VolumeInfo?.FormattedWidth ?? 0),
                            Volume = (float)(model.VolumeInfo?.FormattedVolume ?? 0),
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

                    _submitItems.Enqueue(submitItem);
                    //添加到推送队列
                    if (shouldTrackSubmission && _submissionUploader is not null)
                    {
                        _packageSubmissionPushItems.TryAdd(
                            submissionKey,
                            new PackageSubmissionPushInfo()
                            {
                                PackageInfo = model
                            });
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
                                _apiSettingsDto = await _configRepository.FirstOrDefaultEntity<ApiSettingsDto>(model.SettingsName) ?? new ApiSettingsDto();
                                _submissionUploader = _apiSettingsDto?.Type switch
                                {
                                    ApiType.CaiNiaoApi => new CaiNiaoApi(_httpClientFactory),
                                    ApiType.JtExpressApi => new JtExpressApi(_httpClientFactory),
                                    ApiType.JtPolarDayApi => new JtPolarDayApi(_httpClientFactory),
                                    ApiType.PostInApi => new PostInApi(_httpClientFactory),
                                    ApiType.PostApi => new PostApi(_httpClientFactory),
                                    _ => null
                                };

                                break;

                            case "DefaultApiParameters":
                                {
                                    //默认上传接口改参数
                                    var entity = await _configRepository.FirstOrDefaultEntity<DefaultApiDto>(model.SettingsName) ?? new DefaultApiDto();
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
                                    var entity = await _configRepository.FirstOrDefaultEntity<SzjyApiDto>(model.SettingsName) ?? new SzjyApiDto();
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
                                    var entity = await _configRepository.FirstOrDefaultEntity<WdtWmsApiDto>(model.SettingsName) ?? new WdtWmsApiDto();
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
                                    var entity = await _configRepository.FirstOrDefaultEntity<WdtFlagshipApiDto>(model.SettingsName) ?? new WdtFlagshipApiDto();
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
                                _jtExpressDto = await _configRepository.FirstOrDefaultEntity<JtExpressDto>(model.SettingsName) ?? new JtExpressDto();
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
                                    var entity = await _configRepository
                                        .FirstOrDefaultEntity<JtPolarDayDto>(
                                            model.SettingsName) ??
                                                 new JtPolarDayDto();
                                    _jtPolarDayApiParam =
                                        CreateJtPolarDayParameters(entity);
                                    break;
                                }

                            case "RoutDataApiParameters":
                                {
                                    var entity = await _configRepository.FirstOrDefaultEntity<RoutDataApiDto>(model.SettingsName) ?? new RoutDataApiDto();
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
                                    var entity = await _configRepository.FirstOrDefaultEntity<CaiNiaoApiDto>(model.SettingsName) ?? new CaiNiaoApiDto();
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
                                    var entity = await _configRepository.FirstOrDefaultEntity<EshippingitApiDto>(model.SettingsName) ?? new EshippingitApiDto();
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
                                    var entity = await _configRepository.FirstOrDefaultEntity<JushuitanErpApiDto>(model.SettingsName) ?? new JushuitanErpApiDto();
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
                                    var entity = await _configRepository.FirstOrDefaultEntity<ZhouYiApiDto>(model.SettingsName) ?? new ZhouYiApiDto();
                                    _zhouYiApiParam = new ZhouYiApi.ApiParameters()
                                    {
                                        AppKey = entity.AppKey,
                                        AppId = entity.AppId,
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
                _savedImageItems.Enqueue(savedImageInfo);
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
                    _packageAggregationInfoItems.Enqueue(info);
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
                        value.PackageExitUpdateItems.Enqueue(model);
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
                await Task.Delay(30, stoppingToken);
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
                                        uploader = new DefaultApi(_httpClientFactory);
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
                                        uploader = new SunnenApi(_httpClientFactory);
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
                                        uploader = new SzjyApi(_httpClientFactory);
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
                                        uploader = new WdtWmsApi(_httpClientFactory);
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
                                        uploader = new WdtFlagshipApi(_httpClientFactory);
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
                                        uploader = new JdyWmsApi(_httpClientFactory);
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
                                        uploader = new JtExpressApi(_httpClientFactory);
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
                                        uploader = new JtPolarDayApi(
                                            _httpClientFactory);
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
                                        uploader = new RoutDataApi(_httpClientFactory);
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
                                        uploader = new GeekPlusApi(_httpClientFactory);
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
                                        uploader = new CaiNiaoApi(_httpClientFactory);
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
                                        uploader = new EshippingitApi(_httpClientFactory);
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
                                        uploader = new PostApi(_httpClientFactory);
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
                                        uploader = new PostInApi(_httpClientFactory);
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
                                        uploader = new ZhuoYanScmApi(_httpClientFactory);
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
                                        uploader = new TtxApi(_httpClientFactory);
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
                                            var apiUploader = new WdtWmsApi(_httpClientFactory);
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
                                            var apiUploader = new TtxApi(_httpClientFactory);
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
                                        uploader = new JushuitanErpApi(_httpClientFactory);
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
                                        uploader = new ZhouYiApi(_httpClientFactory);
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

                                    uploader = new GeekPlusApi(_httpClientFactory);
                                    using (var uploadImage = LoadImageSnapshot(model.FilePath))
                                    {
                                        await uploader.UploadInBackground(model.BarCode ?? string.Empty, 0,
                                            model.ScanTime, imageInfo: new UploadImageInfo()
                                            {
                                                CameraCustomName = model.CameraSerialNumber,
                                                CameraName = model.CameraSerialNumber,
                                                CameraSerialNumber = model.CameraSerialNumber,
                                                Image = uploadImage
                                            }, token: stoppingToken);
                                    }
                                    break;

                                case ApiType.EshippingitApi:
                                    uploader = new EshippingitApi(_httpClientFactory);
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
                                                Image = uploadImage
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
                                    new CaiNiaoApi(_httpClientFactory);
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
                catch (Exception e)
                {
                    if (inFlightSubmit is not null)
                    {
                        _submitItems.Enqueue(inFlightSubmit);
                    }
                    if (inFlightSavedImage is not null)
                    {
                        _savedImageItems.Enqueue(inFlightSavedImage);
                    }
                    LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            }
        }

        private async Task ReadDefaultConfig()
        {
            //上传类型
            _apiSettingsDto = await _configRepository.FirstOrDefaultEntity<ApiSettingsDto>("ApiSettings") ?? new ApiSettingsDto();

            //默认接口
            var defaultentity = await _configRepository.FirstOrDefaultEntity<DefaultApiDto>("DefaultApiParameters") ?? new DefaultApiDto();
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
            var szjyEntity = await _configRepository.FirstOrDefaultEntity<SzjyApiDto>("SzjyApiParameters") ?? new SzjyApiDto();
            _szjyApiParam = new SzjyApi.ApiParameter()
            {
                Machine = szjyEntity.Machine,
                Password = szjyEntity.Password,
                TimeOut = szjyEntity.TimeOut,
                UserName = szjyEntity.UserName,
                Url = szjyEntity.Url,
            };

            //旺店通Wms
            var wdtWmsApiDto = await _configRepository.FirstOrDefaultEntity<WdtWmsApiDto>("WdtWmsApiParameters") ?? new WdtWmsApiDto();

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
            var wdtFlagshipApiDto = await _configRepository.FirstOrDefaultEntity<WdtFlagshipApiDto>("WdtFlagshipApiParameters") ?? new WdtFlagshipApiDto();

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
            _jtExpressDto = await _configRepository.FirstOrDefaultEntity<JtExpressDto>("JtExpressApiParameters") ?? new JtExpressDto();
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
            var jtPolarDayDto = await _configRepository
                .FirstOrDefaultEntity<JtPolarDayDto>(
                    "JtPolarDayApiParameters") ??
                                    new JtPolarDayDto();
            _jtPolarDayApiParam =
                CreateJtPolarDayParameters(jtPolarDayDto);
            //络道科技Api
            var routDataApiDto = await _configRepository.FirstOrDefaultEntity<RoutDataApiDto>("RoutDataApiParameters") ?? new RoutDataApiDto();
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
            var caiNiaoApiDto = await _configRepository.FirstOrDefaultEntity<CaiNiaoApiDto>("CaiNiaoApiParameters") ?? new CaiNiaoApiDto();

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
            var eshippingitApiDto = await _configRepository.FirstOrDefaultEntity<EshippingitApiDto>("EshippingitApiParameters") ?? new EshippingitApiDto();
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
            var jushuitanErpApiDto = await _configRepository.FirstOrDefaultEntity<JushuitanErpApiDto>("JushuitanErpApiParameters") ?? new JushuitanErpApiDto();
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
            var zhouYiApiDto = await _configRepository.FirstOrDefaultEntity<ZhouYiApiDto>("ZhouYiApiParameters") ?? new ZhouYiApiDto();
            _zhouYiApiParam = new ZhouYiApi.ApiParameters()
            {
                AppKey = zhouYiApiDto.AppKey,
                AppId = zhouYiApiDto.AppId,
                NeedUpload = zhouYiApiDto.NeedUpload,
                IsFstCode = zhouYiApiDto.IsFstCode,

                TimeOut = zhouYiApiDto.TimeOut,
                Url = zhouYiApiDto.Url,
            };
            _submissionUploader = _apiSettingsDto?.Type switch
            {
                ApiType.CaiNiaoApi => new CaiNiaoApi(_httpClientFactory),
                ApiType.JtExpressApi => new JtExpressApi(_httpClientFactory),
                ApiType.JtPolarDayApi => new JtPolarDayApi(_httpClientFactory),
                ApiType.PostInApi => new PostInApi(_httpClientFactory),
                ApiType.PostApi => new PostApi(_httpClientFactory),
                _ => null
            };
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

                                if (_memoryCache.TryGetValue(
                                        packageValue.Key,
                                        out _))
                                {
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
                                        Image = image,
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
                                    packageValue.Key,
                                    packageValue.Value,
                                    new MemoryCacheEntryOptions
                                    {
                                        AbsoluteExpirationRelativeToNow =
                                            TimeSpan.FromMinutes(1)
                                    });
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
                                if (!string.IsNullOrEmpty(packageValue.Value.ApiResponse.UploadResponse?.RequestContent) &&
                                    !packageValue.Value.ApiResponse.UploadResponse.IsSuccess)
                                {
                                    packageValue.Value.ApiResponse.UploadResponse.RequestContent += $"落格:[{exitName}]";
                                }

                                await uploader.UploadInBackground(packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty, packageValue.Value.PackageInfo?.WeightInfo?.FormattedWeight ?? 0,
                                    packageValue.Value.PackageInfo?.BarCodeInfo?.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo(), other:
                                    packageValue.Value.ApiResponse.UploadResponse, token: token);
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
