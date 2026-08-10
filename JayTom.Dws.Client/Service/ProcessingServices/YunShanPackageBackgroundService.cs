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
        private readonly IImageStorageService _imageStorageService;
        private readonly ISettingsStore _settingsStore;
        private readonly ISortingService _sortingService;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly IExternalDataService _externalDataService;
        private CreatePackageSettingsDto _createPackageSettingsDto = new();
        private readonly ConcurrentDictionary<string, BarCodeFrameInfo> _barCodeFrameInfoItem = new();
        /// <summary>
        /// 复用用户配置的条码正则，避免在相机热回调中重复编译。
        /// </summary>
        private readonly ConcurrentDictionary<string, Regex> _regexCache = new(StringComparer.Ordinal);
        private BarcodeFilterSettingsDto _barcodeFilterSettingsDto = new();
        private readonly SemaphoreSlim _createPackageSlim = new(1, 1);
        private DateTime _lastNoReadTime = DateTime.MinValue;
        private ICamera[] _cameras = [];
        private int _isWindowsClose;
        private WeightSettingsDto _weightSettingsDto = new();
        private WdtWmsApiDto _wdtWmsApiDto = new();

        public YunShanPackageBackgroundService(IDeviceService deviceService,
            IImageStorageService imageStorageService,
            ISettingsStore settingsStore,
            ISortingService sortingService,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IExternalDataService externalDataService)
        {
            _deviceService = deviceService;
            _imageStorageService = imageStorageService;
            _settingsStore = settingsStore;
            _sortingService = sortingService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _externalDataService = externalDataService;
            _deviceService.CameraInitialized += (_, cameras) =>
            {
                Volatile.Write(ref _cameras, [.. cameras]);
            };

            //条码返回
            _deviceService.BarcodeScanned += async delegate (object? sender, BarcodeReadEventArgs args)
            {
                //验证多条码
                try
                {
                    await _createPackageSlim.WaitAsync();
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
                            Image = args.Image
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

                        var info = PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: not null } &&
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
                                PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                                PackageInfoManager.GetLastPackage(f => f.Value is { BarCodeInfo: null } &&
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
                                Image = args.Image,
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
                                Image? replacedImage;
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
                                    packageInfo.Image = args.Image;
                                }
                                if (!ReferenceEquals(replacedImage, args.Image))
                                {
                                    replacedImage?.Dispose();
                                }
                                EventAggregator.Instance.Publish(new TriggerPositionEvent()
                                {
                                    IsSuccess = true,
                                    TriggerPosition = TriggerPositionEnum.BarCodeSetValueAfter,
                                    PackageInfo = packageInfo,
                                });
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
                finally
                {
                    _createPackageSlim.Release();
                }
            };
            //空包裹
            _deviceService.NotBarcodeHitEvent += async delegate (object? sender, BarcodeReadEventArgs args)
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
                            Image = args.Image
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
                                PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                                PackageInfoManager.GetLastPackage(f => f.Value is { BarCodeInfo: null });
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
                                Image = args.Image,
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
                                Image? replacedImage;
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
                                    packageInfo.Image = args.Image;
                                }
                                if (!ReferenceEquals(replacedImage, args.Image))
                                {
                                    replacedImage?.Dispose();
                                }
                                EventAggregator.Instance.Publish(new TriggerPositionEvent()
                                {
                                    IsSuccess = true,
                                    TriggerPosition = TriggerPositionEnum.BarCodeSetValueAfter,
                                    PackageInfo = packageInfo
                                });
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
                catch (Exception exception)
                {
                    QueueError("处理下位机创建包裹事件失败", exception);
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
                    //测试间隔200,记得删掉

                    var tryParse = int.TryParse(args.Keyword, out var num);
                    if (tryParse)
                    {
                        var packageInfo = PackageInfoManager.GetPackage(f => f.Value != null && f.Value.Guid.Equals(num));

                        if (packageInfo is not null)
                        {
                            EventAggregator.Instance.Publish(new InstructionReceived()
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
                            /*EventAggregator.Instance.Publish(new CallBackPackageInfo {
                                CallBackTime = DateTime.Now,
                                PackageCreateTime = keyValuePair.Value.CreateTime,
                                PackageInfo = keyValuePair.Value,
                                InstructionContent = args.Instruction,
                            });*/
                            if (_createPackageSettingsDto.PackageRemoveMethods == PackageRemoveMethodsEnum.LowerMachineRemoval)
                            {
                                PackageInfoManager.RemovePackage(packageInfo.CreateTime, "下位机移除");
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
                        var packageInfo = PackageInfoManager.GetPackage(f => f.Value != null && f.Value.Guid.Equals(num));
                        if (packageInfo is not null)
                        {
                            EventAggregator.Instance.Publish(new InstructionReceived()
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
                catch (Exception exception)
                {
                    QueueError("处理包裹异常事件失败", exception);
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
                            PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: null } &&
                                                               (!_createPackageSettingsDto.IsUseBarcodeAssignmentInterval ||
                                                                (DateTime.Now.Subtract(f.Key).TotalMilliseconds >= _createPackageSettingsDto.MinimumAssignmentTime &&
                                                                 DateTime.Now.Subtract(f.Key).TotalMilliseconds <= _createPackageSettingsDto.MaximumAssignmentTime)
                                                               )) :
                            PackageInfoManager.GetLastPackage(f => f.Value is { BarCodeInfo: null } &&
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
                                FormattedHeight = args.Height,
                                FormattedLength = args.Length,
                                FormattedVolume = args.Volume,
                                FormattedWidth = args.Width,
                                SourceType = SourceType.Input,
                                OriginalText = args.SourceContent
                            },
                            Other = boxBarCode,
                            CreateTime = DateTime.Now,
                            IsCreatedByLowerMachine = false,
                            IsSavedImage = true,
                        };

                        EventAggregator.Instance.Publish(new TriggerPositionEvent
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
                                    FormattedHeight = args.Height,
                                    FormattedLength = args.Length,
                                    FormattedVolume = args.Volume,
                                    FormattedWidth = args.Width,
                                    SourceType = SourceType.Input,
                                    OriginalText = args.SourceContent
                                };
                                packageInfo.Other = boxBarCode;
                            }
                            EventAggregator.Instance.Publish(new TriggerPositionEvent
                            {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.ExternalDataInputAfter,
                                PackageInfo = packageInfo
                            });
                        }
                    }
                }
                catch (Exception exception)
                {
                    QueueError("处理外部数据输入事件失败", exception);
                }
                finally
                {
                    _createPackageSlim.Release();
                }
            };
            //称重
            _deviceService.StableWeight += async delegate (object? sender, StableWeightEventArgs args)
            {
                if (args.Weight <= 0)
                {
                    return;
                }

                try
                {
                    await _createPackageSlim.WaitAsync();
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            PackageInfoManager.GetPackage(f => f.Value is { IsCompleted: false, WeightInfo: null }) :
                            PackageInfoManager.GetLastPackage(f => f.Value is { IsCompleted: false, WeightInfo: null });
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
                            EventAggregator.Instance.Publish(new TriggerPositionEvent()
                            {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.WeightSetValueAfter,
                                PackageInfo = packageInfo
                            });
                        }
                    }
                }
                catch (Exception exception)
                {
                    QueueError("处理称重事件失败", exception);
                }
                finally
                {
                    _createPackageSlim.Release();
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
            //创建包裹后触发
            EventAggregator.Instance.Subscribe<TriggerPositionEvent>(item =>
            {
                if (item is { TriggerPosition: TriggerPositionEnum.PackageTrigger, PackageInfo: { } packageInfo })
                {
                    var info = PackageInfoManager.GetLastPackage(f => f is { Value: not null });

                    if (info is not null &&
                        packageInfo.CreateTime.Subtract(info.CreateTime).TotalMilliseconds <
                        _createPackageSettingsDto.PackageCreationInterval)
                    {
                        packageInfo.TakeImage()?.Dispose();
                        return;
                    }

                    packageInfo.Timestamp = new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds();
                    //判断是否称重
                    if (_weightSettingsDto.Mode == WeightMode.None)
                    {
                        packageInfo.WeightInfo = new WeightInfoModel();
                    }
                    //判断是否需要体积(直接不使用)
                    packageInfo.VolumeInfo = new VolumeInfoModel();
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
                    PackageInfoManager.AddPackage(packageInfo, packageRemoveTimers);
                    var addedPackage = PackageInfoManager.GetPackage(
                        pair => pair.Key.Equals(packageInfo.CreateTime));
                    if (!ReferenceEquals(addedPackage, packageInfo))
                    {
                        packageInfo.TakeImage()?.Dispose();
                        packageInfo.DisposeTimers();
                        return;
                    }
                    //触发创建包裹事件
                    EventAggregator.Instance.Publish(new TriggerPositionEvent()
                    {
                        IsSuccess = true,
                        TriggerPosition = TriggerPositionEnum.CreateTimePackageAfter,
                        PackageInfo = packageInfo
                    });
                }
                else if (item is { PackageInfo: { BarCodeInfo: not null, WeightInfo: not null } info, TriggerPosition: TriggerPositionEnum.BarCodeSetValueAfter or TriggerPositionEnum.WeightSetValueAfter or TriggerPositionEnum.ExternalDataInputAfter })
                {
                    PackageInfoManager.CompletedPackage(f => f.Key.Equals(info.CreateTime));
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
                        PackageInfoManager.ClearAllPackages();
                    }
                }
            });

            //移除包裹事件
            PackageInfoManager.PackageRemoved += (sender, args) =>
            {
                EventAggregator.Instance.Publish(new TriggerPositionEvent()
                {
                    IsSuccess = true,
                    TriggerPosition = TriggerPositionEnum.RemovePackageAfter,
                    PackageInfo = args.RemovedPackage,
                    Description = args.Description
                });
            };
            PackageInfoManager.PackageCompleted += (sender, args) =>
            {
                //执行输出
                if (args.CompletedPackage?.BarCodeInfo is not null &&
                    args.CompletedPackage?.WeightInfo is not null &&
                    args.CompletedPackage?.VolumeInfo is not null)
                {
                    EventAggregator.Instance.Publish(args.CompletedPackage);
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

        private async Task SwapSettingsAsync(Action update)
        {
            await _createPackageSlim.WaitAsync();
            try
            {
                update();
            }
            finally
            {
                _createPackageSlim.Release();
            }
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
                    if (PackageInfoManager.GetPackageCount() > 0 && _deviceService.RunningStatus)
                    {
                        //判断存图路径等于空
                        var codeInfo = PackageInfoManager.GetPackage(f => f.Value is
                        {
                            IsSavedImage: false,
                            BarCodeInfo: not null, IsCompleted: true
                        });
                        //存图
                        if (codeInfo is not null)
                        {
                            ImageMessageInfo? imageMessage = null;
                            Image? image = null;
                            lock (codeInfo.SyncRoot)
                            {
                                if (codeInfo.Image is not null && !codeInfo.IsSavedImage)
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
                                        Weight = (float)(codeInfo.WeightInfo?.FormattedWeight ?? 0),
                                        Height = (float)(codeInfo.VolumeInfo?.FormattedHeight ?? 0),
                                        Image = image,
                                        Length = (float)(codeInfo.VolumeInfo?.FormattedLength ?? 0),
                                        Width = (float)(codeInfo.VolumeInfo?.FormattedWidth ?? 0),
                                        Volume = (float)(codeInfo.VolumeInfo?.FormattedVolume ?? 0),
                                        ScanTime = codeInfo.BarCodeInfo?.ScanTime ?? DateTime.Now,
                                        Type = SaveImageType.BarcodeImage,
                                        CameraName = camera?.Info?.Name ?? string.Empty,
                                        CameraCustomName = camera?.Info?.CustomName ?? string.Empty,
                                    };
                                    codeInfo.IsSavedImage = true;
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
                                        codeInfo.IsSavedImage = false;
                                    }
                                    throw;
                                }
                            }
                        }

                        //移除包裹
                        if (_createPackageSettingsDto.PackageRemoveMethods ==
                            PackageRemoveMethodsEnum.FillInformation)
                        {
                            var packageInfos = PackageInfoManager.GetPackages(w =>
                                w.Value is { IsCompleted: true, IsSavedImage: true } &&
                                (w.Value.PanoramaCameraImageInfo.All(info => info.IsExists) ||
                                 DateTime.Now.Subtract(w.Value.CreateTime)
                                     .TotalMinutes > 5)) ?? new List<PackageInfo>();

                            foreach (var kvp in packageInfos)
                            {
                                PackageInfoManager.RemovePackage(kvp.CreateTime, "填充完整信息移除");
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
