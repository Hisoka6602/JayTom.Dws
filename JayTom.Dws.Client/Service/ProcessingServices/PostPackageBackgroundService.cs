using JayTom.Dws.Application.Configuration;
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
using WindowsAction = JayTom.Dws.Domain.EventMediators.WindowsAction;
using ApplicationStatus = JayTom.Dws.Domain.EventMediators.ApplicationStatus;
using WindowsActionType = JayTom.Dws.Domain.EventMediators.WindowsActionType;
using SettingsChangedEvent = JayTom.Dws.Domain.EventMediators.SettingsChangedEvent;
using TriggerPositionEvent = JayTom.Dws.Domain.EventMediators.TriggerPositionEvent;
using ApplicationStatusChanged = JayTom.Dws.Domain.EventMediators.ApplicationStatusChanged;

namespace JayTom.Dws.Client.Service.ProcessingServices
{

    /// <summary>
    /// 深圳邮政三台分拣机逻辑
    /// </summary>
    public class PostPackageBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IDeviceService _deviceService;
        /// <summary>
        /// 获取运行期包裹会话存储。
        /// </summary>
        private readonly IPackageSessionStore _packageSessionStore;
        private readonly IImageStorageService _imageStorageService;
        private readonly ISettingsStore _settingsStore;
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
        private IReadOnlyList<ICamera> _cameras = Array.Empty<ICamera>();
        private GrayscaleDeviceSettingsDto _grayscaleDeviceSettingsDto = new();
        private static bool _isWindowsClose;
        private ConcurrentDictionary<int, GrayscaleResult> _grayscaleResultItems = new();

        /// <summary>
        /// 灰度仪跳过的车辆
        /// </summary>
        public int GrayScaleSkippedVehicles { get; set; } = 0;

