using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Models.LocalConf.CloudConfig;
using JayTom.Dws.Models.LocalConf.IpcNvrConfig;
using JayTom.Dws.Models.LocalConf.CameraConfig;
using JayTom.Dws.Application.CameraConfigurations;
using NLog;
using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Camera;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Legacy.Contracts.Model;
using JayTom.Dws.Legacy.Contracts.Packages;
using JayTom.Dws.Models.LocalConf;
using JayTom.Dws.Models.LocalLog;
using System.Collections.Generic;
using JayTom.Dws.Legacy.Contracts.Dto.ApiDto;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Application.Workflows;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Legacy.Contracts.DownstreamProtocols;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Legacy.Contracts.Services.ImageService;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.Service.ProcessingServices
{

    /// <summary>
    /// 云山分拣机项目
    /// </summary>
    public class YunShanPackageBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
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
        private readonly ICameraConfigurationCatalog<BarcodeScannerCameraConfigInfoModel> _barcodeScannerCameraConfigRepository;
        private readonly IExternalDataService _externalDataService;
        private volatile CreatePackageSettingsDto _createPackageSettingsDto = new();
        /// <summary>仅由包裹关键线程访问的有序多相机组帧，允许慢相机补入对应的较早机械包裹。</summary>
        private readonly List<MultiCameraFrameGroup> _multiCameraFrameGroups = [];
        /// <summary>使用单调时钟精确唤醒多相机融合期限，避免100ms轮询造成尾延迟。</summary>
        private readonly MonotonicDeadlineScheduler _multiCameraDeadlineScheduler;
        /// <summary>
        /// 复用用户配置的条码正则，避免在相机热回调中重复编译。
        /// </summary>
        private readonly ConcurrentDictionary<string, Regex> _regexCache = new(StringComparer.Ordinal);
        /// <summary>仅跟踪已完成且等待安全清理的包裹，避免每 100ms 扫描全部活动会话。</summary>
        private readonly ConcurrentQueue<PackageInfo> _packagesAwaitingCleanup = new();
        /// <summary>维护待清理队列的 O(1) 近似长度，避免热循环枚举分段队列。</summary>
        private int _packagesAwaitingCleanupCount;
        private volatile BarcodeFilterSettingsDto _barcodeFilterSettingsDto = new();
        /// <summary>串行处理创建、扫码、空读、回调和赋值的关键队列。</summary>
        private readonly NonBlockingOrderedDispatcher<Action> _packageEventDispatcher;
        /// <summary>隔离日志、UI、持久化、API和输出订阅者的通知队列。</summary>
        private readonly NonBlockingOrderedDispatcher<Action> _packageNotificationDispatcher;
        /// <summary>包裹完成后优先发布给格口 API，不与日志、UI和图片通知排同一队列。</summary>
        private readonly NonBlockingOrderedDispatcher<PackageInfo> _packageCompletionDispatcher;
        /// <summary>包裹完成后立即转交条码图片，消除每 100ms 仅处理一张图片的轮询吞吐上限。</summary>
        private readonly NonBlockingOrderedDispatcher<PackageInfo> _imageHandoffDispatcher;
        private DateTime _lastNoReadTime = DateTime.MinValue;
        private IReadOnlyDictionary<string, ICamera> _camerasBySerialNumber =
            new Dictionary<string, ICamera>(StringComparer.Ordinal);
        /// <summary>缓存当前扫描相机数量，避免每个条码事件都遍历设备集合。</summary>
        private int _scannerCameraCount;
        /// <summary>关键队列中最后一次成功创建包裹的时间键。</summary>
        private long _lastAcceptedPackageCreateTicks = DateTime.MinValue.Ticks;
        private int _isWindowsClose;
        private volatile WeightSettingsDto _weightSettingsDto = new();
        private volatile WdtWmsApiDto _wdtWmsApiDto = new();
        /// <summary>最近一次输出关键队列性能水位的单调时钟时间戳。</summary>
        private long _lastPerformanceReportTimestamp = Stopwatch.GetTimestamp();

        public YunShanPackageBackgroundService(IPackageSessionStore packageSessionStore,
            IDeviceService deviceService,
            IImageStorageService imageStorageService,
            ISettingsStore settingsStore,
            ISortingService sortingService,
            ICameraConfigurationCatalog<BarcodeScannerCameraConfigInfoModel> barcodeScannerCameraConfigRepository,
            IExternalDataService externalDataService,
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            _packageSessionStore = packageSessionStore;
            _deviceService = deviceService;
            _imageStorageService = imageStorageService;
            _settingsStore = settingsStore;
            _sortingService = sortingService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _externalDataService = externalDataService;
            _multiCameraDeadlineScheduler = new MonotonicDeadlineScheduler(
                "MultiCameraDeadlines",
                ThreadPriority.AboveNormal);
            _packageEventDispatcher = new NonBlockingOrderedDispatcher<Action>(
                static work =>
                {
                    work();
                },
                (_, exception) => QueueError("执行关键包裹事件失败", exception),
                "PackageCritical",
                ThreadPriority.AboveNormal);
            _packageCompletionDispatcher = new NonBlockingOrderedDispatcher<PackageInfo>(
                package => _eventBus.PublishPackage(package),
                (_, exception) => QueueError("发布包裹完成事件失败", exception),
                "PackageCompletion",
                ThreadPriority.AboveNormal);
            _imageHandoffDispatcher = new NonBlockingOrderedDispatcher<PackageInfo>(
                ProcessCompletedPackageImage,
                (_, exception) => QueueError("转交包裹条码图片失败", exception),
                "PackageImageHandoff");
            _packageNotificationDispatcher = new NonBlockingOrderedDispatcher<Action>(
                static notification =>
                {
                    notification();
                },
                (_, exception) => QueueError("发布包裹通知失败", exception),
                "PackageNotification");
            _deviceService.CameraInitialized += (_, cameras) =>
            {
                var cameraLookup = new Dictionary<string, ICamera>(
                    cameras.Count,
                    StringComparer.Ordinal);
                var scannerCameraCount = 0;
                for (var index = 0; index < cameras.Count; index++)
                {
                    var camera = cameras[index];
                    var serialNumber = camera.Info?.SerialNumber;
                    if (!string.IsNullOrWhiteSpace(serialNumber))
                    {
                        cameraLookup[serialNumber] = camera;
                    }
                    if (camera.BindingType == CameraBindingType.ScannerCamera)
                    {
                        scannerCameraCount++;
                    }
                }
                Volatile.Write(ref _camerasBySerialNumber, cameraLookup);
                Volatile.Write(ref _scannerCameraCount, scannerCameraCount);
            };

            //条码返回
            _deviceService.BarcodeScanned += delegate (object? sender, BarcodeReadEventArgs args)
            {
                if (!TryQueuePackageEvent(() =>
                {
                    //验证多条码
                    try
                    {
                    if (Volatile.Read(ref _scannerCameraCount) > 1)
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

                        AcceptMultiCameraFrame(args.CameraSerialNumber, barCodeFrameInfo);
                    }
                    else
                    {
                        //多条码判断------

                        var info = _packageSessionStore.GetPackage(f => f.Value is { BarCodeInfo: not null } &&
                                                                      f.Value.BarCodeInfo.ScanTime.Equals(
                                                                          args.ScanTime) &&
                                                                      f.Value.BarCodeInfo.SerialNumber.Equals(
                                                                          args.CameraSerialNumber));
                        if (info is { BarCodeInfo: not null } && _createPackageSettingsDto.BarcodeHandlingMethod != BarcodeHandlingMethodEnum.UseMultipleBarcodes)
                        {
                            if (_createPackageSettingsDto.BarcodeHandlingMethod == BarcodeHandlingMethodEnum.MergeBarcodes)
                            {
                                lock (info.SyncRoot)
                                {
                                    info.BarCodeInfo.Barcode +=
                                        $"{_barcodeFilterSettingsDto.MultiBarcodeDelimiter}{args.Barcode}";
                                }
                            }
                            args.Image?.Dispose();
                            return;
                        }

                        JayTom.Dws.Abstractions.Imaging.ImageHandle? replacedImage = null;
                        var hadPendingPackage = _packageSessionStore.HasUnassignedPackage();
                        var packageInfo = TryBindBarcode(args.ScanTime, package =>
                        {
                            replacedImage = package.Image;
                            package.BarCodeInfo = new BarCodeInfoModel()
                            {
                                Barcode = args.Barcode,
                                SerialNumber = args.CameraSerialNumber,
                                DisplayIdentifier = args.CameraSerialNumber,
                                ScanTime = args.ScanTime,
                                Source = SourceType.Camera,
                                BindTime = DateTime.Now
                            };
                            package.Image = JayTom.Dws.Abstractions.Imaging.ImageHandle.TakeOwnershipIfPresent(args.Image);
                        });

                        if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.ScanBarcodeCamera)
                            == PackageCreationMethodsEnum.ScanBarcodeCamera && packageInfo is null &&
                            !hadPendingPackage)
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
                            ProcessPackageTrigger(packageInfo);
                        }
                        else
                        {
                            if (packageInfo is not null)
                            {
                                if (!ReferenceEquals(replacedImage, args.Image))
                                {
                                    replacedImage?.Dispose();
                                }
                                ProcessPackageValueChanged(
                                    packageInfo,
                                    TriggerPositionEnum.BarCodeSetValueAfter);
                            }
                            else
                            {
                                args.Image?.Dispose();
                                QueueBarcodeAssignmentRejected("camera", args.ScanTime);
                            }
                        }
                    }
                    }
                    catch (Exception exception)
                    {
                        QueueError("处理相机条码事件失败", exception);
                    }
                }, "相机条码"))
                {
                    args.Image?.Dispose();
                }
            };
            //空包裹
            _deviceService.BarcodeMissed += delegate (object? sender, BarcodeReadEventArgs args)
            {
                if (!TryQueuePackageEvent(() =>
                {
                    if (!_createPackageSettingsDto.IsUseNoRead)
                    {
                        args.Image?.Dispose();
                        return;
                    }
                    try
                    {
                    if (Volatile.Read(ref _scannerCameraCount) > 1)
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
                        AcceptMultiCameraFrame(args.CameraSerialNumber, barCodeFrameInfo);
                    }
                    else
                    {
                        if (_createPackageSettingsDto.IsUseNoReadFilter)
                        {
                            if (args.ScanTime.Subtract(_lastNoReadTime).TotalMilliseconds <
                                _createPackageSettingsDto.FilterInterval)
                            {
                                args.Image?.Dispose();
                                return;
                            }
                            else
                            {
                                _lastNoReadTime = args.ScanTime;
                            }
                        }
                        JayTom.Dws.Abstractions.Imaging.ImageHandle? replacedImage = null;
                        var hadPendingPackage = _packageSessionStore.HasUnassignedPackage();
                        var packageInfo = TryBindBarcode(args.ScanTime, package =>
                        {
                            replacedImage = package.Image;
                            package.BarCodeInfo = new BarCodeInfoModel()
                            {
                                Barcode = args.Barcode,
                                SerialNumber = args.CameraSerialNumber,
                                DisplayIdentifier = args.CameraSerialNumber,
                                ScanTime = args.ScanTime,
                                Source = SourceType.Camera,
                                BindTime = DateTime.Now
                            };
                            package.Image = JayTom.Dws.Abstractions.Imaging.ImageHandle.TakeOwnershipIfPresent(args.Image);
                        });
                        if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.ScanBarcodeCamera) ==
                            PackageCreationMethodsEnum.ScanBarcodeCamera && packageInfo is null &&
                            !hadPendingPackage)
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
                            ProcessPackageTrigger(packageInfo);
                        }
                        else
                        {
                            if (packageInfo is not null)
                            {
                                if (!ReferenceEquals(replacedImage, args.Image))
                                {
                                    replacedImage?.Dispose();
                                }
                                ProcessPackageValueChanged(
                                    packageInfo,
                                    TriggerPositionEnum.BarCodeSetValueAfter);
                            }
                            else
                            {
                                args.Image?.Dispose();
                                QueueBarcodeAssignmentRejected("camera-noread", args.ScanTime);
                            }
                        }
                    }
                    }
                    catch (Exception exception)
                    {
                        QueueError("处理相机空读事件失败", exception);
                    }
                }, "相机空读"))
                {
                    args.Image?.Dispose();
                }
            };
            //下位机创建包裹
            _sortingService.CreatePackageEvent += delegate (object? sender, PackageInstructionEventArgs args)
            {
                TryQueuePackageEvent(() =>
                {
                    try
                    {
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

                            if (ProcessPackageTrigger(packageInfo))
                            {
                                QueueInstructionNotification(new InstructionReceived
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
                                }, "创建指令");
                            }
                        }
                    }
                    }
                    catch (Exception exception)
                    {
                        QueueError("处理下位机创建包裹事件失败", exception);
                    }
                }, "下位机创建包裹");
            };
            //下位机(移除包裹)
            _sortingService.RemovePackageEvent += delegate (object? sender, PackageInstructionEventArgs args)
            {
                /*//测试,记得删
                return;*/

                TryQueuePackageEvent(() =>
                {
                    try
                    {
                    var tryParse = int.TryParse(args.Keyword, out var num);
                    if (tryParse)
                    {
                        var packageInfo = _packageSessionStore.GetPackageById(num);

                        if (packageInfo is not null)
                        {
                            QueueInstructionNotification(new InstructionReceived
                            {
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
                            }, "分拣完成指令");
                            /*_eventBus.Publish(new CallBackPackageInfo {
                                CallBackTime = DateTime.Now,
                                PackageCreateTime = keyValuePair.Value.CreateTime,
                                PackageInfo = keyValuePair.Value,
                                InstructionContent = args.Instruction,
                            });*/
                            if (_createPackageSettingsDto.PackageRemoveMethods == PackageRemoveMethodsEnum.LowerMachineRemoval)
                            {
                                _packageSessionStore.RemovePackage(packageInfo.CreateTime, "下位机移除");
                            }
                        }
                        else
                        {
                            QueueError($"序号匹配包裹失败,序号:{num},原文:{args.Keyword}");
                        }
                    }
                    else
                    {
                        QueueError($"关键字节转数字失败:{args.Keyword}");
                    }
                    }
                    catch (Exception exception)
                    {
                        QueueError("处理下位机移除包裹事件失败", exception);
                    }
                }, "下位机移除包裹");

                //其他协议
            };
            //下位机(包裹异常)
            _sortingService.PackageException += (sender, args) =>
            {
                TryQueuePackageEvent(() =>
                {
                    try
                    {
                    var tryParse = int.TryParse(args.Keyword, out var num);
                    if (tryParse)
                    {
                        var packageInfo = _packageSessionStore.GetPackageById(num);
                        if (packageInfo is not null)
                        {
                            QueueInstructionNotification(new InstructionReceived
                            {
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
                            }, "包裹异常指令");
                        }
                    }
                    }
                    catch (Exception exception)
                    {
                        QueueError("处理包裹异常事件失败", exception);
                    }
                }, "下位机包裹异常");
            };
            //外部全量数据
            _externalDataService.ContentInputReceived += (sender, args) =>
            {
                TryQueuePackageEvent(() =>
                {
                    try
                    {
                    var barCode = "NoRead";
                    var replace = _wdtWmsApiDto.AnyStartCodes.Replace(";", "|");

                    //分割条码包装码、正则验证包装码
                    var strings = args.Barcode.Split(',');
                    //取出包装码
                    var boxBarCode = strings.FirstOrDefault(f =>
                        !string.IsNullOrEmpty(replace) &&
                        IsRegexMatch(f, $"(^(?={replace}).*)")) ?? string.Empty;

                    /*if (!strings.All(f => !string.IsNullOrEmpty(replace) && Regex.IsMatch(f, $"(^(?={replace}).*)"))) {
                        barCode = strings.FirstOrDefault(f => !f.Equals(boxBarCode)) ?? "NoRead";
                    }*/
                    barCode = strings.FirstOrDefault(f => !f.Equals(boxBarCode)) ?? "NoRead";
                    /*barCode = strings.FirstOrDefault(f =>
                        !string.IsNullOrEmpty(replace) && !Regex.IsMatch(f, $"(^(?={replace}).*)")) ?? "NoRead";*/
                    var receivedAt = args.ReceiveTime == default ? DateTime.Now : args.ReceiveTime;
                    var timestamp = new DateTimeOffset(receivedAt).ToUnixTimeMilliseconds();
                    // 先完成可失败的转换，避免在包裹锁内停留或产生部分赋值。
                    var formattedHeight = Convert.ToDecimal(args.Height);
                    var formattedLength = Convert.ToDecimal(args.Length);
                    var formattedVolume = Convert.ToDecimal(args.Volume);
                    var formattedWidth = Convert.ToDecimal(args.Width);
                    // 只要当前仍有待赋值包裹，绑定失败就必须拒绝本次输入，不能把同一机械包裹
                    // 再创建成下一条记录，否则后续所有条码都会整体错位。
                    var hadPendingPackage = _packageSessionStore.HasUnassignedPackage();
                    var packageInfo = TryBindBarcode(receivedAt, package =>
                    {
                        package.BarCodeInfo = new BarCodeInfoModel
                        {
                            Barcode = barCode,
                            ScanTime = receivedAt,
                            BindTime = DateTime.Now,
                            Source = SourceType.Input,
                        };
                        package.WeightInfo = new WeightInfoModel
                        {
                            CreateTime = receivedAt,
                            FormattedWeight = args.Weight,
                            SourceType = SourceType.Input,
                            OriginalText = args.SourceContent
                        };
                        package.VolumeInfo = new VolumeInfoModel
                        {
                            CreateTime = receivedAt,
                            FormattedHeight = formattedHeight,
                            FormattedLength = formattedLength,
                            FormattedVolume = formattedVolume,
                            FormattedWidth = formattedWidth,
                            SourceType = SourceType.Input,
                            OriginalText = args.SourceContent
                        };
                        package.Other = boxBarCode;
                    });
                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.TcpInput) ==
                        PackageCreationMethodsEnum.TcpInput && packageInfo is null &&
                        !hadPendingPackage)
                    {
                        packageInfo = new PackageInfo
                        {
                            Guid = timestamp,
                            BarCodeInfo = new BarCodeInfoModel
                            {
                                Barcode = barCode,
                                ScanTime = receivedAt,
                                BindTime = DateTime.Now,
                                Source = SourceType.Input,
                            },
                            WeightInfo = new WeightInfoModel
                            {
                                CreateTime = receivedAt,
                                FormattedWeight = args.Weight,
                                SourceType = SourceType.Input,
                                OriginalText = args.SourceContent
                            },
                            VolumeInfo = new VolumeInfoModel
                            {
                                CreateTime = receivedAt,
                                FormattedHeight = formattedHeight,
                                FormattedLength = formattedLength,
                                FormattedVolume = formattedVolume,
                                FormattedWidth = formattedWidth,
                                SourceType = SourceType.Input,
                                OriginalText = args.SourceContent
                            },
                            Other = boxBarCode,
                            CreateTime = receivedAt,
                            IsCreatedByLowerMachine = false,
                            IsImageSaveRequested = true,
                        };

                        ProcessPackageTrigger(packageInfo);
                    }
                    else
                    {
                        if (packageInfo is not null)
                        {
                            ProcessPackageValueChanged(
                                packageInfo,
                                TriggerPositionEnum.ExternalDataInputAfter);
                        }
                        else
                        {
                            QueueBarcodeAssignmentRejected("external-tcp", receivedAt);
                        }
                    }
                    }
                    catch (Exception exception)
                    {
                        QueueError("处理外部数据输入事件失败", exception);
                    }
                }, "外部数据输入");
            };
            //称重
            _deviceService.StableWeight += delegate (object? sender, StableWeightEventArgs args)
            {
                if (args.Weight <= 0)
                {
                    return;
                }

                TryQueuePackageEvent(() =>
                {
                    try
                    {
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            _packageSessionStore.GetPackage(f => f.Value is { IsCompleted: false, WeightInfo: null }) :
                            _packageSessionStore.GetLastPackage(f => f.Value is { IsCompleted: false, WeightInfo: null });
                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.StableWeight) ==
                        PackageCreationMethodsEnum.StableWeight && packageInfo is null)
                    {
                        packageInfo = new PackageInfo()
                        {
                            Guid = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                            WeightInfo = new WeightInfoModel
                            {
                                CreateTime = DateTime.Now,
                                FormattedWeight = args.Weight,
                                SourceType = SourceType.SerialPort,
                                WeighingMode = WeighingMode.Static
                            }
                        };
                        //-----
                        ProcessPackageTrigger(packageInfo);
                    }
                    else
                    {
                        if (packageInfo is not null)
                        {
                            lock (packageInfo.SyncRoot)
                            {
                                packageInfo.WeightInfo = new WeightInfoModel
                                {
                                    CreateTime = DateTime.Now,
                                    FormattedWeight = args.Weight,
                                    SourceType = SourceType.SerialPort,
                                    WeighingMode = WeighingMode.Static
                                };
                            }
                            ProcessPackageValueChanged(
                                packageInfo,
                                TriggerPositionEnum.WeightSetValueAfter);
                        }
                    }
                    }
                    catch (Exception exception)
                    {
                        QueueError("处理称重事件失败", exception);
                    }
                }, "稳定重量");
            };
            //配置更改
            _eventBus.SubscribeAsync<SettingsChangedEvent>(async item =>
            {
                if (item is { } model)
                {
                    switch (model.SettingsName)
                    {
                        case "CreatePackageSettings":
                            {
                                var settings = await _settingsStore.GetAsync<CreatePackageSettingsDto>(model.SettingsName) ??
                                               new CreatePackageSettingsDto();
                                ActivateCreatePackageSettings(settings, "运行期刷新");
                                break;
                            }
                        case "WeightSettings":
                            {
                                var settings = await _settingsStore.GetAsync<WeightSettingsDto>(model.SettingsName) ??
                                               new WeightSettingsDto();
                                await SwapSettingsAsync(() => _weightSettingsDto = settings);
                                break;
                            }
                        case "BarcodeFilterSettings":
                            {
                                var settings = await _settingsStore.GetAsync<BarcodeFilterSettingsDto>(model.SettingsName) ??
                                               new BarcodeFilterSettingsDto();
                                await SwapSettingsAsync(() => _barcodeFilterSettingsDto = settings);
                                break;
                            }
                        case "WdtWmsApiParameters":
                            {
                                var settings = await _settingsStore.GetAsync<WdtWmsApiDto>(model.SettingsName) ??
                                               new WdtWmsApiDto();
                                await SwapSettingsAsync(() => _wdtWmsApiDto = settings);
                                break;
                            }
                    }
                    //其他设置
                }
            });
            //程序停止
            _eventBus.Subscribe<ApplicationStatusChanged>(item =>
            {
                if (item is { } info)
                {
                    if (info.Status == ApplicationStatus.Stop &&
                        _createPackageSettingsDto.ClearPackageQueueOnStop)
                    {
                        _packageSessionStore.ClearAllPackages();
                    }
                }
            });

            //移除包裹事件
            _packageSessionStore.PackageRemoved += (sender, args) =>
            {
                QueuePackageNotification(() =>
                    _eventBus.Publish(new TriggerPositionEvent()
                    {
                        IsSuccess = true,
                        TriggerPosition = TriggerPositionEnum.RemovePackageAfter,
                        PackageInfo = args.RemovedPackage,
                        Description = args.Description
                    }), "移除包裹");
            };
            _packageSessionStore.PackageCompleted += (sender, args) =>
            {
                //执行输出
                var completedPackage = args.CompletedPackage;
                if (completedPackage?.BarCodeInfo is not null &&
                    completedPackage.WeightInfo is not null &&
                    completedPackage.VolumeInfo is not null)
                {
                    if (_sortingService.RequiresSortingInstruction)
                    {
                        completedPackage.MarkSortingInstructionExpected();
                    }
                    if (_createPackageSettingsDto.PackageRemoveMethods ==
                        PackageRemoveMethodsEnum.FillInformation)
                    {
                        EnqueuePackageForCleanup(completedPackage);
                    }
                    if (!_imageHandoffDispatcher.TryEnqueue(completedPackage))
                    {
                        QueueError("包裹图片转交队列已停止，图片未入队");
                    }
                    if (!_packageCompletionDispatcher.TryEnqueue(completedPackage))
                    {
                        QueueError("包裹完成队列已停止，完成事件未入队");
                    }
                    QueuePackageNotification(
                        () => _eventBus.Publish(completedPackage),
                        "包裹完成普通通知");
                }
            };
            _eventBus.Subscribe<WindowsAction>(item =>
            {
                if (item is { Type: WindowsActionType.Close })
                {
                    Interlocked.Exchange(ref _isWindowsClose, 1);
                }
            });
        }

        /// <summary>
        /// 将错误发布到内存日志队列，避免设备事件热路径直接写日志文件。
        /// </summary>
        /// <param name="message">错误说明。</param>
        /// <param name="exception">可选的异常信息。</param>
        private void QueueError(string message, Exception? exception = null)
        {
            _eventBus.Publish(new AppLogInfoModel
            {
                CreateTime = DateTime.Now,
                Type = LogType.Exception,
                Message = exception is null ? message : $"{message}:{exception}"
            });
        }

        /// <summary>按设备观测时间在配置窗口内原子绑定条码。</summary>
        private PackageInfo? TryBindBarcode(DateTime observedAt, Action<PackageInfo> assignment)
        {
            var settings = _createPackageSettingsDto;
            var emptyPackageExpiryMilliseconds =
                settings is { IsUseEmptyPackageExpiry: true, EmptyPackageExpiryTime: > 0 }
                    ? settings.EmptyPackageExpiryTime
                    : (int?)null;
            return _packageSessionStore.TryBindBarcode(
                observedAt,
                settings.BarcodeQueueOrder,
                settings.IsUseBarcodeAssignmentInterval,
                settings.MinimumAssignmentTime,
                settings.MaximumAssignmentTime,
                emptyPackageExpiryMilliseconds,
                DateTime.Now,
                assignment);
        }

        /// <summary>接收同一机械包裹的单相机帧，并在相机齐备或冲突时立即完成融合。</summary>
        private void AcceptMultiCameraFrame(
            string cameraSerialNumber,
            BarCodeFrameInfo frame)
        {
            var frameScanTime = frame.BarCodeInfo?.ScanTime ?? DateTime.Now;
            var mergeTimeoutTicks = Math.Max(
                1L,
                (long)_barcodeFilterSettingsDto.MergeTimeout) *
                TimeSpan.TicksPerMillisecond;
            var configuredCreationInterval =
                _createPackageSettingsDto.PackageCreationInterval;
            // 创建间隔为 0 表示关闭该防抖，不能因此把多相机关联窗口收窄到 0.5ms。
            // 启用防抖时最多采用半个机械周期，避免把相邻包裹的相机帧合并在一起。
            var maximumCorrelationTicks = configuredCreationInterval > 0
                ? Math.Min(
                    mergeTimeoutTicks,
                    Math.Max(1L, (long)configuredCreationInterval) *
                    TimeSpan.TicksPerMillisecond / 2L)
                : mergeTimeoutTicks;
            MultiCameraFrameGroup? targetGroup = null;
            var nearestDifference = long.MaxValue;
            for (var index = 0; index < _multiCameraFrameGroups.Count; index++)
            {
                var candidate = _multiCameraFrameGroups[index];
                if (candidate.Frames.ContainsKey(cameraSerialNumber))
                {
                    continue;
                }
                var timeDifference = Math.Abs(
                    frameScanTime.Ticks - candidate.AnchorScanTime.Ticks);
                if (timeDifference > maximumCorrelationTicks)
                {
                    continue;
                }
                if (frame.Frame > 0 && candidate.FrameNumber == frame.Frame)
                {
                    targetGroup = candidate;
                    break;
                }
                if (timeDifference < nearestDifference)
                {
                    nearestDifference = timeDifference;
                    targetGroup = candidate;
                }
            }
            if (targetGroup is null)
            {
                targetGroup = new MultiCameraFrameGroup
                {
                    AnchorScanTime = frameScanTime,
                    FrameNumber = frame.Frame,
                    ExpectedCameraCount = Math.Max(
                        1,
                        Volatile.Read(ref _scannerCameraCount))
                };
                _multiCameraFrameGroups.Add(targetGroup);
            }

            targetGroup.Frames.Add(cameraSerialNumber, frame);
            var barcode = frame.BarCodeInfo?.Barcode;
            if (!IsNoReadBarcode(barcode))
            {
                targetGroup.ValidBarcode ??= barcode;
                if (!string.Equals(
                        targetGroup.ValidBarcode,
                        barcode,
                        StringComparison.OrdinalIgnoreCase))
                {
                    targetGroup.HasBarcodeConflict = true;
                }
            }
            if (targetGroup.Frames.Count == 1)
            {
                var mergeTimeout = TimeSpan.FromMilliseconds(
                    Math.Max(1, _barcodeFilterSettingsDto.MergeTimeout));
                targetGroup.Deadline = _multiCameraDeadlineScheduler.Schedule(
                    mergeTimeout,
                    () => TryQueuePackageEvent(
                        () => CompleteMultiCameraGroup(targetGroup),
                        "多相机融合到期"));
            }
            if (targetGroup.Frames.Count >= targetGroup.ExpectedCameraCount ||
                targetGroup.HasBarcodeConflict)
            {
                CompleteMultiCameraGroup(targetGroup);
            }
        }

        /// <summary>选择单个有序相机组的唯一有效条码；冲突时拒绝整组，避免错配。</summary>
        private void CompleteMultiCameraGroup(MultiCameraFrameGroup frameGroup)
        {
            if (!_multiCameraFrameGroups.Remove(frameGroup))
            {
                return;
            }
            frameGroup.Deadline?.Dispose();
            frameGroup.Deadline = null;
            if (frameGroup.Frames.Count == 0)
            {
                return;
            }

            if (frameGroup.Frames.Count < frameGroup.ExpectedCameraCount)
            {
                DisposeMultiCameraGroup(frameGroup.Frames.Values, null);
                QueueError(
                    $"多相机帧未到齐，已拒绝赋值:{frameGroup.Frames.Count}/{frameGroup.ExpectedCameraCount}");
                return;
            }

            var selected = default(BarCodeFrameInfo);
            var fallback = default(BarCodeFrameInfo);
            var oldestScanTime = DateTime.MaxValue;
            foreach (var item in frameGroup.Frames.Values)
            {
                fallback ??= item;
                var itemScanTime = item.BarCodeInfo?.ScanTime;
                if (itemScanTime.HasValue && itemScanTime.Value < oldestScanTime)
                {
                    oldestScanTime = itemScanTime.Value;
                }
                if (!IsNoReadBarcode(item.BarCodeInfo?.Barcode) &&
                    (selected is null || selected.Image is null && item.Image is not null))
                {
                    selected = item;
                }
            }
            selected ??= fallback!;
            if (oldestScanTime == DateTime.MaxValue)
            {
                oldestScanTime = DateTime.Now;
            }
            if (frameGroup.HasBarcodeConflict)
            {
                DisposeMultiCameraGroup(frameGroup.Frames.Values, null);
                QueueError("多相机同组条码冲突，已拒绝赋值");
                return;
            }

            // 同一机械触发的多相机帧存在天然先后差；匹配包裹必须使用组内首帧时间，
            // 不能使用较慢相机的条码帧时间把本来位于窗口内的包裹推到 400ms 之外。
            var scanTime = oldestScanTime;
            var hadPendingPackage = _packageSessionStore.HasUnassignedPackage();
            JayTom.Dws.Abstractions.Imaging.ImageHandle? replacedImage = null;
            var package = TryBindBarcode(scanTime, target =>
            {
                replacedImage = target.Image;
                target.BarCodeInfo = selected.BarCodeInfo;
                target.Image = selected.Image;
            });
            if (package is null &&
                !hadPendingPackage &&
                (_createPackageSettingsDto.PackageCreationMethods &
                    PackageCreationMethodsEnum.ScanBarcodeCamera) ==
                PackageCreationMethodsEnum.ScanBarcodeCamera)
            {
                package = new PackageInfo
                {
                    Guid = selected.Timestamp,
                    BarCodeInfo = selected.BarCodeInfo,
                    Image = selected.Image
                };
                ProcessPackageTrigger(package);
            }
            else if (package is not null)
            {
                replacedImage?.Dispose();
                ProcessPackageValueChanged(package, TriggerPositionEnum.BarCodeSetValueAfter);
            }
            else
            {
                QueueBarcodeAssignmentRejected("multi-camera", scanTime);
            }
            DisposeMultiCameraGroup(
                frameGroup.Frames.Values,
                package is null ? null : selected);
        }

        /// <summary>判断条码是否表示空读或过滤占位值。</summary>
        private static bool IsNoReadBarcode(string? barcode) =>
            string.IsNullOrWhiteSpace(barcode) ||
            string.Equals(barcode, "noread", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(barcode, "filtered", StringComparison.OrdinalIgnoreCase);

        /// <summary>释放多相机组中未转交给包裹的图像所有权。</summary>
        private static void DisposeMultiCameraGroup(
            IEnumerable<BarCodeFrameInfo> group,
            BarCodeFrameInfo? retained)
        {
            foreach (var frame in group)
            {
                if (!ReferenceEquals(frame, retained))
                {
                    frame.Image?.Dispose();
                }
            }
            if (retained is null)
            {
                return;
            }
            retained.Image = null;
        }

        /// <summary>将超出赋值窗口的诊断信息移交到非关键通知队列。</summary>
        private void QueueBarcodeAssignmentRejected(string source, DateTime observedAt)
        {
            var settings = _createPackageSettingsDto;
            var minimumAssignmentTime = settings.MinimumAssignmentTime;
            var maximumAssignmentTime = settings.MaximumAssignmentTime;
            var intervalEnabled = settings.IsUseBarcodeAssignmentInterval;
            var pendingCount = _packageEventDispatcher.PendingCount;
            QueuePackageNotification(() =>
                _eventBus.Publish(new AppLogInfoModel
                {
                    CreateTime = DateTime.Now,
                    Type = LogType.Warning,
                    Message =
                        $"条码赋值被拒绝;来源={source},观测时间={observedAt:O}," +
                        $"窗口={minimumAssignmentTime}-{maximumAssignmentTime}ms," +
                        $"已启用={intervalEnabled},关键队列待处理数={pendingCount}"
                }), "条码赋值拒绝");
        }

        private bool ProcessPackageTrigger(PackageInfo packageInfo)
        {
            var createPackageSettings = _createPackageSettingsDto;
            var previousCreateTicks = _lastAcceptedPackageCreateTicks;
            var creationIntervalTicks = Math.Max(
                0L,
                (long)createPackageSettings.PackageCreationInterval *
                TimeSpan.TicksPerMillisecond);
            if (packageInfo.CreateTime.Ticks - previousCreateTicks < creationIntervalTicks)
            {
                packageInfo.TakeImage()?.Dispose();
                return false;
            }

            packageInfo.Timestamp = new DateTimeOffset(packageInfo.CreateTime)
                .ToUnixTimeMilliseconds();
            if (_weightSettingsDto.Mode == WeightMode.None)
            {
                packageInfo.WeightInfo ??= new WeightInfoModel();
            }
            packageInfo.VolumeInfo ??= new VolumeInfoModel();

            var packageRemoveTimers = new List<PackageTimer>();
            if (createPackageSettings is
                { IsUseEmptyPackageExpiry: true, EmptyPackageExpiryTime: > 0 })
            {
                packageRemoveTimers.Add(new PackageRemoveTimer
                {
                    Description = "空包裹过期",
                    Predicate = pair => pair.Value.BarCodeInfo == null,
                    RemovalTimeSpan = TimeSpan.FromMilliseconds(
                        createPackageSettings.EmptyPackageExpiryTime),
                    TryDispatch = removal =>
                        TryQueuePriorityPackageEvent(removal, "空包裹过期")
                });
            }
            if (createPackageSettings is
                { IsUsePackageExpiry: true, PackageExpiryTime: > 0 })
            {
                packageRemoveTimers.Add(new PackageRemoveTimer
                {
                    Description = "包裹超过生存周期",
                    RemovalTimeSpan = TimeSpan.FromMilliseconds(
                        createPackageSettings.PackageExpiryTime),
                    TryDispatch = removal =>
                        TryQueuePackageEvent(removal, "包裹超过生存周期")
                });
            }

            if (!_packageSessionStore.TryAddPackage(packageInfo, packageRemoveTimers))
            {
                packageInfo.TakeImage()?.Dispose();
                packageInfo.DisposeTimers();
                return false;
            }
            _lastAcceptedPackageCreateTicks = packageInfo.CreateTime.Ticks;

            QueueTriggerNotification(
                packageInfo,
                TriggerPositionEnum.PackageTrigger,
                "包裹触发");
            QueueTriggerNotification(
                packageInfo,
                TriggerPositionEnum.CreateTimePackageAfter,
                "创建包裹完成");
            // 扫码或外部全量数据直接创建时，数据可能在入会话前就已齐全；立即完成，避免等待不存在的后续赋值事件。
            _packageSessionStore.CompletePackage(packageInfo.CreateTime);
            return true;
        }

        /// <summary>在关键队列内完成包裹，并将赋值通知移出热路径。</summary>
        private void ProcessPackageValueChanged(
            PackageInfo packageInfo,
            TriggerPositionEnum triggerPosition)
        {
            if (packageInfo.BarCodeInfo is not null && packageInfo.WeightInfo is not null)
            {
                _packageSessionStore.CompletePackage(packageInfo.CreateTime);
            }
            QueueTriggerNotification(packageInfo, triggerPosition, "包裹赋值");
        }

        /// <summary>构造并排队一个不阻塞关键包裹状态的触发通知。</summary>
        private void QueueTriggerNotification(
            PackageInfo packageInfo,
            TriggerPositionEnum triggerPosition,
            string description)
        {
            QueuePackageNotification(() =>
                _eventBus.Publish(new TriggerPositionEvent
                {
                    IsSuccess = true,
                    TriggerPosition = triggerPosition,
                    PackageInfo = packageInfo
                }), description);
        }

        /// <summary>将创建、回调和异常指令的日志及持久化通知移出关键队列。</summary>
        private void QueueInstructionNotification(
            InstructionReceived instruction,
            string description)
        {
            QueuePackageNotification(
                () => _eventBus.Publish(instruction),
                description);
        }

        /// <summary>立即将关键包裹工作加入有序队列，绝不等待消费者。</summary>
        private bool TryQueuePackageEvent(Action work, string description)
        {
            if (_packageEventDispatcher.TryEnqueue(work))
            {
                return true;
            }

            QueueError($"关键包裹队列已停止，工作未入队:{description}");
            return false;
        }

        /// <summary>把已到期的空包裹删除放入最高优先级，避免被密集相机或赋值事件压在队尾。</summary>
        private bool TryQueuePriorityPackageEvent(Action work, string description)
        {
            if (_packageEventDispatcher.TryEnqueuePriority(work))
            {
                return true;
            }

            QueueError($"关键包裹优先队列已停止，工作未入队:{description}");
            return false;
        }

        /// <summary>将非关键通知移出包裹状态变更热路径。</summary>
        private void QueuePackageNotification(Action notification, string description)
        {
            if (!_packageNotificationDispatcher.TryEnqueue(notification))
            {
                QueueError($"包裹通知队列已停止，通知未入队:{description}");
            }
        }

        /// <summary>使用原子引用替换不可变配置快照，不占用关键包裹队列。</summary>
        private static Task SwapSettingsAsync(Action update)
        {
            update();
            return Task.CompletedTask;
        }

        /// <summary>仅激活通过校验的创建包裹配置，刷新失败时继续使用上一份稳定快照。</summary>
        private void ActivateCreatePackageSettings(
            CreatePackageSettingsDto settings,
            string source)
        {
            if (!settings.TryValidate(out var validationMessage))
            {
                QueueError($"创建包裹配置无效，已保留上一份配置;来源={source};原因={validationMessage}");
                return;
            }

            _createPackageSettingsDto = settings;
        }

        private bool IsRegexMatch(string input, string pattern)
        {
            var regex = _regexCache.GetOrAdd(
                pattern,
                static value => new Regex(
                    value,
                    RegexOptions.CultureInvariant | RegexOptions.Compiled,
                    TimeSpan.FromMilliseconds(100)));
            return regex.IsMatch(input);
        }

        /// <summary>在独立图片转交线程中把已完成包裹的图像所有权移交给存图服务。</summary>
        private void ProcessCompletedPackageImage(PackageInfo packageInfo)
        {
            ImageMessageInfo? imageMessage = null;
            JayTom.Dws.Abstractions.Imaging.ImageHandle? image = null;
            lock (packageInfo.SyncRoot)
            {
                if (packageInfo.Image is null && !packageInfo.IsImageSaveRequested)
                {
                    packageInfo.MarkImageSaveRequested();
                    return;
                }
                if (packageInfo.Image is not null && !packageInfo.IsImageSaveRequested)
                {
                    Volatile.Read(ref _camerasBySerialNumber).TryGetValue(
                        packageInfo.BarCodeInfo?.SerialNumber ?? string.Empty,
                        out var camera);
                    image = packageInfo.TakeImage();
                    imageMessage = new ImageMessageInfo
                    {
                        PackageTimestamped = packageInfo.Timestamp,
                        BarCode = packageInfo.BarCodeInfo?.Barcode ?? string.Empty,
                        CameraSerialNumber = packageInfo.BarCodeInfo?.SerialNumber ?? string.Empty,
                        Weight = (decimal)(packageInfo.WeightInfo?.FormattedWeight ?? 0),
                        Height = (decimal)(packageInfo.VolumeInfo?.FormattedHeight ?? 0),
                        Image = image,
                        Length = (decimal)(packageInfo.VolumeInfo?.FormattedLength ?? 0),
                        Width = (decimal)(packageInfo.VolumeInfo?.FormattedWidth ?? 0),
                        Volume = (decimal)(packageInfo.VolumeInfo?.FormattedVolume ?? 0),
                        ScanTime = packageInfo.BarCodeInfo?.ScanTime ?? DateTime.Now,
                        Type = SaveImageType.BarcodeImage,
                        CameraName = camera?.Info?.Name ?? string.Empty,
                        CameraCustomName = camera?.Info?.CustomName ?? string.Empty
                    };
                    packageInfo.MarkImageSaveRequested();
                }
            }

            if (imageMessage is null)
            {
                return;
            }

            try
            {
                _eventBus.Publish(imageMessage);
            }
            catch
            {
                lock (packageInfo.SyncRoot)
                {
                    if (packageInfo.Image is null)
                    {
                        packageInfo.Image = image;
                    }
                    else
                    {
                        image?.Dispose();
                    }
                    packageInfo.ResetImageSaveRequest();
                }
                throw;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //逻辑->外部数据回传后判断是否包含箱子码和包裹条码->如果有则上传并分拣->则上传错误码让它去异常口
            try
            {
                //读配置

                var createPackageSettings = await _settingsStore.GetAsync<CreatePackageSettingsDto>("CreatePackageSettings", stoppingToken) ?? new CreatePackageSettingsDto();
                ActivateCreatePackageSettings(createPackageSettings, "启动加载");
                _barcodeFilterSettingsDto = await _settingsStore.GetAsync<BarcodeFilterSettingsDto>("BarcodeFilterSettings", stoppingToken) ?? new BarcodeFilterSettingsDto();
                _weightSettingsDto = await _settingsStore.GetAsync<WeightSettingsDto>("WeightSettings", stoppingToken) ?? new WeightSettingsDto();
                _wdtWmsApiDto = await _settingsStore.GetAsync<WdtWmsApiDto>("WdtWmsApiParameters", stoppingToken) ?? new WdtWmsApiDto();
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            while (!stoppingToken.IsCancellationRequested &&
                   Volatile.Read(ref _isWindowsClose) == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
                try
                {
                    ReportPerformanceWatermarks();
                    if (_packageSessionStore.GetPackageCount() > 0 && _deviceService.RunningStatus)
                    {
                        if (_createPackageSettingsDto.PackageRemoveMethods ==
                            PackageRemoveMethodsEnum.FillInformation)
                        {
                            RemoveCompletedPackagesReadyForCleanup();
                        }
                    }
                }
                catch (Exception e)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            }
        }

        /// <summary>每分钟异步报告一次关键队列的最大排队和执行耗时水位。</summary>
        /// <summary>仅检查已完成包裹队列的队首并移除满足图片和格口生命周期条件的包裹。</summary>
        private void RemoveCompletedPackagesReadyForCleanup()
        {
            var now = DateTime.Now;
            var attempts = Math.Min(Volatile.Read(ref _packagesAwaitingCleanupCount), 4096);
            while (attempts-- > 0 &&
                   _packagesAwaitingCleanup.TryDequeue(out var packageInfo))
            {
                Interlocked.Decrement(ref _packagesAwaitingCleanupCount);
                var activePackage = _packageSessionStore.GetPackage(packageInfo.CreateTime);
                if (!ReferenceEquals(activePackage, packageInfo))
                {
                    continue;
                }

                var panoramaReady = packageInfo.PanoramaCameraImageInfo.All(info => info.IsExists) ||
                    now.Subtract(packageInfo.CreateTime).TotalMinutes > 5;
                if (!packageInfo.IsImageSaveRequested ||
                    packageInfo.IsWaitingForSortingInstruction ||
                    !panoramaReady)
                {
                    EnqueuePackageForCleanup(packageInfo);
                    continue;
                }

                _packageSessionStore.RemovePackage(
                    packageInfo.CreateTime,
                    "填充完整信息移除");
            }
        }

        private void EnqueuePackageForCleanup(PackageInfo packageInfo)
        {
            _packagesAwaitingCleanup.Enqueue(packageInfo);
            Interlocked.Increment(ref _packagesAwaitingCleanupCount);
        }

        private void ReportPerformanceWatermarks()
        {
            var now = Stopwatch.GetTimestamp();
            var previous = Volatile.Read(ref _lastPerformanceReportTimestamp);
            if (Stopwatch.GetElapsedTime(previous, now) < TimeSpan.FromMinutes(1) ||
                Interlocked.CompareExchange(
                    ref _lastPerformanceReportTimestamp,
                    now,
                    previous) != previous)
            {
                return;
            }

            var eventQueueDelay = _packageEventDispatcher.TakeMaximumQueueDelayMicroseconds();
            var eventHandlerDuration = _packageEventDispatcher.TakeMaximumHandlerDurationMicroseconds();
            var completionQueueDelay = _packageCompletionDispatcher.TakeMaximumQueueDelayMicroseconds();
            var completionHandlerDuration = _packageCompletionDispatcher.TakeMaximumHandlerDurationMicroseconds();
            var imageQueueDelay = _imageHandoffDispatcher.TakeMaximumQueueDelayMicroseconds();
            var imageHandlerDuration = _imageHandoffDispatcher.TakeMaximumHandlerDurationMicroseconds();
            var notificationQueueDelay = _packageNotificationDispatcher.TakeMaximumQueueDelayMicroseconds();
            var notificationHandlerDuration = _packageNotificationDispatcher.TakeMaximumHandlerDurationMicroseconds();
            if (eventQueueDelay < 50_000 && eventHandlerDuration < 50_000 &&
                completionQueueDelay < 50_000 && completionHandlerDuration < 50_000 &&
                imageQueueDelay < 50_000 && imageHandlerDuration < 50_000 &&
                notificationQueueDelay < 50_000 && notificationHandlerDuration < 50_000 &&
                _packageEventDispatcher.PendingCount == 0 &&
                _packageCompletionDispatcher.PendingCount == 0 &&
                _imageHandoffDispatcher.PendingCount == 0 &&
                _packageNotificationDispatcher.PendingCount == 0)
            {
                return;
            }

            QueuePackageNotification(() => _eventBus.Publish(new AppLogInfoModel
            {
                CreateTime = DateTime.Now,
                Type = LogType.Warning,
                Message =
                    $"热路径性能水位(us):包裹排队={eventQueueDelay},包裹执行={eventHandlerDuration}," +
                    $"完成排队={completionQueueDelay},完成执行={completionHandlerDuration}," +
                    $"图片排队={imageQueueDelay},图片执行={imageHandlerDuration}," +
                    $"通知排队={notificationQueueDelay},通知执行={notificationHandlerDuration};" +
                    $"当前待处理={_packageEventDispatcher.PendingCount}/" +
                    $"{_packageCompletionDispatcher.PendingCount}/" +
                    $"{_imageHandoffDispatcher.PendingCount}/" +
                    $"{_packageNotificationDispatcher.PendingCount}"
            }), "热路径性能水位");
        }

        /// <summary>
        /// 停止包裹处理工作器并释放尚未转移所有权的相机帧。
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Interlocked.Exchange(ref _isWindowsClose, 1);
            _multiCameraDeadlineScheduler.Dispose();
            await _packageEventDispatcher.DisposeAsync();
            await _packageCompletionDispatcher.DisposeAsync();
            await _imageHandoffDispatcher.DisposeAsync();
            await _packageNotificationDispatcher.DisposeAsync();
            foreach (var group in _multiCameraFrameGroups)
            {
                group.Deadline?.Dispose();
                foreach (var frameInfo in group.Frames.Values)
                {
                    frameInfo.Image?.Dispose();
                }
            }
            _multiCameraFrameGroups.Clear();
            _regexCache.Clear();
            await base.StopAsync(cancellationToken);
        }
    }
}
