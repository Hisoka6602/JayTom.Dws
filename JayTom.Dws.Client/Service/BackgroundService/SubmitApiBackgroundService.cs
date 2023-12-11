using System;
using System.Linq;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using JayTom.Dws.Interface;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Interface.Wdt;
using JayTom.Dws.PluginInterface;
using System.Collections.Generic;
using JayTom.Dws.Interface.Sunnen;
using JayTom.Dws.Interface.JdyWms;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Interface.Szjy188;
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using UploadResponse = JayTom.Dws.Interface.UploadResponse;

namespace JayTom.Dws.Client.Service.BackgroundService {

    /// <summary>
    /// Api提交处理器
    /// </summary>
    public class SubmitApiBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfigRepository _configRepository;
        private ConcurrentQueue<SubmitItemInfo> _submitItems = new();
        private ApiSettingsDto? _apiSettingsDto;
        private static DefaultApi.DefaultApiParameters _defaultApiParameters = new();
        private static SzjyApi.ApiParameter _szjyApiParam = new();
        private static WdtWmsApi.ApiParameter _wdtWmsApiParameter = new();
        private static WdtFlagshipApi.ApiParameter _wdtFlagshipApiParameter = new();

        #region 非通用版本变量(临时)

        private static string _sunnenApiPackage = string.Empty;

        #endregion 非通用版本变量(临时)

        public SubmitApiBackgroundService(IHttpClientFactory httpClientFactory, IConfigRepository configRepository) {
            _httpClientFactory = httpClientFactory;
            _configRepository = configRepository;
            EventAggregator.Instance.Subscribe<PackageInfo>(item => {
                if (item is PackageInfo model) {
                    if (!string.IsNullOrEmpty(model.BarCode)) {
                        _submitItems.Enqueue(new SubmitItemInfo() {
                            Barcode = model.BarCode,
                            Weight = (float)(model.Weight ?? 0),
                            Length = (float)(model.Length ?? 0),
                            Width = (float)(model.Width ?? 0),
                            Height = (float)(model.Height ?? 0),
                            Volume = (float)(model.Volume ?? 0),
                            ScanTime = model.ScanTime,
                            Guid = model.Guid,
                            PanoramaCameraCount = model.PanoramaCameraCount,
                            //图片暂时不写
                        });
                    }
                }
            });
            //图片
            EventAggregator.Instance.Subscribe<ImageMessageInfo>(async info => {
                if (info is ImageMessageInfo imageInfo) {
                    await Task.Delay(200);
                    var submitItemInfo = _submitItems?.FirstOrDefault(f =>
                        (bool)f.Barcode?.Equals(imageInfo.BarCode));
                    if (submitItemInfo is not null && imageInfo.Image is not null) {
                        Image copyImage = new Bitmap(imageInfo.Image);

                        if (imageInfo.Type == SaveImageType.BarcodeImage) {
                            submitItemInfo.BarCodeImage = new UploadImageInfo() {
                                CameraName = imageInfo.CameraName,
                                CameraCustomName = imageInfo.CameraCustomName,
                                CameraSerialNumber = imageInfo.CameraSerialNumber,
                                Image = copyImage
                            };
                        }
                        else if (imageInfo.Type == SaveImageType.PanoramaImage) {
                            submitItemInfo.PanoramaImages ??= new List<UploadImageInfo>();
                            submitItemInfo.PanoramaImages.Add(new UploadImageInfo() {
                                CameraName = imageInfo.CameraName,
                                CameraCustomName = imageInfo.CameraCustomName,
                                CameraSerialNumber = imageInfo.CameraSerialNumber,
                                Image = copyImage
                            });
                        }
                    }
                    else {
                        NLog.LogManager.GetCurrentClassLogger().Error($"获取不到:{submitItemInfo?.Barcode}--{imageInfo?.BarCode}");
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
                                        IsUploadPanoramaImage = defaultApiDto.IsUploadPanoramaImage,
                                        IsUploadScanImage = defaultApiDto.IsUploadScanImage,
                                        IsUseUploadImage = defaultApiDto.IsUseUploadImage
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
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //读参数
            await ReadDefaultConfig();
            while (!stoppingToken.IsCancellationRequested) {
                //取出
                //需要判断用户选择的接口和参数设置

                //判断拦截
                if (_apiSettingsDto?.Type == ApiType.DefaultApi &&
                    _defaultApiParameters.IsUseUploadImage &&
                    _submitItems.FirstOrDefault()?.BarCodeImage is null &&
                    _submitItems.FirstOrDefault()?.PanoramaCameraCount != _submitItems.FirstOrDefault()?.PanoramaImages?.Count) {
                    continue;
                }

                var tryDequeue = _submitItems.TryDequeue(out var info);

                if (tryDequeue && info is not null) {
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
                                        if (_defaultApiParameters.IsUseUploadImage) {
                                            if (info.PanoramaCameraCount == info.PanoramaImages?.Count &&
                                                info.BarCodeImage is not null) {
                                                uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                    info.Weight, info.ScanTime,
                                                    info.Length, info.Width,
                                                    info.Height, info.Volume,
                                                    info.BarCodeImage, info.PanoramaImages,
                                                    null, stoppingToken);
                                            }
                                            else {
                                                NLog.LogManager.GetCurrentClassLogger().Error("跳过上传");
                                            }
                                        }
                                        else {
                                            uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
                                                info.Weight, info.ScanTime,
                                                info.Length, info.Width,
                                                info.Height, info.Volume,
                                                info.BarCodeImage, info.PanoramaImages,
                                                null, stoppingToken);
                                        }
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
                                        info.BarCodeImage, info.PanoramaImages,
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
                                            info.BarCodeImage, info.PanoramaImages,
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
                                            info.BarCodeImage, info.PanoramaImages,
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
                                            info.BarCodeImage, info.PanoramaImages,
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
                                        info.BarCodeImage, info.PanoramaImages,
                                        null, stoppingToken);
                                    break;
                                }
                        }
                        if (_apiSettingsDto?.Type is not null &&
                            _apiSettingsDto.Type != ApiType.None &&
                            uploadResponse is not null) {
                            //临时单线程
                            EventAggregator.Instance.Publish(new ApiResponseReceived {
                                Guid = info.Guid,
                                Barcode = info.Barcode,
                                ScanTime = info.ScanTime,
                                UploadResponse = uploadResponse
                            });
                            EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                IsSuccess = uploadResponse?.IsSuccess ?? false,
                                TriggerPosition = TriggerPositionEnum.HttpOutput
                            });
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
                            IsUploadPanoramaImage = defaultApiDto.IsUploadPanoramaImage,
                            IsUploadScanImage = defaultApiDto.IsUploadScanImage,
                            IsUseUploadImage = defaultApiDto.IsUseUploadImage
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
            public UploadImageInfo? BarCodeImage { get; set; }

            /// <summary>
            /// 全景图
            /// </summary>
            public List<UploadImageInfo>? PanoramaImages { get; set; }

            /// <summary>
            /// 全景相机数量
            /// </summary>
            public int PanoramaCameraCount { get; set; }
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