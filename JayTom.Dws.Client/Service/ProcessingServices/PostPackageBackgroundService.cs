using System;
using System.Linq;
using System.Threading;
using JayTom.Dws.Camera;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Model;
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using WindowsAction = JayTom.Dws.Client.EventMediators.WindowsAction;
using ApplicationStatus = JayTom.Dws.Client.EventMediators.ApplicationStatus;
using WindowsActionType = JayTom.Dws.Client.EventMediators.WindowsActionType;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;
using TriggerPositionEvent = JayTom.Dws.Client.EventMediators.TriggerPositionEvent;
using ApplicationStatusChanged = JayTom.Dws.Client.EventMediators.ApplicationStatusChanged;

namespace JayTom.Dws.Client.Service.ProcessingServices {

    /// <summary>
    /// 深圳邮政三台分拣机逻辑
    /// </summary>
    public class PostPackageBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IDeviceService _deviceService;
        private readonly IImageStorageService _imageStorageService;
        private readonly IConfigRepository _configRepository;
        private readonly ISortingService _sortingService;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly IGrayscaleService _grayscaleService;
        private readonly IExternalDataService _externalDataService;
        private CreatePackageSettingsDto _createPackageSettingsDto = new();
        private ConcurrentDictionary<string, BarCodeFrameInfo> _barCodeFrameInfoItem = new();
        private BarcodeFilterSettingsDto _barcodeFilterSettingsDto = new();
        private SemaphoreSlim _createPackageSlim = new(1);
        private DateTime _lastReadTime = DateTime.Now;
        private DateTime _lastNoReadTime = DateTime.Now;
        private List<ICamera> _cameras = new();
        private GrayscaleDeviceSettingsDto _grayscaleDeviceSettingsDto = new();
        private static bool _isWindowsClose;

        /// <summary>
        /// 灰度仪跳过的车辆
        /// </summary>
        public int GrayScaleSkippedVehicles { get; set; } = 0;

