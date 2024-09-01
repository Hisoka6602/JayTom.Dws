using System;
using DryIoc;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Camera;
using SixLabors.ImageSharp;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Model;
using JayTom.Dws.Domain.Manager;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Interface.Cloud;
using JayTom.Dws.Interface.License;
using JayTom.Dws.Domain.Dto.CloudDto;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Camera.FilterContainer;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Data.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.NVR;
using JayTom.Dws.Infrastructure.Repository.LocalConf.IpcNvrConfig;

namespace JayTom.Dws.Client.Service.ProcessingServices {

    public class KeyboardDeviceBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IDeviceService _deviceService;
        private readonly IImageStorageService _imageStorageService;
        private readonly IConfigRepository _configRepository;
        private readonly ISortingService _sortingService;
        private readonly IExternalDataService _externalDataService;
        private readonly IIpcNvrConfigRepository _ipcNvrConfigRepository;
        private readonly INvrWatermarkConfigRepository _nvrWatermarkConfigRepository;
        private readonly INvrCameraBindingRepository _nvrCameraBindingRepository;
        private readonly ICloud _cloud;
        private CreatePackageSettingsDto _createPackageSettingsDto = new();
        private ContentInputSettingsDto _contentInputSettingsDto = new();
        private BarcodeFilterSettingsDto _barcodeFilterSettingsDto = new();
        private CloudVideoSettingsDto _cloudVideoSettingsDto = new();
        private SemaphoreSlim _createPackageSlim = new(1);
        private DateTime _lastReadTime = DateTime.Now;
        private DateTime _lastNoReadTime = DateTime.Now;
        private WeightSettingsDto _weightSettingsDto = new();
        private static bool _isWindowsClose;
        private List<ICamera> _cameras = new();

        private BarCodeFilterContainer _barCodeFilterContainer = new();

        //private BaseDaHuatech? _baseDaHuatech;
        private DaHuatechNVR? _daHuatechNvr;

