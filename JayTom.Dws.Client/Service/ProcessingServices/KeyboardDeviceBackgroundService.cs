using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Models.LocalConf.CloudConfig;
using JayTom.Dws.Application.CameraConfigurations;
using System;
using DryIoc;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using JayTom.Dws.Camera;
using SixLabors.ImageSharp;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Legacy.Contracts.Model;
using JayTom.Dws.Legacy.Contracts.Packages;
using System.Collections.Generic;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Legacy.Contracts.Services.ImageService;
using JayTom.Dws.Models.LocalConf.CameraConfig;
using JayTom.Dws.Models.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.CloudConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.CameraConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.IpcNvrConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.IpcNvrConfig;

namespace JayTom.Dws.Client.Service.ProcessingServices
{

    public class KeyboardDeviceBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;
        private readonly IDeviceService _deviceService;
        /// <summary>
        /// 获取运行期包裹会话存储。
        /// </summary>
        private readonly IPackageSessionStore _packageSessionStore;
        private readonly IImageStorageService _imageStorageService;
        private readonly ISettingsStore _settingsStore;
        private readonly ISortingService _sortingService;
        private readonly IExternalDataService _externalDataService;
        private readonly ICameraConfigurationCatalog<IpcNvrConfigInfoModel> _ipcNvrConfigRepository;
        private readonly ICameraConfigurationCatalog<NvrWatermarkConfigInfoModel> _nvrWatermarkConfigRepository;
        private readonly ICameraConfigurationCatalog<NvrCameraBindingInfoModel> _nvrCameraBindingRepository;
        private CreatePackageSettingsDto _createPackageSettingsDto = new();
        private ContentInputSettingsDto _contentInputSettingsDto = new();
        private SemaphoreSlim _createPackageSlim = new(1);
        private DateTime _lastReadTime = DateTime.Now;
        private DateTime _lastNoReadTime = DateTime.Now;
        private WeightSettingsDto _weightSettingsDto = new();
        private static bool _isWindowsClose;
        private IReadOnlyList<ICamera> _cameras = Array.Empty<ICamera>();
        private BaseDaHuatech? _baseDaHuatech;