        public PostPackageBackgroundService(IDeviceService deviceService,
            IImageStorageService imageStorageService,
            IConfigRepository configRepository,
            ISortingService sortingService,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IGrayscaleService grayscaleService,
            IExternalDataService externalDataService) {
            _deviceService = deviceService;
            _imageStorageService = imageStorageService;
            _configRepository = configRepository;
            _sortingService = sortingService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _grayscaleService = grayscaleService;
            _externalDataService = externalDataService;
            //条码返回
            _deviceService.BarcodeScanned += async delegate (object? sender, BarcodeReadEventArgs args) {
                //验证多条码
                try {
                    await _createPackageSlim.WaitAsync();
                    _lastReadTime = DateTime.Now;
                    if (_cameras.Count(c => c.BindingType == CameraBindingType.ScannerCamera) > 1) {
                        var barCodeFrameInfo = new BarCodeFrameInfo() {
                            Timestamp = args.Timestamp,
                            Frame = args.FrameNo,
                            BarCodeInfo = new BarCodeInfoModel() {
                                Barcode = args.Barcode,
                                CameraSerialNumber = args.CameraSerialNumber,
                                ScanTime = args.ScanTime,
                                Source = SourceType.Camera,
                                BindTime = DateTime.Now
                            },
                            Image = args.Image
                        };

                        _barCodeFrameInfoItem.AddOrUpdate(args.CameraSerialNumber, key => barCodeFrameInfo,
                            (key, oldValue) => barCodeFrameInfo);
                    }
                    else {
                        //多条码判断------

                        var info = PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: not null } &&
                                                                      f.Value.BarCodeInfo.ScanTime.Equals(
                                                                          args.ScanTime) &&
                                                                      f.Value.BarCodeInfo.CameraSerialNumber.Equals(
                                                                          args.CameraSerialNumber));
                        if (info is { BarCodeInfo: not null } && _createPackageSettingsDto.BarcodeHandlingMethod != BarcodeHandlingMethodEnum.UseMultipleBarcodes) {
                            if (_createPackageSettingsDto.BarcodeHandlingMethod == BarcodeHandlingMethodEnum.MergeBarcodes) {
                                info.BarCodeInfo.Barcode += $"{_barcodeFilterSettingsDto.MultiBarcodeDelimiter}{args.Barcode}";
                            }
                            return;
                        }

                        var packageInfo =
                            _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                                PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                                PackageInfoManager.GetLastPackage(f => f.Value is { BarCodeInfo: null } &&
                                                                       args.ScanTime.Subtract(f.Value.CreateTime).TotalMilliseconds > 100);

                        if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.ScanBarcodeCamera)
                            == PackageCreationMethodsEnum.ScanBarcodeCamera && packageInfo is null) {
                            //支持扫码创建
                            packageInfo = new PackageInfo() {
                                Guid = args.Timestamp,
                                BarCodeInfo = new BarCodeInfoModel() {
                                    Barcode = args.Barcode,
                                    CameraSerialNumber = args.CameraSerialNumber,
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
                                    CameraSerialNumber = args.CameraSerialNumber,
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
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            //空包裹
            _deviceService.NotBarcodeHitEvent += async delegate (object? sender, BarcodeReadEventArgs args) {
                await Task.Yield();
                if (!_createPackageSettingsDto.IsUseNoRead) {
                    args.Image?.Dispose();
                    return;
                }
                try {
                    await _createPackageSlim.WaitAsync();
                    if (_cameras.Count(c => c.BindingType == CameraBindingType.ScannerCamera) > 1) {
                        var barCodeFrameInfo = new BarCodeFrameInfo() {
                            Timestamp = args.Timestamp,
                            Frame = args.FrameNo,
                            BarCodeInfo = new BarCodeInfoModel() {
                                Barcode = args.Barcode,
                                CameraSerialNumber = args.CameraSerialNumber,
                                ScanTime = args.ScanTime,
                                Source = SourceType.Camera,
                                BindTime = DateTime.Now
                            },
                            Image = args.Image
                        };
                        _barCodeFrameInfoItem.AddOrUpdate(args.CameraSerialNumber, key => barCodeFrameInfo,
                            (key, oldValue) => oldValue.BarCodeInfo?.Barcode?.ToLower()?.Equals("noread") != true ? oldValue : barCodeFrameInfo);
                    }
                    else {
                        if (_createPackageSettingsDto.IsUseNoReadFilter) {
                            if (DateTime.Now.Subtract(_lastNoReadTime).TotalMilliseconds < _createPackageSettingsDto.FilterInterval) {
                                args.Image?.Dispose();
                                return;
                            }
                            else {
                                _lastNoReadTime = args.ScanTime;
                            }
                        }
                        var packageInfo =
                            _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                                PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                                PackageInfoManager.GetLastPackage(f => f.Value is { BarCodeInfo: null });
                        if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.ScanBarcodeCamera) ==
                            PackageCreationMethodsEnum.ScanBarcodeCamera && packageInfo is null) {
                            //扫码相机创建

                            packageInfo = new PackageInfo() {
                                Guid = args.Timestamp,
                                BarCodeInfo = new BarCodeInfoModel() {
                                    Barcode = args.Barcode,
                                    CameraSerialNumber = args.CameraSerialNumber,
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
                                    CameraSerialNumber = args.CameraSerialNumber,
                                    ScanTime = args.ScanTime,
                                    Source = SourceType.Camera,
                                    BindTime = DateTime.Now
                                };
                                packageInfo.Image = args.Image;
                                EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                    IsSuccess = true,
                                    TriggerPosition = TriggerPositionEnum.BarCodeSetValueAfter,
                                    PackageInfo = packageInfo
                                });
                            }
                        }
                    }
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            //下位机创建包裹
            _sortingService.CreatePackageEvent += async delegate (object? sender, PackageInstructionEventArgs args) {
                try {
                    await _createPackageSlim.WaitAsync();
                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.LowerMachineCreation) ==
                        PackageCreationMethodsEnum.LowerMachineCreation) {
                        var tryParse = int.TryParse(args.Keyword, out var num);
                        if (tryParse) {
                            //创建包裹
                            var packageInfo = new PackageInfo() {
                                Guid = num,
                                IsCreatedByLowerMachine = true,
                                PackageCreationInstruction = args.Instruction,
                                CreateTime = args.InstructionTime,
                            };

                            EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.PackageTrigger,
                                PackageInfo = packageInfo
                            });
                            EventAggregator.Instance.Publish(new InstructionReceived() {
                                Timestamp = new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds(),
                                IsCreatedByLowerMachine = true,
                                SortingCode = packageInfo.Guid.ToString(),
                                InstructionInfos = new List<InstructionInfoModel>()
                                {
                                    new()
                                    {
                                        InstructionContent = args.Instruction,
                                        InstructionGeneratedTime = args.InstructionTime,
                                        InstructionType = InstructionType.CreatePackage
                                    }
                                },
                                ConnectionName = args.ConnectionName,
                            });
                        }
                    }
                }
                finally {
                    _createPackageSlim.Release();
                }
            };

            //外部全量数据
            _externalDataService.ContentInputReceived += async (sender, args) => {
                try {
                    await _createPackageSlim.WaitAsync();
                    var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                            PackageInfoManager.GetLastPackage(f => f.Value is { BarCodeInfo: null });
                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.TcpInput) ==
                        PackageCreationMethodsEnum.TcpInput && packageInfo is null) {
                        packageInfo = new PackageInfo() {
                            Guid = timestamp,
                            BarCodeInfo = new BarCodeInfoModel() {
                                Barcode = args.Barcode,
                                ScanTime = DateTime.Now,
                                Source = SourceType.Input,
                            },
                            WeightInfo = new WeightInfoModel() {
                                CreateTime = DateTime.Now,
                                FormattedWeight = args.Weight,
                                SourceType = SourceType.Input,
                                OriginalText = args.SourceContent
                            },
                            VolumeInfo = new VolumeInfoModel() {
                                CreateTime = DateTime.Now,
                                FormattedHeight = args.Height,
                                FormattedLength = args.Length,
                                FormattedVolume = args.Volume,
                                FormattedWidth = args.Width,
                                SourceType = SourceType.Input,
                                OriginalText = args.SourceContent
                            },
                            CreateTime = DateTime.Now,
                            IsCreatedByLowerMachine = false,
                            IsSavedImage = true
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
                                ScanTime = DateTime.Now,
                                Source = SourceType.Input,
                            };
                            packageInfo.WeightInfo = new WeightInfoModel() {
                                CreateTime = DateTime.Now,
                                FormattedWeight = args.Weight,
                                SourceType = SourceType.Input,
                                OriginalText = args.SourceContent
                            };
                            packageInfo.VolumeInfo = new VolumeInfoModel() {
                                CreateTime = DateTime.Now,
                                FormattedHeight = args.Height,
                                FormattedLength = args.Length,
                                FormattedVolume = args.Volume,
                                FormattedWidth = args.Width,
                                SourceType = SourceType.Input,
                                OriginalText = args.SourceContent
                            };
                            EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.BarCodeSetValueAfter,
                                PackageInfo = packageInfo
                            });
                            EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.WeightSetValueAfter,
                                PackageInfo = packageInfo
                            });
                            EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.VolumeSetValueAfter,
                                PackageInfo = packageInfo
                            });
                        }
                    }
                }
                finally {
                    _createPackageSlim.Release();
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

                        case "BarcodeFilterSettings":
                            _barcodeFilterSettingsDto = await _configRepository.FirstOrDefaultEntity<BarcodeFilterSettingsDto>(model.SettingsName) ??
                                                        new BarcodeFilterSettingsDto();
                            break;

                        case "GrayscaleDeviceSettings":
                            _grayscaleDeviceSettingsDto = await _configRepository.FirstOrDefaultEntity<GrayscaleDeviceSettingsDto>(model.SettingsName)
                                                          ?? new GrayscaleDeviceSettingsDto();

                            break;
                    }
                    //其他设置
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

                    if (_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector &&
                        _grayscaleService.IsConnected) {
                        //跳过车辆
                        var increaseCarCount = _grayscaleService.IncreaseCarCount((int)packageInfo.Guid,
                            _grayscaleDeviceSettingsDto.CarNumberOffset);

                        var package = PackageInfoManager.GetLastPackage(s => s.Value != null && s.Value.Guid.Equals(increaseCarCount));

                        if (GrayScaleSkippedVehicles > 1) {
                            GrayScaleSkippedVehicles--;
                            NLog.LogManager.GetCurrentClassLogger().Error("前车联动了多车,该车跳过");
                            if (package?.BarCodeInfo != null && package.BarCodeInfo?.Barcode.Equals("noread",
                                    StringComparison.CurrentCultureIgnoreCase) != true) {
                                package.LinkedCarCount = 1;
                                PackageInfoManager.CompletedPackage(f => f.Value?.CreateTime.Equals(package.CreateTime) == true);
                            }

                            return;
                        }
                        //动态时间
                        var milliseconds = DateTime.Now.Subtract(packageInfo.CreateTime).TotalMilliseconds;
                        if (milliseconds < 50) {
                            await Task.Delay((int)(50 - milliseconds));
                        }
                        else {
                            NLog.LogManager.GetCurrentClassLogger().Error($"创建包裹到现在的间隔:{milliseconds}ms");
                        }

                        var singleGrayscaleSensorResult = await _grayscaleService.GetSingleGrayscaleSensorResult(packageInfo.Guid, _grayscaleDeviceSettingsDto.TimeOut);

                        if (singleGrayscaleSensorResult is not null) {
                            //联动车辆
                            GrayScaleSkippedVehicles = singleGrayscaleSensorResult.LinkedCarCount;

                            //双车赋值

                            if (package is { BarCodeInfo: not null }) {
                                if (package.BarCodeInfo?.Barcode.Equals("noread", StringComparison.CurrentCultureIgnoreCase) == true &&
                                    singleGrayscaleSensorResult.MainRectangleBoxInfos?.Any(a => a.PackageRatio >=
                                                                                                (decimal)_grayscaleDeviceSettingsDto.AdditionalBoxSpacePercentage / 100) != true) {
                                    package.Image?.Dispose();
                                    PackageInfoManager.RemovePackage(package.CreateTime);
                                    package.BarCodeInfo = null;
                                }
                                else {
                                    package.LinkedCarCount = singleGrayscaleSensorResult.LinkedCarCount;
                                    PackageInfoManager.CompletedPackage(f => f.Value?.CreateTime.Equals(package.CreateTime) == true);
                                }
                            }
                            packageInfo.GrayscaleResultInfo = singleGrayscaleSensorResult;
                        }

                        if (_grayscaleDeviceSettingsDto.IsCheckPackageOrientation &&
                            packageInfo.GrayscaleResultInfo is not null &&
                            packageInfo.GrayscaleResultInfo?.MainRectangleBoxInfos?.Any(a => a.PackageRatio > (decimal)0.15) == true) {
                            //发送包裹居中指令
                            _sortingService.SendPackageCenter(packageInfo.GrayscaleResultInfo.CarNumber, new InstructionsAttach() {
                                BarCode = string.Empty,
                                Guid = packageInfo.GrayscaleResultInfo.CarNumber,
                                Timestamp = packageInfo.Timestamp,
                                LinkedCarCount = packageInfo.GrayscaleResultInfo.LinkedCarCount,
                                PackagePositionInfo = new PackagePositionInfo() {
                                    CenterX = packageInfo.GrayscaleResultInfo.CenterPoint.X,
                                    CenterY = packageInfo.GrayscaleResultInfo.CenterPoint.Y,
                                    OffsetDirection = (OffsetDirection)(packageInfo.GrayscaleResultInfo.MainRectangleBoxInfos?.FirstOrDefault()?.PackageOrientation ?? PackageOrientation.Left),
                                    OffsetDistance = packageInfo.GrayscaleResultInfo.MainRectangleBoxInfos?.FirstOrDefault()?.OrientationValue ?? 0,
                                    OffsetPercentage = packageInfo.GrayscaleResultInfo.MainRectangleBoxInfos?.FirstOrDefault()?.OffsetPercentage ?? 0
                                },
                            });
                            //如果是没包裹则返回
                        }
                    }
                    packageInfo.Timestamp = new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds();

                    //添加包裹
                    var packageRemoveTimers = new List<PackageRemoveTimer>();
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
                else if (item is { TriggerPosition: TriggerPositionEnum.BarCodeSetValueAfter, PackageInfo: { } info }) {
                    //邮政专供
                    if (!_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector) {
                        PackageInfoManager.CompletedPackage(f => f.Key.Equals(info.CreateTime));
                    }
                }
            });
            //程序停止
            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(item => {
                if (item is { } info) {
                    if (info.Status == ApplicationStatus.Stop &&
                        _createPackageSettingsDto.ClearPackageQueueOnStop) {
                        PackageInfoManager.ClearAllPackages();
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
            };
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                if (item is { Type: WindowsActionType.Close }) {
                    _isWindowsClose = true;
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                //读配置

                _createPackageSettingsDto = await _configRepository.FirstOrDefaultEntity<CreatePackageSettingsDto>("CreatePackageSettings", stoppingToken) ?? new CreatePackageSettingsDto();
                _barcodeFilterSettingsDto = await _configRepository.FirstOrDefaultEntity<BarcodeFilterSettingsDto>("BarcodeFilterSettings", stoppingToken) ?? new BarcodeFilterSettingsDto();
                _grayscaleDeviceSettingsDto = await _configRepository.FirstOrDefaultEntity<GrayscaleDeviceSettingsDto>("GrayscaleDeviceSettings", stoppingToken) ?? new GrayscaleDeviceSettingsDto();
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken).ContinueWith(a => {
                    try {
                        if (PackageInfoManager.GetPackageCount() > 0 && _deviceService.RunningStatus) {
                            /*var value = PackageInfoManager.GetPackage(f => f.Value is { IsCompleted: false, BarCodeInfo: not null });
                            if (value != null) {
                                if (!_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector) {
                                    value.IsCompleted = true;
                                    PackageInfoManager.CompletedPackage(f => f.Value?.CreateTime.Equals(value.CreateTime) == true);
                                }
                                else {
                                    //填充灰度仪
                                    if (!_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector) {
                                        value.GrayscaleResultInfo = new GrayscaleResult();
                                    }
                                }
                            }*/

                            if (PackageInfoManager.GetPackageCount() > 0) {
                                //判断存图路径等于空
                                var codeInfo = PackageInfoManager.GetPackage(f => f.Value is {
                                    IsSavedImage: false,
                                    BarCodeInfo: not null, IsCompleted: true
                                });
                                //存图
                                if (codeInfo?.Image != null) {
                                    EventAggregator.Instance.Publish(new ImageMessageInfo {
                                        BarCode = codeInfo.BarCodeInfo?.Barcode ?? string.Empty,
                                        CameraSerialNumber = codeInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
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
                                                    codeInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty))?.Info
                                            ?.Name ?? string.Empty,
                                        CameraCustomName = _cameras.FirstOrDefault(f =>
                                                (bool)f.Info?.SerialNumber.Equals(
                                                    codeInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty))?.Info
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
                    }
                    catch (Exception e) {
                        NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                    }
                }, stoppingToken);
            }
        }
    }
}