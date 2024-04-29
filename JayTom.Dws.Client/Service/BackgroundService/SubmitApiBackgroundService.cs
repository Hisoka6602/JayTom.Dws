using System;
using ImTools;
using System.Net;
using System.Linq;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using JayTom.Dws.Interface;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Interface.Wdt;
using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.PluginInterface;
using JayTom.Dws.Interface.geek_;
using System.Collections.Generic;
using NPOI.XSSF.Streaming.Values;
using JayTom.Dws.Interface.Sunnen;
using JayTom.Dws.Interface.JdyWms;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Interface.Szjy188;
using JayTom.Dws.Interface.CaiNiao;
using System.Collections.Concurrent;
using JayTom.Dws.Interface.Routdata;
using JayTom.Dws.Interface.Jtexpress;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Interface.Eshippingit;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Service.ImageStorage;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using static JayTom.Dws.Interface.CaiNiao.CaiNiaoApi;
using static Aliyun.OSS.Model.ListMultipartUploadsResult;
using UploadResponse = JayTom.Dws.Interface.UploadResponse;
using JayTom.Dws.Domain.DownstreamProtocols.CommunicationProtocols;

namespace JayTom.Dws.Client.Service.BackgroundService {

    /// <summary>
    /// Api提交处理器
    /// </summary>
    public class SubmitApiBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfigRepository _configRepository;
        private readonly IImageStorageService _imageStorageService;
        private readonly IPackageRepository _packageRepository;
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
        private ConcurrentQueue<SavedImageInfo> _savedImageItems = new();
        /*private ConcurrentQueue<CallBackPackageInfo> _callBackItems = new();
        private ConcurrentDictionary<long, SortingExitReceived> _sortingExitItems = new();*/
        private ConcurrentQueue<PackageAggregationInfo> _packageAggregationInfoItems = new();
        private SemaphoreSlim _takePackageSlim = new(1);
        private ConcurrentDictionary<long, PackageSubmissionPushInfo> _packageSubmissionPushItems = new();

        private JtExpressDto _jtExpressDto = new();

        #region 非通用版本变量(临时)

        private static string _sunnenApiPackage = string.Empty;
        private static bool _isWindowsClose;

        #endregion 非通用版本变量(临时)