        public PostPackageBackgroundService(IPackageSessionStore packageSessionStore,
            IDeviceService deviceService,
            IImageStorageService imageStorageService,
            ISettingsStore settingsStore,
            ISortingService sortingService,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IGrayscaleService grayscaleService,
            IExternalDataService externalDataService)
        {
            _packageSessionStore = packageSessionStore;
            _deviceService = deviceService;
            _imageStorageService = imageStorageService;
            _settingsStore = settingsStore;
            _sortingService = sortingService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _grayscaleService = grayscaleService;
            _externalDataService = externalDataService;
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
                            _packageSessionStore.GetLastPackage(f => f.Value is { BarCodeInfo: null } &&
                                                                   args.ScanTime.Subtract(f.Value.CreateTime).TotalMilliseconds > 100);

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
                        EventAggregator.Instance.Publish(new TriggerPositionEvent()
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
                            EventAggregator.Instance.Publish(new TriggerPositionEvent()
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
            //空包裹
            _deviceService.BarcodeMissed += async delegate (object? sender, BarcodeReadEventArgs args)
            {
                await Task.Yield();
                if (!_createPackageSettingsDto.IsUseNoRead)
                {
                    args.Image?.Dispose();
                    return;
                }
                try
                {
                    await _createPackageSlim.WaitAsync();
                    if (_cameras.Count(c => c.BindingType == CameraBindingType.ScannerCamera) > 1)
                    {
                        var barCodeFrameInfo = new BarCodeFrameInfo()
                        {
                            Timestamp = args.Timestamp,
                            Frame = args.FrameNo,
                            BarCodeInfo = new BarCodeInfoModel()
                            {
                                Barcode = args.Barcode,
                                SerialNumber = args.CameraSerialNumber,
                                DisplayIdentifier = args.CameraSerialNumber,
                                ScanTime = args.ScanTime,
                                Source = SourceType.Camera,
                                BindTime = DateTime.Now
                            },
                            Image = JayTom.Dws.Abstractions.Imaging.ImageHandle.TakeOwnershipIfPresent(args.Image)
                        };
                        _barCodeFrameInfoItem.AddOrUpdate(args.CameraSerialNumber, key => barCodeFrameInfo,
                            (key, oldValue) => oldValue.BarCodeInfo?.Barcode?.ToLower()?.Equals("noread") != true ? oldValue : barCodeFrameInfo);
                    }
                    else
                    {
                        if (_createPackageSettingsDto.IsUseNoReadFilter)
                        {
                            if (DateTime.Now.Subtract(_lastNoReadTime).TotalMilliseconds < _createPackageSettingsDto.FilterInterval)
                            {
                                args.Image?.Dispose();
                                return;
                            }
                            else
                            {
                                _lastNoReadTime = args.ScanTime;
                            }
                        }
                        var packageInfo =
                            _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                                _packageSessionStore.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                                _packageSessionStore.GetLastPackage(f => f.Value is { BarCodeInfo: null });
                        if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.ScanBarcodeCamera) ==
                            PackageCreationMethodsEnum.ScanBarcodeCamera && packageInfo is null)
                        {
                            //扫码相机创建

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
                            EventAggregator.Instance.Publish(new TriggerPositionEvent()
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
                                EventAggregator.Instance.Publish(new TriggerPositionEvent()
                                {
                                    IsSuccess = true,
                                    TriggerPosition = TriggerPositionEnum.BarCodeSetValueAfter,
                                    PackageInfo = packageInfo
                                });
                            }
                        }
                    }
                }
                finally
                {
                    _createPackageSlim.Release();
                }
            };
            //下位机创建包裹
            _sortingService.CreatePackageEvent += async delegate (object? sender, PackageInstructionEventArgs args)
            {
                try
                {
                    await _createPackageSlim.WaitAsync();
                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.LowerMachineCreation) ==
                        PackageCreationMethodsEnum.LowerMachineCreation)
                    {
                        var tryParse = int.TryParse(args.Keyword, out var num);
                        if (tryParse)
                        {
                            //创建包裹
                            var packageInfo = new PackageInfo()
                            {
                                Guid = num,
                                IsCreatedByLowerMachine = true,
                                PackageCreationInstruction = args.Instruction,
                                CreateTime = args.InstructionTime,
                            };

                            EventAggregator.Instance.Publish(new TriggerPositionEvent()
                            {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.PackageTrigger,
                                PackageInfo = packageInfo
                            });
                            EventAggregator.Instance.Publish(new InstructionReceived()
                            {
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
                finally
                {
                    _createPackageSlim.Release();
                }
            };

            //外部全量数据
            _externalDataService.ContentInputReceived += async (sender, args) =>
            {
                try
                {
                    await _createPackageSlim.WaitAsync();
                    var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            _packageSessionStore.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                            _packageSessionStore.GetLastPackage(f => f.Value is { BarCodeInfo: null });
                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.TcpInput) ==
                        PackageCreationMethodsEnum.TcpInput && packageInfo is null)
                    {
                        packageInfo = new PackageInfo()
                        {
                            Guid = timestamp,
                            BarCodeInfo = new BarCodeInfoModel()
                            {
                                Barcode = args.Barcode,
                                ScanTime = DateTime.Now,
                                Source = SourceType.Input,
                            },
                            WeightInfo = new WeightInfoModel()
                            {
                                CreateTime = DateTime.Now,
                                FormattedWeight = args.Weight,
                                SourceType = SourceType.Input,
                                OriginalText = args.SourceContent
                            },
                            VolumeInfo = new VolumeInfoModel()
                            {
                                CreateTime = DateTime.Now,
                                FormattedHeight = Convert.ToDecimal(args.Height),
                                FormattedLength = Convert.ToDecimal(args.Length),
                                FormattedVolume = Convert.ToDecimal(args.Volume),
                                FormattedWidth = Convert.ToDecimal(args.Width),
                                SourceType = SourceType.Input,
                                OriginalText = args.SourceContent
                            },
                            CreateTime = DateTime.Now,
                            IsCreatedByLowerMachine = false,
                            IsImageSaveRequested = true
                        };

                        EventAggregator.Instance.Publish(new TriggerPositionEvent()
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
                                ScanTime = DateTime.Now,
                                Source = SourceType.Input,
                            };
                            packageInfo.WeightInfo = new WeightInfoModel()
                            {
                                CreateTime = DateTime.Now,
                                FormattedWeight = args.Weight,
                                SourceType = SourceType.Input,
                                OriginalText = args.SourceContent
                            };
                            packageInfo.VolumeInfo = new VolumeInfoModel()
                            {
                                CreateTime = DateTime.Now,
                                FormattedHeight = Convert.ToDecimal(args.Height),
                                FormattedLength = Convert.ToDecimal(args.Length),
                                FormattedVolume = Convert.ToDecimal(args.Volume),
                                FormattedWidth = Convert.ToDecimal(args.Width),
                                SourceType = SourceType.Input,
                                OriginalText = args.SourceContent
                            };
                            EventAggregator.Instance.Publish(new TriggerPositionEvent()
                            {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.BarCodeSetValueAfter,
                                PackageInfo = packageInfo
                            });
                            EventAggregator.Instance.Publish(new TriggerPositionEvent()
                            {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.WeightSetValueAfter,
                                PackageInfo = packageInfo
                            });
                            EventAggregator.Instance.Publish(new TriggerPositionEvent()
                            {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.VolumeSetValueAfter,
                                PackageInfo = packageInfo
                            });
                        }
                    }
                }
                finally
                {
                    _createPackageSlim.Release();
                }
            };
            _grayscaleService.GrayscaleSensorResultReceived += (sender, result) =>
            {
                //给小车赋值
                //取出超时的删除

                var pairs = _grayscaleResultItems.Where(s =>
                        s.Value != null && DateTime.Now.Subtract(s.Value.ResultTime).TotalSeconds > 3)
                    .ToList();
                foreach (var pair in pairs)
                {
                    _grayscaleResultItems.TryRemove(pair);
                }

                _grayscaleResultItems.TryAdd(result.CarNumber, result);

                var package = _packageSessionStore.GetLastPackage(s => s.Value != null && s.Value.Guid.Equals(result.CarNumber));
                if (package is not null)
                {
                    /*if (package.BarCodeInfo != null && package.BarCodeInfo?.Barcode.Equals("noread",
                            StringComparison.CurrentCultureIgnoreCase) != true) {
                        package.LinkedCarCount = 1;
                        _packageSessionStore.CompletePackage(f => f.Value?.CreateTime.Equals(package.CreateTime) == true);
                    }*/

                    //联动车辆
                    GrayScaleSkippedVehicles = result.LinkedCarCount;

                    if (package is { BarCodeInfo: not null })
                    {
                        if (package.BarCodeInfo?.Barcode.Equals("noread", StringComparison.CurrentCultureIgnoreCase) == true &&
                            result.MainRectangleBoxInfos?.Any(a => a.PackageRatio >=
                                                                   (decimal)_grayscaleDeviceSettingsDto.AdditionalBoxSpacePercentage / 100) != true)
                        {
                            _packageSessionStore.RemovePackage(package.CreateTime);
                            package.BarCodeInfo = null;
                            return;
                        }
                        else
                        {
                            package.LinkedCarCount = result.LinkedCarCount;
                            _packageSessionStore.CompletePackage(f => f.Value?.CreateTime.Equals(package.CreateTime) == true);
                        }
                    }

                    package.GrayscaleResultInfo = result;
                    if (_grayscaleDeviceSettingsDto.IsCheckPackageOrientation &&
                        package.GrayscaleResultInfo is not null &&
                        package.GrayscaleResultInfo?.MainRectangleBoxInfos?.Any(a => a.PackageRatio >= (decimal)_grayscaleDeviceSettingsDto.MainBoxPackageRatio / 100) == true &&
                        result.ResultTime.Subtract(package.CreateTime).TotalMilliseconds <= _grayscaleDeviceSettingsDto.TimeOut)
                    {
                        _sortingService.SendPackageCenter(result.CarNumber, new InstructionsAttach()
                        {
                            BarCode = string.Empty,
                            Guid = result.CarNumber,
                            Timestamp = package.Timestamp,
                            LinkedCarCount = result.LinkedCarCount,
                            PackagePositionInfo = new PackagePositionInfo()
                            {
                                CenterX = result.CenterPoint.X,
                                CenterY = result.CenterPoint.Y,
                                OffsetDirection = (OffsetDirection)(result.MainRectangleBoxInfos?.FirstOrDefault(f => f.PackageRatio >= (decimal)_grayscaleDeviceSettingsDto.MainBoxPackageRatio / 100)?.PackageOrientation ?? PackageOrientation.Left),
                                OffsetDistance = result.MainRectangleBoxInfos?.FirstOrDefault(f => f.PackageRatio >= (decimal)_grayscaleDeviceSettingsDto.MainBoxPackageRatio / 100)?.OrientationValue ?? 0,
                                OffsetPercentage = result.MainRectangleBoxInfos?.FirstOrDefault(f => f.PackageRatio >= (decimal)_grayscaleDeviceSettingsDto.MainBoxPackageRatio / 100)?.OffsetPercentage ?? 0
                            },
                        });
                        //如果是没包裹则返回
                    }
                }
            };
            //配置更改
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item =>
            {
                if (item is { } model)
                {
                    switch (model.SettingsName)
                    {
                        case "CreatePackageSettings":
                            _createPackageSettingsDto = await _settingsStore.GetAsync<CreatePackageSettingsDto>(model.SettingsName) ??
                                                        new CreatePackageSettingsDto();

                            break;

                        case "BarcodeFilterSettings":
                            _barcodeFilterSettingsDto = await _settingsStore.GetAsync<BarcodeFilterSettingsDto>(model.SettingsName) ??
                                                        new BarcodeFilterSettingsDto();
                            break;

                        case "GrayscaleDeviceSettings":
                            _grayscaleDeviceSettingsDto = await _settingsStore.GetAsync<GrayscaleDeviceSettingsDto>(model.SettingsName)
                                                          ?? new GrayscaleDeviceSettingsDto();

                            break;
                    }
                    //其他设置
                }
            });
            //创建包裹后触发
            EventAggregator.Instance.Subscribe<TriggerPositionEvent>(async item =>
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

                    //添加包裹
                    var packageTimers = new List<PackageTimer>()
                    {
                        //无检测包裹自动完成
                        new PackCompletedTimer()
                        {
                            Predicate =  w => w.Value.BarCodeInfo != null
                                              &&!w.Value.BarCodeInfo.Barcode.
                                                  Equals("noread", StringComparison.CurrentCultureIgnoreCase)
                            &&!w.Value.IsCompleted,
                            CompletTimeSpan =  TimeSpan.FromMilliseconds(_grayscaleDeviceSettingsDto.TimeOut+130)
                        }
                    };
                    if (_createPackageSettingsDto is { IsUseEmptyPackageExpiry: true, EmptyPackageExpiryTime: > 0 })
                    {
                        packageTimers.Add(new PackageRemoveTimer()
                        {
                            Description = "空包裹过期",
                            Predicate = w => w.Value.BarCodeInfo == null,
                            RemovalTimeSpan = TimeSpan.FromMilliseconds(_createPackageSettingsDto.EmptyPackageExpiryTime)
                        });
                    }
                    if (_createPackageSettingsDto is { IsUsePackageExpiry: true, PackageExpiryTime: > 0 })
                    {
                        packageTimers.Add(new PackageRemoveTimer()
                        {
                            Description = "包裹超过生存周期",
                            RemovalTimeSpan = TimeSpan.FromMilliseconds(_createPackageSettingsDto.PackageExpiryTime)
                        });
                    }
                    packageInfo.Timestamp = new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds();
                    _packageSessionStore.AddPackage(packageInfo, packageTimers);

                    //触发创建包裹事件
                    EventAggregator.Instance.Publish(new TriggerPositionEvent()
                    {
                        IsSuccess = true,
                        TriggerPosition = TriggerPositionEnum.CreateTimePackageAfter,
                        PackageInfo = packageInfo
                    });
                }
                else if (item is { TriggerPosition: TriggerPositionEnum.CreateTimePackageAfter, PackageInfo: { } createPackageInfo })
                {
                    //触发灰度仪
                    if (_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector &&
                        _grayscaleService.IsConnected)
                    {
                        //灰度仪操作放在这里
                        if (_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector &&
                         _grayscaleService.IsConnected)
                        {
                            //跳过车辆
                            /*var increaseCarCount = _grayscaleService.IncreaseCarCount((int)createPackageInfo.Guid,
                                _grayscaleDeviceSettingsDto.CarNumberOffset);*/

                            //var package = _grayscaleDeviceSettingsDto.CarNumberOffset == 0 ? packageInfo : _packageSessionStore.GetLastPackage(s => s.Value != null && s.Value.Guid.Equals(increaseCarCount));

                            if (GrayScaleSkippedVehicles > 1)
                            {
                                GrayScaleSkippedVehicles--;
                                /*NLog.LogManager.GetCurrentClassLogger().Error("前车联动了多车,该车跳过");
                                if (package?.BarCodeInfo != null && package.BarCodeInfo?.Barcode.Equals("noread",
                                        StringComparison.CurrentCultureIgnoreCase) != true) {
                                    package.LinkedCarCount = 1;
                                    _packageSessionStore.CompletePackage(f => f.Value?.CreateTime.Equals(package.CreateTime) == true);
                                }
                                else {
                                    _packageSessionStore.RemovePackage(packageInfo.CreateTime);
                                }*/

                                return;
                            }

                            var result = await _grayscaleService.GetSingleGrayscaleSensorResult(createPackageInfo.Guid, _grayscaleDeviceSettingsDto.TimeOut);

                            if (result is { IsTimeOut: true })
                            {
                                var package = _packageSessionStore.GetLastPackage(s => s.Value != null && s.Value.Guid.Equals(result.CarNumber));
                                if (package is { IsCompleted: false, BarCodeInfo: not null } && !package.BarCodeInfo.Barcode.Equals("noread", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    _packageSessionStore.CompletePackage(f => f.Value?.CreateTime.Equals(package.CreateTime) == true);
                                }
                            }

                            /*if (singleGrayscaleSensorResult is not null) {
                                //联动车辆
                                GrayScaleSkippedVehicles = singleGrayscaleSensorResult.LinkedCarCount;

                                //双车赋值

                                if (package is { BarCodeInfo: not null }) {
                                    if (package.BarCodeInfo?.Barcode.Equals("noread", StringComparison.CurrentCultureIgnoreCase) == true &&
                                        singleGrayscaleSensorResult.MainRectangleBoxInfos?.Any(a => a.PackageRatio >=
                                                                                                    (decimal)_grayscaleDeviceSettingsDto.AdditionalBoxSpacePercentage / 100) != true) {
                                        package.Image?.Dispose();
                                        _packageSessionStore.RemovePackage(package.CreateTime);
                                        package.BarCodeInfo = null;
                                    }
                                    else {
                                        package.LinkedCarCount = singleGrayscaleSensorResult.LinkedCarCount;
                                        _packageSessionStore.CompletePackage(f => f.Value?.CreateTime.Equals(package.CreateTime) == true);
                                    }
                                }
                                else if (package?.CreateTime.Equals(packageInfo.CreateTime) == true) {
                                    package.LinkedCarCount = singleGrayscaleSensorResult.LinkedCarCount;
                                }
                                packageInfo.GrayscaleResultInfo = singleGrayscaleSensorResult;
                            }

                            if (_grayscaleDeviceSettingsDto.IsCheckPackageOrientation &&
                                packageInfo.GrayscaleResultInfo is not null &&
                                packageInfo.GrayscaleResultInfo?.MainRectangleBoxInfos?.Any(a => a.PackageRatio >= (decimal)0.15) == true) {
                                //发送包裹居中指令
                                _sortingService.SendPackageCenter(packageInfo.GrayscaleResultInfo.CarNumber, new InstructionsAttach() {
                                    BarCode = string.Empty,
                                    Guid = packageInfo.GrayscaleResultInfo.CarNumber,
                                    Timestamp = packageInfo.Timestamp,
                                    LinkedCarCount = packageInfo.GrayscaleResultInfo.LinkedCarCount,
                                    PackagePositionInfo = new PackagePositionInfo() {
                                        CenterX = packageInfo.GrayscaleResultInfo.CenterPoint.X,
                                        CenterY = packageInfo.GrayscaleResultInfo.CenterPoint.Y,
                                        OffsetDirection = (OffsetDirection)(packageInfo.GrayscaleResultInfo.MainRectangleBoxInfos?.FirstOrDefault(f => f.PackageRatio >= (decimal)0.15)?.PackageOrientation ?? PackageOrientation.Left),
                                        OffsetDistance = packageInfo.GrayscaleResultInfo.MainRectangleBoxInfos?.FirstOrDefault(f => f.PackageRatio >= (decimal)0.15)?.OrientationValue ?? 0,
                                        OffsetPercentage = packageInfo.GrayscaleResultInfo.MainRectangleBoxInfos?.FirstOrDefault(f => f.PackageRatio >= (decimal)0.15)?.OffsetPercentage ?? 0
                                    },
                                });
                                //如果是没包裹则返回
                            }*/
                        }
                    }
                    else if (_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector &&
                             !_grayscaleService.IsConnected)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error($"灰度仪未连接");
                    }
                }
                else if (item is { TriggerPosition: TriggerPositionEnum.BarCodeSetValueAfter, PackageInfo: { } info })
                {
                    //邮政专供
                    if (!_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector)
                    {
                        _packageSessionStore.CompletePackage(f => f.Key.Equals(info.CreateTime));
                    }
                    else if (_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector &&
                             DateTime.Now.Subtract(info.CreateTime).TotalMilliseconds > _grayscaleDeviceSettingsDto.TimeOut &&
                             info.BarCodeInfo?.Barcode?.Equals("noread", StringComparison.CurrentCultureIgnoreCase) != true)
                    {
                        info.LinkedCarCount = info.GrayscaleResultInfo?.LinkedCarCount ?? 1;
                        _packageSessionStore.CompletePackage(f => f.Key.Equals(info.CreateTime));
                    }
                    else if (_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector &&
                             info.BarCodeInfo?.Barcode?.Equals("noread", StringComparison.CurrentCultureIgnoreCase) != true)
                    {
                        info.LinkedCarCount = info.GrayscaleResultInfo?.LinkedCarCount ?? 1;
                        //_packageSessionStore.CompletePackage(f => f.Key.Equals(info.CreateTime));
                    }
                    else if ((info.LinkedCarCount > 0 && info.GrayscaleResultInfo is not null &&
                              info.GrayscaleResultInfo.MainRectangleBoxInfos.Any(a => a.PackageRatio >= (decimal)_grayscaleDeviceSettingsDto.MainBoxPackageRatio / 100)))
                    {
                        info.LinkedCarCount = info.GrayscaleResultInfo.LinkedCarCount;
                        _packageSessionStore.CompletePackage(f => f.Key.Equals(info.CreateTime));
                    }
                    else
                    {
                        //获取灰度仪的结果

                        _grayscaleResultItems.TryGetValue((int)info.Guid, out var result);
                        if (result is { LinkedCarCount: > 0 } &&
                            result.MainRectangleBoxInfos.Any(a => a.PackageRatio >= (decimal)_grayscaleDeviceSettingsDto.MainBoxPackageRatio / 100))
                        {
                            info.LinkedCarCount = result.LinkedCarCount;
                            info.GrayscaleResultInfo = result;
                            _packageSessionStore.CompletePackage(f => f.Key.Equals(info.CreateTime));
                        }
                    }
                }
                else if (item is { TriggerPosition: TriggerPositionEnum.CreateTimePackageAfter, PackageInfo: { } package })
                {
                    if (package.BarCodeInfo is not null && package.BarCodeInfo?.Barcode?.Equals("NoRead") != true)
                    {
                        //正式使用需要判断 LinkedCarCount和 GrayscaleResultInfo
                        _packageSessionStore.CompletePackage(f => f.Key.Equals(package.CreateTime));
                    }
                }
            });

            //程序停止
            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(item =>
            {
                if (item is { } info)
                {
                    if (info.Status == ApplicationStatus.Stop &&
                        _createPackageSettingsDto.ClearPackageQueueOnStop)
                    {
                        _packageSessionStore.ClearAllPackages();
                        _grayscaleResultItems.Clear();
                    }
                }
            });

            //移除包裹事件
            _packageSessionStore.PackageRemoved += (sender, args) =>
            {
                EventAggregator.Instance.Publish(new TriggerPositionEvent()
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
                    EventAggregator.Instance.Publish(args.CompletedPackage);
                }
            };
            EventAggregator.Instance.Subscribe<WindowsAction>(async item =>
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
                _barcodeFilterSettingsDto = await _settingsStore.GetAsync<BarcodeFilterSettingsDto>("BarcodeFilterSettings", stoppingToken) ?? new BarcodeFilterSettingsDto();
                _grayscaleDeviceSettingsDto = await _settingsStore.GetAsync<GrayscaleDeviceSettingsDto>("GrayscaleDeviceSettings", stoppingToken) ?? new GrayscaleDeviceSettingsDto();
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken).ConfigureAwait(false);
                try
                {
                    if (_packageSessionStore.GetPackageCount() > 0 && _deviceService.RunningStatus)
                    {
                        /*var value = _packageSessionStore.GetPackage(f => f.Value is { IsCompleted: false, BarCodeInfo: not null });
                        if (value != null) {
                            if (!_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector) {
                                value.MarkCompleted();
                                _packageSessionStore.CompletePackage(f => f.Value?.CreateTime.Equals(value.CreateTime) == true);
                            }
                            else {
                                //填充灰度仪
                                if (!_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector) {
                                    value.GrayscaleResultInfo = new GrayscaleResult();
                                }
                            }
                        }*/

                        if (_packageSessionStore.GetPackageCount() > 0)
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
                                EventAggregator.Instance.Publish(new ImageMessageInfo
                                {
                                    PackageTimestamped = codeInfo.Timestamp,
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
                }
                catch (Exception e)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            }
        }
    }
}
