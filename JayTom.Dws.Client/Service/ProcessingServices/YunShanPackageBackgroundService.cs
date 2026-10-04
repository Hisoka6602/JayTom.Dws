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
        /// <summary>无生命周期规则时共享只读空列表，避免每辆小车分配一个空计时器列表。</summary>
        private static readonly List<PackageTimer> EmptyPackageTimers = [];
        /// <summary>下位机创建指令待包裹完成后再通知，避免空车和超时小车在持久化队列暂存十分钟。</summary>
        private readonly ConcurrentDictionary<DateTime, PackageInstructionEventArgs> _pendingCreationInstructions = new();
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
        /// <summary>仅由关键队列分配的唯一包裹关联标识，避免同毫秒 TCP 粘包覆盖持久化及回传索引。</summary>
        private long _lastAcceptedPackageTimestamp;
        /// <summary>按唯一标识直接定位等待真实回执或超时补记的原车，不扫描包裹队列。</summary>
        private readonly ConcurrentDictionary<long, PackageInfo> _completedLowerMachinePackages = new();
        /// <summary>保留本轮已消费或已完成的小车编号，清理后的迟到/重复移除回执不再误报异常。</summary>
        private readonly ConcurrentDictionary<long, byte> _retiredLowerMachineCartIds = new();
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
                    if (UsesLowerMachineCameraOrder)
                    {
                        AcceptLowerMachineCameraResult(args, IsNoReadBarcode(args.Barcode));
                        return;
                    }
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
                        if (IsNoReadBarcode(args.Barcode) && ShouldSuppressNoRead(args.ScanTime))
                        {
                            try
                            {
                                ConsumeSuppressedNoRead(args.ScanTime, "camera-noread", args.Barcode);
                            }
                            finally
                            {
                                args.Image?.Dispose();
                            }
                            return;
                        }
                        //多条码判断------

                        var info = _createPackageSettingsDto.BarcodeHandlingMethod == BarcodeHandlingMethodEnum.UseMultipleBarcodes
                            ? null
                            : _packageSessionStore.GetPackageByBarcodeObservation(args.ScanTime, args.CameraSerialNumber);
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
                        }, out var assignmentRejection);

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
                                QueueBarcodeAssignmentRejected("camera", args.ScanTime, assignmentRejection, args.Barcode);
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
                    try
                    {
                    if (UsesLowerMachineCameraOrder)
                    {
                        AcceptLowerMachineCameraResult(args, isNoRead: true);
                        return;
                    }
                    if (Volatile.Read(ref _scannerCameraCount) > 1)
                    {
                        var retainImage = _createPackageSettingsDto.IsUseNoRead;
                        if (!retainImage)
                        {
                            args.Image?.Dispose();
                        }
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
                            Image = retainImage
                                ? JayTom.Dws.Abstractions.Imaging.ImageHandle.TakeOwnershipIfPresent(args.Image)
                                : null
                        };
                        AcceptMultiCameraFrame(args.CameraSerialNumber, barCodeFrameInfo);
                    }
                    else
                    {
                        if (ShouldSuppressNoRead(args.ScanTime))
                        {
                            try
                            {
                                ConsumeSuppressedNoRead(args.ScanTime, "camera-noread", args.Barcode);
                            }
                            finally
                            {
                                args.Image?.Dispose();
                            }
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
                        }, out var assignmentRejection);
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
                                QueueBarcodeAssignmentRejected("camera-noread", args.ScanTime, assignmentRejection, args.Barcode);
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
            _deviceService.BarcodeFiltered += (_, args) =>
            {
                args.IsFiltered = true;
                if (!TryQueuePackageEvent(() =>
                {
                    if (UsesLowerMachineCameraOrder)
                    {
                        AcceptLowerMachineCameraResult(args, isNoRead: true);
                    }
                    else
                    {
                        ReleaseLowerMachineCameraResult(args);
                    }
                }, "相机过滤占位"))
                {
                    ReleaseLowerMachineCameraResult(args);
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
                            // 一次 TCP 接收可能拆出多条创建指令，其接收时间完全相同。
                            // 时间键仅递增一个 tick，保留窗口使用的真实接收时刻；业务标识另行保证唯一。
                            var createTime = args.InstructionTime;
                            if (createTime.Ticks <= _lastAcceptedPackageCreateTicks)
                            {
                                createTime = new DateTime(_lastAcceptedPackageCreateTicks + 1, createTime.Kind);
                            }
                            var packageInfo = new PackageInfo()
                            {
                                Guid = num,
                                IsCreatedByLowerMachine = true,
                                PackageCreationInstruction = args.Instruction,
                                CreateTime = createTime,
                            };

                            if (ProcessPackageTrigger(packageInfo))
                            {
                                _retiredLowerMachineCartIds.TryRemove(packageInfo.Guid, out _);
                                _pendingCreationInstructions[packageInfo.CreateTime] = args;
                                // 清空会话可从其他线程发生；不保留已在插入前被移除的指令。
                                if (!ReferenceEquals(_packageSessionStore.GetPackage(packageInfo.CreateTime), packageInfo))
                                {
                                    _pendingCreationInstructions.TryRemove(packageInfo.CreateTime, out _);
                                }
                                EnqueueLowerMachineCameraSlot(packageInfo);
                            }
                            else
                            {
                                // 本次物理触发即使创建失败，也必须接住其最终相机结果。
                                EnqueueLowerMachineCameraSlot(packageInfo, creationAccepted: false);
                            }
                        }
                        else
                        {
                            JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Write("package.creation.rejected",
                                "下位机创建指令中的小车编号无法解析，仍保留相机消费位置", LogType.Warning,
                                occurredAt: args.InstructionTime,
                                fields: [("reasonCode", "InvalidCartId"), ("keyword", args.Keyword),
                                    ("rawContent", args.Instruction), ("receivedAt", args.InstructionTime)]);
                            EnqueueLowerMachineCameraSlot(null);
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
                            if (!packageInfo.TryConfirmFall())
                            {
                                return;
                            }
                            _retiredLowerMachineCartIds[packageInfo.Guid] = 0;
                            QueueInstructionNotification(new InstructionReceived
                            {
                                Timestamp = packageInfo.Timestamp,
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
                        else if (!_retiredLowerMachineCartIds.ContainsKey(num))
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
                                Timestamp = packageInfo.Timestamp,
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
                    }, out var assignmentRejection);
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
                            QueueBarcodeAssignmentRejected("external-tcp", receivedAt, assignmentRejection, barCode);
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
                    if (info.Status == ApplicationStatus.Stop)
                    {
                        // 设备及下位机已停，排在所有已接收事件之后结束本轮触发计数。
                        TryQueuePackageEvent(() =>
                        {
                            DisposeLowerMachineCameraState();
                            _pendingCreationInstructions.Clear();
                            _completedLowerMachinePackages.Clear();
                            if (_createPackageSettingsDto.ClearPackageQueueOnStop)
                            {
                                _packageSessionStore.ClearAllPackages();
                            }
                            _retiredLowerMachineCartIds.Clear();
                        }, "停止后结束下位机触发计数");
                    }
                }
            });

            //移除包裹事件
            _packageSessionStore.PackageRemoved += (sender, args) =>
            {
                JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Package("package.removed",
                    "原包裹退出会话", args.RemovedPackage,
                    fields: [("reason", args.Description), ("discarded", args.IsDiscarded),
                        ("pendingCameraSlots", _lowerMachineCameraSlots.Count)]);
                _pendingCreationInstructions.TryRemove(args.RemovedPackage.CreateTime, out _);
                _completedLowerMachinePackages.TryRemove(args.RemovedPackage.Timestamp, out _);
                if (args.RemovedPackage.IsCreatedByLowerMachine)
                {
                    _retiredLowerMachineCartIds[args.RemovedPackage.Guid] = 0;
                }
                if (args.IsDiscarded || args.RemovedPackage is { IsCreatedByLowerMachine: true, BarCodeInfo: null })
                {
                    return;
                }
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
                    JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Package("package.completed",
                        "组包齐套，进入分拣与持久化分发", completedPackage,
                        fields: [("requiresSortingInstruction", _sortingService.RequiresSortingInstruction),
                            ("removeMethod", _createPackageSettingsDto.PackageRemoveMethods)]);
                    if (completedPackage.IsCreatedByLowerMachine)
                    {
                        _completedLowerMachinePackages[completedPackage.Timestamp] = completedPackage;
                    }
                    if (_pendingCreationInstructions.TryRemove(completedPackage.CreateTime, out var creationInstruction))
                    {
                        QueueCreationInstruction(completedPackage, creationInstruction);
                    }
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
            _eventBus.Subscribe<PackageExitUpdateEvent>(item =>
            {
                if (item.ExitType == SortingExitType.PhysicalExit &&
                    _completedLowerMachinePackages.TryRemove(item.Timestamp, out var originalPackage))
                {
                    originalPackage.MarkFallConfirmed();
                }
            });
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
        private PackageInfo? TryBindBarcode(
            DateTime observedAt,
            Action<PackageInfo> assignment,
            out BarcodeAssignmentRejection? rejection)
        {
            var settings = _createPackageSettingsDto;
            var windowEnabled = settings.IsUseBarcodeAssignmentInterval;
            var queueOrder = settings.BarcodeQueueOrder;
            var minimumMilliseconds = settings.MinimumAssignmentTime;
            var maximumMilliseconds = settings.MaximumAssignmentTime;
            var emptyPackageExpiryMilliseconds =
                settings is { IsUseEmptyPackageExpiry: true, EmptyPackageExpiryTime: > 0 }
                    ? settings.EmptyPackageExpiryTime
                    : (int?)null;
            var matched = _packageSessionStore.TryBindBarcode(
                observedAt,
                queueOrder,
                windowEnabled,
                minimumMilliseconds,
                maximumMilliseconds,
                emptyPackageExpiryMilliseconds,
                DateTime.Now,
                assignment,
                out rejection);
            if (matched is not null)
            {
                QueueBarcodeAssignmentSelected(matched, observedAt);
            }
            return matched;
        }

        /// <summary>在非关键线程记录实际匹配的小车与条码，支持按触发顺序的关联证据。</summary>
        private void QueueBarcodeAssignmentSelected(PackageInfo matched, DateTime observedAt, bool lowerMachineOrder = false,
            long? cameraOrdinal = null, long? triggerSequence = null)
        {
                var settings = _createPackageSettingsDto;
                var windowEnabled = settings.IsUseBarcodeAssignmentInterval;
                var queueOrder = lowerMachineOrder ? BarcodeQueueOrderEnum.TimeAscending : settings.BarcodeQueueOrder;
                var minimumMilliseconds = settings.MinimumAssignmentTime;
                var maximumMilliseconds = settings.MaximumAssignmentTime;
                var timestamp = matched.Timestamp;
                var sequence = matched.Guid.ToString();
                var barcode = matched.BarCodeInfo?.Barcode;
                var createdAt = matched.CreateTime;
                var boundAt = DateTime.Now;
                var camera = matched.BarCodeInfo?.SerialNumber;
                var interval = (observedAt.Ticks - createdAt.Ticks) / (decimal)TimeSpan.TicksPerMillisecond;
                QueuePackageNotification(() => JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Write(
                    "barcode.assignment.selected", lowerMachineOrder
                        ? "按下位机触发顺序绑定原小车，窗口只校验原包裹" : windowEnabled
                        ? "按配置时间顺序选择区间内首个可赋值包裹" : "按配置队列顺序赋值包裹",
                    packageTimestamp: timestamp, barcode: barcode, sequence: sequence, occurredAt: boundAt,
                    fields: [("observedAt", observedAt), ("createdAt", createdAt),
                        ("creationTimeTicks", createdAt.Ticks), ("processingAt", boundAt), ("camera", camera),
                        ("cameraOrdinal", cameraOrdinal), ("triggerSequence", triggerSequence),
                        ("reasonCode", lowerMachineOrder ? "OriginalTriggerOrder" : "ConfiguredQueueOrder"),
                        ("actualIntervalMilliseconds", interval),
                        ("windowEnabled", windowEnabled), ("queueOrder", queueOrder), ("minimumMilliseconds", minimumMilliseconds),
                        ("maximumMilliseconds", maximumMilliseconds)]), "条码赋值匹配证据");
        }

        /// <summary>NoRead 开关和过滤只决定业务输出；下位机占位仍需消费。</summary>
        private bool ShouldSuppressNoRead(DateTime observedAt)
        {
            var settings = _createPackageSettingsDto;
            if (!settings.IsUseNoRead)
            {
                return true;
            }
            if (!settings.IsUseNoReadFilter)
            {
                return false;
            }
            if (observedAt.Ticks - _lastNoReadTime.Ticks <
                (long)settings.FilterInterval * TimeSpan.TicksPerMillisecond)
            {
                return true;
            }
            _lastNoReadTime = observedAt;
            return false;
        }

        /// <summary>直接消费 NoRead 对应占位，不创建条码模型、图片句柄或包裹完成通知。</summary>
        private void ConsumeSuppressedNoRead(DateTime observedAt, string source, string? barcode)
        {
            var settings = _createPackageSettingsDto;
            if ((settings.PackageCreationMethods & PackageCreationMethodsEnum.LowerMachineCreation) == 0)
            {
                return;
            }
            var discarded = _packageSessionStore.TryDiscardUnassignedPackage(
                observedAt,
                settings.BarcodeQueueOrder,
                settings.IsUseBarcodeAssignmentInterval,
                settings.MinimumAssignmentTime,
                settings.MaximumAssignmentTime,
                settings is { IsUseEmptyPackageExpiry: true, EmptyPackageExpiryTime: > 0 }
                    ? settings.EmptyPackageExpiryTime
                    : (int?)null,
                DateTime.Now,
                out var rejection);
            if (discarded is null)
            {
                QueueBarcodeAssignmentRejected(source, observedAt, rejection, barcode);
            }
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
                    () =>
                    {
                        if (Volatile.Read(ref _isWindowsClose) == 0)
                        {
                            TryQueuePackageEvent(
                                () => CompleteMultiCameraGroup(targetGroup),
                                "多相机融合到期");
                        }
                    });
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
            if (IsNoReadBarcode(selected.BarCodeInfo?.Barcode) && ShouldSuppressNoRead(scanTime))
            {
                try
                {
                    ConsumeSuppressedNoRead(scanTime, "multi-camera-noread", selected.BarCodeInfo?.Barcode);
                }
                finally
                {
                    DisposeMultiCameraGroup(frameGroup.Frames.Values, null);
                }
                return;
            }
            var hadPendingPackage = _packageSessionStore.HasUnassignedPackage();
            JayTom.Dws.Abstractions.Imaging.ImageHandle? replacedImage = null;
            var package = TryBindBarcode(scanTime, target =>
            {
                replacedImage = target.Image;
                target.BarCodeInfo = selected.BarCodeInfo;
                target.Image = selected.Image;
            }, out var assignmentRejection);
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
                QueueBarcodeAssignmentRejected("multi-camera", scanTime, assignmentRejection, selected.BarCodeInfo?.Barcode);
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

        /// <summary>将绑定决策时取得的拒绝原因与实际间隔移交到非关键通知队列。</summary>
        private void QueueBarcodeAssignmentRejected(
            string source,
            DateTime observedAt,
            BarcodeAssignmentRejection? rejection,
            string? barcode)
        {
            static string FormatMilliseconds(decimal? value) => value is { } milliseconds
                ? milliseconds.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + "ms"
                : "无候选包裹";
            var reason = rejection?.Reason switch
            {
                BarcodeAssignmentRejectionReason.BelowMinimumInterval => "实际间隔小于赋值下限",
                BarcodeAssignmentRejectionReason.AboveMaximumInterval => "实际间隔大于赋值上限",
                BarcodeAssignmentRejectionReason.ObservedBeforePackageCreation => "扫码时间早于包裹创建时间",
                BarcodeAssignmentRejectionReason.EmptyPackageExpiredAtObservation => "扫码时空包裹已过期",
                BarcodeAssignmentRejectionReason.EmptyPackageExpiredWhileQueued => "排队处理时空包裹已过期",
                BarcodeAssignmentRejectionReason.InvalidAssignmentWindow => "赋值窗口配置无效",
                BarcodeAssignmentRejectionReason.OriginalPackageUnavailable => "原小车已移除或赋值，本次结果不顺延",
                _ => "没有待赋值包裹"
            };
            var candidateId = rejection?.PackageId?.ToString() ?? "无候选包裹";
            var candidateCreatedAt = rejection?.PackageCreatedAt?.ToString("O") ?? "无候选包裹";
            var actualInterval = FormatMilliseconds(rejection?.ObservedAgeMilliseconds);
            var processingAge = FormatMilliseconds(rejection?.ProcessingAgeMilliseconds);
            var processingDelay = FormatMilliseconds(rejection?.ProcessingDelayMilliseconds);
            var processingTime = rejection?.ProcessingAt.ToString("O") ?? "未知";
            var settings = _createPackageSettingsDto;
            var minimumAssignmentTime = settings.MinimumAssignmentTime;
            var maximumAssignmentTime = settings.MaximumAssignmentTime;
            var intervalEnabled = settings.IsUseBarcodeAssignmentInterval;
            var pendingCount = _packageEventDispatcher.PendingCount;
            var packageCount = _packageSessionStore.GetPackageCount();
            var hasUnassignedPackage = _packageSessionStore.HasUnassignedPackage();
            var emptyPackageExpiry = settings.IsUseEmptyPackageExpiry
                ? settings.EmptyPackageExpiryTime.ToString() + "ms" : "关闭";
            var rejectedAt = DateTime.Now;
            var originalTimestamp = rejection?.PackageTimestamp ?? 0;
            QueuePackageNotification(() =>
            {
                var message =
                        $"条码赋值被拒绝;原因={reason},来源={source},条码={barcode},观测时间={observedAt:O}," +
                        $"候选包裹序号={candidateId},包裹创建时间={candidateCreatedAt},实际间隔={actualInterval}," +
                        $"处理时间={processingTime},处理时包裹年龄={processingAge},扫码到处理延迟={processingDelay}," +
                        $"窗口={minimumAssignmentTime}-{maximumAssignmentTime}ms," +
                        $"赋值窗口校验已启用={intervalEnabled},包裹总数={packageCount}," +
                        $"存在待赋值包裹={hasUnassignedPackage},空包裹有效期={emptyPackageExpiry}," +
                        $"关键队列待处理数={pendingCount}";
                // 文件日志也保留同一诊断；写入仍在非关键通知队列，不阻塞设备绑定。
                LogManager.GetCurrentClassLogger().Warn(message);
                JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Write("barcode.assignment.rejected",
                    "没有满足窗口与有效期条件的可赋值包裹", LogType.Warning,
                    packageTimestamp: originalTimestamp, barcode: barcode, sequence: candidateId, occurredAt: rejectedAt,
                    fields: [("source", source), ("reason", reason), ("observedAt", observedAt),
                        ("reasonCode", rejection?.Reason ?? BarcodeAssignmentRejectionReason.NoUnassignedPackage),
                        ("creationTimeTicks", rejection?.PackageCreatedAt?.Ticks), ("processingAt", rejection?.ProcessingAt),
                        ("observedAgeMilliseconds", rejection?.ObservedAgeMilliseconds),
                        ("processingAgeMilliseconds", rejection?.ProcessingAgeMilliseconds),
                        ("processingDelayMilliseconds", rejection?.ProcessingDelayMilliseconds),
                        ("pendingEvents", pendingCount), ("hasUnassignedPackage", hasUnassignedPackage),
                        ("candidateCreatedAt", candidateCreatedAt), ("actualInterval", actualInterval),
                        ("processingDelay", processingDelay), ("windowEnabled", intervalEnabled),
                        ("minimumMilliseconds", minimumAssignmentTime), ("maximumMilliseconds", maximumAssignmentTime),
                        ("packageCount", packageCount), ("emptyPackageExpiry", emptyPackageExpiry)]);
                _eventBus.Publish(new AppLogInfoModel
                {
                    CreateTime = rejectedAt,
                    Type = LogType.Warning,
                    Message = message
                });
            }, "条码赋值拒绝");
        }

        private bool ProcessPackageTrigger(PackageInfo packageInfo)
        {
            var createPackageSettings = _createPackageSettingsDto;
            var previousCreateTicks = _lastAcceptedPackageCreateTicks;
            var creationIntervalTicks = Math.Max(
                0L,
                (long)createPackageSettings.PackageCreationInterval *
                TimeSpan.TicksPerMillisecond);
            // 下位机每条有效小车指令都是独立占位，由序号索引防重；防抖丢指令会使后续识别结果错位。
            if (!packageInfo.IsCreatedByLowerMachine &&
                packageInfo.CreateTime.Ticks - previousCreateTicks < creationIntervalTicks)
            {
                JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Package("package.creation.rejected",
                    "包裹创建被防抖间隔拒绝", packageInfo, LogType.Warning,
                    fields: [("reasonCode", "CreationDebounce"), ("minimumMilliseconds", createPackageSettings.PackageCreationInterval),
                        ("previousCreationTimeTicks", previousCreateTicks)]);
                packageInfo.TakeImage()?.Dispose();
                return false;
            }

            packageInfo.Timestamp = Math.Max(
                new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds(),
                _lastAcceptedPackageTimestamp + 1);
            if (_weightSettingsDto.Mode == WeightMode.None)
            {
                packageInfo.WeightInfo ??= new WeightInfoModel();
            }
            packageInfo.VolumeInfo ??= new VolumeInfoModel();

            var packageRemoveTimers = createPackageSettings is
                { IsUseEmptyPackageExpiry: true, EmptyPackageExpiryTime: > 0 } or
                { IsUsePackageExpiry: true, PackageExpiryTime: > 0 }
                    ? new List<PackageTimer>(2)
                    : EmptyPackageTimers;
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
                JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Package("package.creation.rejected",
                    "包裹未能加入会话，拒绝本次创建", packageInfo, LogType.Warning,
                    fields: [("reasonCode", "SessionAddRejected"), ("packageCount", _packageSessionStore.GetPackageCount())]);
                packageInfo.TakeImage()?.Dispose();
                packageInfo.DisposeTimers();
                return false;
            }
            _lastAcceptedPackageCreateTicks = packageInfo.CreateTime.Ticks;
            _lastAcceptedPackageTimestamp = packageInfo.Timestamp;
            JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Package("package.created", "包裹创建成功", packageInfo,
                fields: [("creationMethods", createPackageSettings.PackageCreationMethods),
                    ("packageCount", _packageSessionStore.GetPackageCount()), ("pendingEvents", _packageEventDispatcher.PendingCount)]);

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
            JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Package("package.fields.changed",
                "原包裹赋值后的齐套状态", packageInfo, fields: [("triggerPosition", triggerPosition)]);
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
        private void QueueCreationInstruction(PackageInfo package, PackageInstructionEventArgs instruction)
        {
            QueueInstructionNotification(new InstructionReceived
            {
                Timestamp = package.Timestamp,
                IsCreatedByLowerMachine = true,
                SortingCode = package.Guid.ToString(),
                InstructionInfos =
                [
                    new()
                    {
                        InstructionContent = instruction.Instruction,
                        InstructionGeneratedTime = instruction.InstructionTime,
                        InstructionType = InstructionType.CreatePackage
                    }
                ],
                ConnectionName = instruction.ConnectionName
            }, "创建指令");
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
            JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Write("package.configuration", "组包配置快照已启用",
                fields: [("source", source), ("creationMethods", settings.PackageCreationMethods),
                    ("removeMethod", settings.PackageRemoveMethods), ("useNoRead", settings.IsUseNoRead),
                    ("useNoReadFilter", settings.IsUseNoReadFilter), ("noReadFilterMilliseconds", settings.FilterInterval),
                    ("windowEnabled", settings.IsUseBarcodeAssignmentInterval), ("queueOrder", settings.BarcodeQueueOrder),
                    ("minimumMilliseconds", settings.MinimumAssignmentTime), ("maximumMilliseconds", settings.MaximumAssignmentTime),
                    ("emptyExpiryEnabled", settings.IsUseEmptyPackageExpiry), ("emptyExpiryMilliseconds", settings.EmptyPackageExpiryTime),
                    ("expiryEnabled", settings.IsUsePackageExpiry), ("expiryMilliseconds", settings.PackageExpiryTime)]);
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
                    packageInfo.IsWaitingForFallConfirmation ||
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

            JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Write("package.queue.pressure",
                "组包关键队列或下游队列出现等待", LogType.Warning,
                fields: [("reasonCode", "PipelineQueuePressure"), ("eventQueueDelayMicroseconds", eventQueueDelay),
                    ("eventHandlerMicroseconds", eventHandlerDuration), ("completionQueueDelayMicroseconds", completionQueueDelay),
                    ("imageQueueDelayMicroseconds", imageQueueDelay), ("notificationQueueDelayMicroseconds", notificationQueueDelay),
                    ("pendingEvents", _packageEventDispatcher.PendingCount), ("pendingCompletions", _packageCompletionDispatcher.PendingCount),
                    ("pendingImages", _imageHandoffDispatcher.PendingCount), ("pendingNotifications", _packageNotificationDispatcher.PendingCount)]);

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
            // 已接收的帧仍可能建立融合期限；先排空关键队列，再释放期限调度器。
            await _packageEventDispatcher.DisposeAsync();
            _multiCameraDeadlineScheduler.Dispose();
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
            DisposeLowerMachineCameraState();
            _pendingCreationInstructions.Clear();
            _regexCache.Clear();
            await base.StopAsync(cancellationToken);
        }

        /// <summary>仅保留原创建时间；业务包裹被移除后仍消费这一位置，不保留图片、包裹或计时器。</summary>
        private readonly Queue<LowerMachineCameraSlot> _lowerMachineCameraSlots = new();
        /// <summary>暂存早于下位机创建指令到达的单相机最终结果。</summary>
        private readonly Queue<(BarcodeReadEventArgs Args, bool IsNoRead, long Ordinal)> _earlyLowerMachineCameraResults = new();
        /// <summary>只用于证据关联的本轮物理触发流水号，不依赖小车编号或设备时钟。</summary>
        private long _lowerMachineTriggerSequence;
        /// <summary>分别计数各相机的触发结果，并识别同次触发发布的重复条码。</summary>
        private readonly Dictionary<string, (uint? TriggerIndex, long FrameNo, long Sequence)> _lowerMachineCameraObservations =
            new(StringComparer.Ordinal);
        /// <summary>按每台相机的触发次数组帧，不使用不同设备的响应时间差关联相邻小车。</summary>
        private readonly Dictionary<long, LowerMachineCameraFrameGroup> _lowerMachineCameraGroups = new();
        /// <summary>下一组最终结果应消费的触发次序，迟到相机帧不得重新打开已处理的小车。</summary>
        private long _nextLowerMachineCameraSequence = 1;

        /// <summary>下位机创建模式使用物理触发顺序，相机响应窗口仅校验原包裹。</summary>
        private bool UsesLowerMachineCameraOrder =>
            (_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.LowerMachineCreation) != 0;

        /// <summary>每个创建指令保留一个消费位置；创建失败的位置也接住自己的相机结果。</summary>
        private void EnqueueLowerMachineCameraSlot(PackageInfo? package, bool creationAccepted = true)
        {
            var slot = new LowerMachineCameraSlot(creationAccepted ? package?.CreateTime : null, package?.Timestamp ?? 0,
                package?.Guid ?? 0, ++_lowerMachineTriggerSequence);
            _lowerMachineCameraSlots.Enqueue(slot);
            JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Write("trigger.slot.queued", "本次触发保留固定相机消费位置",
                packageTimestamp: slot.PackageTimestamp, sequence: slot.CartId.ToString(),
                fields: [("createdAt", slot.CreatedAt), ("creationTimeTicks", slot.CreatedAt?.Ticks),
                    ("triggerSequence", slot.TriggerSequence), ("creationAccepted", creationAccepted && package is not null),
                    ("pendingCameraSlots", _lowerMachineCameraSlots.Count), ("earlyCameraResults", _earlyLowerMachineCameraResults.Count)]);
            DrainLowerMachineCameraResults();
            DrainLowerMachineCameraGroups();
        }

        /// <summary>接管一个保序最终结果，先按触发次数确定归属，再处理业务过滤。</summary>
        private void AcceptLowerMachineCameraResult(BarcodeReadEventArgs args, bool isNoRead)
        {
            var serialNumber = args.CameraSerialNumber;
            _lowerMachineCameraObservations.TryGetValue(serialNumber, out var previous);
            var duplicate = previous.Sequence > 0 &&
                (args.TriggerIndex.HasValue
                    ? args.TriggerIndex == previous.TriggerIndex
                    : args.FrameNo > 0 && args.FrameNo == previous.FrameNo);
            if (duplicate)
            {
                LogLowerMachineCamera("camera.duplicate", default, args, previous.Sequence,
                    "DuplicateCameraResult", LogType.Information);
                // 同次触发的额外条码不能再消费一辆车；保留原有合并条码设置。
                var package = _packageSessionStore.GetPackageByBarcodeObservation(args.ScanTime, serialNumber);
                if (!isNoRead && package?.BarCodeInfo is { } barcode &&
                    _createPackageSettingsDto.BarcodeHandlingMethod == BarcodeHandlingMethodEnum.MergeBarcodes)
                {
                    lock (package.SyncRoot)
                    {
                        barcode.Barcode += $"{_barcodeFilterSettingsDto.MultiBarcodeDelimiter}{args.Barcode}";
                    }
                }
                ReleaseLowerMachineCameraResult(args);
                return;
            }

            var sequence = previous.Sequence + 1;
            _lowerMachineCameraObservations[serialNumber] = (args.TriggerIndex, args.FrameNo, sequence);
            LogLowerMachineCamera("camera.result.ordered", default, args, sequence, "FinalCameraResult", LogType.Information,
                ("isNoRead", isNoRead));
            if (isNoRead && !_createPackageSettingsDto.IsUseNoRead)
            {
                ReleaseLowerMachineCameraResult(args);
            }
            var expectedCameraCount = Math.Max(1, Volatile.Read(ref _scannerCameraCount));
            if (expectedCameraCount == 1)
            {
                _earlyLowerMachineCameraResults.Enqueue((args, isNoRead, sequence));
                DrainLowerMachineCameraResults();
                return;
            }
            if (sequence < _nextLowerMachineCameraSequence)
            {
                LogLowerMachineCamera("camera.late", default, args, sequence, "FusionAlreadyClosed", LogType.Warning,
                    ("nextCameraOrdinal", _nextLowerMachineCameraSequence));
                ReleaseLowerMachineCameraResult(args);
                return;
            }
            if (!_lowerMachineCameraGroups.TryGetValue(sequence, out var group))
            {
                group = new LowerMachineCameraFrameGroup { ExpectedCameraCount = expectedCameraCount };
                _lowerMachineCameraGroups.Add(sequence, group);
                var deadlineGroup = group;
                group.Deadline = _multiCameraDeadlineScheduler.Schedule(
                    TimeSpan.FromMilliseconds(Math.Max(1, _barcodeFilterSettingsDto.MergeTimeout)),
                    () =>
                    {
                        if (Volatile.Read(ref _isWindowsClose) == 0)
                        {
                            TryQueuePackageEvent(() =>
                            {
                                deadlineGroup.IsReady = true;
                                if (_lowerMachineCameraGroups.TryGetValue(sequence, out var pending) &&
                                    ReferenceEquals(pending, deadlineGroup) && pending.Frames.Count < pending.ExpectedCameraCount)
                                    JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Write("camera.fusion.deadline",
                                        "多相机融合期限到达，缺帧只关闭本次触发", LogType.Warning,
                                        fields: [("reasonCode", "FusionIncomplete"), ("cameraOrdinal", sequence),
                                            ("expectedCameras", pending.ExpectedCameraCount), ("receivedCameras", pending.Frames.Count),
                                            ("mergeTimeoutMilliseconds", _barcodeFilterSettingsDto.MergeTimeout)]);
                                DrainLowerMachineCameraGroups();
                            }, "下位机多相机融合到期");
                        }
                    });
            }
            group.Frames.Add(serialNumber, (args, isNoRead));
            if (group.Frames.Count >= group.ExpectedCameraCount)
            {
                group.IsReady = true;
            }
            DrainLowerMachineCameraGroups();
        }

        /// <summary>同时存在原小车位置和最终结果时逐对消费，单次失败不重试下一辆车。</summary>
        private void DrainLowerMachineCameraResults()
        {
            while (_lowerMachineCameraSlots.Count > 0 && _earlyLowerMachineCameraResults.TryDequeue(out var result))
            {
                var slot = _lowerMachineCameraSlots.Dequeue();
                try
                {
                    ProcessLowerMachineCameraPair(slot, result.Args, result.IsNoRead, result.Ordinal);
                }
                catch (Exception exception)
                {
                    LogLowerMachineCamera("camera.pair.error", slot, result.Args, result.Ordinal,
                        "CameraPairProcessingFailed", LogType.Exception, ("error", exception.ToString()));
                    QueueError("处理下位机原小车相机结果失败", exception);
                }
                finally
                {
                    ReleaseLowerMachineCameraResult(result.Args);
                }
            }
        }

        /// <summary>按创建顺序完成多相机组；组超时只关闭本车，慢相机后续帧按自己的次序跳过已关闭组。</summary>
        private void DrainLowerMachineCameraGroups()
        {
            while (_lowerMachineCameraSlots.Count > 0 &&
                _lowerMachineCameraGroups.TryGetValue(_nextLowerMachineCameraSequence, out var group) && group.IsReady)
            {
                var cameraOrdinal = _nextLowerMachineCameraSequence++;
                _lowerMachineCameraGroups.Remove(cameraOrdinal);
                var slot = _lowerMachineCameraSlots.Dequeue();
                group.Deadline?.Dispose();
                group.Deadline = null;
                try
                {
                    (BarcodeReadEventArgs Args, bool IsNoRead)? selected = null;
                    string? validBarcode = null;
                    var conflict = false;
                    foreach (var frame in group.Frames.Values)
                    {
                        if (!frame.IsNoRead)
                        {
                            if (validBarcode is not null &&
                                !string.Equals(validBarcode, frame.Args.Barcode, StringComparison.OrdinalIgnoreCase))
                            {
                                conflict = true;
                            }
                            validBarcode ??= frame.Args.Barcode;
                            if (selected is null || selected.Value.IsNoRead ||
                                selected.Value.Args.Image is null && frame.Args.Image is not null)
                            {
                                selected = frame;
                            }
                        }
                        else
                        {
                            selected ??= frame;
                        }
                    }
                    if (selected is { } result)
                    {
                        var incomplete = group.Frames.Count < group.ExpectedCameraCount;
                        var reason = conflict ? "FusionBarcodeConflict" : incomplete ? "FusionIncomplete" : "FusionComplete";
                        LogLowerMachineCamera("camera.fusion.completed", slot, result.Args, cameraOrdinal, reason,
                            conflict || incomplete ? LogType.Warning : LogType.Information,
                            ("expectedCameras", group.ExpectedCameraCount), ("receivedCameras", group.Frames.Count),
                            ("frames", string.Join("|", group.Frames.Select(frame =>
                                $"{frame.Key}:{frame.Value.Args.Barcode}:{frame.Value.Args.ScanTime:O}"))));
                        ProcessLowerMachineCameraPair(slot, result.Args, result.IsNoRead, cameraOrdinal,
                            forceDiscard: conflict || incomplete, discardReason: reason);
                    }
                }
                catch (Exception exception)
                {
                    JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Write("camera.fusion.error",
                        "原小车多相机融合处理异常", LogType.Exception, slot.PackageTimestamp, sequence: slot.CartId.ToString(),
                        fields: [("reasonCode", "FusionProcessingFailed"), ("triggerSequence", slot.TriggerSequence),
                            ("cameraOrdinal", cameraOrdinal), ("createdAt", slot.CreatedAt), ("error", exception.ToString())]);
                    QueueError("处理下位机原小车融合结果失败", exception);
                }
                finally
                {
                    foreach (var frame in group.Frames.Values)
                    {
                        ReleaseLowerMachineCameraResult(frame.Args);
                    }
                }
            }
        }

        /// <summary>过滤和丢弃也消费固定原车位置，不按窗口搜索仍存活的下一辆车。</summary>
        private void ProcessLowerMachineCameraPair(LowerMachineCameraSlot slot, BarcodeReadEventArgs args,
            bool isNoRead, long cameraOrdinal, bool forceDiscard = false, string? discardReason = null)
        {
            LogLowerMachineCamera("camera.paired", slot, args, cameraOrdinal, "OriginalTriggerOrder", LogType.Information,
                ("isNoRead", isNoRead));
            if (slot.CreatedAt is not { } originalCreatedAt)
            {
                LogLowerMachineCamera("camera.pair.rejected", slot, args, cameraOrdinal, "CreationRejected", LogType.Warning);
                QueueBarcodeAssignmentRejected("lower-machine-camera", args.ScanTime,
                    new BarcodeAssignmentRejection(BarcodeAssignmentRejectionReason.OriginalPackageUnavailable,
                        args.ScanTime, DateTime.Now, null, null), args.Barcode);
                return;
            }
            var settings = _createPackageSettingsDto;
            var expiry = settings is { IsUseEmptyPackageExpiry: true, EmptyPackageExpiryTime: > 0 }
                ? settings.EmptyPackageExpiryTime : (int?)null;
            if (forceDiscard || args.IsFiltered || isNoRead && ShouldSuppressNoRead(args.ScanTime))
            {
                var reason = forceDiscard ? discardReason ?? "FusionRejected" : args.IsFiltered ? "BarcodeFiltered" :
                    !settings.IsUseNoRead ? "NoReadDisabled" : "NoReadFiltered";
                _packageSessionStore.TryConsumeCameraResultForPackage(originalCreatedAt, args.ScanTime,
                    settings.IsUseBarcodeAssignmentInterval, settings.MinimumAssignmentTime, settings.MaximumAssignmentTime,
                    expiry, DateTime.Now, assignment: null, out var rejection);
                if (rejection is not null)
                {
                    LogLowerMachineCamera("camera.pair.rejected", slot, args, cameraOrdinal, rejection.Reason.ToString(),
                        LogType.Warning, ("businessDiscardReason", reason));
                    QueueBarcodeAssignmentRejected("lower-machine-camera", args.ScanTime, rejection, args.Barcode);
                }
                else LogLowerMachineCamera("package.discarded", slot, args, cameraOrdinal, reason,
                    forceDiscard ? LogType.Warning : LogType.Information, ("resultConsumed", true), ("useNoRead", settings.IsUseNoRead));
                return;
            }

            BindLowerMachineCameraPair(slot, args, settings, expiry, cameraOrdinal);
        }

        /// <summary>校验通过后才构造条码模型和图片句柄；校验失败只丢弃原车及本次结果。</summary>
        private void BindLowerMachineCameraPair(LowerMachineCameraSlot slot, BarcodeReadEventArgs args,
            CreatePackageSettingsDto settings, int? expiry, long cameraOrdinal)
        {
            JayTom.Dws.Abstractions.Imaging.ImageHandle? replacedImage = null;
            var package = _packageSessionStore.TryConsumeCameraResultForPackage(slot.CreatedAt!.Value, args.ScanTime,
                settings.IsUseBarcodeAssignmentInterval, settings.MinimumAssignmentTime, settings.MaximumAssignmentTime,
                expiry, DateTime.Now, target =>
                {
                    replacedImage = target.Image;
                    target.BarCodeInfo = new BarCodeInfoModel
                    {
                        Barcode = args.Barcode,
                        SerialNumber = args.CameraSerialNumber,
                        DisplayIdentifier = args.CameraSerialNumber,
                        ScanTime = args.ScanTime,
                        Source = SourceType.Camera,
                        BindTime = DateTime.Now
                    };
                    target.Image = JayTom.Dws.Abstractions.Imaging.ImageHandle.TakeOwnershipIfPresent(args.Image);
                    args.Image = null;
                }, out var rejection);
            if (package is not null)
            {
                replacedImage?.Dispose();
                QueueBarcodeAssignmentSelected(package, args.ScanTime, lowerMachineOrder: true, cameraOrdinal, slot.TriggerSequence);
                ProcessPackageValueChanged(package, TriggerPositionEnum.BarCodeSetValueAfter);
            }
            else
            {
                LogLowerMachineCamera("camera.pair.rejected", slot, args, cameraOrdinal,
                    rejection?.Reason.ToString() ?? "OriginalPackageUnavailable", LogType.Warning);
                QueueBarcodeAssignmentRejected("lower-machine-camera", args.ScanTime, rejection, args.Barcode);
            }
        }

        /// <summary>在决策当时保存相机和原车的标量证据，不读取后续可能被复用的小车对象。</summary>
        private void LogLowerMachineCamera(string stage, LowerMachineCameraSlot slot, BarcodeReadEventArgs args,
            long cameraOrdinal, string reasonCode, LogType type, params (string Key, object? Value)[] additionalFields)
        {
            var settings = _createPackageSettingsDto;
            (string Key, object? Value)[] fields =
            [
                ("reasonCode", reasonCode), ("camera", args.CameraSerialNumber), ("cameraOrdinal", cameraOrdinal),
                ("triggerSequence", slot.TriggerSequence), ("createdAt", slot.CreatedAt), ("creationTimeTicks", slot.CreatedAt?.Ticks),
                ("observedAt", args.ScanTime), ("processingAt", DateTime.Now), ("triggerIndex", args.TriggerIndex),
                ("observedAgeMilliseconds", slot.CreatedAt is { } createdAt
                    ? (args.ScanTime.Ticks - createdAt.Ticks) / (decimal)TimeSpan.TicksPerMillisecond : (decimal?)null),
                ("frameNo", args.FrameNo), ("sdkFrameNo", args.SdkFrameNo), ("filtered", args.IsFiltered),
                ("windowEnabled", settings.IsUseBarcodeAssignmentInterval), ("minimumMilliseconds", settings.MinimumAssignmentTime),
                ("maximumMilliseconds", settings.MaximumAssignmentTime), ("pendingEvents", _packageEventDispatcher.PendingCount),
                ("pendingCameraSlots", _lowerMachineCameraSlots.Count), ("earlyCameraResults", _earlyLowerMachineCameraResults.Count),
                .. additionalFields
            ];
            JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Write(stage, "相机最终结果消费决策", type,
                slot.PackageTimestamp, args.Barcode, slot.TriggerSequence > 0 ? slot.CartId.ToString() : null, fields: fields);
        }

        /// <summary>释放尚未交给包裹的 SDK 图片，转交成功的结果已经清空图片引用。</summary>
        private static void ReleaseLowerMachineCameraResult(BarcodeReadEventArgs args)
        {
            args.Image?.Dispose();
            args.Image = null;
        }

        /// <summary>仅在工作器停止后释放消费位置、提前结果和多相机组，不因单包超时清空流水线。</summary>
        private void DisposeLowerMachineCameraState()
        {
            JayTom.Dws.Client.Observability.PipelineFileLog.Instance.Write("package.pipeline.reset", "组包工作器停止并释放待处理相机状态",
                fields: [("reasonCode", "PipelineStopped"), ("pendingCameraSlots", _lowerMachineCameraSlots.Count),
                    ("earlyCameraResults", _earlyLowerMachineCameraResults.Count), ("pendingFusionGroups", _lowerMachineCameraGroups.Count),
                    ("triggerCount", _lowerMachineTriggerSequence), ("nextCameraOrdinal", _nextLowerMachineCameraSequence)]);
            while (_earlyLowerMachineCameraResults.TryDequeue(out var result))
            {
                ReleaseLowerMachineCameraResult(result.Args);
            }
            foreach (var group in _lowerMachineCameraGroups.Values)
            {
                group.Deadline?.Dispose();
                foreach (var frame in group.Frames.Values)
                {
                    ReleaseLowerMachineCameraResult(frame.Args);
                }
            }
            _lowerMachineCameraSlots.Clear();
            _lowerMachineCameraGroups.Clear();
            _lowerMachineCameraObservations.Clear();
            _nextLowerMachineCameraSequence = 1;
            _lowerMachineTriggerSequence = 0;
        }


    }
}