        public SubmitApiBackgroundService(IHttpClientFactory httpClientFactory,
            IConfigRepository configRepository, IImageStorageService imageStorageService,
            IPackageRepository packageRepository) {
            _httpClientFactory = httpClientFactory;
            _configRepository = configRepository;
            _imageStorageService = imageStorageService;
            _packageRepository = packageRepository;
            //包裹信息完成
            EventAggregator.Instance.Subscribe<PackageInfo>(item => {
                if (item is PackageInfo { BarCodeInfo: not null } model) {
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
                        Timestamp = model?.Timestamp ?? 0
                        //图片暂时不写
                    });

                    //添加到推送队列
                    if (model is not null && model.IsCreatedByLowerMachine) {
                        _packageSubmissionPushItems.TryAdd(new DateTimeOffset(model.CreateTime).ToUnixTimeMilliseconds(), new PackageSubmissionPushInfo() {
                            PackageInfo = model
                        });
                    }
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                if (item is SettingsChangedEvent model) {
                    if (model.SettingsName.Equals("ApiSettings")) {
                        _apiSettingsDto = await _configRepository.FirstOrDefaultEntity<ApiSettingsDto>(model.SettingsName) ?? new ApiSettingsDto();
                    }
                    else if (model.SettingsName.Equals("DefaultApiParameters")) {
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
                    }
                    else if (model.SettingsName.Equals("SzjyApiParameters")) {
                        //默认上传接口改参数
                        var entity = await _configRepository.FirstOrDefaultEntity<SzjyApiDto>(model.SettingsName) ?? new SzjyApiDto();
                        _szjyApiParam = new SzjyApi.ApiParameter() {
                            Machine = entity.Machine,
                            Password = entity.Password,
                            TimeOut = entity.TimeOut,
                            UserName = entity.UserName,
                            Url = entity.Url,
                        };
                    }
                    else if (model.SettingsName.Equals("WdtWmsApiParameters")) {
                        //默认上传接口改参数
                        var entity = await _configRepository.FirstOrDefaultEntity<WdtWmsApiDto>(model.SettingsName) ?? new WdtWmsApiDto();
                        _wdtWmsApiParameter = new WdtWmsApi.ApiParameter {
                            AppKey = entity.AppKey,
                            AppSecret = entity.AppSecret,
                            TimeOut = entity.TimeOut,
                            Method = entity.Method,
                            Url = entity.Url,
                            Sid = entity.Sid
                        };
                    }
                    else if (model.SettingsName.Equals("WdtFlagshipApiParameters")) {
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
                            Salt = entity.Salt,
                            V = entity.V
                        };
                    }
                    else if (model.SettingsName.Equals("JtExpressApiParameters")) {
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
                    }
                    else if (model.SettingsName.Equals("RoutDataApiParameters")) {
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
                    }
                    else if (model.SettingsName.Equals("CaiNiaoApiParameters")) {
                        var entity = await _configRepository.FirstOrDefaultEntity<CaiNiaoApiDto>(model.SettingsName) ?? new CaiNiaoApiDto();
                        _caiNiaoApiParam = new CaiNiaoApi.ApiParameters() {
                            BcrName = entity.BcrName,
                            BcrCode = entity.BcrCode,
                            Source = entity.Source,
                            TimeOut = entity.TimeOut,
                            Url = entity.Url,
                            Version = entity.Version
                        };
                    }
                    else if (model.SettingsName.Equals("EshippingitApiParameters")) {
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
                    }
                    //其他接口
                }
            });
            EventAggregator.Instance.Subscribe<PluginParamChangedEvent>(item => {
                if (item is PluginParamChangedEvent model) {
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
            //包裹完结
            EventAggregator.Instance.Subscribe<CallBackPackageInfo>(async item => {
                if (item is CallBackPackageInfo info) {
                    // _callBackItems.Enqueue(info);

                    try {
                        await _takePackageSlim.WaitAsync();

                        var (key, package) = _packageSubmissionPushItems.FirstOrDefault(f => f.Key.Equals(info.PackageCreateTime));
                        if (package is not null) {
                            package.CallBackPackageInfo = info;
                        }
                    }
                    finally {
                        _takePackageSlim.Release();
                    }
                }
            });
            //集包推送
            EventAggregator.Instance.Subscribe<PackageAggregationInfo>(async item => {
                //加入队列
                if (item is PackageAggregationInfo info) {
                    _packageAggregationInfoItems.Enqueue(info);
                }
            });
            //格口信息
            EventAggregator.Instance.Subscribe<SortingExitReceived>(async item => {
                if (item is SortingExitReceived info) {
                    await Task.Yield();
                    //_sortingExitItems.TryAdd(info.Timestamp, info);
                    try {
                        await _takePackageSlim.WaitAsync();
                        _packageSubmissionPushItems.TryGetValue(info.Timestamp, out var package);
                        if (package?.PackageInfo != null) {
                            info.ExitType = SortingExitType.TheoreticalExit;
                            package?.SortingExitReceivedInfos?.Add(info);
                        }
                    }
                    finally {
                        _takePackageSlim.Release();
                    }
                }
            });
            //分拣指令
            EventAggregator.Instance.Subscribe<InstructionReceived>(async item => {
                await Task.Yield();
                try {
                    await _takePackageSlim.WaitAsync();

                    if (item is InstructionReceived model && model.InstructionInfos?.Any() == true
                       ) {
                        var instructionInfoModel = model.InstructionInfos.FirstOrDefault();
                        if (instructionInfoModel?.InstructionType == InstructionType.PackageException) {
                            //返回异常

                            _packageSubmissionPushItems.TryGetValue(model.Timestamp, out var package);
                            if (package?.PackageInfo != null) {
                                //找到包裹

                                /*if (!string.IsNullOrEmpty(instructionInfoModel?.SortingInfo?.ChecksumProtocolName)) {
                                    var communicationProtocol = (CommunicationProtocol)Enum.Parse(typeof(CommunicationProtocol), instructionInfoModel.SortingInfo.ChecksumProtocolName);
                                    IDeviceCommunicationProtocol? protocol = communicationProtocol switch {
                                        CommunicationProtocol.Wxkc => new WxkcCommunicationProtocol(),
                                        CommunicationProtocol.JT_ST => new JtstCommunicationProtocol(),
                                        CommunicationProtocol.CaiNiao => new CaiNiaoCommunicationProtocol(),
                                        _ => null
                                    };
                                }*/

                                //暂时默认无限创科

                                //更新到包裹
                                IDeviceCommunicationProtocol? protocol = new WxkcCommunicationProtocol();
                                var type = protocol?.SortingExceptionReturnTypeConvert(instructionInfoModel.InstructionContent) ?? SortingExceptionReturnType.None;
                                package.PackageInfo.SortingExceptionReturnTypes.Add(type);

                                //取出格口
                                var commandParsingConvert = protocol?.CommandParsingConvert(instructionInfoModel.InstructionContent);

                                package.SortingExitReceivedInfos?.Add(new SortingExitReceived() {
                                    ExitType = SortingExitType.PhysicalExit,
                                    //后面这里需要实际格口号名称
                                    ExitName = commandParsingConvert?.CompartmentNumber.ToString() ?? string.Empty
                                });
                            }
                        }
                    }
                }
                finally {
                    _takePackageSlim.Release();
                }
            });
            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(item => {
                if (item is ApplicationStatusChanged { Status: ApplicationStatus.Stop }) {
                    _packageSubmissionPushItems.Clear();
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //读参数
            await ReadDefaultConfig();
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                //取出
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
                                });
                                EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                    IsSuccess = uploadResponse?.IsSuccess ?? false,
                                    TriggerPosition = TriggerPositionEnum.HttpOutput
                                });
                            }
                        });
                    }

                    //取出图片
                    var dequeue = _savedImageItems.TryDequeue(out var model);
                    if (dequeue && model is not null && !string.IsNullOrEmpty(model.FilePath) &&
                        model.ImageType == SaveImageType.BarcodeImage) {
                        Task.Factory.StartNew(async () => {
                            //后续上传
                            IDataUploader uploader;
                            UploadResponse? uploadResponse = null;
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
                                        uploadResponse = new UploadResponse() {
                                            ExceptionMsg = value
                                        };
                                        NLog.LogManager.GetCurrentClassLogger().Error("设置参数失败!");
                                    }

                                    break;
                            }
                        });
                    }

                    /*//格口分拣后回调提交
                    var callBackDequeue = _callBackItems.TryDequeue(out var callBackModel);
                    if (callBackDequeue && callBackModel is not null) {
                        Task.Factory.StartNew(async () => {
                            if (callBackModel.PackageInfo is { } packageInfo &&
                                DateTime.Now.Subtract(callBackModel.CallBackTime).TotalMilliseconds >= 0) {
                                //获取返回的格口

                                var (l, sortingExitReceived) = _sortingExitItems.FirstOrDefault(f =>
                                    packageInfo.BarCodeInfo != null &&
                                    f.Value.ScanTime.Equals(packageInfo.BarCodeInfo.ScanTime));

                                if (sortingExitReceived is not null) {
                                    IDataUploader uploader;
                                    UploadResponse? uploadResponse = null;
                                    switch (_apiSettingsDto?.Type) {
                                        case ApiType.None:
                                            _sortingExitItems.TryRemove(l, out _);
                                            return;

                                            /*case ApiType.CaiNiaoApi:
                                                //60秒后再提交
                                                if (callBackModel?.PackageInfo?.BarCodeInfo is not null) {
                                                    //在页面上加个配置设置这个时间
                                                    if (DateTime.Now.Subtract(callBackModel.PackageInfo.BarCodeInfo.ScanTime).TotalSeconds < 60) {
                                                        _callBackItems.Enqueue(callBackModel);
                                                        NLog.LogManager.GetCurrentClassLogger().Error($"小于60秒");
                                                        return;
                                                    }

                                                    uploader = new CaiNiaoApi(_httpClientFactory);
                                                    var (key, value) = await uploader.SetParameters(_caiNiaoApiParam);
                                                    if (key) {
                                                        uploader.UploadInBackground(packageInfo.BarCodeInfo?.Barcode ?? string.Empty, packageInfo.WeightInfo?.FormattedWeight ?? 0,
                                                            packageInfo.BarCodeInfo?.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo() {
                                                                CameraCustomName = packageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                                                                CameraName = packageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                                                                CameraSerialNumber = packageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                                                            }, other: new ReportChuteInfo {
                                                                ChuteCode = callBackModel.ExitNum.ToString(),
                                                                ChuteCodePhysical = sortingExitReceived.ExitName ?? string.Empty,
                                                                ErrorReson = packageInfo.PackageExceptionMsg,
                                                                Status = packageInfo.PackageExceptionStatus,
                                                            }, token: stoppingToken);
                                                    }
                                                    else {
                                                        NLog.LogManager.GetCurrentClassLogger().Error("设置Api参数失败");
                                                    }
                                                    _sortingExitItems.TryRemove(l, out _);
                                                }

                                                break;#1#
                                    }
                                }
                                else {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"未找到sortingExitReceived");
                                    NLog.LogManager.GetCurrentClassLogger().Error($"callBackModel:{callBackModel.ExitNum}");
                                    NLog.LogManager.GetCurrentClassLogger().Error($"_sortingExitItems:{JsonConvert.SerializeObject(_sortingExitItems)}");
                                    _callBackItems.Enqueue(callBackModel);
                                }
                            }
                            else {
                                _callBackItems.Enqueue(callBackModel);
                            }
                        });
                    }

                    //提交的格口列表
                    if (_sortingExitItems.Any()) {
                        IDataUploader? uploader;
                        switch (_apiSettingsDto?.Type) {
                            case ApiType.None:
                                _sortingExitItems.Clear();
                                return;

                            case ApiType.CaiNiaoApi:
                                //判断是否超过60秒,如果超过则强制提交(菜鸟专用)
                                var sortingReport = _sortingExitItems
                                    .Where(w => w.Value.ScanTime != null &&
                                                DateTime.Now.Subtract(w.Value.ScanTime.Value).TotalSeconds >= 60)
                                    ?.ToList();
                                if (sortingReport?.Any() == true) {
                                    uploader = new CaiNiaoApi(_httpClientFactory);
                                    Parallel.ForEach(sortingReport, sValue => {
                                        _sortingExitItems.TryRemove(sValue.Key, out var sortingValue);

                                        if (sortingValue is not null) {
                                            Task.Factory.StartNew(async () => {
                                                /*var (key, value) = await uploader.SetParameters(_caiNiaoApiParam);
                                                if (key) {
                                                    uploader.UploadInBackground(sortingValue.BarCode ?? string.Empty, 0,
                                                        sortingValue.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo() {
                                                            CameraCustomName = string.Empty,
                                                            CameraName = string.Empty,
                                                            CameraSerialNumber = string.Empty,
                                                        }, other: new ReportChuteInfo {
                                                            ChuteCode = sortingValue.ExitName ?? string.Empty,
                                                            ChuteCodePhysical = sortingValue.ExitName ?? string.Empty,
                                                            ErrorReson = (string.IsNullOrEmpty(sortingValue.BarCode) || sortingValue.BarCode.ToLower().Equals("noread") == true) ?
                                                                "无条码" : "分拣成功 ",
                                                            Status = (string.IsNullOrEmpty(sortingValue.BarCode) || sortingValue.BarCode.ToLower().Equals("noread") == true) ?
                                                                1 : 0,
                                                        }, token: stoppingToken);
                                                }
                                                else {
                                                    NLog.LogManager.GetCurrentClassLogger().Error("设置Api参数失败");
                                                }#1#
                                                var callBackPackageInfo = _callBackItems.FirstOrDefault(f =>
                                                    f.PackageInfo.Timestamp.Equals(sortingValue.Timestamp));
                                                if (callBackPackageInfo is not null && callBackPackageInfo.PackageInfo is { } packageInfo) {
                                                    var (key, value) = await uploader.SetParameters(_caiNiaoApiParam);
                                                    if (key) {
                                                        uploader.UploadInBackground(packageInfo.BarCodeInfo?.Barcode ?? string.Empty, packageInfo.WeightInfo?.FormattedWeight ?? 0,
                                                            packageInfo.BarCodeInfo?.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo() {
                                                                CameraCustomName = packageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                                                                CameraName = packageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                                                                CameraSerialNumber = packageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                                                            }, other: new ReportChuteInfo {
                                                                ChuteCode = callBackPackageInfo.ExitNum.ToString(),
                                                                ChuteCodePhysical = sortingValue.ExitName ?? string.Empty,
                                                                ErrorReson = packageInfo.PackageExceptionMsg,
                                                                Status = packageInfo.PackageExceptionStatus,
                                                            }, token: stoppingToken);
                                                    }
                                                    else {
                                                        NLog.LogManager.GetCurrentClassLogger().Error("设置Api参数失败");
                                                    }
                                                }
                                            }, stoppingToken);
                                        }
                                    });
                                }
                                break;

                            case ApiType.JtExpressApi:
                                uploader = new JtExpressApi(_httpClientFactory);
                                var (b, s) = await uploader.SetParameters(_jtExpressApiParam);
                                Parallel.ForEach(_sortingExitItems, sValue => {
                                    _sortingExitItems.TryRemove(sValue.Key, out var sortingValue);
                                    if (sortingValue is not null) {
                                        if (_jtExpressDto.IsUploadAfterReturn && sortingValue.Type == ExitType.AbnormalExit) {
                                            return;
                                        }

                                        Task.Factory.StartNew(async () => {
                                            var keyValuePair = await uploader.SetParameters(_jtExpressApiParam);
                                            if (keyValuePair.Key) {
                                                uploader.UploadInBackground(sortingValue.BarCode ?? string.Empty, sortingValue.SortingParam?.Weight ?? 0,
                                                    sortingValue.SortingParam?.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo(), other:
                                                    sortingValue.SortingParam?.ApiResponse ?? new UploadResponse(), token: stoppingToken);
                                            }
                                            else {
                                                NLog.LogManager.GetCurrentClassLogger().Error("设置Api参数失败");
                                            }
                                        }, stoppingToken);
                                    }
                                });
                                break;

                            default: {
                                    _sortingExitItems.Clear();
                                    break;
                                }
                        }
                    }*/

                    //获取包裹
                    var pairs = _packageSubmissionPushItems?.Any(f => (f.Value.SortingExitReceivedInfos?.Any() == true || f.Value.CallBackPackageInfo is not null)
                                                                              && f.Value.PackageInfo is not null) == true
                        ? _packageSubmissionPushItems?.Where(f => (f.Value.SortingExitReceivedInfos?.Any() == true || f.Value.CallBackPackageInfo is not null)
                                                                  && f.Value.PackageInfo is not null)?.ToList()
                        : new List<KeyValuePair<long, PackageSubmissionPushInfo>>();

                    if (pairs?.Any() == true) {
                        IDataUploader? uploader = _apiSettingsDto switch { { Type: ApiType.CaiNiaoApi } => new CaiNiaoApi(_httpClientFactory), { Type: ApiType.JtExpressApi } => new JtExpressApi(_httpClientFactory),
                            _ => null
                        };

                        if (uploader is not null) {
                            Parallel.ForEach(pairs, async packageValue => {
                                Task.Factory.StartNew(async () => {
                                    try {
                                        await _takePackageSlim.WaitAsync(stoppingToken);
                                        //提交
                                        if (packageValue.Value is { PackageInfo: not null } && packageValue.Value.SortingExitReceivedInfos?.Any() == true) {
                                            switch (_apiSettingsDto) {
                                                case { Type: ApiType.CaiNiaoApi }:
                                                    //判断提交(从创建包裹时间开始判断) 60秒
                                                    if (DateTime.Now.Subtract(packageValue.Value.PackageInfo.CreateTime).TotalSeconds < 50) {
                                                        return;
                                                    }
                                                    var (key, value) = await uploader.SetParameters(_caiNiaoApiParam);
                                                    if (key) {
                                                        var caiNiaoStatusConvert = CaiNiaoStatusConvert(
                                                            packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty,
                                                            packageValue.Value.PackageInfo.SortingExceptionReturnTypes);

                                                        uploader.UploadInBackground(packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty, packageValue.Value.PackageInfo.WeightInfo?.FormattedWeight ?? 0,
                                                            packageValue.Value.PackageInfo.BarCodeInfo?.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo() {
                                                                CameraCustomName = packageValue.Value.PackageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                                                                CameraName = packageValue.Value.PackageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                                                                CameraSerialNumber = packageValue.Value.PackageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                                                            }, other: new ReportChuteInfo {
                                                                ChuteCode = packageValue.Value.SortingExitReceivedInfos?.LastOrDefault(l => l.ExitType == SortingExitType.PhysicalExit)?.ExitName ?? (packageValue.Value.SortingExitReceivedInfos?.FirstOrDefault(l => l.ExitType == SortingExitType.TheoreticalExit)?.ExitName ?? string.Empty),
                                                                ChuteCodePhysical = packageValue.Value.SortingExitReceivedInfos?.FirstOrDefault(l => l.ExitType == SortingExitType.TheoreticalExit)?.ExitName ?? string.Empty,
                                                                ErrorReson = caiNiaoStatusConvert.Value,
                                                                Status = caiNiaoStatusConvert.Key,
                                                            }, token: stoppingToken);
                                                    }
                                                    else {
                                                        NLog.LogManager.GetCurrentClassLogger().Error("设置Api参数失败");
                                                    }
                                                    break;

                                                case { Type: ApiType.JtExpressApi }:
                                                    if (_jtExpressDto.IsUploadAfterReturn && packageValue.Value.SortingExitReceivedInfos.Any(a => a.Type == ExitType.AbnormalExit)) {
                                                        //删除这条
                                                        _packageSubmissionPushItems?.TryRemove(packageValue);
                                                        return;
                                                    }

                                                    var keyValuePair = await uploader.SetParameters(_jtExpressApiParam);
                                                    if (keyValuePair.Key) {
                                                        uploader.UploadInBackground(packageValue.Value.PackageInfo.BarCodeInfo?.Barcode ?? string.Empty, packageValue.Value.SortingExitReceivedInfos?.FirstOrDefault()?.SortingParam?.Weight ?? 0,
                                                            packageValue.Value.SortingExitReceivedInfos?.FirstOrDefault()?.SortingParam?.ScanTime ?? DateTime.Now, imageInfo: new UploadImageInfo(), other:
                                                            packageValue.Value.SortingExitReceivedInfos?.FirstOrDefault()?.SortingParam?.ApiResponse ?? new UploadResponse(), token: stoppingToken);
                                                    }
                                                    else {
                                                        NLog.LogManager.GetCurrentClassLogger().Error("设置Api参数失败");
                                                    }

                                                    await uploader.SetParameters(_jtExpressApiParam);
                                                    break;
                                            }
                                            //删除这条
                                            _packageSubmissionPushItems?.TryRemove(packageValue);
                                        }
                                    }
                                    finally {
                                        _takePackageSlim.Release();
                                    }
                                });
                            });
                        }
                    }

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
                        });
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }

                await Task.Delay(10, stoppingToken);
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
                Sid = wdtWmsApiDto.Sid
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
        }

        private KeyValuePair<int, string> CaiNiaoStatusConvert(string barcode, List<SortingExceptionReturnType> sortingExceptionReturnTypes) {
            if (string.IsNullOrEmpty(barcode) || barcode.ToLower().Equals("noread")) {
                return new KeyValuePair<int, string>(1, "无条码");
            }

            var lastOrDefault = sortingExceptionReturnTypes?.LastOrDefault();
            if (lastOrDefault == SortingExceptionReturnType.Locked) {
                return new KeyValuePair<int, string>(3, "锁格");
            }
            return lastOrDefault is not null ? new KeyValuePair<int, string>(6, lastOrDefault.Value.GetDescription()) : new KeyValuePair<int, string>(0, "分拣成功");
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
            /// 返回分拣信息(列表)
            /// </summary>
            public List<SortingExitReceived>? SortingExitReceivedInfos { get; set; } = new();

            /// <summary>
            /// 包裹完结信息
            /// </summary>
            public CallBackPackageInfo? CallBackPackageInfo { get; set; }
        }
    }
}