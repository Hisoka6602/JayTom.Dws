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
        private static DefaultApi.DefaultApiParameters _defaultApiParameters = new();
        private static SzjyApi.ApiParameter _szjyApiParam = new();
        private static WdtWmsApi.ApiParameter _wdtWmsApiParameter = new();
        private static WdtFlagshipApi.ApiParameter _wdtFlagshipApiParameter = new();
        private static JtExpressApi.ApiParameter _jtExpressApiParam = new();
        private static RoutDataApi.ApiParameters _rstDataApiParam = new();
        private static CaiNiaoApi.ApiParameters _caiNiaoApiParam = new();
        private static EshippingitApi.ApiParameters _eshippingitApiParam = new();
        private static PostApi.ApiParameters _postApiParam = new();
        private static PostInApi.ApiParameters _postInApiParam = new();
        private static ZhuoYanScmApi.ApiParameters _zhuoYanScmApiParam = new();
        private static JushuitanErpApi.ApiParameters _jushuitanErpParam = new();
        private ConcurrentQueue<SavedImageInfo> _savedImageItems = new();
        /*private ConcurrentQueue<CallBackPackageInfo> _callBackItems = new();
        private ConcurrentDictionary<long, SortingExitReceived> _sortingExitItems = new();*/
        private ConcurrentQueue<PackageAggregationInfo> _packageAggregationInfoItems = new();
        private SemaphoreSlim _takePackageSlim = new(1);
        private ConcurrentDictionary<long, PackageSubmissionPushInfo> _packageSubmissionPushItems = new();
        private JtExpressDto _jtExpressDto = new();
        private IDataUploader? _submissionUploader;

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
                    switch (model.SettingsName) {
                        case "ApiSettings":
                            _apiSettingsDto = await _configRepository.FirstOrDefaultEntity<ApiSettingsDto>(model.SettingsName) ?? new ApiSettingsDto();
                            _submissionUploader = _apiSettingsDto?.Type switch {
                                ApiType.CaiNiaoApi => new CaiNiaoApi(_httpClientFactory),
                                ApiType.JtExpressApi => new JtExpressApi(_httpClientFactory),
                                ApiType.PostInApi => new PostInApi(_httpClientFactory),
                                ApiType.PostApi => new PostApi(_httpClientFactory),
                                _ => null
                            };

                            break;

                        case "DefaultApiParameters": {
                                //默认上传接口改参数
                                var entity = await _configRepository.FirstOrDefaultEntity<DefaultApiDto>(model.SettingsName) ?? new DefaultApiDto();
                                _defaultApiParameters = new DefaultApi.DefaultApiParameters() {
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
                        case "SzjyApiParameters": {
                                //默认上传接口改参数
                                var entity = await _configRepository.FirstOrDefaultEntity<SzjyApiDto>(model.SettingsName) ?? new SzjyApiDto();
                                _szjyApiParam = new SzjyApi.ApiParameter() {
                                    Machine = entity.Machine,
                                    Password = entity.Password,
                                    TimeOut = entity.TimeOut,
                                    UserName = entity.UserName,
                                    Url = entity.Url,
                                };
                                break;
                            }
                        case "WdtWmsApiParameters": {
                                //默认上传接口改参数
                                var entity = await _configRepository.FirstOrDefaultEntity<WdtWmsApiDto>(model.SettingsName) ?? new WdtWmsApiDto();
                                _wdtWmsApiParameter = new WdtWmsApi.ApiParameter {
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
                        case "WdtFlagshipApiParameters": {
                                //默认上传接口改参数
                                var entity = await _configRepository.FirstOrDefaultEntity<WdtFlagshipApiDto>(model.SettingsName) ?? new WdtFlagshipApiDto();
                                _wdtFlagshipApiParameter = new WdtFlagshipApi.ApiParameter {
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
                            _jtExpressApiParam = new JtExpressApi.ApiParameter {
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

                        case "RoutDataApiParameters": {
                                var entity = await _configRepository.FirstOrDefaultEntity<RoutDataApiDto>(model.SettingsName) ?? new RoutDataApiDto();
                                _rstDataApiParam = new RoutDataApi.ApiParameters() {
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
                        case "CaiNiaoApiParameters": {
                                var entity = await _configRepository.FirstOrDefaultEntity<CaiNiaoApiDto>(model.SettingsName) ?? new CaiNiaoApiDto();
                                _caiNiaoApiParam = new CaiNiaoApi.ApiParameters() {
                                    BcrName = entity.BcrName,
                                    BcrCode = entity.BcrCode,
                                    Source = entity.Source,
                                    TimeOut = entity.TimeOut,
                                    Url = entity.Url,
                                    Version = entity.Version
                                };
                                break;
                            }
                        case "EshippingitApiParameters": {
                                var entity = await _configRepository.FirstOrDefaultEntity<EshippingitApiDto>(model.SettingsName) ?? new EshippingitApiDto();
                                _eshippingitApiParam = new EshippingitApi.ApiParameters() {
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
                        case "JushuitanErpApiParameters": {
                                var entity = await _configRepository.FirstOrDefaultEntity<JushuitanErpApiDto>(model.SettingsName) ?? new JushuitanErpApiDto();
                                _jushuitanErpParam = new JushuitanErpApi.ApiParameters() {
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
                                    IDataUploader uploader;
                                    UploadResponse? uploadResponse = null;
                                    switch (_apiSettingsDto?.Type) {
                                        case ApiType.None:
                                            return;

                                        case ApiType.DefaultApi: {
                                                //基础接口
                                                uploader = new DefaultApi(_httpClientFactory);
                                                //设置参数
                                                var (key, value) = await uploader.SetParameters(_defaultApiParameters);
                                                if (key) {
                                                    uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                        info.Weight, info.ScanTime,
                                                        info.Length, info.Width,
                                                        info.Height, info.Volume,
                                                        null, null,
                                                        null, stoppingToken);
                                                }
                                                else {
                                                    uploadResponse = new UploadResponse() {
                                                        ExceptionMsg = value
                                                    };
                                                    Console.WriteLine("设置参数失败!");
                                                }

                                                break;
                                            }
                                        case ApiType.SunnenApi: {
                                                uploader = new SunnenApi(_httpClientFactory);
                                                uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                    info.Weight, info.ScanTime,
                                                    info.Length, info.Width,
                                                    info.Height, info.Volume,
                                                    null, null,
                                                    _sunnenApiPackage, stoppingToken);
                                                break;
                                            }
                                        case ApiType.SzjyApi: {
                                                //神州集运后台
                                                uploader = new SzjyApi(_httpClientFactory);
                                                var (key, value) = await uploader.SetParameters(_szjyApiParam);
                                                if (key) {
                                                    uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                        info.Weight, info.ScanTime,
                                                        info.Length, info.Width,
                                                        info.Height, info.Volume,
                                                        null, null,
                                                        null, stoppingToken);
                                                }
                                                else {
                                                    uploadResponse = new UploadResponse() {
                                                        ExceptionMsg = value
                                                    };
                                                    Console.WriteLine("设置参数失败!");
                                                }
                                                break;
                                            }
                                        case ApiType.WdtWmsApi: {
                                                uploader = new WdtWmsApi(_httpClientFactory);
                                                var (key, value) = await uploader.SetParameters(_wdtWmsApiParameter);
                                                if (key) {
                                                    uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                        info.Weight, info.ScanTime,
                                                        info.Length, info.Width,
                                                        info.Height, info.Volume,
                                                        null, null,
                                                        info.Other, stoppingToken);
                                                }
                                                else {
                                                    uploadResponse = new UploadResponse() {
                                                        ExceptionMsg = value
                                                    };
                                                    Console.WriteLine("设置参数失败!");
                                                }
                                                break;
                                            }
                                        case ApiType.WdtErpFlagShipApi: {
                                                uploader = new WdtFlagshipApi(_httpClientFactory);
                                                var (key, value) = await uploader.SetParameters(_wdtFlagshipApiParameter);
                                                if (key) {
                                                    uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                        info.Weight, info.ScanTime,
                                                        info.Length, info.Width,
                                                        info.Height, info.Volume,
                                                        null, null,
                                                        null, stoppingToken);
                                                }
                                                else {
                                                    uploadResponse = new UploadResponse() {
                                                        ExceptionMsg = value
                                                    };
                                                    Console.WriteLine("设置参数失败!");
                                                }
                                                break;
                                            }
                                        case ApiType.JdyWms: {
                                                uploader = new JdyWmsApi(_httpClientFactory);
                                                uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                    info.Weight, info.ScanTime,
                                                    info.Length, info.Width,
                                                    info.Height, info.Volume,
                                                    null, null,
                                                    null, stoppingToken);
                                                break;
                                            }
                                        case ApiType.JtExpressApi: {
                                                uploader = new JtExpressApi(_httpClientFactory);
                                                var (key, value) = await uploader.SetParameters(_jtExpressApiParam);
                                                if (key) {
                                                    uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                        info.Weight, info.ScanTime,
                                                        info.Length, info.Width,
                                                        info.Height, info.Volume,
                                                        null, null,
                                                        null, stoppingToken);
                                                }
                                                else {
                                                    uploadResponse = new UploadResponse() {
                                                        ExceptionMsg = value
                                                    };
                                                    Console.WriteLine("设置参数失败!");
                                                }

                                                break;
                                            }
                                        case ApiType.RoutDataApi: {
                                                uploader = new RoutDataApi(_httpClientFactory);
                                                var (key, value) = await uploader.SetParameters(_rstDataApiParam);
                                                if (key) {
                                                    uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                        info.Weight, info.ScanTime,
                                                        info.Length, info.Width,
                                                        info.Height, info.Volume,
                                                        null, null,
                                                        null, stoppingToken);
                                                }
                                                else {
                                                    uploadResponse = new UploadResponse() {
                                                        ExceptionMsg = value
                                                    };
                                                    Console.WriteLine("设置参数失败!");
                                                }
                                                break;
                                            }
                                        case ApiType.GeekPlusApi: {
                                                uploader = new GeekPlusApi(_httpClientFactory);
                                                var (key, value) = await uploader.SetParameters(_rstDataApiParam);
                                                if (key) {
                                                    uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                        info.Weight, info.ScanTime,
                                                        info.Length, info.Width,
                                                        info.Height, info.Volume,
                                                        null, null,
                                                        null, stoppingToken);
                                                }
                                                else {
                                                    uploadResponse = new UploadResponse() {
                                                        ExceptionMsg = value
                                                    };
                                                    Console.WriteLine("设置参数失败!");
                                                }
                                                break;
                                            }
                                        case ApiType.CaiNiaoApi: {
                                                uploader = new CaiNiaoApi(_httpClientFactory);
                                                var (key, value) = await uploader.SetParameters(_caiNiaoApiParam);
                                                if (key) {
                                                    uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                        info.Weight, info.ScanTime,
                                                        info.Length, info.Width,
                                                        info.Height, info.Volume,
                                                        null, null,
                                                        info.IsStackedPackage, stoppingToken);
                                                }
                                                else {
                                                    uploadResponse = new UploadResponse() {
                                                        ExceptionMsg = value
                                                    };
                                                    Console.WriteLine("设置参数失败!");
                                                }
                                                break;
                                            }
                                        case ApiType.EshippingitApi: {
                                                uploader = new EshippingitApi(_httpClientFactory);
                                                var (key, value) = await uploader.SetParameters(_eshippingitApiParam);
                                                if (key) {
                                                    uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                        info.Weight, info.ScanTime,
                                                        info.Length, info.Width,
                                                        info.Height, info.Volume,
                                                        null, null,
                                                        info.IsStackedPackage, stoppingToken);
                                                }
                                                else {
                                                    uploadResponse = new UploadResponse() {
                                                        ExceptionMsg = value
                                                    };
                                                    Console.WriteLine("设置参数失败!");
                                                }
                                                break;
                                            }
                                        case ApiType.PostApi: {
                                                uploader = new PostApi(_httpClientFactory);
                                                var (key, value) = await uploader.SetParameters(_postApiParam);
                                                if (key) {
                                                    uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                        info.Weight, info.ScanTime,
                                                        info.Length, info.Width,
                                                        info.Height, info.Volume,
                                                        null, null,
                                                        info.IsStackedPackage, stoppingToken);
                                                }
                                                else {
                                                    uploadResponse = new UploadResponse() {
                                                        ExceptionMsg = value
                                                    };
                                                    Console.WriteLine("设置参数失败!");
                                                }
                                            }

                                            break;

                                        case ApiType.PostInApi: {
                                                uploader = new PostInApi(_httpClientFactory);
                                                var (key, value) = await uploader.SetParameters(_postInApiParam);
                                                if (key) {
                                                    uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                        info.Weight, info.ScanTime,
                                                        info.Length, info.Width,
                                                        info.Height, info.Volume,
                                                        null, null,
                                                        info.IsStackedPackage, stoppingToken);
                                                }
                                                else {
                                                    uploadResponse = new UploadResponse() {
                                                        ExceptionMsg = value
                                                    };
                                                    Console.WriteLine("设置参数失败!");
                                                }
                                            }

                                            break;

                                        case ApiType.ZhuoYanScm: {
                                                uploader = new ZhuoYanScmApi(_httpClientFactory);
                                                var (key, value) = await uploader.SetParameters(_zhuoYanScmApiParam);

                                                if (key) {
                                                    uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                        info.Weight, info.ScanTime,
                                                        info.Length, info.Width,
                                                        info.Height, info.Volume,
                                                        null, null,
                                                        info.IsStackedPackage, stoppingToken);
                                                }
                                                else {
                                                    uploadResponse = new UploadResponse() {
                                                        ExceptionMsg = value
                                                    };
                                                    Console.WriteLine("设置参数失败!");
                                                }
                                            }
                                            break;

                                        case ApiType.TtxApi: {
                                                uploader = new TtxApi(_httpClientFactory);
                                                uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                    info.Weight, info.ScanTime,
                                                    info.Length, info.Width,
                                                    info.Height, info.Volume,
                                                    null, null,
                                                    info.IsStackedPackage, stoppingToken);
                                            }
                                            break;

                                        case ApiType.WdtWmsApiAndTtxApi: {
                                                var cancellationTokenSource = new CancellationTokenSource();
                                                var wdtTask = Task.Run(async () => {
                                                    var apiUploader = new WdtWmsApi(_httpClientFactory);
                                                    var (key, value) = await apiUploader.SetParameters(_wdtWmsApiParameter);
                                                    if (key) {
                                                        return await apiUploader.UploadData(info.Barcode ?? string.Empty,
                                                             info.Weight, info.ScanTime,
                                                             info.Length, info.Width,
                                                             info.Height, info.Volume,
                                                             null, null,
                                                             info.Other, stoppingToken);
                                                    }
                                                    else {
                                                        Console.WriteLine("设置参数失败!");
                                                        return new UploadResponse() {
                                                            ExceptionMsg = value
                                                        };
                                                    }
                                                }, cancellationTokenSource.Token);

                                                var ttxTask = Task.Run(async () => {
                                                    var apiUploader = new TtxApi(_httpClientFactory);
                                                    return await apiUploader.UploadData(info.Barcode ?? string.Empty,
                                                         info.Weight, info.ScanTime,
                                                         info.Length, info.Width,
                                                         info.Height, info.Volume,
                                                         null, null,
                                                         info.IsStackedPackage, stoppingToken);
                                                }, cancellationTokenSource.Token);

                                                var completedTask = await Task.WhenAny(wdtTask, ttxTask);

                                                if (completedTask == wdtTask && wdtTask.Result.IsSuccess) {
                                                    cancellationTokenSource.Cancel(); // 取消 其他
                                                    uploadResponse = wdtTask.Result;
                                                }
                                                else if (completedTask == ttxTask && ttxTask.Result.IsSuccess) {
                                                    cancellationTokenSource.Cancel(); // 取消 其他
                                                    uploadResponse = ttxTask.Result;
                                                }
                                                else {
                                                    var timeoutTask = Task.Delay(2000, stoppingToken);
                                                    var completedTasks = await Task.WhenAny(Task.WhenAll(wdtTask, ttxTask), timeoutTask);
                                                    if (completedTasks == timeoutTask) {
                                                        cancellationTokenSource.Cancel(); // 超时后取消其他任务
                                                        uploadResponse = new UploadResponse() {
                                                            ExceptionMsg = "多个上传接口皆超时"
                                                        };
                                                    }
                                                    else {
                                                        uploadResponse = wdtTask.Result;
                                                    }
                                                }
                                            }
                                            break;

                                        case ApiType.Jushuitan: {
                                                uploader = new JushuitanErpApi(_httpClientFactory);
                                                var (key, value) = await uploader.SetParameters(_jushuitanErpParam);
                                                if (key) {
                                                    uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                        info.Weight, info.ScanTime,
                                                        info.Length, info.Width,
                                                        info.Height, info.Volume,
                                                        null, null,
                                                        null, stoppingToken);
                                                }
                                                else {
                                                    uploadResponse = new UploadResponse() {
                                                        ExceptionMsg = value
                                                    };
                                                    Console.WriteLine("设置参数失败!");
                                                }
                                            }
                                            break;
                                    }
                                    if (_apiSettingsDto?.Type is not null &&
                                        _apiSettingsDto.Type != ApiType.None) {
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

                            //取出图片
                            var dequeue = _savedImageItems.TryDequeue(out var model);
                            if (dequeue && model is not null && !string.IsNullOrEmpty(model.FilePath) &&
                                model.ImageType == SaveImageType.BarcodeImage) {
                                Task.Factory.StartNew(async () => {
                                    //后续上传
                                    IDataUploader uploader;
                                    switch (_apiSettingsDto?.Type) {
                                        case ApiType.None:
                                            return;

                                        case ApiType.GeekPlusApi:

                                            uploader = new GeekPlusApi(_httpClientFactory);
                                            uploader.UploadInBackground(model.BarCode ?? string.Empty, 0,
                                                model.ScanTime, imageInfo: new UploadImageInfo() {
                                                    CameraCustomName = model.CameraSerialNumber,
                                                    CameraName = model.CameraSerialNumber,
                                                    CameraSerialNumber = model.CameraSerialNumber,
                                                    Image = Image.FromFile(model.FilePath ?? string.Empty)
                                                }, token: stoppingToken);
                                            break;

                                        case ApiType.EshippingitApi:
                                            uploader = new EshippingitApi(_httpClientFactory);
                                            var (key, value) = await uploader.SetParameters(_eshippingitApiParam);
                                            if (key) {
                                                uploader.UploadInBackground(model.BarCode ?? string.Empty, 0,
                                                    model.ScanTime, imageInfo: new UploadImageInfo() {
                                                        CameraCustomName = model.CameraSerialNumber,
                                                        CameraName = model.CameraSerialNumber,
                                                        CameraSerialNumber = model.CameraSerialNumber,
                                                        Image = Image.FromFile(model.FilePath ?? string.Empty)
                                                    }, token: stoppingToken);
                                            }
                                            else {
                                                LogManager.GetCurrentClassLogger().Error("设置参数失败!");
                                            }

                                            break;
                                    }
                                }, stoppingToken);
                            }

                            //获取需要提交到备用格口的数据

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
                                */

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
                            }
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

            //默认接口
            var defaultentity = await _configRepository.FirstOrDefaultEntity<DefaultApiDto>("DefaultApiParameters") ?? new DefaultApiDto();
            _defaultApiParameters = new DefaultApi.DefaultApiParameters() {
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
            _szjyApiParam = new SzjyApi.ApiParameter() {
                Machine = szjyEntity.Machine,
                Password = szjyEntity.Password,
                TimeOut = szjyEntity.TimeOut,
                UserName = szjyEntity.UserName,
                Url = szjyEntity.Url,
            };

            //旺店通Wms
            var wdtWmsApiDto = await _configRepository.FirstOrDefaultEntity<WdtWmsApiDto>("WdtWmsApiParameters") ?? new WdtWmsApiDto();

            _wdtWmsApiParameter = new WdtWmsApi.ApiParameter {
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

            _wdtFlagshipApiParameter = new WdtFlagshipApi.ApiParameter {
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
            _jtExpressApiParam = new JtExpressApi.ApiParameter {
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
                InterceptorEnabled = _jtExpressApiParam.InterceptorEnabled
            };
            //络道科技Api
            var routDataApiDto = await _configRepository.FirstOrDefaultEntity<RoutDataApiDto>("RoutDataApiParameters") ?? new RoutDataApiDto();
            _rstDataApiParam = new RoutDataApi.ApiParameters() {
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

            _caiNiaoApiParam = new CaiNiaoApi.ApiParameters() {
                BcrName = caiNiaoApiDto.BcrName,
                BcrCode = caiNiaoApiDto.BcrCode,
                Source = caiNiaoApiDto.Source,
                TimeOut = caiNiaoApiDto.TimeOut,
                Url = caiNiaoApiDto.Url,
                Version = caiNiaoApiDto.Version
            };
            //海通智运Api
            var eshippingitApiDto = await _configRepository.FirstOrDefaultEntity<EshippingitApiDto>("EshippingitApiParameters") ?? new EshippingitApiDto();
            _eshippingitApiParam = new EshippingitApi.ApiParameters() {
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
            _jushuitanErpParam = new JushuitanErpApi.ApiParameters() {
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
            _submissionUploader = _apiSettingsDto?.Type switch {
                ApiType.CaiNiaoApi => new CaiNiaoApi(_httpClientFactory),
                ApiType.JtExpressApi => new JtExpressApi(_httpClientFactory),
                ApiType.PostInApi => new PostInApi(_httpClientFactory),
                ApiType.PostApi => new PostApi(_httpClientFactory),
                _ => null
            };
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

        private async void ReportProgress(KeyValuePair<long, PackageSubmissionPushInfo> packageValue,
            IDataUploader uploader, CancellationToken token) {
            try {
                await _takePackageSlim.WaitAsync(token);
                //提交
                if (packageValue.Value is { PackageInfo: not null } && packageValue.Value.PackageExitUpdateItems?.Any() == true) {
                    switch (_apiSettingsDto?.Type) {
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
                                uploader.UploadInBackground(packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty, packageValue.Value.PackageInfo.WeightInfo?.FormattedWeight ?? 0,
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
                            if (DateTime.Now.Subtract(packageValue.Value.PackageInfo.CreateTime).TotalSeconds >= 60) {
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
                                    a.InstructionType == InstructionType.SendSorting) != true) {
                                return;
                            }

                            NLog.LogManager.GetCurrentClassLogger().Error($"准备发送");

                            var (key, value) = await uploader.SetParameters(_caiNiaoApiParam);

                            if (key) {
                                if (!_memoryCache.TryGetValue(packageValue.Key, out _)) {
                                    _memoryCache.Set(packageValue.Key, packageValue.Value, new MemoryCacheEntryOptions {
                                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                                    });
                                    var caiNiaoStatusConvert = CaiNiaoStatusConvert(
                                        packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty,
                                        packageValue.Value.PackageExitUpdateItems);
                                    uploader.UploadInBackground(packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty, packageValue.Value.PackageInfo.WeightInfo?.FormattedWeight ?? 0,
                                        packageValue.Value.PackageInfo.BarCodeInfo?.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo() {
                                            CameraCustomName = packageValue.Value.PackageInfo.BarCodeInfo?.SerialNumber ?? string.Empty,
                                            CameraName = packageValue.Value.PackageInfo.BarCodeInfo?.SerialNumber ?? string.Empty,
                                            CameraSerialNumber = packageValue.Value.PackageInfo.BarCodeInfo?.SerialNumber ?? string.Empty,
                                        }, other: new ReportChuteInfo {
                                            ChuteCode = caiNiaoStatusConvert.Value.ChuteCode,
                                            ChuteCodePhysical = packageValue.Value.PackageExitUpdateItems?.LastOrDefault(l => l.ExitType == SortingExitType.TheoreticalExit)?.ExitName ?? string.Empty,
                                            ErrorReson = caiNiaoStatusConvert.Value.ErrorReson,
                                            Status = caiNiaoStatusConvert.Key,
                                        }, token: token);
                                }
                            }
                            else {
                                NLog.LogManager.GetCurrentClassLogger().Error("设置Api参数失败");
                            }

                            break;

                        case ApiType.JtExpressApi:
                            if (_jtExpressDto.IsUploadAfterReturn && packageValue.Value.PackageExitUpdateItems.Any(a => a.Type == ExitType.AbnormalExit)) {
                                //删除这条
                                _packageSubmissionPushItems?.TryRemove(packageValue.Key, out _);
                                return;
                            }
                            if (packageValue.Value.ApiResponse.UploadResponse is null || DateTime.Now.Subtract(packageValue.Value.ApiResponse.UploadResponse.ResponseTime).TotalSeconds < 2) {
                                return;
                            }

                            var keyValuePair = await uploader.SetParameters(_jtExpressApiParam);
                            if (keyValuePair.Key) {
                                if (!_memoryCache.TryGetValue(packageValue.Key, out _)) {
                                    _memoryCache.Set(packageValue.Key, packageValue.Value, new MemoryCacheEntryOptions {
                                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                                    });
                                    uploader.UploadInBackground(packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty, packageValue.Value.PackageInfo?.WeightInfo?.FormattedWeight ?? 0,
                                        packageValue.Value.PackageInfo?.BarCodeInfo?.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo(), other:
                                        packageValue.Value.ApiResponse.UploadResponse, token: token);
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交");
                                }
                            }
                            else {
                                NLog.LogManager.GetCurrentClassLogger().Error("设置Api参数失败");
                            }

                            break;

                        case ApiType.PostInApi:
                            if (DateTime.Now.Subtract(packageValue.Value.PackageInfo.CreateTime).TotalSeconds >= 100) {
                                _packageSubmissionPushItems?.TryRemove(packageValue.Key, out _);
                                return;
                            }
                            if (DateTime.Now.Subtract(packageValue.Value.PackageInfo.CreateTime).TotalSeconds < 80 &&
                                packageValue.Value.PackageExitUpdateItems?.Any(a =>
                                    a.InstructionType == InstructionType.SignalCallback) != true) {
                                return;
                            }
                            var (b, s) = await uploader.SetParameters(_postInApiParam);
                            if (b) {
                                var exitName = packageValue.Value.PackageExitUpdateItems?.FirstOrDefault(f =>
                                        f.InstructionType == InstructionType.SignalCallback)
                                    ?.ExitName ?? string.Empty;
                                if (!string.IsNullOrEmpty(packageValue.Value.ApiResponse.UploadResponse?.RequestContent) &&
                                    !packageValue.Value.ApiResponse.UploadResponse.IsSuccess) {
                                    packageValue.Value.ApiResponse.UploadResponse.RequestContent += $"落格:[{exitName}]";
                                }

                                uploader.UploadInBackground(packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty, packageValue.Value.PackageInfo?.WeightInfo?.FormattedWeight ?? 0,
                                    packageValue.Value.PackageInfo?.BarCodeInfo?.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo(), other:
                                    packageValue.Value.ApiResponse.UploadResponse, token: token);
                                _packageSubmissionPushItems?.TryRemove(packageValue.Key, out _);
                            }
                            break;

                        case ApiType.PostApi:
                            if (DateTime.Now.Subtract(packageValue.Value.PackageInfo.CreateTime).TotalSeconds >= 60) {
                                _packageSubmissionPushItems?.TryRemove(packageValue.Key, out _);
                                return;
                            }
                            if (DateTime.Now.Subtract(packageValue.Value.PackageInfo.CreateTime).TotalSeconds < 35 ||
                                packageValue.Value.PackageExitUpdateItems?.Any(a =>
                                    a.InstructionType == InstructionType.SendSorting) != true) {
                                return;
                            }

                            var valuePair = await uploader.SetParameters(_postApiParam);
                            if (valuePair.Key) {
                                if (!_memoryCache.TryGetValue(packageValue.Key, out _)) {
                                    _memoryCache.Set(packageValue.Key, packageValue.Value, new MemoryCacheEntryOptions {
                                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                                    });
                                    uploader.UploadInBackground(packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty, packageValue.Value.PackageInfo?.WeightInfo?.FormattedWeight ?? 0,
                                        packageValue.Value.PackageInfo?.BarCodeInfo?.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo(), other:
                                        packageValue.Value.ApiResponse.UploadResponse, token: token);
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交");
                                }
                            }
                            break;
                    }
                    //判断推送锁格(条码、原格口、包裹信息)
                    //推送集包信息
                    EventAggregator.Instance.Publish(new PushPackageInfo() {
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
            finally {
                _takePackageSlim.Release();
            }
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