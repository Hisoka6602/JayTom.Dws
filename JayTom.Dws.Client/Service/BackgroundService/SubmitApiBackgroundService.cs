using System;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using JayTom.Dws.Interface;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Interface.Wdt;
using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.PluginInterface;
using JayTom.Dws.Interface.geek_;
using JayTom.Dws.Interface.Sunnen;
using JayTom.Dws.Interface.JdyWms;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Interface.Szjy188;
using System.Collections.Concurrent;
using JayTom.Dws.Interface.Routdata;
using JayTom.Dws.Interface.Jtexpress;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Service.ImageStorage;
using UploadResponse = JayTom.Dws.Interface.UploadResponse;

namespace JayTom.Dws.Client.Service.BackgroundService {

    /// <summary>
    /// Api提交处理器
    /// </summary>
    public class SubmitApiBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfigRepository _configRepository;
        private readonly IImageStorageService _imageStorageService;
        private ConcurrentQueue<SubmitItemInfo> _submitItems = new();
        private ApiSettingsDto? _apiSettingsDto;
        private static DefaultApi.DefaultApiParameters _defaultApiParameters = new();
        private static SzjyApi.ApiParameter _szjyApiParam = new();
        private static WdtWmsApi.ApiParameter _wdtWmsApiParameter = new();
        private static WdtFlagshipApi.ApiParameter _wdtFlagshipApiParameter = new();
        private static JtExpressApi.ApiParameter _jtExpressApiParam = new();
        private static RoutDataApi.ApiParameters _rstDataApiParam = new();
        private ConcurrentQueue<SavedImageInfo> _savedImageItems = new();

        #region 非通用版本变量(临时)

        private static string _sunnenApiPackage = string.Empty;
        private static bool _isWindowsClose;

        #endregion 非通用版本变量(临时)

