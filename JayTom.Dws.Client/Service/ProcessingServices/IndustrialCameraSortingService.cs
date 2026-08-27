using JayTom.Dws.Application.Configuration;
using NLog;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Camera;
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
using JayTom.Dws.Client.Service.ExternalDataService;

namespace JayTom.Dws.Client.Service.ProcessingServices
{

    public class IndustrialCameraSortingService : Microsoft.Extensions.Hosting.BackgroundService
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
        private IReadOnlyList<ICamera> _cameras = Array.Empty<ICamera>();
        private CreatePackageSettingsDto _createPackageSettingsDto = new();
        private SemaphoreSlim _createPackageSlim = new(1);
        private DateTime _lastReadTime = DateTime.Now;
        private DateTime _lastNoReadTime = DateTime.Now;
        private static bool _isWindowsClose;

        public IndustrialCameraSortingService(IPackageSessionStore packageSessionStore,
            IDeviceService deviceService,
            IImageStorageService imageStorageService,
            ISettingsStore settingsStore,
            ISortingService sortingService,
            IExternalDataService externalDataService,
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            _packageSessionStore = packageSessionStore;
            _deviceService = deviceService;
            _imageStorageService = imageStorageService;
            _settingsStore = settingsStore;
            _sortingService = sortingService;
            _externalDataService = externalDataService;

            //相机
            _deviceService.CameraInitialized += delegate (object? sender, IReadOnlyList<ICamera> list)
            {
                _cameras = list;
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
                    NLog.LogManager.GetCurrentClassLogger().Error($"条码返回:{args.Barcode}");
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

                            _eventBus.Publish(new TriggerPositionEvent()
                            {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.PackageTrigger,
                                PackageInfo = packageInfo
                            });
                            _eventBus.Publish(new InstructionReceived()
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
            //下位机(移除包裹)
            _sortingService.RemovePackageEvent += async delegate (object? sender, PackageInstructionEventArgs args)
            {
                /*//测试,记得删
                return;*/

                try
                {
                    await Task.Delay(200);
                    await _createPackageSlim.WaitAsync();

                    var tryParse = int.TryParse(args.Keyword, out var num);
                    if (tryParse)
                    {
                        var packageInfo = _packageSessionStore.GetPackage(f => f.Value != null && f.Value.Guid.Equals(num));

                        if (packageInfo is not null)
                        {
                            _eventBus.Publish(new InstructionReceived()
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
                            });
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
                            LogManager.GetCurrentClassLogger().Error($"序号匹配包裹失败,序号:{num},原文:{args.Keyword}");
                        }
                    }
                    else
                    {
                        LogManager.GetCurrentClassLogger().Error($"关键字节转数字失败:{args.Keyword}");
                    }
                }
                finally
                {
                    _createPackageSlim.Release();
                }

                //其他协议
            };
            //下位机(包裹异常)
            _sortingService.PackageException += async (sender, args) =>
            {
                try
                {
                    await Task.Delay(500);
                    await _createPackageSlim.WaitAsync();
                    var tryParse = int.TryParse(args.Keyword, out var num);
                    if (tryParse)
                    {
                        var packageInfo = _packageSessionStore.GetPackage(f => f.Value != null && f.Value.Guid.Equals(num));
                        if (packageInfo is not null)
                        {
                            _eventBus.Publish(new InstructionReceived()
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
            };
            _eventBus.SubscribeAsync<WindowsAction>(async item =>
            {
                if (item is { Type: WindowsActionType.Close })
                {
                    _isWindowsClose = true;
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

                    //空条码赋值

                    if (_createPackageSettingsDto is { IsUseBarcodeAssignmentInterval: true, MaximumAssignmentTime: > 0 })
                    {
                        packageRemoveTimers.Add(new PackageAssignmentTimer()
                        {
                            AssignmentTimeSpan = TimeSpan.FromMilliseconds(_createPackageSettingsDto.MaximumAssignmentTime + 10),
                            Predicate = w => w.Value.BarCodeInfo == null,
                            AssignmentCallback = a =>
                            {
                                a.BarCodeInfo = new BarCodeInfoModel()
                                {
                                    Barcode = "NoRead",
                                    BindTime = DateTime.Now,
                                    ScanTime = DateTime.Now,
                                    SerialNumber = "Null",
                                    Source = SourceType.None
                                };
                                if (a.BarCodeInfo is not null)
                                {
                                    _packageSessionStore.CompletePackage(f => f.Key.Equals(a.CreateTime));
                                }
                                return false;
                            },
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
                else if (item is { PackageInfo: { BarCodeInfo: not null } info, TriggerPosition: TriggerPositionEnum.BarCodeSetValueAfter })
                {
                    _packageSessionStore.CompletePackage(f => f.Key.Equals(info.CreateTime));
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                //读配置

                _createPackageSettingsDto = await _settingsStore.GetAsync<CreatePackageSettingsDto>("CreatePackageSettings", stoppingToken) ?? new CreatePackageSettingsDto();
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
                                PackageTimestamped = codeInfo.Timestamp,
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