        public KeyboardDeviceBackgroundService(IDeviceService deviceService,
            IImageStorageService imageStorageService,
            IConfigRepository configRepository,
            ISortingService sortingService,
            IExternalDataService externalDataService,
            IPanoramaCameraConfigRepository panoramaCameraConfigRepository,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IIpcNvrConfigRepository ipcNvrConfigRepository,
            INvrWatermarkConfigRepository nvrWatermarkConfigRepository,
            INvrCameraBindingRepository nvrCameraBindingRepository,
            ICloud cloud) {
            _deviceService = deviceService;
            _imageStorageService = imageStorageService;
            _configRepository = configRepository;
            _sortingService = sortingService;
            _externalDataService = externalDataService;
            _ipcNvrConfigRepository = ipcNvrConfigRepository;
            _nvrWatermarkConfigRepository = nvrWatermarkConfigRepository;
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
            _cloud = cloud;

            //相机
            _deviceService.CameraInitialized += delegate (object? sender, List<ICamera> list) {
                _cameras = list;

                List<PanoramaCameraConfigInfoModel> panoramaCameras = panoramaCameraConfigRepository.Select(s => s.Id > 0, o => o.Id)
                    ?.ConfigureAwait(false).GetAwaiter().GetResult()?.ToList() ?? new List<PanoramaCameraConfigInfoModel>();
                var scannerCameraConfigInfoModels = barcodeScannerCameraConfigRepository.Select(s => s.Id > 0, o => o.Id)
                    ?.ConfigureAwait(false).GetAwaiter().GetResult()?.ToList() ?? new List<BarcodeScannerCameraConfigInfoModel>();
                _cameras.ForEach(f => {
                    if (f.Info != null) {
                        f.Info.CustomName = f.BindingType switch {
                            CameraBindingType.PanoramaCamera => panoramaCameras
                                .FirstOrDefault(f1 => f1.SerialNumber.Equals(f.Info.SerialNumber))
                                ?.CustomName ?? string.Empty,
                            CameraBindingType.ScannerCamera or CameraBindingType.OcrCamera =>
                                scannerCameraConfigInfoModels
                                    .FirstOrDefault(f1 => f1.SerialNumber.Equals(f.Info.SerialNumber))
                                    ?.CustomName ?? string.Empty,
                            _ => f.Info.CustomName
                        };
                    }
                });
            };
            //条码返回
            _deviceService.BarcodeScanned += async delegate (object? sender, BarcodeReadEventArgs args) {
                //验证多条码
                try {
                    await _createPackageSlim.WaitAsync();
                    _lastReadTime = DateTime.Now;
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                            PackageInfoManager.GetLastPackage(f => f.Value is { BarCodeInfo: null });

                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.ScanBarcodeCamera)
                        == PackageCreationMethodsEnum.ScanBarcodeCamera && packageInfo is null) {
                        //支持扫码创建
                        packageInfo = new PackageInfo() {
                            Guid = args.Timestamp,
                            BarCodeInfo = new BarCodeInfoModel() {
                                Barcode = args.Barcode,
                                SerialNumber = args.CameraSerialNumber,
                                DisplayIdentifier = args.CameraSerialNumber,
                                ScanTime = args.ScanTime,
                                Source = SourceType.Camera,
                                BindTime = DateTime.Now
                            },
                            Image = args.Image,
                        };
                        EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                            IsSuccess = true,
                            TriggerPosition = TriggerPositionEnum.PackageTrigger,
                            PackageInfo = packageInfo
                        });
                    }
                    else {
                        if (packageInfo is not null) {
                            packageInfo.BarCodeInfo = new BarCodeInfoModel() {
                                Barcode = args.Barcode,
                                SerialNumber = args.CameraSerialNumber,
                                DisplayIdentifier = args.CameraSerialNumber,
                                ScanTime = args.ScanTime,
                                Source = SourceType.Camera,
                                BindTime = DateTime.Now
                            };
                            packageInfo.Image = args.Image;
                            EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.BarCodeSetValueAfter,
                                PackageInfo = packageInfo,
                            });
                        }
                    }
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            _deviceService.BarCodeKeyReceived += async (sender, args) => {
                //效验正则
                var validationResult = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo() {
                    BarCode = args.Barcode,
                    ScanTime = DateTime.Now,
                });
                if (validationResult.IsValidationPassed) {
                    try {
                        await _createPackageSlim.WaitAsync();
                        _lastReadTime = DateTime.Now;
                        var packageInfo =
                            _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                                PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                                PackageInfoManager.GetLastPackage(f => f.Value is { BarCodeInfo: null });

                        if (_createPackageSettingsDto.PackageCreationMethods.HasFlag(PackageCreationMethodsEnum.BarcodeScannerInput) && packageInfo is null) {
                            //支持扫码枪创建
                            packageInfo = new PackageInfo() {
                                Guid = args.Timestamp,
                                BarCodeInfo = new BarCodeInfoModel() {
                                    Barcode = args.Barcode,
                                    SerialNumber = $"{args.Device?.DevicePath}",
                                    DisplayIdentifier = $"{args.Device?.DeviceName}-{args.Device?.ManufacturerName}",
                                    ScanTime = args.ScanTime,
                                    Source = SourceType.Camera,
                                    BindTime = DateTime.Now
                                },
                            };
                            EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.PackageTrigger,
                                PackageInfo = packageInfo
                            });
                        }
                        else {
                            if (packageInfo is not null) {
                                packageInfo.BarCodeInfo = new BarCodeInfoModel() {
                                    Barcode = args.Barcode,
                                    SerialNumber = $"{args.Device?.DevicePath}",
                                    DisplayIdentifier = $"{args.Device?.DeviceName}-{args.Device?.ManufacturerName}",
                                    ScanTime = args.ScanTime,
                                    Source = SourceType.Camera,
                                    BindTime = DateTime.Now
                                };
                                EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                    IsSuccess = true,
                                    TriggerPosition = TriggerPositionEnum.BarcodeScannerReturn,
                                    PackageInfo = packageInfo,
                                });
                            }
                        }
                    }
                    finally {
                        _createPackageSlim.Release();
                    }
                }
            };
            //配置更改
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                if (item is { } model) {
                    switch (model.SettingsName) {
                        case "CreatePackageSettings":
                            _createPackageSettingsDto = await _configRepository.FirstOrDefaultEntity<CreatePackageSettingsDto>(model.SettingsName) ??
                                                        new CreatePackageSettingsDto();

                            break;

                        case "ContentInputSettings":
                            _contentInputSettingsDto = await _configRepository.FirstOrDefaultEntity<ContentInputSettingsDto>(model.SettingsName) ??
                                                       new ContentInputSettingsDto();

                            break;

                        case "BarcodeFilterSettings":
                            _barcodeFilterSettingsDto = await _configRepository.FirstOrDefaultEntity<BarcodeFilterSettingsDto>(model.SettingsName) ??
                                                        new BarcodeFilterSettingsDto();

                            break;
                    }
                }
            });
            //创建包裹后触发
            EventAggregator.Instance.Subscribe<TriggerPositionEvent>(async item => {
                if (item is { TriggerPosition: TriggerPositionEnum.PackageTrigger, PackageInfo: { } packageInfo }) {
                    var info = PackageInfoManager.GetLastPackage(f => f is { Value: not null });

                    if (info is not null &&
                        packageInfo.CreateTime.Subtract(info.CreateTime).TotalMilliseconds <
                        _createPackageSettingsDto.PackageCreationInterval) {
                        return;
                    }

                    packageInfo.Timestamp = new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds();

                    //添加包裹
                    var packageRemoveTimers = new List<PackageTimer>();
                    if (_createPackageSettingsDto is { IsUseEmptyPackageExpiry: true, EmptyPackageExpiryTime: > 0 }) {
                        packageRemoveTimers.Add(new PackageRemoveTimer() {
                            Description = "空包裹过期",
                            Predicate = w => w.Value.BarCodeInfo == null,
                            RemovalTimeSpan = TimeSpan.FromMilliseconds(_createPackageSettingsDto.EmptyPackageExpiryTime)
                        });
                    }
                    if (_createPackageSettingsDto is { IsUsePackageExpiry: true, PackageExpiryTime: > 0 }) {
                        packageRemoveTimers.Add(new PackageRemoveTimer() {
                            Description = "包裹超过生存周期",
                            RemovalTimeSpan = TimeSpan.FromMilliseconds(_createPackageSettingsDto.PackageExpiryTime)
                        });
                    }
                    PackageInfoManager.AddPackage(packageInfo, packageRemoveTimers);
                    //触发创建包裹事件
                    EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                        IsSuccess = true,
                        TriggerPosition = TriggerPositionEnum.CreateTimePackageAfter,
                        PackageInfo = packageInfo
                    });
                }
                else if (item is {
                    PackageInfo: { BarCodeInfo: not null } info, TriggerPosition: TriggerPositionEnum.BarCodeSetValueAfter or
                             TriggerPositionEnum.CreateTimePackageAfter or TriggerPositionEnum.BarcodeScannerReturn
                }) {
                    if (info.NvrInfo?.Any() != true) {
                        var nvrCameraBindingInfoModels = await _nvrCameraBindingRepository.MemoryCacheData();

                        info.NvrInfo = nvrCameraBindingInfoModels.Where(w => w.SerialNumber.Equals(info.BarCodeInfo.SerialNumber))
                            ?.Select(s => new NvrInfoModel() {
                                Channel = s.Channel,
                                IpAddress = s.IpAddress,
                                Password = s.Password,
                                Port = s.Port,
                                Username = s.Username,
                            })?.ToList() ?? new List<NvrInfoModel>();
                    }

                    PackageInfoManager.CompletedPackage(f => f.Key.Equals(info.CreateTime));
                }
            });
            //程序停止
            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(async item => {
                if (item is { } info) {
                    if (info.Status == ApplicationStatus.Stop &&
                        _createPackageSettingsDto.ClearPackageQueueOnStop) {
                        PackageInfoManager.ClearAllPackages();
                    }
                    else if (info.Status == ApplicationStatus.Start) {
                        //重新登录大华相机(临时)
                        var ipcNvrConfigInfoModels = (await _ipcNvrConfigRepository.MemoryCacheData()).Where(w => !string.IsNullOrEmpty(w.SerialNumber)).ToList();

                        if (ipcNvrConfigInfoModels.Any(a => a.Type == 1)) {
                            _daHuatechNvr = DaHuatechNVR.Instance;

                            foreach (var model in ipcNvrConfigInfoModels.Where(model => _daHuatechNvr.GetDevLogInInfo(f => f.IpAddress.Equals(model.IpAddress))?.Any() != true)) {
                                var (b, s) = await _daHuatechNvr.LogIn(model.IpAddress, model.Port, model.Username, model.Password);
                                if (!b) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"大华安防相机:{model.IpAddress},登录失败!");
                                }
                            }
                        }
                    }
                }
            });
            //移除包裹事件
            PackageInfoManager.PackageRemoved += (sender, args) => {
                EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                    IsSuccess = true,
                    TriggerPosition = TriggerPositionEnum.RemovePackageAfter,
                    PackageInfo = args.RemovedPackage,
                    Description = args.Description
                });
            };
            PackageInfoManager.PackageCompleted += (sender, args) => {
                //执行输出
                if (args.CompletedPackage?.BarCodeInfo is not null &&
                    args.CompletedPackage?.WeightInfo is not null &&
                    args.CompletedPackage?.VolumeInfo is not null) {
                    EventAggregator.Instance.Publish(args.CompletedPackage);
                }
                //写水印
                Task.Run(async () => {
                    if (_daHuatechNvr is not null && args.CompletedPackage?.BarCodeInfo?.SerialNumber is not null) {
                        var nvrCameraBindingInfoModels = await _nvrCameraBindingRepository.MemoryCacheData();
                        if (nvrCameraBindingInfoModels.Any(a => a.SerialNumber.Equals(args.CompletedPackage.BarCodeInfo.SerialNumber))) {
                            var infoModels = nvrCameraBindingInfoModels.Where(w => w.SerialNumber.Equals(args.CompletedPackage.BarCodeInfo.SerialNumber))
                                .ToList();
                            var results = infoModels.Select(s => {
                                // 获取异步数据并同步等待完成
                                var infoModel = _ipcNvrConfigRepository.MemoryCacheData().Result
                                    .FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress) && f.Username.Equals(s.Username));

                                if (infoModel != null) {
                                    var watermarkConfigInfoModel = _nvrWatermarkConfigRepository.MemoryCacheData().Result
                                        .FirstOrDefault(f => f.IpcNvrConfigId.Equals(infoModel.Id));

                                    if (watermarkConfigInfoModel != null) {
                                        var watermarkConfig = new SecurityCameraWatermarkConfig {
                                            Duration = watermarkConfigInfoModel.Duration,
                                            MaxWatermarks = 8,
                                            Position = 0,
                                            BackgroundColor = ColorTranslator.FromHtml(watermarkConfigInfoModel.BackgroundColorHex)
                                        };
                                        return (infoModel.IpAddress, s.Channel, watermarkConfig, watermarkConfigInfoModel.DisplayMode);
                                    }
                                }

                                // 如果未找到匹配的 infoModel，返回默认值
                                return (string.Empty, 0, null, 0);
                            }).Where(w => !string.IsNullOrEmpty(w.IpAddress)).ToList();

                            if (results?.Any() == true && results?.ToList()?.FirstOrDefault().watermarkConfig is not null) {
                                if (results.FirstOrDefault().DisplayMode == 0) {
                                    _daHuatechNvr.AddRealTimeWatermark(results.Select(s => (s.IpAddress, s.Channel)).ToList(),
                                        args.CompletedPackage.Timestamp, args.CompletedPackage.BarCodeInfo.Barcode,
                                        results.FirstOrDefault().watermarkConfig);
                                }
                                else {
                                    _daHuatechNvr.AddSingleRealTimeWatermark(results.Select(s => (s.IpAddress, s.Channel)).ToList(),
                                        args.CompletedPackage.Timestamp, args.CompletedPackage.BarCodeInfo.Barcode,
                                        results.FirstOrDefault().watermarkConfig);
                                }
                            }
                        }
                    }
                });
            };
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                if (item is { Type: WindowsActionType.Close }) {
                    _isWindowsClose = true;
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                _cloudVideoSettingsDto = await _configRepository.FirstOrDefaultEntity<CloudVideoSettingsDto>("CloudVideoSettings", stoppingToken) ?? new CloudVideoSettingsDto();
                if (_cloudVideoSettingsDto.IsUseCloudVideoUpload) {
                    await _cloud.SetParameters(new Dictionary<string, object>()
                    {
                        { "WebDoMain", _cloudVideoSettingsDto.WebDoMain },
                        { "Timeout", _cloudVideoSettingsDto.RequestTimeout },
                    });
                    // 创建任务列表
                    var tasks = new List<Task>
                    {
                        FetchAndUpdateConfig("CreatePackageSettings", stoppingToken),
                        FetchAndUpdateConfig("ContentInputSettings", stoppingToken),
                        FetchAndUpdateConfig("BarcodeFilterSettings", stoppingToken)
                    };

                    // 等待所有任务完成
                    await Task.WhenAll(tasks);
                }

                _createPackageSettingsDto = await _configRepository.FirstOrDefaultEntity<CreatePackageSettingsDto>("CreatePackageSettings", stoppingToken) ?? new CreatePackageSettingsDto();
                _contentInputSettingsDto = await _configRepository.FirstOrDefaultEntity<ContentInputSettingsDto>("ContentInputSettings", stoppingToken) ??
                                           new ContentInputSettingsDto();
                _barcodeFilterSettingsDto = await _configRepository.FirstOrDefaultEntity<BarcodeFilterSettingsDto>("BarcodeFilterSettings", stoppingToken) ??
                                            new BarcodeFilterSettingsDto();

                _barCodeFilterContainer = new BarCodeFilterContainer {
                    Pattern = _barcodeFilterSettingsDto.BasicFilterInfo.RegularExpression,
                    MaxSize = _barcodeFilterSettingsDto.DuplicateBarcodeFilterCount,
                    ExpirationTime = TimeSpan.FromMilliseconds(_barcodeFilterSettingsDto.ScanInterval),
                    BarCodeFilterMode = (BarCodeFilterMode)_barcodeFilterSettingsDto.BarCodeFilterOptions,
                    CustomRegularExpressionItems = _barcodeFilterSettingsDto.CustomRegexFilterItems.Where(w => w.IsActive)
                        .Select(s => s.RegexPattern).ToList(),
                    IsUseCustomRegexReplacement = _barcodeFilterSettingsDto.IsUseCustomRegexReplacement,
                    IsUseFilteredBarcodeTypes = _barcodeFilterSettingsDto.IsUseFilteredBarcodeTypes,
                    CustomRegexReplacementItems = _barcodeFilterSettingsDto.CustomRegexReplacementItems
                        .Where(w => w.IsActive).Select(
                            s => new CustomRegexReplacementItemInfo {
                                RegexPattern = s.RegexPattern,
                                ReplaceContent = s.ReplaceContent
                            }).ToList()
                };
                BarCodeFilterContainer.ResetFilter();
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken).ContinueWith(a => {
                    try {
                        if (PackageInfoManager.GetPackageCount() > 0 && _deviceService.RunningStatus) {
                            //判断存图路径等于空
                            var codeInfo = PackageInfoManager.GetPackage(f => f.Value is {
                                IsSavedImage: false,
                                BarCodeInfo: not null, IsCompleted: true
                            });
                            //存图
                            if (codeInfo?.Image != null) {
                                EventAggregator.Instance.Publish(new ImageMessageInfo {
                                    BarCode = codeInfo.BarCodeInfo?.Barcode ?? string.Empty,
                                    CameraSerialNumber = codeInfo.BarCodeInfo?.SerialNumber ?? string.Empty,
                                    Weight = (float)(codeInfo.WeightInfo?.FormattedWeight ?? 0),
                                    Height = (float)(codeInfo.VolumeInfo?.FormattedHeight ?? 0),
                                    Image = codeInfo.Image,
                                    Length = (float)(codeInfo.VolumeInfo?.FormattedLength ?? 0),
                                    Width = (float)(codeInfo.VolumeInfo?.FormattedWidth ?? 0),
                                    Volume = (float)(codeInfo.VolumeInfo?.FormattedVolume ?? 0),
                                    ScanTime = codeInfo.BarCodeInfo?.ScanTime ?? DateTime.Now,
                                    Type = SaveImageType.BarcodeImage,
                                    CameraName = _cameras.FirstOrDefault(f =>
                                            (bool)f.Info?.SerialNumber.Equals(
                                                codeInfo.BarCodeInfo?.SerialNumber ?? string.Empty))?.Info
                                        ?.Name ?? string.Empty,
                                    CameraCustomName = _cameras.FirstOrDefault(f =>
                                            (bool)f.Info?.SerialNumber.Equals(
                                                codeInfo.BarCodeInfo?.SerialNumber ?? string.Empty))?.Info
                                        ?.CustomName ?? string.Empty,
                                });
                                codeInfo.IsSavedImage = true;
                            }

                            //移除包裹
                            if (_createPackageSettingsDto.PackageRemoveMethods ==
                                PackageRemoveMethodsEnum.FillInformation) {
                                var packageInfos = PackageInfoManager.GetPackages(w =>
                                    w.Value is { IsCompleted: true, IsSavedImage: true } &&
                                    (w.Value.PanoramaCameraImageInfo.All(info => info.IsExists) ||
                                     DateTime.Now.Subtract(w.Value.CreateTime)
                                         .TotalMinutes > 5)) ?? new List<PackageInfo>();

                                foreach (var kvp in packageInfos) {
                                    PackageInfoManager.RemovePackage(kvp.CreateTime, "填充完整信息移除");
                                }
                            }
                        }
                    }
                    catch (Exception e) {
                        NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                    }
                }, stoppingToken);
            }
        }

        private async Task FetchAndUpdateConfig(string configName, CancellationToken token) {
            var (key, value) = await _cloud.GetCloudConfiguration(configName, "/api/Config/GetConfig", token: token);

            if (key && value is ApiResult { Result: true, Data: not null } result) {
                var configInfoModel = JsonConvert.DeserializeObject<ConfigInfoModel>(result.Data.ToString());
                if (configInfoModel is not null) {
                    await _configRepository.InsertOrUpdate(configInfoModel, token);
                }
            }
        }
    }
}