        public KeyboardDeviceBackgroundService(IPackageSessionStore packageSessionStore,
            IDeviceService deviceService,
            IImageStorageService imageStorageService,
            ISettingsStore settingsStore,
            ISortingService sortingService,
            IExternalDataService externalDataService,
            ICameraConfigurationCatalog<PanoramaCameraConfigInfoModel> panoramaCameraConfigRepository,
            ICameraConfigurationCatalog<BarcodeScannerCameraConfigInfoModel> barcodeScannerCameraConfigRepository,
            ICameraConfigurationCatalog<IpcNvrConfigInfoModel> ipcNvrConfigRepository,
            ICameraConfigurationCatalog<NvrWatermarkConfigInfoModel> nvrWatermarkConfigRepository,
            ICameraConfigurationCatalog<NvrCameraBindingInfoModel> nvrCameraBindingRepository,
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            _packageSessionStore = packageSessionStore;
            _deviceService = deviceService;
            _imageStorageService = imageStorageService;
            _settingsStore = settingsStore;
            _sortingService = sortingService;
            _externalDataService = externalDataService;
            _ipcNvrConfigRepository = ipcNvrConfigRepository;
            _nvrWatermarkConfigRepository = nvrWatermarkConfigRepository;
            _nvrCameraBindingRepository = nvrCameraBindingRepository;

            //相机
            _deviceService.CameraInitialized += async delegate (object? sender, IReadOnlyList<ICamera> list)
            {
                _cameras = list;
                var panoramaCameras = (await panoramaCameraConfigRepository.Select(s => s.Id > 0, o => o.Id)
                    .ConfigureAwait(false)).ToList();
                var scannerCameraConfigInfoModels = (await barcodeScannerCameraConfigRepository.Select(s => s.Id > 0, o => o.Id)
                    .ConfigureAwait(false)).ToList();
                foreach (var f in _cameras)
                {
                    if (f.Info != null)
                    {
                        f.Info.CustomName = f.BindingType switch
                        {
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
                }
            };
            //条码返回
            _deviceService.BarcodeScanned += async delegate (object? sender, BarcodeReadEventArgs args)
            {
                //验证多条码
                try
                {
                    await _createPackageSlim.WaitAsync();
                    _lastReadTime = DateTime.Now;
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            _packageSessionStore.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                            _packageSessionStore.GetLastPackage(f => f.Value is { BarCodeInfo: null });

                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.ScanBarcodeCamera)
                        == PackageCreationMethodsEnum.ScanBarcodeCamera && packageInfo is null)
                    {
                        //支持扫码创建
                        packageInfo = new PackageInfo()
                        {
                            Guid = args.Timestamp,
                            BarCodeInfo = new BarCodeInfoModel()
                            {
                                Barcode = args.Barcode,
                                SerialNumber = args.CameraSerialNumber,
                                DisplayIdentifier = args.CameraSerialNumber,
                                ScanTime = args.ScanTime,
                                Source = SourceType.Camera,
                                BindTime = DateTime.Now
                            },
                            Image = JayTom.Dws.Abstractions.Imaging.ImageHandle.TakeOwnershipIfPresent(args.Image),
                        };
                        _eventBus.Publish(new TriggerPositionEvent()
                        {
                            IsSuccess = true,
                            TriggerPosition = TriggerPositionEnum.PackageTrigger,
                            PackageInfo = packageInfo
                        });
                    }
                    else
                    {
                        if (packageInfo is not null)
                        {
                            packageInfo.BarCodeInfo = new BarCodeInfoModel()
                            {
                                Barcode = args.Barcode,
                                SerialNumber = args.CameraSerialNumber,
                                DisplayIdentifier = args.CameraSerialNumber,
                                ScanTime = args.ScanTime,
                                Source = SourceType.Camera,
                                BindTime = DateTime.Now
                            };
                            packageInfo.Image = JayTom.Dws.Abstractions.Imaging.ImageHandle.TakeOwnershipIfPresent(args.Image);
                            _eventBus.Publish(new TriggerPositionEvent()
                            {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.BarCodeSetValueAfter,
                                PackageInfo = packageInfo,
                            });
                        }
                    }
                }
                finally
                {
                    _createPackageSlim.Release();
                }
            };
            _deviceService.BarCodeKeyReceived += async (sender, args) =>
            {
                //验证多条码
                try
                {
                    await _createPackageSlim.WaitAsync();
                    _lastReadTime = DateTime.Now;
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            _packageSessionStore.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                            _packageSessionStore.GetLastPackage(f => f.Value is { BarCodeInfo: null });

                    if (_createPackageSettingsDto.PackageCreationMethods.HasFlag(PackageCreationMethodsEnum.BarcodeScannerInput) && packageInfo is null)
                    {
                        //支持扫码枪创建
                        packageInfo = new PackageInfo()
                        {
                            Guid = args.Timestamp,
                            BarCodeInfo = new BarCodeInfoModel()
                            {
                                Barcode = args.Barcode,
                                SerialNumber = $"{args.Device?.DevicePath}",
                                DisplayIdentifier = $"{args.Device?.DeviceName}-{args.Device?.ManufacturerName}",
                                ScanTime = args.ScanTime,
                                Source = SourceType.Camera,
                                BindTime = DateTime.Now
                            },
                        };
                        _eventBus.Publish(new TriggerPositionEvent()
                        {
                            IsSuccess = true,
                            TriggerPosition = TriggerPositionEnum.PackageTrigger,
                            PackageInfo = packageInfo
                        });
                    }
                    else
                    {
                        if (packageInfo is not null)
                        {
                            packageInfo.BarCodeInfo = new BarCodeInfoModel()
                            {
                                Barcode = args.Barcode,
                                SerialNumber = $"{args.Device?.DevicePath}",
                                DisplayIdentifier = $"{args.Device?.DeviceName}-{args.Device?.ManufacturerName}",
                                ScanTime = args.ScanTime,
                                Source = SourceType.Camera,
                                BindTime = DateTime.Now
                            };
                            _eventBus.Publish(new TriggerPositionEvent()
                            {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.BarcodeScannerReturn,
                                PackageInfo = packageInfo,
                            });
                        }
                    }
                }
                finally
                {
                    _createPackageSlim.Release();
                }
            };
            //配置更改
            _eventBus.SubscribeAsync<SettingsChangedEvent>(async item =>
            {
                if (item is { } model)
                {
                    switch (model.SettingsName)
                    {
                        case "CreatePackageSettings":
                            _createPackageSettingsDto = await _settingsStore.GetAsync<CreatePackageSettingsDto>(model.SettingsName) ??
                                                        new CreatePackageSettingsDto();

                            break;

                        case "ContentInputSettings":
                            _contentInputSettingsDto = await _settingsStore.GetAsync<ContentInputSettingsDto>(model.SettingsName) ??
                                                       new ContentInputSettingsDto();

                            break;
                    }
                }
            });
            //创建包裹后触发
            _eventBus.SubscribeAsync<TriggerPositionEvent>(async item =>
            {
                if (item is { TriggerPosition: TriggerPositionEnum.PackageTrigger, PackageInfo: { } packageInfo })
                {
                    var info = _packageSessionStore.GetLastPackage(f => f is { Value: not null });

                    if (info is not null &&
                        packageInfo.CreateTime.Subtract(info.CreateTime).TotalMilliseconds <
                        _createPackageSettingsDto.PackageCreationInterval)
                    {
                        return;
                    }

                    packageInfo.Timestamp = new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds();

                    //添加包裹
                    var packageRemoveTimers = new List<PackageTimer>();
                    if (_createPackageSettingsDto is { IsUseEmptyPackageExpiry: true, EmptyPackageExpiryTime: > 0 })
                    {
                        packageRemoveTimers.Add(new PackageRemoveTimer()
                        {
                            Description = "空包裹过期",
                            Predicate = w => w.Value.BarCodeInfo == null,
                            RemovalTimeSpan = TimeSpan.FromMilliseconds(_createPackageSettingsDto.EmptyPackageExpiryTime)
                        });
                    }
                    if (_createPackageSettingsDto is { IsUsePackageExpiry: true, PackageExpiryTime: > 0 })
                    {
                        packageRemoveTimers.Add(new PackageRemoveTimer()
                        {
                            Description = "包裹超过生存周期",
                            RemovalTimeSpan = TimeSpan.FromMilliseconds(_createPackageSettingsDto.PackageExpiryTime)
                        });
                    }
                    _packageSessionStore.AddPackage(packageInfo, packageRemoveTimers);
                    //触发创建包裹事件
                    _eventBus.Publish(new TriggerPositionEvent()
                    {
                        IsSuccess = true,
                        TriggerPosition = TriggerPositionEnum.CreateTimePackageAfter,
                        PackageInfo = packageInfo
                    });
                }
                else if (item is
                {
                    PackageInfo: { BarCodeInfo: not null } info, TriggerPosition: TriggerPositionEnum.BarCodeSetValueAfter or
                             TriggerPositionEnum.CreateTimePackageAfter or TriggerPositionEnum.BarcodeScannerReturn
                })
                {
                    _packageSessionStore.CompletePackage(f => f.Key.Equals(info.CreateTime));
                }
            });
            //程序停止
            _eventBus.SubscribeAsync<ApplicationStatusChanged>(async item =>
            {
                if (item is { } info)
                {
                    if (info.Status == ApplicationStatus.Stop &&
                        _createPackageSettingsDto.ClearPackageQueueOnStop)
                    {
                        _packageSessionStore.ClearAllPackages();
                    }
                    else if (info.Status == ApplicationStatus.Start)
                    {
                        //重新登录大华相机(临时)
                        var ipcNvrConfigInfoModels = (await _ipcNvrConfigRepository.MemoryCacheData()).Where(w => !string.IsNullOrEmpty(w.SerialNumber)).ToList();

                        if (ipcNvrConfigInfoModels.Any(a => a.Type == 1))
                        {
                            _baseDaHuatech = BaseDaHuatech.CreateInstance();

                            foreach (var model in ipcNvrConfigInfoModels)
                            {
                                var (b, s) = await _baseDaHuatech.LogIn(model.SerialNumber, model.Username, model.Password);
                                if (!b)
                                {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"大华安防相机:{model.SerialNumber},登录失败!");
                                }
                            }
                        }
                    }
                }
            });