        public SubmitApiBackgroundService(IHttpClientFactory httpClientFactory,
            IConfigRepository configRepository, IImageStorageService imageStorageService) {
            _httpClientFactory = httpClientFactory;
            _configRepository = configRepository;
            _imageStorageService = imageStorageService;
            EventAggregator.Instance.Subscribe<PackageInfo>(item => {
                if (item is PackageInfo model) {
                    if (model.BarCodeInfo != null) {
                        _submitItems.Enqueue(new SubmitItemInfo() {
                            Barcode = model?.BarCodeInfo?.Barcode ?? string.Empty,
                            Height = (float)(model?.VolumeInfo?.FormattedHeight ?? 0),
                            ScanTime = model?.BarCodeInfo?.ScanTime ?? DateTime.Now,
                            Weight = (float)(model.WeightInfo?.FormattedWeight ?? 0),
                            Length = (float)(model.VolumeInfo?.FormattedLength ?? 0),
                            Width = (float)(model.VolumeInfo?.FormattedWidth ?? 0),
                            Volume = (float)(model.VolumeInfo?.FormattedVolume ?? 0),
                            Guid = model.Guid,
                            IsCreatedByLowerMachine = (bool)model?.IsCreatedByLowerMachine,
                            PackageCreationInstruction = model?.PackageCreationInstruction ?? string.Empty,
                            //图片暂时不写
                        });
                    }
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                if (item is SettingsChangedEvent model) {
                    if (model.SettingsName.Equals("ApiSettings")) {
                        var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("ApiSettings"));
                        if (configInfoModel is not null) {
                            try {
                                _apiSettingsDto = JsonConvert.DeserializeObject<ApiSettingsDto>(configInfoModel.Value);
                            }
                            catch (Exception e) {
                                //抛出异常事件
                                Console.WriteLine(e);
                            }
                        }
                    }
                    else if (model.SettingsName.Equals("DefaultApiParameters")) {
                        //默认上传接口改参数
                        var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("DefaultApiParameters"));
                        if (configInfoModel is not null) {
                            try {
                                var defaultApiDto = JsonConvert.DeserializeObject<DefaultApiDto>(configInfoModel.Value);
                                if (defaultApiDto != null) {
                                    _defaultApiParameters = new DefaultApi.DefaultApiParameters() {
                                        CompleteMatch = defaultApiDto.CompleteMatch,
                                        IsUseJsonUpload = defaultApiDto.IsUseJsonUpload,
                                        JsonTemplate = defaultApiDto.JsonTemplate,
                                        RegularExpression = defaultApiDto.RegularExpression,
                                        StringContains = defaultApiDto.StringContains,
                                        Timeout = defaultApiDto.Timeout,
                                        StringTemplate = defaultApiDto.StringTemplate,
                                        Url = defaultApiDto.Url,
                                        ValidationMode = (int)defaultApiDto.ValidationMode,
                                    };
                                }
                            }
                            catch (Exception e) {
                                //抛出异常事件
                                Console.WriteLine(e);
                            }
                        }
                    }
                    else if (model.SettingsName.Equals("SzjyApiParameters")) {
                        //默认上传接口改参数
                        var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("SzjyApiParameters"));
                        if (configInfoModel is not null) {
                            try {
                                var szjyApiDto = JsonConvert.DeserializeObject<SzjyApiDto>(configInfoModel.Value);
                                if (szjyApiDto != null) {
                                    _szjyApiParam = new SzjyApi.ApiParameter() {
                                        Machine = szjyApiDto.Machine,
                                        Password = szjyApiDto.Password,
                                        TimeOut = szjyApiDto.TimeOut,
                                        UserName = szjyApiDto.UserName,
                                        Url = szjyApiDto.Url,
                                    };
                                }
                            }
                            catch (Exception e) {
                                //抛出异常事件
                                Console.WriteLine(e);
                            }
                        }
                    }
                    else if (model.SettingsName.Equals("WdtWmsApiParameters")) {
                        //默认上传接口改参数
                        var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("WdtWmsApiParameters"));
                        if (configInfoModel is not null) {
                            try {
                                var wdtWmsApiDto = JsonConvert.DeserializeObject<WdtWmsApiDto>(configInfoModel.Value);
                                if (wdtWmsApiDto != null) {
                                    _wdtWmsApiParameter = new WdtWmsApi.ApiParameter {
                                        AppKey = wdtWmsApiDto.AppKey,
                                        AppSecret = wdtWmsApiDto.AppSecret,
                                        TimeOut = wdtWmsApiDto.TimeOut,
                                        Method = wdtWmsApiDto.Method,
                                        Url = wdtWmsApiDto.Url,
                                        Sid = wdtWmsApiDto.Sid
                                    };
                                }
                            }
                            catch (Exception e) {
                                //抛出异常事件
                                Console.WriteLine(e);
                            }
                        }
                    }
                    else if (model.SettingsName.Equals("WdtFlagshipApiParameters")) {
                        //默认上传接口改参数
                        var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("WdtFlagshipApiParameters"));
                        if (configInfoModel is not null) {
                            try {
                                var wdtFlagshipApiDto = JsonConvert.DeserializeObject<WdtFlagshipApiDto>(configInfoModel.Value);
                                if (wdtFlagshipApiDto != null) {
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
                                }
                            }
                            catch (Exception e) {
                                //抛出异常事件
                                Console.WriteLine(e);
                            }
                        }
                    }
                    else if (model.SettingsName.Equals("JtExpressApiParameters")) {
                        //默认上传接口改参数
                        var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("JtExpressApiParameters"));
                        if (configInfoModel is not null) {
                            try {
                                var jtExpressDto = JsonConvert.DeserializeObject<JtExpressDto>(configInfoModel.Value);
                                if (jtExpressDto != null) {
                                    _jtExpressApiParam = new JtExpressApi.ApiParameter {
                                        AppSecret = jtExpressDto.AppSecret,
                                        AppKey = jtExpressDto.AppKey,
                                        BusinessType = (JtExpressApi.BusinessType)jtExpressDto.BusinessType,
                                        Password = jtExpressDto.Password,
                                        ScanPda = jtExpressDto.ScanPda,
                                        ScanType = jtExpressDto.ScanType,
                                        ScanTypeCode = jtExpressDto.ScanTypeCode,
                                        SegmentCodeTimeOut = jtExpressDto.SegmentCodeTimeOut,
                                        SegmentCodeUrl = jtExpressDto.SegmentCodeUrl,
                                        TimeOut = jtExpressDto.TimeOut,
                                        TransportTypeCode = jtExpressDto.TransportTypeCode,
                                        Url = jtExpressDto.Url,
                                        UserName = jtExpressDto.UserName,
                                        WeightFlag = jtExpressDto.WeightFlag,
                                    };
                                }
                            }
                            catch (Exception e) {
                                //抛出异常事件
                                Console.WriteLine(e);
                            }
                        }
                    }
                    else if (model.SettingsName.Equals("RoutDataApiParameters")) {
                        var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("RoutDataApiParameters"));
                        if (configInfoModel is not null) {
                            try {
                                var routDataApiDto = JsonConvert.DeserializeObject<RoutDataApiDto>(configInfoModel.Value);
                                if (routDataApiDto != null) {
                                    _rstDataApiParam = new RoutDataApi.ApiParameters() {
                                        Url = routDataApiDto.Url,
                                        TimeOut = routDataApiDto.TimeOut,
                                        DeviceCode = routDataApiDto.DeviceCode,
                                        RetryCount = routDataApiDto.RetryCount,
                                        RetryInterval = routDataApiDto.RetryInterval,
                                        SignKey = routDataApiDto.SignKey,
                                        OrgCode = routDataApiDto.OrgCode
                                    };
                                }
                            }
                            catch (Exception e) {
                                //抛出异常事件
                                Console.WriteLine(e);
                            }
                        }
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
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //读参数
            await ReadDefaultConfig();
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                //取出
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
                                IsCreatedByLowerMachine = info.IsCreatedByLowerMachine
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
                    Task.Factory.StartNew(() => {
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
                        }
                    });
                }

                await Task.Delay(10, stoppingToken);
            }
        }

        private async Task ReadDefaultConfig() {
            //上传类型
            var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("ApiSettings"));
            if (configInfoModel is not null) {
                try {
                    _apiSettingsDto = JsonConvert.DeserializeObject<ApiSettingsDto>(configInfoModel.Value);
                }
                catch (Exception e) {
                    //抛出异常事件
                    Console.WriteLine(e);
                }
            }
            //默认接口
            configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("DefaultApiParameters"));
            if (configInfoModel is not null) {
                try {
                    var defaultApiDto = JsonConvert.DeserializeObject<DefaultApiDto>(configInfoModel.Value);
                    if (defaultApiDto != null) {
                        _defaultApiParameters = new DefaultApi.DefaultApiParameters() {
                            CompleteMatch = defaultApiDto.CompleteMatch,
                            IsUseJsonUpload = defaultApiDto.IsUseJsonUpload,
                            JsonTemplate = defaultApiDto.JsonTemplate,
                            RegularExpression = defaultApiDto.RegularExpression,
                            StringContains = defaultApiDto.StringContains,
                            Timeout = defaultApiDto.Timeout,
                            StringTemplate = defaultApiDto.StringTemplate,
                            Url = defaultApiDto.Url,
                            ValidationMode = (int)defaultApiDto.ValidationMode,
                        };
                    }
                }
                catch (Exception e) {
                    //抛出异常事件
                    Console.WriteLine(e);
                }
            }
            //神州
            configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("SzjyApiParameters"));
            if (configInfoModel is not null) {
                try {
                    var szjyApiDto = JsonConvert.DeserializeObject<SzjyApiDto>(configInfoModel.Value);
                    if (szjyApiDto != null) {
                        _szjyApiParam = new SzjyApi.ApiParameter() {
                            Machine = szjyApiDto.Machine,
                            Password = szjyApiDto.Password,
                            TimeOut = szjyApiDto.TimeOut,
                            UserName = szjyApiDto.UserName,
                            Url = szjyApiDto.Url,
                        };
                    }
                }
                catch (Exception e) {
                    //抛出异常事件
                    Console.WriteLine(e);
                }
            }
            //旺店通Wms
            configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("WdtWmsApiParameters"));
            if (configInfoModel is not null) {
                try {
                    var wdtWmsApiDto = JsonConvert.DeserializeObject<WdtWmsApiDto>(configInfoModel.Value);
                    if (wdtWmsApiDto != null) {
                        _wdtWmsApiParameter = new WdtWmsApi.ApiParameter {
                            AppKey = wdtWmsApiDto.AppKey,
                            AppSecret = wdtWmsApiDto.AppSecret,
                            TimeOut = wdtWmsApiDto.TimeOut,
                            Method = wdtWmsApiDto.Method,
                            Url = wdtWmsApiDto.Url,
                            Sid = wdtWmsApiDto.Sid
                        };
                    }
                }
                catch (Exception e) {
                    //抛出异常事件
                    Console.WriteLine(e);
                }
            }
            //旺店通旗舰版
            configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("WdtFlagshipApiParameters"));
            if (configInfoModel is not null) {
                try {
                    var wdtFlagshipApiDto = JsonConvert.DeserializeObject<WdtFlagshipApiDto>(configInfoModel.Value);
                    if (wdtFlagshipApiDto != null) {
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
                    }
                }
                catch (Exception e) {
                    //抛出异常事件
                    Console.WriteLine(e);
                }
            }
            //极兔
            configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("JtExpressApiParameters"));
            if (configInfoModel is not null) {
                try {
                    var jtExpressDto = JsonConvert.DeserializeObject<JtExpressDto>(configInfoModel.Value);
                    if (jtExpressDto != null) {
                        _jtExpressApiParam = new JtExpressApi.ApiParameter {
                            AppSecret = jtExpressDto.AppSecret,
                            AppKey = jtExpressDto.AppKey,
                            BusinessType = (JtExpressApi.BusinessType)jtExpressDto.BusinessType,
                            Password = jtExpressDto.Password,
                            ScanPda = jtExpressDto.ScanPda,
                            ScanType = jtExpressDto.ScanType,
                            ScanTypeCode = jtExpressDto.ScanTypeCode,
                            SegmentCodeTimeOut = jtExpressDto.SegmentCodeTimeOut,
                            SegmentCodeUrl = jtExpressDto.SegmentCodeUrl,
                            TimeOut = jtExpressDto.TimeOut,
                            TransportTypeCode = jtExpressDto.TransportTypeCode,
                            Url = jtExpressDto.Url,
                            UserName = jtExpressDto.UserName,
                            WeightFlag = jtExpressDto.WeightFlag,
                        };
                    }
                }
                catch (Exception e) {
                    //抛出异常事件
                    Console.WriteLine(e);
                }
            }
            //络道科技Api
            configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("RoutDataApiParameters"));
            if (configInfoModel is not null) {
                try {
                    var routDataApiDto = JsonConvert.DeserializeObject<RoutDataApiDto>(configInfoModel.Value);
                    if (routDataApiDto != null) {
                        _rstDataApiParam = new RoutDataApi.ApiParameters() {
                            Url = routDataApiDto.Url,
                            TimeOut = routDataApiDto.TimeOut,
                            DeviceCode = routDataApiDto.DeviceCode,
                            RetryCount = routDataApiDto.RetryCount,
                            RetryInterval = routDataApiDto.RetryInterval,
                            SignKey = routDataApiDto.SignKey,
                            OrgCode = routDataApiDto.OrgCode
                        };
                    }
                }
                catch (Exception e) {
                    //抛出异常事件
                    Console.WriteLine(e);
                }
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
        }
    }
}