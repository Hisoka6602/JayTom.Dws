using System;
using System.Linq;
using System.Drawing;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Camera;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.Sorting;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.Client.Service.ImageStorage;
using JayTom.Dws.Client.Service.ResultOutput;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Service.ExternalDataService;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class PackageBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IDeviceService _deviceService;
        private readonly IResultOutputService _resultOutputService;
        private readonly IImageStorageService _imageStorageService;
        private readonly IExternalDataService _externalDataService;
        private readonly IConfigRepository _configRepository;
        private readonly ISortingService _sortingService;
        private ExternalDataSourceEventArgs _externalDataSource = new();
        private List<ConfigInfoModel> _configInfoModels = new();
        private CommunicationsSettingsDto _communicationsSettingsDto = new();
        private VolumeSettingsDto _volumeSettingsDto = new();
        private WeightSettingsDto _weightSettingsDto = new();
        private List<ICamera> _cameras = new();
        private ConcurrentQueue<CameraImageInfo> _panoramicImageItems = new();
        private ConcurrentQueue<CameraImageInfo> _volumeCameraImageItems = new();
        private ConcurrentQueue<PackageInfo> _packageInfos = new();
        private int[] _timeIntervals;

        public PackageBackgroundService(IDeviceService deviceService,
            IResultOutputService resultOutputService,
            IImageStorageService imageStorageService,
            IExternalDataService externalDataService, IConfigRepository configRepository,
            ISortingService sortingService) {
            _deviceService = deviceService;
            _resultOutputService = resultOutputService;
            _imageStorageService = imageStorageService;
            _externalDataService = externalDataService;
            _configRepository = configRepository;
            _sortingService = sortingService;
            //获取间隔数组
            try {
                IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath($"{AppContext.BaseDirectory}")
                    .AddJsonFile("TimeIntervals.json", optional: false, reloadOnChange: true)
                    .Build();
                _timeIntervals = configuration.GetSection("TimeIntervals").Get<int[]>() ?? Array.Empty<int>();
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            //相机
            _deviceService.CameraInitialized += delegate (object? sender, List<ICamera> list) {
                _cameras = list;
            };
            //扫码
            _deviceService.BarcodeScanned += async delegate (object? sender, BarcodeReadEventArgs args) {
                if (_communicationsSettingsDto.Type == CommunicationsType.None ||
                    !_communicationsSettingsDto.DeviceControlSettingsInfo.IsUseCreatePackageByDevice) {
                    var packageInfo = new PackageInfo() {
                        Guid = args.Timestamp,
                        BarCode = args.Barcode,
                        CameraSerialNumber = args.CameraSerialNumber,
                        Image = args.Image,
                        ScanTime = args.ScanTime,
                        Timestamp = args.Timestamp,
                    };
                    _packageInfos.Enqueue(packageInfo);
                    EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                        IsSuccess = true,
                        TriggerPosition = TriggerPositionEnum.PackageTrigger
                    });
                    //触发全景拍照
                    var enumerable = _cameras.Where(w => w.BindingType == CameraBindingType.PanoramicCamera);
                    foreach (var c in enumerable) {
                        if (c is IIndustrialCamera camera && _deviceService.RunningStatus) {
                            //循环次数
                            foreach (var timeInterval in _timeIntervals ?? Array.Empty<int>()) {
                                await Task.Delay(timeInterval);
                                await camera.TakePhotoAsync(args.Barcode, args.Timestamp);
                            }
                        }
                    }
                    //获取外部数据
                    //体积
                    if (_externalDataSource.IsVolumeInput) {
                        await _externalDataService.GetVolume(args.Barcode);
                    }
                }
                else {
                    var info = _packageInfos.OrderBy(o => o.CreateTime).FirstOrDefault(f => f.BarCode == null);
                    if (info != null) {
                        info.BarCode = args.Barcode;
                        info.CameraSerialNumber = args.CameraSerialNumber;
                        info.Image = args.Image;
                        info.ScanTime = args.ScanTime;
                        info.Timestamp = args.Timestamp;
                    }
                }
            };
            //空包裹
            _deviceService.NotBarcodeHitEvent += async delegate (object? sender, BarcodeReadEventArgs args) {
                await Task.Delay(200);
                //等200ms,如果仍没有创建包裹则由空条码创建创建(有危险，看实际应用调整)
                var orDefault = _packageInfos.OrderBy(o => o.CreateTime)
                    .FirstOrDefault(f => DateTime.Now.Subtract(f.CreateTime).TotalMilliseconds < 500);
                if (orDefault is null) {
                    if (_communicationsSettingsDto.Type == CommunicationsType.None ||
                        !_communicationsSettingsDto.DeviceControlSettingsInfo.IsUseCreatePackageByDevice) {
                        var packageInfo = new PackageInfo() {
                            Guid = args.Timestamp,
                            BarCode = args.Barcode,
                            CameraSerialNumber = args.CameraSerialNumber,
                            Image = args.Image,
                            ScanTime = args.ScanTime,
                            Timestamp = args.Timestamp,
                        };
                        _packageInfos.Enqueue(packageInfo);
                        EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                            IsSuccess = true,
                            TriggerPosition = TriggerPositionEnum.PackageTrigger
                        });
                    }
                    else {
                        var info = _packageInfos.OrderBy(o => o.CreateTime).FirstOrDefault(f => f.BarCode == null);
                        if (info != null) {
                            info.BarCode = args.Barcode;
                            info.CameraSerialNumber = args.CameraSerialNumber;
                            info.Image = args.Image;
                            info.ScanTime = args.ScanTime;
                            info.Timestamp = args.Timestamp;
                        }
                    }
                    //获取外部数据
                    //体积
                    if (_externalDataSource.IsVolumeInput) {
                        await _externalDataService.GetVolume(args.Barcode);
                    }
                }
            };
            //全景相机
            _deviceService.PanoramaCaptured += async delegate (object? sender, PanoramaCaptureEventArgs args) {
                await Task.Yield();
                _panoramicImageItems.Enqueue(new CameraImageInfo() {
                    CameraSerialNumber = args.CameraSerialNumber,
                    Image = args.Image,
                    Barcode = args.Barcode,
                    BarcodeTimestamp = args.BarcodeTimestamp
                });
            };
            //体积相机
            _deviceService.VolumeCaptured += async delegate (object? sender, VolumeCapturedEventArgs args) {
                await Task.Yield();
                /*_volumeCameraImageItems.Enqueue(new CameraImageInfo() {
                    CameraSerialNumber = args.CameraSerialNumber,
                    Image = args.Image,
                    Barcode = args.Barcode,
                    BarcodeTimestamp = args.BarcodeTimestamp
                });*/
                //填充长宽高
                var info = _packageInfos.FirstOrDefault(f => f.Length == null ||
                                                             f.Width == null ||
                                                             f.Height == null);
                //增加体积单位转换
                if (info is not null) {
                    info.Length = args.Length - info.LengthToDeduct;
                    info.Width = args.Width - info.WidthToDeduct;
                    info.Height = args.Height - info.HeightToDeduct;
                    info.Volume = args.Volume - info.VolumeToDeduct;
                }
            };
            //称重
            _deviceService.StableWeight += delegate (object? sender, StableWeightEventArgs args) {
                var info = _packageInfos.OrderBy(o => o.CreateTime).FirstOrDefault(f => !f.IsCompleted && f.Weight is null);
                if (info is not null) {
                    info.Weight = args.Weight;
                }
            };
            //外部数据源
            _externalDataService.DataSourceEnabled += delegate (object? sender, ExternalDataSourceEventArgs args) {
                _externalDataSource = args;
            };
            //输入体积
            _externalDataService.VolumeReceived += delegate (object? sender, ExternalVolumeInputEventArgs args) {
                var info = _packageInfos.OrderBy(o => o.CreateTime).FirstOrDefault(f => f.BarCode.Equals(args.BarCode));
                if (info is not null) {
                    //增加体积单位转换
                    info.Length = args.Length - info.LengthToDeduct;
                    info.Width = args.Width - info.WidthToDeduct;
                    info.Height = args.Height - info.HeightToDeduct;
                    info.Volume = args.Volume - info.VolumeToDeduct;
                }
                EventAggregator.Instance.Publish(new VolumeLogInfoModel() {
                    Type = LogType.Information,
                    Message = $"获取体积信息:{args.Length},{args.Width},{args.Height}",
                    DataSourceType = DataSourceType.ExternalInput
                });
            };
            //下位机(创建包裹)
            _sortingService.CreatePackageEvent += delegate (object? sender, PackageInstructionEventArgs args) {
                if (_communicationsSettingsDto.Protocol == CommunicationProtocol.Wxkc) {
                    var tryParse = int.TryParse(args.Keyword, out var num);
                    if (tryParse) {
                        //创建包裹
                        var packageInfo = new PackageInfo() {
                            Guid = num,
                            IsCreatedByLowerMachine = true,
                            PackageCreationInstruction = args.Instruction,
                        };
                        _packageInfos.Enqueue(packageInfo);
                    }
                }
                //其他协议
            };
            _sortingService.RemovePackageEvent += delegate (object? sender, PackageInstructionEventArgs args) {
                if (_communicationsSettingsDto.Protocol == CommunicationProtocol.Wxkc) {
                    var tryParse = int.TryParse(args.Keyword, out var num);
                    if (tryParse) {
                        var count = _packageInfos.Count;
                        while (count-- > 0) {
                            _packageInfos.TryDequeue(out var info);
                            if (info is null) continue;
                            if (!info.Guid.Equals(num)) {
                                _packageInfos.Enqueue(info);
                            }
                        }
                    }
                }
                //其他协议
            };
            //下位机(移除包裹)

            //下位机(清空异常)
            _sortingService.ClearExceptionEvent += delegate (object? sender, string o) {
                _packageInfos.Clear();
            };
            //Ocr算法
            _deviceService.OcrContentRecognized += async delegate (object? sender, OcrResult args) {
                //创建条码
                if (_communicationsSettingsDto.Type == CommunicationsType.None ||
                    !_communicationsSettingsDto.DeviceControlSettingsInfo.IsUseCreatePackageByDevice) {
                    var packageInfo = new PackageInfo() {
                        Guid = args.RecognitionTimestamp,
                        BarCode = args.BarCode,
                        CameraSerialNumber = args.CameraSerialNumber,
                        Image = args.Image,
                        ScanTime = args.RecognitionTime,
                        Timestamp = args.RecognitionTimestamp,
                    };
                    _packageInfos.Enqueue(packageInfo);
                    EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                        IsSuccess = true,
                        TriggerPosition = TriggerPositionEnum.PackageTrigger
                    });
                    EventAggregator.Instance.Publish(new PackageOcrInfo() {
                        BarCode = args.BarCode,
                        VirtualNumber = args.VirtualNumber,
                        RecipientAddress = args.RecipientAddress,
                        RecipientName = args.RecipientName,
                        RecipientPhone = args.RecipientPhone,
                        SenderName = args.SenderName,
                        SenderPhone = args.SenderPhone,
                        SenderAddress = args.SenderAddress,
                        ThreeSegmentCode = args.ThreeSegmentCode,
                        VirtualNumberLast4 = args.VirtualNumberLast4,
                        RecognitionTime = args.RecognitionTime,
                        ElapsedTime = args.ElapsedTime,
                        RecognitionTimestamp = args.RecognitionTimestamp,
                        CameraSerialNumber = args.CameraSerialNumber,
                        SubmitTimestamp = args.SubmitTimestamp
                    });
                }
                else {
                    var info = _packageInfos.OrderBy(o => o.CreateTime).FirstOrDefault(f => f.BarCode == null);
                    if (info != null) {
                        info.BarCode = args.BarCode;
                        info.CameraSerialNumber = args.CameraSerialNumber;
                        info.Image = args.Image;
                        info.ScanTime = args.RecognitionTime;
                        info.Timestamp = args.RecognitionTimestamp;
                    }
                }
                //获取外部数据
                //体积
                if (_externalDataSource.IsVolumeInput) {
                    await _externalDataService.GetVolume(args.BarCode);
                }
            };
            //手动输入条码
            EventAggregator.Instance.Subscribe<BarcodeTypeProviderEvent>(async barcodeInfo => {
                if (barcodeInfo is BarcodeTypeProviderEvent args) {
                    var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    if (_communicationsSettingsDto.Type == CommunicationsType.None ||
                        !_communicationsSettingsDto.DeviceControlSettingsInfo.IsUseCreatePackageByDevice) {
                        var packageInfo = new PackageInfo() {
                            Guid = timestamp,
                            BarCode = args.Barcode,
                            ScanTime = DateTime.Now,
                            Timestamp = timestamp,
                            HeightToDeduct = args.HeightToDeduct,
                            WidthToDeduct = args.WidthToDeduct,
                            LengthToDeduct = args.LengthToDeduct,
                            VolumeToDeduct = args.VolumeToDeduct,
                            CreateTime = DateTime.Now,
                            IsCreatedByLowerMachine = false,
                        };
                        _packageInfos.Enqueue(packageInfo);
                        EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                            IsSuccess = true,
                            TriggerPosition = TriggerPositionEnum.PackageTrigger
                        });
                        //触发全景拍照
                        var enumerable = _cameras.Where(w => w.BindingType == CameraBindingType.PanoramicCamera);
                        foreach (var c in enumerable) {
                            if (c is IIndustrialCamera camera && _deviceService.RunningStatus) {
                                await camera.TakePhotoAsync(args.Barcode, timestamp);
                            }
                        }
                        //获取外部数据
                        //体积
                        if (_externalDataSource.IsVolumeInput) {
                            await _externalDataService.GetVolume(args.Barcode);
                        }
                    }
                    else {
                        var info = _packageInfos.OrderBy(o => o.CreateTime).FirstOrDefault(f => f.BarCode == null);
                        if (info != null) {
                            info.BarCode = args.Barcode;
                            info.ScanTime = DateTime.Now;
                            info.Timestamp = timestamp;
                        }
                    }
                }
            });
            //配置更改触发事件
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                if (item is SettingsChangedEvent model) {
                    if (model.SettingsName.Equals("CommunicationsSettings")) {
                        try {
                            var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("CommunicationsSettings"));
                            if (configInfoModel != null) {
                                _communicationsSettingsDto = JsonConvert.DeserializeObject<CommunicationsSettingsDto>(configInfoModel.Value) ?? new CommunicationsSettingsDto();
                            }
                        }
                        catch (Exception e) {
                            NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                        }
                    }
                    else if (model.SettingsName.Equals("VolumeSettings")) {
                        try {
                            var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("CommunicationsSettings"));
                            if (configInfoModel != null) {
                                _volumeSettingsDto = JsonConvert.DeserializeObject<VolumeSettingsDto>(configInfoModel.Value) ?? new VolumeSettingsDto();
                            }
                        }
                        catch (Exception e) {
                            NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                        }
                    }
                    else if (model.SettingsName.Equals("WeightSettings")) {
                        try {
                            var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("CommunicationsSettings"));
                            if (configInfoModel != null) {
                                _weightSettingsDto = JsonConvert.DeserializeObject<WeightSettingsDto>(configInfoModel.Value) ?? new WeightSettingsDto();
                            }
                        }
                        catch (Exception e) {
                            NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                        }
                    }
                    //其他设置
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                //读配置
                _configInfoModels = await _configRepository.Select(s => s.Id > 0,
                    o => o.Id, stoppingToken);
                var configInfoModel = _configInfoModels?.FirstOrDefault(f => f.ConfigName.Equals("CommunicationsSettings"));
                if (configInfoModel is not null) {
                    _communicationsSettingsDto = JsonConvert.DeserializeObject<CommunicationsSettingsDto>(configInfoModel.Value) ?? new CommunicationsSettingsDto();
                }
                configInfoModel = _configInfoModels?.FirstOrDefault(f => f.ConfigName.Equals("VolumeSettings"));
                if (configInfoModel is not null) {
                    _volumeSettingsDto = JsonConvert.DeserializeObject<VolumeSettingsDto>(configInfoModel.Value) ?? new VolumeSettingsDto();
                }

                configInfoModel = _configInfoModels?.FirstOrDefault(f => f.ConfigName.Equals("WeightSettings"));
                if (configInfoModel is not null) {
                    _weightSettingsDto = JsonConvert.DeserializeObject<WeightSettingsDto>(configInfoModel.Value) ?? new WeightSettingsDto();
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    if (_packageInfos.Count > 0 && _deviceService.RunningStatus) {
                        //判断包裹过期

                        if (_communicationsSettingsDto.IsUsePackageExpiry) {
                            var count = _packageInfos.Count;
                            while (count-- > 0) {
                                _packageInfos.TryDequeue(out var info);
                                if (info is null) continue;
                                if (DateTime.Now.Subtract(info.CreateTime).TotalMilliseconds < _communicationsSettingsDto.PackageExpiryTime) {
                                    _packageInfos.Enqueue(info);
                                }
                            }
                        }
                        //取出一个未完成包裹
                        var packageInfo = _packageInfos.OrderBy(o => o.CreateTime).FirstOrDefault(f => f is { IsCompleted: false, BarCode: not null });
                        if (packageInfo != null) {
                            //判断填充包裹信息
                            if (packageInfo.Length is not null &&
                                packageInfo.Width is not null &&
                                packageInfo.Height is not null &&
                                packageInfo.Weight is not null &&
                                !string.IsNullOrEmpty(packageInfo.BarCode)) {
                                //执行输出
                                _resultOutputService.ExecuteOutput(
                                    packageInfo.BarCode, (float)(packageInfo.Weight ?? 0),
                                    packageInfo.ScanTime, (float)(packageInfo.Length ?? 0),
                                    (float)(packageInfo.Width ?? 0), (float)(packageInfo.Height ?? 0),
                                    (float)(packageInfo.Volume ?? 0), packageInfo.CameraSerialNumber,
                                    stoppingToken);
                                packageInfo.IsCompleted = true;
                                EventAggregator.Instance.Publish(packageInfo);
                            }
                            else {
                                //填充体积信息
                                if ((_cameras.All(a => a.BindingType != CameraBindingType.VolumeCamera)
                                    && !_externalDataSource.IsVolumeInput) ||
                                    (_volumeSettingsDto.IsUseFusionTimeout &&
                                     DateTime.Now.Subtract(packageInfo.CreateTime).TotalMilliseconds > _volumeSettingsDto.FusionTimeout)) {
                                    //判断是否开启Tcp体积输入
                                    packageInfo.Length = 0;
                                    packageInfo.Width = 0;
                                    packageInfo.Height = 0;
                                    packageInfo.Volume = 0;
                                }
                                //填充重量信息
                                if (_deviceService.ScaleType == ScaleType.None ||
                                    (_weightSettingsDto.AdditionalWeight.IsUseMergedWeightTimeout &&
                                     DateTime.Now.Subtract(packageInfo.CreateTime).TotalMilliseconds >
                                     _weightSettingsDto.AdditionalWeight.MergedWeightTimeout)) {
                                    packageInfo.Weight = 0;
                                }
                            }
                        }
                        if (_panoramicImageItems.Count > 0) {
                            _panoramicImageItems.TryDequeue(out var panoramicImageInfo);
                            if (panoramicImageInfo is not null) {
                                var info = _packageInfos.FirstOrDefault(f => !string.IsNullOrEmpty(f.BarCode) && f.BarCode.Equals(panoramicImageInfo.Barcode));
                                if (info is { Weight: not null, Length: not null, Width: not null, Height: not null, Volume: not null, BarCode: not null }
                                   ) {
                                    //全景图数量+1
                                    info.PanoramicImageCount += 1;
                                    EventAggregator.Instance.Publish(new ImageMessageInfo {
                                        BarCode = info.BarCode,
                                        CameraSerialNumber = panoramicImageInfo.CameraSerialNumber,
                                        Weight = (float)info.Weight,
                                        Height = (float)info.Height,
                                        Image = panoramicImageInfo.Image,
                                        Length = (float)info.Length,
                                        Width = (float)info.Width,
                                        Volume = (float)info.Volume,
                                        ScanTime = info.ScanTime,
                                        Type = SaveImageType.PanoramaImage
                                    });
                                }
                                else {
                                    //如果时间超过3秒则提交到保存
                                    //时间-分秒
                                    var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(panoramicImageInfo.BarcodeTimestamp).LocalDateTime;
                                    if (DateTime.Now.Subtract(dateTime).TotalMilliseconds > 3000) {
                                        EventAggregator.Instance.Publish(new ImageMessageInfo {
                                            BarCode = $"{panoramicImageInfo.Barcode}-{DateTime.Now:mmssfff}",
                                            CameraSerialNumber = panoramicImageInfo.CameraSerialNumber,
                                            Weight = 0,
                                            Height = 0,
                                            Image = panoramicImageInfo.Image,
                                            Length = 0,
                                            Width = 0,
                                            Volume = 0,
                                            ScanTime = dateTime,
                                            Type = SaveImageType.PanoramaImage
                                        });
                                    }
                                    else {
                                        _panoramicImageItems.Enqueue(panoramicImageInfo);
                                    }
                                }
                            }
                        }
                        //体积图
                        if (_volumeCameraImageItems.Count > 0) {
                            _volumeCameraImageItems.TryDequeue(out var volumeCameraImageInfo);
                            if (volumeCameraImageInfo is not null) {
                                var info = _packageInfos.FirstOrDefault(f => !string.IsNullOrEmpty(f.BarCode) && f.BarCode.Equals(volumeCameraImageInfo.Barcode));
                                if (info is { Weight: not null, Length: not null, Width: not null, Height: not null, Volume: not null, BarCode: not null }
                                   ) {
                                    EventAggregator.Instance.Publish(new ImageMessageInfo {
                                        BarCode = info.BarCode,
                                        CameraSerialNumber = volumeCameraImageInfo.CameraSerialNumber,
                                        Weight = (float)info.Weight,
                                        Height = (float)info.Height,
                                        Image = volumeCameraImageInfo.Image,
                                        Length = (float)info.Length,
                                        Width = (float)info.Width,
                                        Volume = (float)info.Volume,
                                        ScanTime = info.ScanTime,
                                        Type = SaveImageType.VolumeImage
                                    });
                                }
                                else {
                                    _volumeCameraImageItems.Enqueue(volumeCameraImageInfo);
                                }
                            }
                        }
                        //扫码图
                        if (_packageInfos.Count > 0) {
                            //判断存图路径等于空
                            var codeInfo = _packageInfos.FirstOrDefault(f => f is {
                                Weight: not null, Length: not null, Width: not null,
                                Height: not null, Volume: not null, IsSavedImage: false,
                                BarCode: not null
                            });
                            if (codeInfo is not null) {
                                EventAggregator.Instance.Publish(new ImageMessageInfo {
                                    BarCode = codeInfo.BarCode ?? string.Empty,
                                    CameraSerialNumber = codeInfo.CameraSerialNumber,
                                    Weight = (float)(codeInfo.Weight ?? 0),
                                    Height = (float)(codeInfo.Height ?? 0),
                                    Image = codeInfo.Image,
                                    Length = (float)(codeInfo.Length ?? 0),
                                    Width = (float)(codeInfo.Width ?? 0),
                                    Volume = (float)(codeInfo.Volume ?? 0),
                                    ScanTime = codeInfo.ScanTime,
                                    Type = SaveImageType.BarcodeImage
                                });
                                codeInfo.IsSavedImage = true;
                            }
                        }

                        //告诉界面这些scanBarCodeInfos已经填充完全部信息，即将移除
                        var packageInfos = _packageInfos.Where(w => w is { IsCompleted: true, IsSavedImage: true }
                                                                    &&
                                                                    w.PanoramicImageCount ==
                                                                    _cameras.Count(c => c.BindingType == CameraBindingType.PanoramicCamera))
                            .ToList();
                        var infosCount = _packageInfos.Count;

                        while (infosCount-- > 0) {
                            _packageInfos.TryDequeue(out var info);
                            if (info is null) continue;

                            if (!packageInfos.Any(a => a.BarCode != null && a.BarCode.Equals(info.BarCode))) {
                                _packageInfos.Enqueue(info);
                            }
                        }
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }

                await Task.Delay(50, stoppingToken);
            }
        }
    }

    public class PackageInfo {

        /// <summary>
        /// 包裹创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Guid
        /// </summary>
        public long Guid { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        public string? BarCode { get; set; }

        /// <summary>
        /// 条码图片
        /// </summary>
        public Image? Image { get; set; }

        /// <summary>
        /// 重量
        /// </summary>
        public double? Weight { get; set; }

        /// <summary>
        /// 长度
        /// </summary>
        public double? Length { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public double? Width { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public double? Height { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        public double? Volume { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 是否已完成(完成输出、上传、但未从集合删除)
        /// </summary>
        public bool IsCompleted;

        /// <summary>
        /// 是否完成存图
        /// </summary>
        public bool IsSavedImage;

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime;

        /// <summary>
        /// 条码时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 需要扣除的重量
        /// </summary>
        public float LengthToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的宽度
        /// </summary>
        public float WidthToDeduct { get; set; }

        /// <summary>
        /// /需要扣除的重量
        /// </summary>
        public float WeightToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的高度
        /// </summary>
        public float HeightToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的体积
        /// </summary>
        public float VolumeToDeduct { get; set; }

        /// <summary>
        /// 创建包裹指令
        /// </summary>
        public string PackageCreationInstruction { get; set; } = string.Empty;

        /// <summary>
        /// 是否由下位机创建
        /// </summary>
        public bool IsCreatedByLowerMachine { get; set; }

        /// <summary>
        /// 全景图数量
        /// </summary>
        public int PanoramicImageCount { get; set; }
    }

    public class CameraImageInfo {

        /// <summary>
        /// 图片
        /// </summary>
        public Image? Image { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 图片路径
        /// </summary>
        public string? ImageFilePath { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 条码时间戳
        /// </summary>
        public long BarcodeTimestamp { get; set; }
    }

    public class SavedImageInfo {

        /// <summary>
        /// 文件路径
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        public string? BarCode { get; set; }

        /// <summary>
        /// 图片类型
        /// </summary>
        public SaveImageType? ImageType { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime { get; set; }
    }

    /// <summary>
    /// 存图参数
    /// </summary>
    public class ImageMessageInfo {
        public Image? Image { get; set; }

        public SaveImageType Type { get; set; }
        public string BarCode { get; set; } = string.Empty;
        public float Weight { get; set; }
        public DateTime ScanTime { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Volume { get; set; }
        public string CameraSerialNumber { get; set; } = string.Empty;
    }

    public class PackageOcrInfo {

        /// <summary>
        /// 条码
        /// </summary>
        public string BarCode { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置虚拟号码。
        /// </summary>
        public string VirtualNumber { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置收件人地址。
        /// </summary>
        public string RecipientAddress { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置收件人姓名。
        /// </summary>
        public string RecipientName { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置收件人电话。
        /// </summary>
        public string RecipientPhone { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置寄件人姓名。
        /// </summary>
        public string SenderName { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置寄件人电话。
        /// </summary>
        public string SenderPhone { get; set; } = string.Empty;

        /// <summary>
        /// 发件人地址
        /// </summary>
        public string SenderAddress { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置三段码。
        /// </summary>
        public string ThreeSegmentCode { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置虚拟号码后四位。
        /// </summary>
        public string VirtualNumberLast4 { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置识别时间。
        /// </summary>
        public DateTime RecognitionTime { get; set; }

        /// <summary>
        /// 获取或设置耗时(ms)
        /// </summary>
        public long ElapsedTime { get; set; }

        /// <summary>
        /// 获取或设置识别时间戳。
        /// </summary>
        public long RecognitionTimestamp { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 提交图时间
        /// </summary>
        public long SubmitTimestamp { get; set; }
    }
}