            //移除包裹事件
            _packageSessionStore.PackageRemoved += (sender, args) =>
            {
                _eventBus.Publish(new TriggerPositionEvent()
                {
                    IsSuccess = true,
                    TriggerPosition = TriggerPositionEnum.RemovePackageAfter,
                    PackageInfo = args.RemovedPackage,
                    Description = args.Description
                });
            };
            _packageSessionStore.PackageCompleted += (sender, args) =>
            {
                //执行输出
                if (args.CompletedPackage?.BarCodeInfo is not null &&
                    args.CompletedPackage?.WeightInfo is not null &&
                    args.CompletedPackage?.VolumeInfo is not null)
                {
                    _eventBus.Publish(args.CompletedPackage);
                }
                //写水印
                Task.Run(async () =>
                {
                    if (_baseDaHuatech is not null && args.CompletedPackage?.BarCodeInfo?.SerialNumber is not null)
                    {
                        var nvrCameraBindingInfoModels = await _nvrCameraBindingRepository.MemoryCacheData();
                        if (nvrCameraBindingInfoModels.Any(a => a.SerialNumber.Equals(args.CompletedPackage.BarCodeInfo.SerialNumber)))
                        {
                            var infoModels = nvrCameraBindingInfoModels.Where(w => w.SerialNumber.Equals(args.CompletedPackage.BarCodeInfo.SerialNumber))
                                .ToList();
                            var ipcNvrConfigs = await _ipcNvrConfigRepository.MemoryCacheData();
                            var watermarkConfigs = await _nvrWatermarkConfigRepository.MemoryCacheData();
                            var results = infoModels.Select(s =>
                            {
                                var infoModel = ipcNvrConfigs
                                    .FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress) && f.Username.Equals(s.Username));

                                if (infoModel != null)
                                {
                                    var watermarkConfigInfoModel = watermarkConfigs
                                        .FirstOrDefault(f => f.IpcNvrConfigId.Equals(infoModel.Id));

                                    if (watermarkConfigInfoModel != null)
                                    {
                                        var watermarkConfig = new SecurityCameraWatermarkConfig
                                        {
                                            Duration = watermarkConfigInfoModel.Duration,
                                            MaxWatermarks = 8,
                                            Position = 0,
                                            BackgroundColor = ColorTranslator.FromHtml(watermarkConfigInfoModel.BackgroundColorHex)
                                        };
                                        return (infoModel.SerialNumber, s.Channel, watermarkConfig, watermarkConfigInfoModel.DisplayMode);
                                    }
                                }

                                // 如果未找到匹配的 infoModel，返回默认值
                                return (string.Empty, 0, null, 0);
                            }).Where(w => !string.IsNullOrEmpty(w.SerialNumber)).ToList();

                            if (results?.Any() == true && results?.FirstOrDefault().watermarkConfig is not null)
                            {
                                if (results.FirstOrDefault().DisplayMode == 0)
                                {
                                    await _baseDaHuatech.AddRealTimeWatermark([.. results.Select(s => (s.SerialNumber, s.Channel))],
                                        args.CompletedPackage.Timestamp, args.CompletedPackage.BarCodeInfo.Barcode,
                                        results.FirstOrDefault().watermarkConfig);
                                }
                                else
                                {
                                    await _baseDaHuatech.AddSingleRealTimeWatermark([.. results.Select(s => (s.SerialNumber, s.Channel))],
                                        args.CompletedPackage.Timestamp, args.CompletedPackage.BarCodeInfo.Barcode,
                                        results.FirstOrDefault().watermarkConfig);
                                }
                            }

                            /*foreach (var model in infoModels) {
                                var infoModel = (await _ipcNvrConfigRepository.MemoryCacheData()).FirstOrDefault(f => f.IpAddress.Equals(model.IpAddress) && f.Username.Equals(model.Username));
                                if (infoModel != null) {
                                    //获取配置
                                    var watermarkConfigInfoModel = (await _nvrWatermarkConfigRepository.MemoryCacheData()).FirstOrDefault(f => f.IpcNvrConfigId.Equals(infoModel.Id));
                                    if (watermarkConfigInfoModel != null) {
                                        if (watermarkConfigInfoModel.DisplayMode == 0) {
                                            _baseDaHuatech.AddRealTimeWatermark(infoModel.SerialNumber, model.Channel,
                                                args.CompletedPackage.Timestamp, args.CompletedPackage.BarCodeInfo.Barcode,
                                                new SecurityCameraWatermarkConfig() {
                                                    Duration = watermarkConfigInfoModel.Duration,
                                                    MaxWatermarks = 8,
                                                    Position = 0,
                                                    BackgroundColor = ColorTranslator.FromHtml(watermarkConfigInfoModel.BackgroundColorHex)
                                                });
                                        }
                                        else {
                                            _baseDaHuatech.AddSingleRealTimeWatermark(infoModel.SerialNumber, model.Channel,
                                                args.CompletedPackage.Timestamp, args.CompletedPackage.BarCodeInfo.Barcode,
                                                new SecurityCameraWatermarkConfig() {
                                                    Duration = watermarkConfigInfoModel.Duration,
                                                    MaxWatermarks = 8,
                                                    Position = 0,
                                                    BackgroundColor = ColorTranslator.FromHtml(watermarkConfigInfoModel.BackgroundColorHex)
                                                });
                                        }
                                    }
                                }
                            }*/
                        }
                    }
                });
            };
            _eventBus.SubscribeAsync<WindowsAction>(async item =>
            {
                if (item is { Type: WindowsActionType.Close })
                {
                    _isWindowsClose = true;
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                //读配置

                _createPackageSettingsDto = await _settingsStore.GetAsync<CreatePackageSettingsDto>("CreatePackageSettings", stoppingToken) ?? new CreatePackageSettingsDto();
                _contentInputSettingsDto = await _settingsStore.GetAsync<ContentInputSettingsDto>("ContentInputSettings", stoppingToken) ??
                                           new ContentInputSettingsDto();
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
                try
                {
                    if (_packageSessionStore.GetPackageCount() > 0 && _deviceService.RunningStatus)
                    {
                        //判断存图路径等于空
                        var codeInfo = _packageSessionStore.GetPackage(f => f.Value is
                        {
                            IsImageSaveRequested: false,
                            BarCodeInfo: not null, IsCompleted: true
                        });
                        //存图
                        if (codeInfo?.Image != null)
                        {
                            _eventBus.Publish(new ImageMessageInfo
                            {
                                BarCode = codeInfo.BarCodeInfo?.Barcode ?? string.Empty,
                                CameraSerialNumber = codeInfo.BarCodeInfo?.SerialNumber ?? string.Empty,
                                Weight = (decimal)(codeInfo.WeightInfo?.FormattedWeight ?? 0),
                                Height = (decimal)(codeInfo.VolumeInfo?.FormattedHeight ?? 0),
                                Image = codeInfo.Image,
                                Length = (decimal)(codeInfo.VolumeInfo?.FormattedLength ?? 0),
                                Width = (decimal)(codeInfo.VolumeInfo?.FormattedWidth ?? 0),
                                Volume = (decimal)(codeInfo.VolumeInfo?.FormattedVolume ?? 0),
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
                            codeInfo.MarkImageSaveRequested();
                        }

                        //移除包裹
                        if (_createPackageSettingsDto.PackageRemoveMethods ==
                            PackageRemoveMethodsEnum.FillInformation)
                        {
                            var packageInfos = _packageSessionStore.GetPackages(w =>
                                w.Value is { IsCompleted: true, IsImageSaveRequested: true } &&
                                (w.Value.PanoramaCameraImageInfo.All(info => info.IsExists) ||
                                 DateTime.Now.Subtract(w.Value.CreateTime)
                                     .TotalMinutes > 5)) ?? new List<PackageInfo>();

                            foreach (var kvp in packageInfos)
                            {
                                _packageSessionStore.RemovePackage(kvp.CreateTime, "填充完整信息移除");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            }
        }
    }
}
