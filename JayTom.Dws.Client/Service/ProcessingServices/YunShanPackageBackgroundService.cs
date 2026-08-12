using JayTom.Dws.Application.Configuration;
using NLog;
using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Camera;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Model;
using JayTom.Dws.Domain.Manager;
using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.ApiDto;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Application.Workflows;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.Service.ProcessingServices
{

    /// <summary>
    /// 云山分拣机项目
    /// </summary>
    public class YunShanPackageBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
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
        private readonly IExternalDataService _externalDataService;
        private volatile CreatePackageSettingsDto _createPackageSettingsDto = new();
        private readonly ConcurrentDictionary<string, BarCodeFrameInfo> _barCodeFrameInfoItem = new();
        /// <summary>
        /// 复用用户配置的条码正则，避免在相机热回调中重复编译。
        /// </summary>
        private readonly ConcurrentDictionary<string, Regex> _regexCache = new(StringComparer.Ordinal);
        private volatile BarcodeFilterSettingsDto _barcodeFilterSettingsDto = new();
        /// <summary>串行处理创建、扫码、空读、回调和赋值的关键队列。</summary>
        private readonly NonBlockingOrderedDispatcher<Action> _packageEventDispatcher;
        /// <summary>隔离日志、UI、持久化、API和输出订阅者的通知队列。</summary>
        private readonly NonBlockingOrderedDispatcher<Action> _packageNotificationDispatcher;
        private DateTime _lastNoReadTime = DateTime.MinValue;
        private ICamera[] _cameras = [];
        private int _isWindowsClose;
        private volatile WeightSettingsDto _weightSettingsDto = new();
        private volatile WdtWmsApiDto _wdtWmsApiDto = new();

        public YunShanPackageBackgroundService(IPackageSessionStore packageSessionStore,
            IDeviceService deviceService,
            IImageStorageService imageStorageService,
            ISettingsStore settingsStore,
            ISortingService sortingService,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IExternalDataService externalDataService)
        {
            _packageSessionStore = packageSessionStore;
            _deviceService = deviceService;
            _imageStorageService = imageStorageService;
            _settingsStore = settingsStore;
            _sortingService = sortingService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _externalDataService = externalDataService;
            _packageEventDispatcher = new NonBlockingOrderedDispatcher<Action>(
                static work =>
                {
                    work();
                },
                static (_, exception) => QueueError("执行关键包裹事件失败", exception));
            _packageNotificationDispatcher = new NonBlockingOrderedDispatcher<Action>(
                static notification =>
                {
                    notification();
                },
                static (_, exception) => QueueError("发布包裹通知失败", exception));
            _deviceService.CameraInitialized += (_, cameras) =>
            {
                Volatile.Write(ref _cameras, [.. cameras]);
            };

            //条码返回
            _deviceService.BarcodeScanned += delegate (object? sender, BarcodeReadEventArgs args)
            {
                if (!TryQueuePackageEvent(() =>
                {
                    //验证多条码
                    try
                    {
                    if (Volatile.Read(ref _cameras)
                            .Count(c => c.BindingType == CameraBindingType.ScannerCamera) > 1)
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

                        _barCodeFrameInfoItem.AddOrUpdate(
                            args.CameraSerialNumber,
                            barCodeFrameInfo,
                            (_, oldValue) =>
                            {
                                oldValue.Image?.Dispose();
                                return barCodeFrameInfo;
                            });
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
                            ProcessPackageTrigger(packageInfo);
                        }
                        else
                        {
                            if (packageInfo is not null)
                            {
                                JayTom.Dws.Abstractions.Imaging.ImageHandle? replacedImage;
                                lock (packageInfo.SyncRoot)
                                {
                                    replacedImage = packageInfo.Image;
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
                                }
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
                    if (Volatile.Read(ref _cameras)
                            .Count(c => c.BindingType == CameraBindingType.ScannerCamera) > 1)
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
                        _barCodeFrameInfoItem.AddOrUpdate(
                            args.CameraSerialNumber,
                            barCodeFrameInfo,
                            (_, oldValue) =>
                            {
                                if (!string.Equals(
                                        oldValue.BarCodeInfo?.Barcode,
                                        "noread",
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    barCodeFrameInfo.Image?.Dispose();
                                    return oldValue;
                                }

                                oldValue.Image?.Dispose();
                                return barCodeFrameInfo;
                            });
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
                            ProcessPackageTrigger(packageInfo);
                        }
                        else
                        {
                            if (packageInfo is not null)
                            {
                                JayTom.Dws.Abstractions.Imaging.ImageHandle? replacedImage;
                                lock (packageInfo.SyncRoot)
                                {
                                    replacedImage = packageInfo.Image;
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
                                }
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

                            ProcessPackageTrigger(packageInfo);
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
                        var packageInfo = _packageSessionStore.GetPackage(f => f.Value != null && f.Value.Guid.Equals(num));

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
                            /*EventAggregator.Instance.Publish(new CallBackPackageInfo {
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
                        var packageInfo = _packageSessionStore.GetPackage(f => f.Value != null && f.Value.Guid.Equals(num));
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
                    var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            _packageSessionStore.GetPackage(f => f.Value is { BarCodeInfo: null } &&
                                                               (!_createPackageSettingsDto.IsUseBarcodeAssignmentInterval ||
                                                                (DateTime.Now.Subtract(f.Key).TotalMilliseconds >= _createPackageSettingsDto.MinimumAssignmentTime &&
                                                                 DateTime.Now.Subtract(f.Key).TotalMilliseconds <= _createPackageSettingsDto.MaximumAssignmentTime)
                                                               )) :
                            _packageSessionStore.GetLastPackage(f => f.Value is { BarCodeInfo: null } &&
                                                                   (!_createPackageSettingsDto.IsUseBarcodeAssignmentInterval ||
                                                                    (DateTime.Now.Subtract(f.Key).TotalMilliseconds >= _createPackageSettingsDto.MinimumAssignmentTime &&
                                                                     DateTime.Now.Subtract(f.Key).TotalMilliseconds <= _createPackageSettingsDto.MaximumAssignmentTime)
                                                                   ));
                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.TcpInput) ==
                        PackageCreationMethodsEnum.TcpInput && packageInfo is null)
                    {
                        packageInfo = new PackageInfo
                        {
                            Guid = timestamp,
                            BarCodeInfo = new BarCodeInfoModel
                            {
                                Barcode = barCode,
                                ScanTime = DateTime.Now,
                                Source = SourceType.Input,
                            },
                            WeightInfo = new WeightInfoModel
                            {
                                CreateTime = DateTime.Now,
                                FormattedWeight = args.Weight,
                                SourceType = SourceType.Input,
                                OriginalText = args.SourceContent
                            },
                            VolumeInfo = new VolumeInfoModel
                            {
                                CreateTime = DateTime.Now,
                                FormattedHeight = Convert.ToDecimal(args.Height),
                                FormattedLength = Convert.ToDecimal(args.Length),
                                FormattedVolume = Convert.ToDecimal(args.Volume),
                                FormattedWidth = Convert.ToDecimal(args.Width),
                                SourceType = SourceType.Input,
                                OriginalText = args.SourceContent
                            },
                            Other = boxBarCode,
                            CreateTime = DateTime.Now,
                            IsCreatedByLowerMachine = false,
                            IsImageSaveRequested = true,
                        };

                        ProcessPackageTrigger(packageInfo);
                    }
                    else
                    {
                        if (packageInfo is not null)
                        {
                            lock (packageInfo.SyncRoot)
                            {
                                packageInfo.BarCodeInfo = new BarCodeInfoModel
                                {
                                    Barcode = barCode,
                                    ScanTime = DateTime.Now,
                                    Source = SourceType.Input,
                                };
                                packageInfo.WeightInfo = new WeightInfoModel
                                {
                                    CreateTime = DateTime.Now,
                                    FormattedWeight = args.Weight,
                                    SourceType = SourceType.Input,
                                    OriginalText = args.SourceContent
                                };
                                packageInfo.VolumeInfo = new VolumeInfoModel
                                {
                                    CreateTime = DateTime.Now,
                                    FormattedHeight = Convert.ToDecimal(args.Height),
                                    FormattedLength = Convert.ToDecimal(args.Length),
                                    FormattedVolume = Convert.ToDecimal(args.Volume),
                                    FormattedWidth = Convert.ToDecimal(args.Width),
                                    SourceType = SourceType.Input,
                                    OriginalText = args.SourceContent
                                };
                                packageInfo.Other = boxBarCode;
                            }
                            ProcessPackageValueChanged(
                                packageInfo,
                                TriggerPositionEnum.ExternalDataInputAfter);
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
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item =>
            {
                if (item is { } model)
                {
                    switch (model.SettingsName)
                    {
                        case "CreatePackageSettings":
                            {
                                var settings = await _settingsStore.GetAsync<CreatePackageSettingsDto>(model.SettingsName) ??
                                               new CreatePackageSettingsDto();
                                await SwapSettingsAsync(() => _createPackageSettingsDto = settings);
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
            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(item =>
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
                    EventAggregator.Instance.Publish(new TriggerPositionEvent()
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
                if (args.CompletedPackage?.BarCodeInfo is not null &&
                    args.CompletedPackage?.WeightInfo is not null &&
                    args.CompletedPackage?.VolumeInfo is not null)
                {
                    QueuePackageNotification(
                        () => EventAggregator.Instance.Publish(args.CompletedPackage),
                        "包裹完成");
                }
            };
            EventAggregator.Instance.Subscribe<WindowsAction>(item =>
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
        private static void QueueError(string message, Exception? exception = null)
        {
            EventAggregator.Instance.Publish(new AppLogInfoModel
            {
                CreateTime = DateTime.Now,
                Type = LogType.Exception,
                Message = exception is null ? message : $"{message}:{exception}"
            });
        }

        /// <summary>
        /// 在关键队列内同步完成防抖、会话创建和400毫秒计时器启动；通知随后异步发布。
        /// </summary>
        private void ProcessPackageTrigger(PackageInfo packageInfo)
        {
            var info = _packageSessionStore.GetLastPackage(f => f is { Value: not null });
            if (info is not null &&
                packageInfo.CreateTime.Subtract(info.CreateTime).TotalMilliseconds <
                _createPackageSettingsDto.PackageCreationInterval)
            {
                packageInfo.TakeImage()?.Dispose();
                return;
            }

            packageInfo.Timestamp = new DateTimeOffset(packageInfo.CreateTime)
                .ToUnixTimeMilliseconds();
            if (_weightSettingsDto.Mode == WeightMode.None)
            {
                packageInfo.WeightInfo = new WeightInfoModel();
            }
            packageInfo.VolumeInfo = new VolumeInfoModel();

            var packageRemoveTimers = new List<PackageTimer>();
            if (_createPackageSettingsDto is
                { IsUseEmptyPackageExpiry: true, EmptyPackageExpiryTime: > 0 })
            {
                packageRemoveTimers.Add(new PackageRemoveTimer
                {
                    Description = "空包裹过期",
                    Predicate = pair => pair.Value.BarCodeInfo == null,
                    RemovalTimeSpan = TimeSpan.FromMilliseconds(
                        _createPackageSettingsDto.EmptyPackageExpiryTime)
                });
            }
            if (_createPackageSettingsDto is
                { IsUsePackageExpiry: true, PackageExpiryTime: > 0 })
            {
                packageRemoveTimers.Add(new PackageRemoveTimer
                {
                    Description = "包裹超过生存周期",
                    RemovalTimeSpan = TimeSpan.FromMilliseconds(
                        _createPackageSettingsDto.PackageExpiryTime)
                });
            }

            _packageSessionStore.AddPackage(packageInfo, packageRemoveTimers);
            var addedPackage = _packageSessionStore.GetPackage(
                pair => pair.Key.Equals(packageInfo.CreateTime));
            if (!ReferenceEquals(addedPackage, packageInfo))
            {
                packageInfo.TakeImage()?.Dispose();
                packageInfo.DisposeTimers();
                return;
            }

            QueueTriggerNotification(
                packageInfo,
                TriggerPositionEnum.PackageTrigger,
                "包裹触发");
            QueueTriggerNotification(
                packageInfo,
                TriggerPositionEnum.CreateTimePackageAfter,
                "创建包裹完成");
        }

        /// <summary>在关键队列内完成包裹，并将赋值通知移出热路径。</summary>
        private void ProcessPackageValueChanged(
            PackageInfo packageInfo,
            TriggerPositionEnum triggerPosition)
        {
            if (packageInfo.BarCodeInfo is not null && packageInfo.WeightInfo is not null)
            {
                _packageSessionStore.CompletePackage(
                    pair => pair.Key.Equals(packageInfo.CreateTime));
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
                EventAggregator.Instance.Publish(new TriggerPositionEvent
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
                () => EventAggregator.Instance.Publish(instruction),
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //逻辑->外部数据回传后判断是否包含箱子码和包裹条码->如果有则上传并分拣->则上传错误码让它去异常口
            try
            {
                //读配置

                _createPackageSettingsDto = await _settingsStore.GetAsync<CreatePackageSettingsDto>("CreatePackageSettings", stoppingToken) ?? new CreatePackageSettingsDto();
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
                    if (_packageSessionStore.GetPackageCount() > 0 && _deviceService.RunningStatus)
                    {
                        //判断存图路径等于空
                        var codeInfo = _packageSessionStore.GetPackage(f => f.Value is
                        {
                            IsImageSaveRequested: false,
                            BarCodeInfo: not null, IsCompleted: true
                        });
                        //存图
                        if (codeInfo is not null)
                        {
                            ImageMessageInfo? imageMessage = null;
                            JayTom.Dws.Abstractions.Imaging.ImageHandle? image = null;
                            lock (codeInfo.SyncRoot)
                            {
                                if (codeInfo.Image is not null && !codeInfo.IsImageSaveRequested)
                                {
                                    var camera = Volatile.Read(ref _cameras).FirstOrDefault(item =>
                                        string.Equals(
                                            item.Info?.SerialNumber,
                                            codeInfo.BarCodeInfo?.SerialNumber,
                                            StringComparison.Ordinal));
                                    image = codeInfo.TakeImage();
                                    imageMessage = new ImageMessageInfo
                                    {
                                        PackageTimestamped = codeInfo.Timestamp,
                                        BarCode = codeInfo.BarCodeInfo?.Barcode ?? string.Empty,
                                        CameraSerialNumber = codeInfo.BarCodeInfo?.SerialNumber ?? string.Empty,
                                        Weight = (decimal)(codeInfo.WeightInfo?.FormattedWeight ?? 0),
                                        Height = (decimal)(codeInfo.VolumeInfo?.FormattedHeight ?? 0),
                                        Image = image,
                                        Length = (decimal)(codeInfo.VolumeInfo?.FormattedLength ?? 0),
                                        Width = (decimal)(codeInfo.VolumeInfo?.FormattedWidth ?? 0),
                                        Volume = (decimal)(codeInfo.VolumeInfo?.FormattedVolume ?? 0),
                                        ScanTime = codeInfo.BarCodeInfo?.ScanTime ?? DateTime.Now,
                                        Type = SaveImageType.BarcodeImage,
                                        CameraName = camera?.Info?.Name ?? string.Empty,
                                        CameraCustomName = camera?.Info?.CustomName ?? string.Empty,
                                    };
                                    codeInfo.MarkImageSaveRequested();
                                }
                            }
                            if (imageMessage is not null)
                            {
                                try
                                {
                                    EventAggregator.Instance.Publish(imageMessage);
                                }
                                catch
                                {
                                    lock (codeInfo.SyncRoot)
                                    {
                                        if (codeInfo.Image is null)
                                        {
                                            codeInfo.Image = image;
                                        }
                                        else
                                        {
                                            image?.Dispose();
                                        }
                                        codeInfo.ResetImageSaveRequest();
                                    }
                                    throw;
                                }
                            }
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

        /// <summary>
        /// 停止包裹处理工作器并释放尚未转移所有权的相机帧。
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Interlocked.Exchange(ref _isWindowsClose, 1);
            await _packageEventDispatcher.DisposeAsync();
            await _packageNotificationDispatcher.DisposeAsync();
            foreach (var cameraSerialNumber in _barCodeFrameInfoItem.Keys)
            {
                if (_barCodeFrameInfoItem.TryRemove(cameraSerialNumber, out var frameInfo))
                {
                    frameInfo.Image?.Dispose();
                }
            }
            _regexCache.Clear();
            await base.StopAsync(cancellationToken);
        }
    }
}
