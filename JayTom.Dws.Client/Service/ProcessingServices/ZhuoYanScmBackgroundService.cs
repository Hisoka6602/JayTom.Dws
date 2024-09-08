using NLog;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Camera;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Domain.Model;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.ApiDto;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.Service.ProcessingServices {

    public class ZhuoYanScmBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IDeviceService _deviceService;
        private readonly IImageStorageService _imageStorageService;
        private readonly IConfigRepository _configRepository;
        private readonly ISortingService _sortingService;
        private readonly IExternalDataService _externalDataService;
        private CreatePackageSettingsDto _createPackageSettingsDto = new();
        private SemaphoreSlim _createPackageSlim = new(1);
        private DateTime _lastReadTime = DateTime.Now;
        private DateTime _lastNoReadTime = DateTime.Now;
        private WeightSettingsDto _weightSettingsDto = new();
        private static bool _isWindowsClose;
        private List<ICamera> _cameras = new();

        public ZhuoYanScmBackgroundService(IDeviceService deviceService,
            IImageStorageService imageStorageService,
            IConfigRepository configRepository,
            ISortingService sortingService,
            IExternalDataService externalDataService) {
            _deviceService = deviceService;
            _imageStorageService = imageStorageService;
            _configRepository = configRepository;
            _sortingService = sortingService;
            _externalDataService = externalDataService;
            //相机
            _deviceService.CameraInitialized += delegate (object? sender, List<ICamera> list) {
                _cameras = list;
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
            //空包裹
            _deviceService.NotBarcodeHitEvent += async delegate (object? sender, BarcodeReadEventArgs args) {
                await Task.Yield();
                if (!_createPackageSettingsDto.IsUseNoRead) {
                    args.Image?.Dispose();
                    return;
                }
                try {
                    await _createPackageSlim.WaitAsync();
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
                                PackageInfo = packageInfo
                            });
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
            //下位机(移除包裹)
            _sortingService.RemovePackageEvent += async delegate (object? sender, PackageInstructionEventArgs args) {
                /*//测试,记得删
                return;*/

                try {
                    await _createPackageSlim.WaitAsync();
                    //测试间隔200,记得删掉
                    await Task.Delay(200);
                    var tryParse = int.TryParse(args.Keyword, out var num);
                    if (tryParse) {
                        var packageInfo = PackageInfoManager.GetPackage(f => f.Value != null && f.Value.Guid.Equals(num));

                        if (packageInfo is not null) {
                            EventAggregator.Instance.Publish(new InstructionReceived() {
                                Timestamp = new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds(),
                                IsCreatedByLowerMachine = true,
                                SortingCode = num.ToString(),
                                InstructionInfos = new List<InstructionInfoModel>()
                                {
                                    new()
                                    {
                                        InstructionContent = args.Instruction,
                                        InstructionGeneratedTime = DateTime.Now,
                                        InstructionType = InstructionType.SignalCallback
                                    },
                                },
                                ConnectionName = args.ConnectionName,
                            });
                            /*EventAggregator.Instance.Publish(new CallBackPackageInfo {
                                CallBackTime = DateTime.Now,
                                PackageCreateTime = keyValuePair.Value.CreateTime,
                                PackageInfo = keyValuePair.Value,
                                InstructionContent = args.Instruction,
                            });*/
                            if (_createPackageSettingsDto.PackageRemoveMethods == PackageRemoveMethodsEnum.LowerMachineRemoval) {
                                PackageInfoManager.RemovePackage(packageInfo.CreateTime, "下位机移除");
                            }
                        }
                        else {
                            LogManager.GetCurrentClassLogger().Error($"序号匹配包裹失败,序号:{num},原文:{args.Keyword}");
                        }
                    }
                    else {
                        LogManager.GetCurrentClassLogger().Error($"关键字节转数字失败:{args.Keyword}");
                    }
                }
                finally {
                    _createPackageSlim.Release();
                }

                //其他协议
            };
            //下位机(包裹异常)
            _sortingService.PackageException += async (sender, args) => {
                try {
                    await Task.Delay(500);
                    await _createPackageSlim.WaitAsync();
                    var tryParse = int.TryParse(args.Keyword, out var num);
                    if (tryParse) {
                        var packageInfo = PackageInfoManager.GetPackage(f => f.Value != null && f.Value.Guid.Equals(num));
                        if (packageInfo is not null) {
                            EventAggregator.Instance.Publish(new InstructionReceived() {
                                Timestamp = new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds(),
                                IsCreatedByLowerMachine = true,
                                SortingCode = num.ToString(),
                                InstructionInfos = new List<InstructionInfoModel>()
                                {
                                    new()
                                    {
                                        InstructionContent = args.Instruction,
                                        InstructionGeneratedTime = DateTime.Now,
                                        InstructionType = InstructionType.PackageException
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
                            packageInfo.VolumeInfo = new VolumeInfoModel() {
                                CreateTime = DateTime.Now,
                                FormattedHeight = args.Height,
                                FormattedLength = args.Length,
                                FormattedVolume = args.Volume,
                                FormattedWidth = args.Width,
                                SourceType = SourceType.Input,
                                OriginalText = args.SourceContent
                            };
                            EventAggregator.Instance.Publish(new TriggerPositionEvent {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.ExternalDataInputAfter,
                                PackageInfo = packageInfo
                            });
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
            //称重
            _deviceService.StableWeight += async delegate (object? sender, StableWeightEventArgs args) {
                try {
                    await _createPackageSlim.WaitAsync();
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            PackageInfoManager.GetPackage(f => f.Value is { IsCompleted: false, WeightInfo: null }) :
                            PackageInfoManager.GetLastPackage(f => f.Value is { IsCompleted: false, WeightInfo: null });
                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.StableWeight) ==
                        PackageCreationMethodsEnum.StableWeight && packageInfo is null) {
                        packageInfo = new PackageInfo() {
                            Guid = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                            WeightInfo = new WeightInfoModel {
                                CreateTime = DateTime.Now,
                                FormattedWeight = args.Weight,
                                SourceType = SourceType.SerialPort,
                                WeighingMode = WeighingMode.Static
                            }
                        };
                        //-----
                        EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                            IsSuccess = true,
                            TriggerPosition = TriggerPositionEnum.PackageTrigger,
                            PackageInfo = packageInfo
                        });
                    }
                    else {
                        if (packageInfo is not null) {
                            packageInfo.WeightInfo = new WeightInfoModel {
                                CreateTime = DateTime.Now,
                                FormattedWeight = args.Weight,
                                SourceType = SourceType.SerialPort,
                                WeighingMode = WeighingMode.Static
                            };
                            EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.WeightSetValueAfter,
                                PackageInfo = packageInfo
                            });
                        }
                    }
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            //体积
            _deviceService.VolumeCaptured += async delegate (object? sender, VolumeCapturedEventArgs args) {
                //填充长宽高
                try {
                    await _createPackageSlim.WaitAsync();
                    var packageInfo =
                   _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                       PackageInfoManager.GetPackage(f => f.Value is { VolumeInfo: null }) :
                       PackageInfoManager.GetLastPackage(f => f.Value is { VolumeInfo: null });
                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.VolumeInput) ==
                        PackageCreationMethodsEnum.VolumeInput && packageInfo is null) {
                        packageInfo = new PackageInfo() {
                            Guid = new DateTimeOffset(args.Timestamp).ToUnixTimeMilliseconds(),
                            VolumeInfo = new VolumeInfoModel() {
                                CreateTime = args.Timestamp,
                                FormattedHeight = args.Height,
                                FormattedWidth = args.Width,
                                FormattedLength = args.Length,
                                FormattedVolume = args.Volume,
                                SourceType = SourceType.Camera,
                            },
                        };

                        EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                            IsSuccess = true,
                            TriggerPosition = TriggerPositionEnum.PackageTrigger,
                            PackageInfo = packageInfo
                        });
                    }
                    else {
                        //判断体积创建包裹

                        //增加体积单位转换
                        if (packageInfo is not null) {
                            //VolumeInfo需要返回是否动态
                            //如果是动态体积就需要满足条码和重量才能使用
                            packageInfo.VolumeInfo = new VolumeInfoModel() {
                                CreateTime = args.Timestamp,
                                FormattedHeight = args.Height - packageInfo.LengthToDeduct,
                                FormattedWidth = args.Width - packageInfo.WidthToDeduct,
                                FormattedLength = args.Length - packageInfo.LengthToDeduct,
                                FormattedVolume = args.Volume - packageInfo.VolumeToDeduct,
                                SourceType = SourceType.Camera,
                            };
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

                        case "WeightSettings":
                            _weightSettingsDto = await _configRepository.FirstOrDefaultEntity<WeightSettingsDto>(model.SettingsName) ??
                                                 new WeightSettingsDto();

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
                    //重量超时
                    if (_weightSettingsDto.AdditionalWeight is { IsUseMergedWeightTimeout: true, MergedWeightTimeout: > 0 }) {
                        packageRemoveTimers.Add(new PackageAssignmentTimer() {
                            AssignmentTimeSpan = TimeSpan.FromMilliseconds(_weightSettingsDto.AdditionalWeight.MergedWeightTimeout),
                            Predicate = w => w.Value.WeightInfo == null,
                            AssignmentCallback = a => {
                                a.WeightInfo = new WeightInfoModel();
                                return false;
                            },
                        });
                    }
                    //体积超时
                    PackageInfoManager.AddPackage(packageInfo, packageRemoveTimers);
                    //触发创建包裹事件
                    EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                        IsSuccess = true,
                        TriggerPosition = TriggerPositionEnum.CreateTimePackageAfter,
                        PackageInfo = packageInfo
                    });
                }
                else if (item is {
                    PackageInfo: { VolumeInfo: null } createInfo, TriggerPosition: TriggerPositionEnum.CreateTimePackageAfter
                }) {
                    if (_cameras.All(camera => camera.BindingType != CameraBindingType.VolumeCamera)) {
                        createInfo.VolumeInfo = new VolumeInfoModel();
                    }
                }
                else if (item is { PackageInfo: { BarCodeInfo: not null, WeightInfo: not null, VolumeInfo: not null } info, TriggerPosition: TriggerPositionEnum.BarCodeSetValueAfter or TriggerPositionEnum.WeightSetValueAfter or TriggerPositionEnum.ExternalDataInputAfter or TriggerPositionEnum.VolumeSetValueAfter }) {
                    PackageInfoManager.CompletedPackage(f => f.Key.Equals(info.CreateTime));
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
                _weightSettingsDto = await _configRepository.FirstOrDefaultEntity<WeightSettingsDto>("WeightSettings", stoppingToken) ?? new WeightSettingsDto();
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
                                    PackageTimestamped = codeInfo.Timestamp,
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
    }
}