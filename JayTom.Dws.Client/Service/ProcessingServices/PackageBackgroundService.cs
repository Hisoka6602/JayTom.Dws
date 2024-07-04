using NLog;
using System;
using DryIoc;
using ImTools;
using System.IO;
using System.Linq;
using System.Drawing;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Camera;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Model;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalConf;
using NPOI.SS.Formula.Functions;
using System.Threading.Channels;
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using NPOI.XSSF.Streaming.Values;
using System.Windows.Media.Media3D;
using System.Collections.Concurrent;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Camera.FilterContainer;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Client.Service.ResultOutput;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Domain.DownstreamProtocols.CommunicationProtocols;
using WindowsAction = JayTom.Dws.Client.EventMediators.WindowsAction;
using ApplicationStatus = JayTom.Dws.Client.EventMediators.ApplicationStatus;
using WindowsActionType = JayTom.Dws.Client.EventMediators.WindowsActionType;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;
using TriggerPositionEvent = JayTom.Dws.Client.EventMediators.TriggerPositionEvent;
using ApplicationStatusChanged = JayTom.Dws.Client.EventMediators.ApplicationStatusChanged;
using BarcodeTypeProviderEvent = JayTom.Dws.Client.EventMediators.BarcodeTypeProviderEvent;

namespace JayTom.Dws.Client.Service.ProcessingServices {

    /// <summary>
    /// 默认分拣机项目
    /// </summary>
    public class PackageBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IDeviceService _deviceService;
        private readonly IResultOutputService _resultOutputService;
        private readonly IImageStorageService _imageStorageService;
        private readonly IExternalDataService _externalDataService;
        private readonly IConfigRepository _configRepository;
        private readonly ISortingService _sortingService;
        private readonly IStackedPackageService _stackedPackageService;
        private readonly IGrayscaleService _grayscaleService;
        private ExternalDataSourceEventArgs _externalDataSource = new();
        private List<ConfigInfoModel> _configInfoModels = new();

        //private CommunicationsSettingsDto _communicationsSettingsDto = new();
        private VolumeSettingsDto _volumeSettingsDto = new();

        private BarcodeFilterSettingsDto _barcodeFilterSettingsDto = new();
        private SupplyCounterSettingsDto _supplyCounterSettingsDto = new();
        private GrayscaleDeviceSettingsDto _grayscaleDeviceSettingsDto = new();
        private WeightSettingsDto _weightSettingsDto = new();
        private CreatePackageSettingsDto _createPackageSettingsDto = new();
        private StackedPackageDetectionSettingsDto _stackedPackageDetectionSettingsDto = new();
        private List<ICamera> _cameras = new();
        private List<PanoramaCameraConfigInfoModel> _panoramaCameras = new();
        private ConcurrentQueue<CameraImageInfo> _panoramaImageItems = new();
        private ConcurrentQueue<CameraImageInfo> _volumeCameraImageItems = new();

        //private ConcurrentDictionary<DateTime, PackageInfo> _packageInfos = new();
        private ConcurrentDictionary<string, BarCodeFrameInfo> _barCodeFrameInfoItem = new();

        private ConcurrentQueue<InstructionsAttach> _instructionsAttachItems = new();

        /// <summary>
        /// 灰度仪跳过的车辆
        /// </summary>
        public int GrayScaleSkippedVehicles { get; set; } = 0;

        /// <summary>
        /// 前置信号是否已回复
        /// </summary>
        //private static bool _isSignalReceived;

        private static bool _isWindowsClose;
        private SemaphoreSlim _createPackageSlim = new(1);
        private SemaphoreSlim _takePackageSlim = new(1);
        private DateTime _lastNoReadTime = DateTime.Now;
        private DateTime _lastReadTime = DateTime.Now;
        private int _preSignal = 1;
        private DateTime _lastStableWeightDateTime = DateTime.Now;

        public PackageBackgroundService(IDeviceService deviceService,
            IResultOutputService resultOutputService,
            IImageStorageService imageStorageService,
            IExternalDataService externalDataService, IConfigRepository configRepository,
            ISortingService sortingService, IPanoramaCameraConfigRepository panoramaCameraConfigRepository,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IStackedPackageService stackedPackageService,
            IGrayscaleService grayscaleService) {
            _deviceService = deviceService;
            _resultOutputService = resultOutputService;
            _imageStorageService = imageStorageService;
            _externalDataService = externalDataService;
            _configRepository = configRepository;
            _sortingService = sortingService;
            _stackedPackageService = stackedPackageService;
            _grayscaleService = grayscaleService;

            //相机
            _deviceService.CameraInitialized += delegate (object? sender, List<ICamera> list) {
                _cameras = list;
                _panoramaCameras = panoramaCameraConfigRepository.Select(s => s.Id > 0, o => o.Id)
                    ?.ConfigureAwait(false).GetAwaiter().GetResult()?.ToList() ?? new List<PanoramaCameraConfigInfoModel>();
                var scannerCameraConfigInfoModels = barcodeScannerCameraConfigRepository.Select(s => s.Id > 0, o => o.Id)
                    ?.ConfigureAwait(false).GetAwaiter().GetResult()?.ToList() ?? new List<BarcodeScannerCameraConfigInfoModel>();
                _cameras.ForEach(f => {
                    if (f.BindingType == CameraBindingType.PanoramaCamera) {
                        f.Info.CustomName =
                            _panoramaCameras.FirstOrDefault(f1 => f1.SerialNumber.Equals(f.Info.SerialNumber))
                                ?.CustomName ?? string.Empty;
                    }
                    else if (f.BindingType == CameraBindingType.ScannerCamera || f.BindingType == CameraBindingType.OcrCamera) {
                        f.Info.CustomName =
                            scannerCameraConfigInfoModels.FirstOrDefault(f1 => f1.SerialNumber.Equals(f.Info.SerialNumber))
                                ?.CustomName ?? string.Empty;
                    }
                });
            };
            //扫码
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
                        //----------
                        /* var packageInfo =
                           _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                               PackageInfoManager.GetPackage(f => f.Value.BarCodeInfo == null) :
                               PackageInfoManager.GetLastPackage(f => f.Value.BarCodeInfo == null);*/

                        //邮政项目临时用
                        var packageInfo =
                            _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                                PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                                PackageInfoManager.GetLastPackage(f => f.Value is { BarCodeInfo: null } && args.ScanTime.Subtract(f.Value.CreateTime).TotalMilliseconds > 100);

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
                            //不支持扫码创建
                            //_packageInfos.OrderBy(o => o.Key).LastOrDefault(f => f.BarCodeInfo == null && DateTime.Now.Subtract(f.CreateTime).TotalMicroseconds > 100);

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
                            else {
                                BarCodeFilterContainer.ResetFilter();
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
                            //不支持扫码创建
                            //_packageInfos.OrderBy(o => o.Key).LastOrDefault(f => f.BarCodeInfo == null && DateTime.Now.Subtract(f.CreateTime).TotalMicroseconds > 100);

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
            //全景相机
            _deviceService.PanoramaCaptured += async delegate (object? sender, PanoramaCaptureEventArgs args) {
                await Task.Yield();
                _panoramaImageItems.Enqueue(new CameraImageInfo() {
                    CameraSerialNumber = args.CameraSerialNumber,
                    Image = args.Image,
                    Barcode = args.Barcode,
                    BarcodeTimestamp = args.BarcodeTimestamp
                });
            };
            //体积相机
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
                            if (args.MeasurementTriggerMode == MeasurementTriggerMode.Continuous) {
                                if (packageInfo is { WeightInfo: not null, BarCodeInfo: not null }) {
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
                            else {
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
                        /*else {
                            _volumeQueueInfos.Enqueue(new VolumeInfoModel {
                                CreateTime = args.Timestamp,
                                FormattedHeight = args.Height,
                                FormattedWidth = args.Width,
                                FormattedLength = args.Length,
                                FormattedVolume = args.Volume,
                                SourceType = SourceType.Camera,
                            });
                        }*/
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
                    //稳定重量创建
                    //需要重量大于0并且和上一个重量至少间隔500
                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.StableWeight) ==
                        PackageCreationMethodsEnum.StableWeight &&
                        (args.Weight < 0 || DateTime.Now.Subtract(_lastStableWeightDateTime).TotalMilliseconds < 800)) {
                        return;
                    }
                    else {
                        _lastStableWeightDateTime = DateTime.Now;
                    }
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
                        /*else {
                            if (_weightSettingsDto.Mode == WeightMode.Dynamic) {
                                _weightQueueInfos.Enqueue(new WeightInfoModel {
                                    CreateTime = DateTime.Now,
                                    FormattedWeight = args.Weight,
                                    SourceType = SourceType.SerialPort,
                                    WeighingMode = WeighingMode.Static
                                });
                            }
                        }*/
                    }
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            //重量清0
            _deviceService.WeightCleared += async (sender, args) => {
                //供包台模式
                if (_supplyCounterSettingsDto.IsUseSupplyCounterMode) {
                    //如果重量为0且没有其他属性数据就需要删除该包裹
                    try {
                        await _createPackageSlim.WaitAsync();
                        var packageInfo =
                            _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                                PackageInfoManager.GetPackage(f => f.Value is { IsCompleted: false, WeightInfo: not null }) :
                                PackageInfoManager.GetLastPackage(f => f.Value is { IsCompleted: false, WeightInfo: not null });

                        if (packageInfo is not null && (packageInfo.VolumeInfo is null || packageInfo.BarCodeInfo is null)) {
                            //移除包裹
                            PackageInfoManager.RemovePackage(packageInfo.CreateTime, "重量置零移除");
                        }
                    }
                    finally {
                        _createPackageSlim.Release();
                    }
                }
            };
            //外部数据源
            _externalDataService.DataSourceEnabled += delegate (object? sender, ExternalDataSourceEventArgs args) {
                _externalDataSource = args;
            };
            //外部全量数据
            _externalDataService.ContentInputReceived += async (sender, args) => {
                await Task.Yield();
                //测试，记得删
                await Task.Delay(10);
                if (!_createPackageSettingsDto.IsUseNoRead &&
                    args.Barcode.ToLower().Equals("noread")) {
                    return;
                }

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
            //输入体积
            _externalDataService.VolumeReceived += async delegate (object? sender, ExternalVolumeInputEventArgs args) {
                try {
                    await _createPackageSlim.WaitAsync();
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            PackageInfoManager.GetPackage(f => f.Value is { VolumeInfo: null }) :
                            PackageInfoManager.GetLastPackage(f => f.Value is { VolumeInfo: null });

                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.VolumeInput) ==
                        PackageCreationMethodsEnum.VolumeInput && packageInfo is null) {
                        packageInfo = new PackageInfo() {
                            Guid = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                            VolumeInfo = new VolumeInfoModel() {
                                CreateTime = DateTime.Now,
                                FormattedHeight = args.Height,
                                FormattedWidth = args.Width,
                                FormattedLength = args.Length,
                                FormattedVolume = args.Volume,
                                SourceType = SourceType.Tcp,
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
                            packageInfo.VolumeInfo = new VolumeInfoModel() {
                                CreateTime = DateTime.Now,
                                FormattedHeight = args.Height - packageInfo.LengthToDeduct,
                                FormattedWidth = args.Width - packageInfo.WidthToDeduct,
                                FormattedLength = args.Length - packageInfo.LengthToDeduct,
                                FormattedVolume = args.Volume - packageInfo.VolumeToDeduct,
                                SourceType = SourceType.Tcp,
                            };
                            EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.VolumeSetValueAfter,
                                PackageInfo = packageInfo
                            });
                        }
                        /*else {
                            _volumeQueueInfos.Enqueue(new VolumeInfoModel {
                                CreateTime = DateTime.Now,
                                FormattedHeight = args.Height,
                                FormattedWidth = args.Width,
                                FormattedLength = args.Length,
                                FormattedVolume = args.Volume,
                                SourceType = SourceType.Tcp,
                            });
                        }*/
                    }

                    EventAggregator.Instance.Publish(new VolumeLogInfoModel() {
                        Type = LogType.Information,
                        Message = $"获取体积信息:{args.Length},{args.Width},{args.Height}",
                        DataSourceType = DataSourceType.ExternalInput
                    });
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            //下位机(创建包裹)
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
            //下位机(包裹异常需要判断操作)
            _sortingService.PackageExceptionEx += async (sender, args) => {
                try {
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
                                        InstructionType = InstructionType.PackageExceptionEx
                                    }
                                },
                                ConnectionName = args.ConnectionName,
                            });
                            //移除包裹
                            if (_createPackageSettingsDto.PackageRemoveMethods == PackageRemoveMethodsEnum.LowerMachineRemoval) {
                                PackageInfoManager.RemovePackage(packageInfo.CreateTime, "下位机移除");
                            }
                        }
                    }
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            //下位机(清空异常)
            _sortingService.ClearExceptionEvent += async delegate (object? sender, string o) {
                try {
                    await _createPackageSlim.WaitAsync();
                    PackageInfoManager.ClearAllPackages();
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            //前置信号回复
            _sortingService.PreSignalReplyReceived += async (sender, args) => {
                if (_supplyCounterSettingsDto is { IsUseSupplyCounterMode: true, SendPreSequenceNumber: true }) {
                    try {
                        await _takePackageSlim.WaitAsync();
                        var info = PackageInfoManager.GetLastPackage(f => f.Value is { });
                        if (info is not null) {
                            //发送信息组合完成信号
                            _sortingService.SendPackageInfoCompletedSignal(0, new InstructionsAttach() {
                                BarCode = string.Empty,
                                Guid = _preSignal,
                                Timestamp = 0
                            });
                            info.SupplyCounterPackageSignalItem.Add(new SupplyCounterPackageSignal() {
                                Time = args.InstructionTime,
                                Type = SignalType.SendingAssignmentCompleteSignal
                            });
                            //--------

                            if (_preSignal < _supplyCounterSettingsDto.PrecedingSignalMaxValue) {
                                _preSignal++;
                            }
                            else {
                                _preSignal = _supplyCounterSettingsDto.StartPrecedingNumber;
                            }

                            if (_supplyCounterSettingsDto is { IsUseSupplyCounterMode: true, IsWaitForPrecedingSignalReplyBeforeCreatingNewPackage: true }
                                && info.SupplyCounterPackageSignalItem.Any(a => a.Type == SignalType.ReturningPreSignal) != true) {
                                info.SupplyCounterPackageSignalItem.Add(new SupplyCounterPackageSignal() {
                                    Instruction = args.Instruction,
                                    Time = args.InstructionTime,
                                    Type = SignalType.ReturningPreSignal
                                });
                            }
                        }
                    }
                    finally {
                        _takePackageSlim.Release();
                    }
                    //匹配包裹
                    EventAggregator.Instance.Publish(new InstructionReceived() {
                        Timestamp = new DateTimeOffset(args.InstructionTime).ToUnixTimeMilliseconds(),
                        IsCreatedByLowerMachine = false,
                        SortingCode = args.Keyword,
                        InstructionInfos = new List<InstructionInfoModel>()
                        {
                            new()
                            {
                                InstructionContent = args.Instruction,
                                InstructionGeneratedTime = args.InstructionTime,
                                InstructionType = InstructionType.ReceivePreSignalReply
                            }
                        },
                        ConnectionName = args.ConnectionName,
                    });
                }
            };
            //车号(序号绑定回复)
            _sortingService.SequenceBinding += async (sender, args) => {
                //修改赋值
                try {
                    await _createPackageSlim.WaitAsync();
                    var tryDequeue = _instructionsAttachItems.TryDequeue(out var info);
                    if (tryDequeue && info is not null) {
                        var value = PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: not null } &&
                                                                       f.Value.BarCodeInfo.Barcode.Equals(info.BarCode) &&
                                                                       f.Value.Timestamp.Equals(info.Timestamp) &&
                                                                       f.Value.SupplyCounterPackageSignalItem.Any(a =>
                                                                           a.Type == SignalType.ReturningBindingSignal) != true);

                        if (value is not null) {
                            value.SupplyCounterPackageSignalItem.Add(new SupplyCounterPackageSignal() {
                                Instruction = args.Instruction,
                                Time = args.InstructionTime,
                                Type = SignalType.ReturningBindingSignal
                            });
                            value.Guid = Convert.ToInt64(args.Keyword);
                        }
                    }
                }
                finally {
                    _createPackageSlim.Release();
                }

                EventAggregator.Instance.Publish(new InstructionReceived() {
                    Timestamp = new DateTimeOffset(args.InstructionTime).ToUnixTimeMilliseconds(),
                    IsCreatedByLowerMachine = false,
                    SortingCode = args.Keyword,
                    InstructionInfos = new List<InstructionInfoModel>()
                    {
                        new()
                        {
                            InstructionContent = args.Instruction,
                            InstructionGeneratedTime = args.InstructionTime,
                            InstructionType = InstructionType.SequenceBindingReply
                        }
                    },
                    ConnectionName = args.ConnectionName,
                });
            };
            //复位按钮触发
            _sortingService.ResetButtonTrigger += (sender, args) => {
                if (_supplyCounterSettingsDto.ClearPackagesOnReset) {
                    PackageInfoManager.ClearAllPackages();
                    _instructionsAttachItems.Clear();
                    BarCodeFilterContainer.ResetFilter();
                }
                EventAggregator.Instance.Publish(new InstructionReceived() {
                    Timestamp = new DateTimeOffset(args.InstructionTime).ToUnixTimeMilliseconds(),
                    IsCreatedByLowerMachine = false,
                    SortingCode = args.Keyword,
                    InstructionInfos = new List<InstructionInfoModel>()
                    {
                        new()
                        {
                            InstructionContent = args.Instruction,
                            InstructionGeneratedTime = args.InstructionTime,
                            InstructionType = InstructionType.ResetButtonTrigger
                        }
                    },
                    ConnectionName = args.ConnectionName,
                });
            };
            //Ocr算法
            _deviceService.OcrContentRecognized += async delegate (object? sender, OcrResult args) {
                try {
                    await _createPackageSlim.WaitAsync();
                    //创建条码
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                            PackageInfoManager.GetLastPackage(f => f.Value is { BarCodeInfo: null });
                    if ((_createPackageSettingsDto.PackageCreationMethods &
                         PackageCreationMethodsEnum.OcrInfo) ==
                        PackageCreationMethodsEnum.OcrInfo && packageInfo is null) {
                        packageInfo = new PackageInfo() {
                            Guid = args.RecognitionTimestamp,
                            BarCodeInfo = new BarCodeInfoModel() {
                                Barcode = args.BarCode,
                                ScanTime = DateTime.Now,
                                CameraSerialNumber = args.CameraSerialNumber,
                                Source = SourceType.Ocr
                            },
                            Image = args.Image,
                        };
                        EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                            IsSuccess = true,
                            TriggerPosition = TriggerPositionEnum.PackageTrigger,
                            PackageInfo = packageInfo
                        });
                        EventAggregator.Instance.Publish(new PackageOcrInfo() {
                            BarCode = args.BarCode,
                            VirtualNumber = args.VirtualNumber,
                            RecipientAddress = args.RecipientAddress,
                            RecipientName = args.RecipientName,
                            RecipientPhone = args.RecipientPhone,
                            SenderName = args.SenderName,
                            SenderPhone = args.SenderPhone,
                            SenderAddress = args.SenderAddress,
                            ThreeSegmentCode = args.ThreeSegmentCode,
                            VirtualNumberLast4 = args.VirtualNumberLast4,
                            RecognitionTime = args.RecognitionTime,
                            ElapsedTime = args.ElapsedTime,
                            RecognitionTimestamp = args.RecognitionTimestamp,
                            CameraSerialNumber = args.CameraSerialNumber,
                            SubmitTimestamp = args.SubmitTimestamp
                        });
                    }
                    else {
                        if (packageInfo != null) {
                            packageInfo.BarCodeInfo = new BarCodeInfoModel() {
                                Barcode = args.BarCode,
                                ScanTime = DateTime.Now,
                                CameraSerialNumber = args.CameraSerialNumber,
                                Source = SourceType.Ocr
                            };
                            packageInfo.Image = args.Image;
                        }
                    }
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            //手动输入条码
            EventAggregator.Instance.Subscribe<BarcodeTypeProviderEvent>(async barcodeInfo => {
                if (barcodeInfo is { } args) {
                    try {
                        await _createPackageSlim.WaitAsync();
                        var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        var packageInfo =
                            _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                                PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                                PackageInfoManager.GetLastPackage(f => f.Value is { BarCodeInfo: null });

                        if ((_createPackageSettingsDto.PackageCreationMethods &
                             PackageCreationMethodsEnum.ControlInput) ==
                            PackageCreationMethodsEnum.ControlInput && packageInfo is null) {
                            packageInfo = new PackageInfo() {
                                Guid = timestamp,
                                BarCodeInfo = new BarCodeInfoModel() {
                                    Barcode = args.Barcode,
                                    ScanTime = DateTime.Now,
                                    Source = SourceType.Input
                                },
                                HeightToDeduct = args.HeightToDeduct,
                                WidthToDeduct = args.WidthToDeduct,
                                LengthToDeduct = args.LengthToDeduct,
                                VolumeToDeduct = args.VolumeToDeduct,
                                CreateTime = DateTime.Now,
                                IsCreatedByLowerMachine = false,
                            };
                            EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.PackageTrigger,
                                PackageInfo = packageInfo
                            });
                        }
                        else {
                            if (packageInfo != null) {
                                packageInfo.BarCodeInfo = new BarCodeInfoModel() {
                                    Barcode = args.Barcode,
                                    ScanTime = DateTime.Now,
                                    Source = SourceType.Input
                                };
                            }
                        }
                    }
                    finally {
                        _createPackageSlim.Release();
                    }
                }
            });
            //配置更改触发事件
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                if (item is { } model) {
                    switch (model.SettingsName) {
                        case "VolumeSettings":
                            _volumeSettingsDto = await _configRepository.FirstOrDefaultEntity<VolumeSettingsDto>(model.SettingsName) ??
                                                 new VolumeSettingsDto();

                            break;

                        case "WeightSettings":
                            _weightSettingsDto = await _configRepository.FirstOrDefaultEntity<WeightSettingsDto>(model.SettingsName) ??
                                                 new WeightSettingsDto();

                            break;

                        case "CreatePackageSettings":
                            _createPackageSettingsDto = await _configRepository.FirstOrDefaultEntity<CreatePackageSettingsDto>(model.SettingsName) ??
                                                        new CreatePackageSettingsDto();

                            break;

                        case "StackedPackageDetectionSettings":

                            _stackedPackageDetectionSettingsDto = await _configRepository.FirstOrDefaultEntity<StackedPackageDetectionSettingsDto>(model.SettingsName) ??
                                                                  new StackedPackageDetectionSettingsDto();
                            break;

                        case "BarcodeFilterSettings":
                            _barcodeFilterSettingsDto = await _configRepository.FirstOrDefaultEntity<BarcodeFilterSettingsDto>(model.SettingsName) ??
                                                        new BarcodeFilterSettingsDto();
                            break;

                        case "SupplyCounterSettings":
                            _supplyCounterSettingsDto = await _configRepository.FirstOrDefaultEntity<SupplyCounterSettingsDto>(model.SettingsName) ?? new SupplyCounterSettingsDto();
                            _preSignal = _supplyCounterSettingsDto.StartPrecedingNumber;
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
                    try {
                        await _takePackageSlim.WaitAsync();
                        var info = PackageInfoManager.GetLastPackage(f => f is { Value: not null });

                        if (info is not null &&
                            packageInfo.CreateTime.Subtract(info.CreateTime).TotalMilliseconds <
                            _createPackageSettingsDto.PackageCreationInterval) {
                            return;
                        }
                        //没返回前置信号禁止创建包裹
                        if (info is not null && _supplyCounterSettingsDto is { IsUseSupplyCounterMode: true, IsWaitForPrecedingSignalReplyBeforeCreatingNewPackage: true } &&
                            info.SupplyCounterPackageSignalItem?.Any(a => a.Type == SignalType.ReturningPreSignal) != true) {
                            //使用新条码
                            if (packageInfo?.BarCodeInfo is not null) {
                                info.BarCodeInfo = new BarCodeInfoModel() {
                                    Barcode = packageInfo.BarCodeInfo.Barcode,
                                    CameraSerialNumber = packageInfo.BarCodeInfo.CameraSerialNumber,
                                    ScanTime = packageInfo.BarCodeInfo.ScanTime,
                                    Source = packageInfo.BarCodeInfo.Source
                                };
                            }

                            return;
                        }
                        //发送包裹居中指令
                        /*await Task.Delay(10);
                        _sortingService.SendPackageCenter((int)packageInfo.Guid, new InstructionsAttach() {
                            BarCode = string.Empty,
                            Guid = packageInfo.Guid,
                            Timestamp = packageInfo.Timestamp,
                            PackagePositionInfo = new PackagePositionInfo() {
                                CenterX = 0,
                                CenterY = 0,
                                OffsetDirection = OffsetDirection.Left,
                                OffsetDistance = 127
                            },
                            // PackagePositionInfo =  这里计算偏移
                        });*/

                        if (_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector &&
                            _grayscaleService.IsConnected) {
                            //跳过车辆
                            if (GrayScaleSkippedVehicles > 1) {
                                GrayScaleSkippedVehicles--;
                                LogManager.GetCurrentClassLogger().Error("前车联动了多车,该车跳过");
                                return;
                            }
                            //动态时间
                            var milliseconds = DateTime.Now.Subtract(packageInfo.CreateTime).TotalMilliseconds;
                            if (milliseconds < 50) {
                                await Task.Delay((int)(50 - milliseconds));
                            }
                            else {
                                LogManager.GetCurrentClassLogger().Error($"创建包裹到现在的间隔:{milliseconds}ms");
                            }

                            var singleGrayscaleSensorResult = await _grayscaleService.GetSingleGrayscaleSensorResult(packageInfo.Guid, _grayscaleDeviceSettingsDto.TimeOut);

                            if (singleGrayscaleSensorResult is not null) {
                                //联动车辆
                                GrayScaleSkippedVehicles = singleGrayscaleSensorResult.LinkedCarCount;
                                /*
                                if (singleGrayscaleSensorResult.AttachmentRectangleBoxInfo.IsPackagePresent == true) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"存在包裹触发车号:{packageInfo.Guid}");
                                    NLog.LogManager.GetCurrentClassLogger().Error($"包裹所在车号:{singleGrayscaleSensorResult.CarNumber}");
                                }
                                */

                                /*if ((singleGrayscaleSensorResult.AttachmentRectangleBoxInfo.IsPackagePresent != true) &&
                                    (packageInfo.BarCodeInfo?.Barcode.Equals("noread", StringComparison.CurrentCultureIgnoreCase) == true ||
                                     packageInfo.BarCodeInfo is null)) {
                                    return;
                                }*/

                                //双车赋值
                                var package = PackageInfoManager.GetLastPackage(s => s.Value != null && s.Value.Guid.Equals(singleGrayscaleSensorResult.CarNumber));
                                if (package != null) {
                                    if (package.BarCodeInfo?.Barcode.Equals("noread", StringComparison.CurrentCultureIgnoreCase) == true &&
                                        singleGrayscaleSensorResult.MainRectangleBoxInfos?.Any() != true) {
                                        package.Image?.Dispose();
                                        PackageInfoManager.RemovePackage(package.CreateTime);
                                        package.BarCodeInfo = null;
                                    }
                                    package.LinkedCarCount = singleGrayscaleSensorResult.LinkedCarCount;
                                }
                                packageInfo.GrayscaleResultInfo = singleGrayscaleSensorResult;
                            }

                            if (_grayscaleDeviceSettingsDto.IsCheckPackageOrientation &&
                                packageInfo.GrayscaleResultInfo is not null &&
                                packageInfo.GrayscaleResultInfo?.MainRectangleBoxInfos?.Any() == true) {
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
                    }
                    finally {
                        _takePackageSlim.Release();
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

                    //触发全景拍照
                    if (packageInfo.BarCodeInfo is not null) {
                        var list = _panoramaCameras?.Where(w => w.SelectedCameraSerialNumber.Equals(packageInfo.BarCodeInfo.CameraSerialNumber))?
                            .Select(s => s.SerialNumber)?.ToList();
                        if (list?.Any() != true) {
                            list = _panoramaCameras?.Where(w => w.SelectedCameraSerialNumber.Equals(string.Empty))?
                                .Select(s => s.SerialNumber)?.ToList();
                        }
                        var cameras = _cameras.Where(w =>
                            list?.Contains(w.Info?.SerialNumber ?? string.Empty) == true && w.BindingType == CameraBindingType.PanoramaCamera)?.ToList();
                        foreach (var c in (cameras ?? new List<ICamera>()).Where(c => _deviceService.RunningStatus)) {
                            await c.TakePhotoAsync(packageInfo.BarCodeInfo.Barcode, packageInfo.Guid);
                        }
                        //填充全景相机数量
                        packageInfo.PanoramaCameraImageInfo = cameras?.Select(s => new PanoramaCameraImageInfo {
                            CameraSerialNumber = s.Info?.SerialNumber ?? string.Empty,
                        })?.ToList()
                                                              ?? new List<PanoramaCameraImageInfo>();
                    }

                    //体积
                    if (packageInfo.VolumeInfo is null) {
                        //获取外部数据
                        //体积
                        if (_externalDataSource.IsVolumeInput) {
                            await _externalDataService.GetVolume(new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds().ToString());
                        }
                        else {
                            var volumeCameras = _cameras?.Where(w => w.BindingType == CameraBindingType.VolumeCamera)?.ToList();
                            if (volumeCameras?.Any() == true) {
                                foreach (var volumeCamera in volumeCameras) {
                                    if (volumeCamera is IVolumeCamera vCamera) {
                                        await vCamera.TriggerMeasurementPhotoAsync(new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds().ToString(), packageInfo.Guid, _volumeSettingsDto.TriggerDelayMilliseconds);
                                    }
                                }
                            }
                        }
                    }
                    /*if (packageInfo.BarCodeInfo is not null) {
                        var list = _panoramaCameras?.Where(w => w.SelectedCameraSerialNumber.Equals(packageInfo.BarCodeInfo.CameraSerialNumber))?
                            .Select(s => s.SerialNumber)?.ToList();
                        if (list?.Any() != true) {
                            list = _panoramaCameras?.Where(w => w.SelectedCameraSerialNumber.Equals(string.Empty))?
                                .Select(s => s.SerialNumber)?.ToList();
                        }
                        var cameras = _cameras.Where(w =>
                            list?.Contains(w.Info?.SerialNumber ?? string.Empty) == true && w.BindingType == CameraBindingType.PanoramaCamera)?.ToList();
                        foreach (var c in (cameras ?? new List<ICamera>()).Where(c => _deviceService.RunningStatus)) {
                            await c.TakePhotoAsync(packageInfo.BarCodeInfo.Barcode, packageInfo.Guid);
                        }
                        //填充全景相机数量
                        packageInfo.PanoramaCameraImageInfo = cameras?.Select(s => new PanoramaCameraImageInfo {
                            CameraSerialNumber = s.Info?.SerialNumber ?? string.Empty,
                        })?.ToList()
                                                              ?? new List<PanoramaCameraImageInfo>();
                        //体积
                        if (packageInfo.VolumeInfo is null) {
                            //获取外部数据
                            //体积
                            if (_externalDataSource.IsVolumeInput) {
                                await _externalDataService.GetVolume(packageInfo.BarCodeInfo.Barcode);
                            }
                            else {
                                var volumeCameras = _cameras?.Where(w => w.BindingType == CameraBindingType.VolumeCamera)?.ToList();
                                if (volumeCameras?.Any() == true) {
                                    foreach (var volumeCamera in volumeCameras) {
                                        if (volumeCamera is IVolumeCamera vCamera) {
                                            await vCamera.TriggerMeasurementPhotoAsync(packageInfo.BarCodeInfo.Barcode, packageInfo.Guid, _volumeSettingsDto.TriggerDelayMilliseconds);
                                        }
                                    }
                                }
                            }
                        }
                    }*/
                }
                else if (item is TriggerPositionEvent { TriggerPosition: TriggerPositionEnum.BarCodeSetValueAfter, PackageInfo: { } info }) {
                    //邮政专供
                    if (!_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector) {
                        PackageInfoManager.CompletedPackage(f => f.Key.Equals(info.CreateTime));
                    }
                }
            });
            //包裹组合完成后触发
            EventAggregator.Instance.Subscribe<PackageInfo>(async item => {
            });
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                if (item is WindowsAction { Type: WindowsActionType.Close }) {
                    _isWindowsClose = true;
                }
            });
            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(item => {
                if (item is { } info) {
                    if (info.Status == ApplicationStatus.Stop &&
                        _createPackageSettingsDto.ClearPackageQueueOnStop) {
                        PackageInfoManager.ClearAllPackages();
                        _instructionsAttachItems.Clear();
                    }
                }
            });
            //叠包事件
            _stackedPackageService.StackedPackageReturned += (sender, args) => {
                var packageInfo = PackageInfoManager.GetPackage(f => f.Key.Equals(args.PackageTime));
                if (packageInfo is not null) {
                    packageInfo.IsStackedPackage = args.IsStacked;
                }
            };
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
                    _resultOutputService.ExecuteOutput(
                        args.CompletedPackage.BarCodeInfo.Barcode,
                        (float)args.CompletedPackage.WeightInfo.FormattedWeight,
                        args.CompletedPackage.BarCodeInfo.ScanTime,
                        (float)args.CompletedPackage.VolumeInfo.FormattedLength,
                        (float)args.CompletedPackage.VolumeInfo.FormattedWidth,
                        (float)args.CompletedPackage.VolumeInfo.FormattedHeight,
                        (float)args.CompletedPackage.VolumeInfo.FormattedVolume,
                        args.CompletedPackage.BarCodeInfo.CameraSerialNumber);

                    EventAggregator.Instance.Publish(args.CompletedPackage);
                }
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                //读配置
                _configInfoModels = await _configRepository.Select(s => s.Id > 0,
                    o => o.Id, stoppingToken);
                // var configInfoModel = _configInfoModels?.FirstOrDefault(f => f.ConfigName.Equals("CommunicationsSettings"));
                var configInfoModel = _configInfoModels?.FirstOrDefault(f => f.ConfigName.Equals("VolumeSettings"));
                if (configInfoModel is not null) {
                    _volumeSettingsDto = JsonConvert.DeserializeObject<VolumeSettingsDto>(configInfoModel.Value) ?? new VolumeSettingsDto();
                }

                configInfoModel = _configInfoModels?.FirstOrDefault(f => f.ConfigName.Equals("WeightSettings"));
                if (configInfoModel is not null) {
                    _weightSettingsDto = JsonConvert.DeserializeObject<WeightSettingsDto>(configInfoModel.Value) ?? new WeightSettingsDto();
                }
                configInfoModel = _configInfoModels?.FirstOrDefault(f => f.ConfigName.Equals("CreatePackageSettings"));
                if (configInfoModel is not null) {
                    _createPackageSettingsDto = JsonConvert.DeserializeObject<CreatePackageSettingsDto>(configInfoModel.Value) ?? new CreatePackageSettingsDto();
                }
                configInfoModel = _configInfoModels?.FirstOrDefault(f => f.ConfigName.Equals("StackedPackageDetectionSettings"));
                if (configInfoModel is not null) {
                    _stackedPackageDetectionSettingsDto = JsonConvert.DeserializeObject<StackedPackageDetectionSettingsDto>(configInfoModel.Value) ?? new StackedPackageDetectionSettingsDto();
                }
                configInfoModel = _configInfoModels?.FirstOrDefault(f => f.ConfigName.Equals("BarcodeFilterSettings"));
                if (configInfoModel is not null) {
                    _barcodeFilterSettingsDto = JsonConvert.DeserializeObject<BarcodeFilterSettingsDto>(configInfoModel.Value) ?? new BarcodeFilterSettingsDto();
                }
                configInfoModel = _configInfoModels?.FirstOrDefault(f => f.ConfigName.Equals("SupplyCounterSettings"));
                if (configInfoModel is not null) {
                    _supplyCounterSettingsDto = JsonConvert.DeserializeObject<SupplyCounterSettingsDto>(configInfoModel.Value) ?? new SupplyCounterSettingsDto();
                    _preSignal = _supplyCounterSettingsDto.StartPrecedingNumber;
                }
                configInfoModel = _configInfoModels?.FirstOrDefault(f => f.ConfigName.Equals("GrayscaleDeviceSettings"));
                if (configInfoModel is not null) {
                    _grayscaleDeviceSettingsDto = JsonConvert.DeserializeObject<GrayscaleDeviceSettingsDto>(configInfoModel.Value) ?? new GrayscaleDeviceSettingsDto();
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                await Task.Delay(80, stoppingToken).ContinueWith(async _task => {
                    if (_task.IsCompletedSuccessfully) {
                        try {
                            //多相机组码
                            if (_cameras.Count(c => c.BindingType == CameraBindingType.ScannerCamera) > 1 && _deviceService.RunningStatus &&
                                _barCodeFrameInfoItem.Count > 0) {
                                if (_barCodeFrameInfoItem.Count == _cameras.Count(c => c.BindingType == CameraBindingType.ScannerCamera) ||
                                    DateTime.Now.Subtract(_barCodeFrameInfoItem.Where(w => w.Value.BarCodeInfo != null)
                                        .OrderBy(o => o.Value?.BarCodeInfo?.ScanTime)
                                        .FirstOrDefault().Value.BarCodeInfo.ScanTime).TotalMilliseconds > _barcodeFilterSettingsDto.MergeTimeout ||
                                    _barCodeFrameInfoItem.Where(w => w.Value.BarCodeInfo != null)
                                        .GroupBy(g => g.Value?.BarCodeInfo?.Barcode).Count() > 1
                                   ) {
                                    //组数据并清除队列
                                    var groupBy = _barCodeFrameInfoItem.Where(w => w.Value.BarCodeInfo != null)
                                        .GroupBy(g => g.Value?.BarCodeInfo?.Barcode)
                                        .Select(s => new {
                                            BarCode = s.Key,
                                            Count = s.Count()
                                        }).ToList();
                                    BarCodeFrameInfo barCodeFrameInfo;
                                    if (groupBy.Count == 1) {
                                        barCodeFrameInfo = _barCodeFrameInfoItem.Where(w => w.Value.BarCodeInfo != null)
                                            .LastOrDefault(keyValuePair =>
                                                keyValuePair.Value.BarCodeInfo.Barcode.Equals(groupBy.FirstOrDefault()?.BarCode)).Value;
                                    }
                                    else {
                                        barCodeFrameInfo = _barCodeFrameInfoItem.Where(w => w.Value.BarCodeInfo != null)
                                            .LastOrDefault(keyValuePair =>
                                                !keyValuePair.Value.BarCodeInfo.Barcode.ToLower().Equals("noread") &&
                                                !keyValuePair.Value.BarCodeInfo.Barcode.ToLower().Equals("filtered")).Value;
                                    }
                                    var packageInfo =
                                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                                            PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: null }) :
                                            PackageInfoManager.GetLastPackage(f => f.Value is { BarCodeInfo: null });
                                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.ScanBarcodeCamera)
                                        == PackageCreationMethodsEnum.ScanBarcodeCamera && packageInfo is null) {
                                        //支持扫码创建
                                        packageInfo = new PackageInfo() {
                                            Guid = barCodeFrameInfo.Timestamp,
                                            BarCodeInfo = barCodeFrameInfo.BarCodeInfo,
                                            Image = barCodeFrameInfo.Image,
                                        };
                                        EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                            IsSuccess = true,
                                            TriggerPosition = TriggerPositionEnum.PackageTrigger,
                                            PackageInfo = packageInfo
                                        });
                                    }
                                    else {
                                        if (packageInfo is not null) {
                                            packageInfo.BarCodeInfo = barCodeFrameInfo.BarCodeInfo;
                                            packageInfo.Image = barCodeFrameInfo.Image;
                                        }
                                    }
                                    _barCodeFrameInfoItem.Clear();
                                }
                            }

                            if (PackageInfoManager.GetPackageCount() > 0 && _deviceService.RunningStatus) {
                                //供包台信号赋值(增加一个过期时间)
                                if (_supplyCounterSettingsDto.IsUseSupplyCounterMode) {
                                    try {
                                        await _createPackageSlim.WaitAsync(stoppingToken);
                                        //分开判断

                                        var packageInfos = PackageInfoManager.GetPackages(w =>
                                            w.Value.SupplyCounterPackageSignalItem.All(supplyCounterPackageSignal =>
                                                supplyCounterPackageSignal.Type != SignalType.ReturningBindingSignal) &&
                                            w.Value.SupplyCounterPackageSignalItem.Any(supplyCounterPackageSignal =>
                                                supplyCounterPackageSignal.Type == SignalType.SendingAssignmentCompleteSignal));
                                        if (packageInfos?.Any() == true) {
                                            //处理绑定信号
                                            foreach (var pair in packageInfos) {
                                                var supplyCounterPackageSignal = pair.SupplyCounterPackageSignalItem.FirstOrDefault(f =>
                                                    f.Type == SignalType.SendingAssignmentCompleteSignal);
                                                if (supplyCounterPackageSignal != null &&
                                                    DateTime.Now.Subtract(supplyCounterPackageSignal.Time).TotalMilliseconds > _supplyCounterSettingsDto.BindingCarSignalReplyTimeout) {
                                                    if (_supplyCounterSettingsDto.RemovePackageAfterSignalTimeout) {
                                                        PackageInfoManager.RemovePackage(pair.CreateTime, "绑定信号超时");
                                                        if (_supplyCounterSettingsDto.ResetFilterAfterRemovingPackage) {
                                                            BarCodeFilterContainer.ResetFilter();
                                                        }
                                                        //移除对应的车号信息
                                                        if (_instructionsAttachItems.Any() && pair.BarCodeInfo is not null) {
                                                            while (_instructionsAttachItems.TryPeek(out var attachItem) &&
                                                                   attachItem.BarCode != null &&
                                                                   attachItem.Guid == pair.Guid && attachItem.BarCode.Equals(pair.BarCodeInfo.Barcode)) {
                                                                _instructionsAttachItems.TryDequeue(out _);
                                                            }
                                                        }

                                                        var totalMilliseconds = DateTime.Now.Subtract(supplyCounterPackageSignal.Time).TotalMilliseconds;
                                                        LogManager.GetCurrentClassLogger().Error($"移除包裹:序号绑定回复超时:{totalMilliseconds},序号:{pair.Guid}");
                                                    }
                                                    else {
                                                        pair.SupplyCounterPackageSignalItem.Add(new SupplyCounterPackageSignal() {
                                                            Type = SignalType.ReturningBindingSignal,
                                                            Time = DateTime.Now
                                                        });
                                                        if (_supplyCounterSettingsDto.IsUseSupplyCounterMode &&
                                                            pair.Guid > _supplyCounterSettingsDto.PrecedingSignalMaxValue) {
                                                            pair.Guid = 0;
                                                        }
                                                        //发送赋值信号
                                                    }
                                                }
                                            }
                                        }

                                        var packages = PackageInfoManager.GetPackages(w =>
                                            w.Value.SupplyCounterPackageSignalItem.All(supplyCounterPackageSignal =>
                                                supplyCounterPackageSignal.Type != SignalType.ReturningPreSignal) &&
                                            w.Value.SupplyCounterPackageSignalItem.Any(supplyCounterPackageSignal =>
                                                supplyCounterPackageSignal.Type == SignalType.SendingPreSignal));
                                        if (packages?.Any() == true) {
                                            //处理前置信号
                                            foreach (var pair in packages) {
                                                var counterPackageSignal = pair.SupplyCounterPackageSignalItem.FirstOrDefault(f =>
                                                    f.Type == SignalType.SendingPreSignal);
                                                if (counterPackageSignal != null &&
                                                    DateTime.Now.Subtract(counterPackageSignal.Time).TotalMilliseconds > _supplyCounterSettingsDto.PrecedingReplySignalTimeout) {
                                                    if (_supplyCounterSettingsDto.RemovePackageAfterSignalTimeout) {
                                                        PackageInfoManager.RemovePackage(pair.CreateTime, "前置信号超时");
                                                        if (_supplyCounterSettingsDto.ResetFilterAfterRemovingPackage) {
                                                            BarCodeFilterContainer.ResetFilter();
                                                        }
                                                        //移除对应的车号信息
                                                        if (_instructionsAttachItems.Any() && pair.BarCodeInfo is not null) {
                                                            while (_instructionsAttachItems.TryPeek(out var attachItem) &&
                                                                   attachItem.BarCode != null &&
                                                                   attachItem.Guid == pair.Guid && attachItem.BarCode.Equals(pair.BarCodeInfo.Barcode)) {
                                                                _instructionsAttachItems.TryDequeue(out _);
                                                            }
                                                        }

                                                        var totalMilliseconds = DateTime.Now.Subtract(counterPackageSignal.Time).TotalMilliseconds;
                                                        LogManager.GetCurrentClassLogger().Error($"移除包裹:前置信号回复超时:{totalMilliseconds},序号:{pair.Guid}");
                                                    }
                                                    else {
                                                        pair.SupplyCounterPackageSignalItem.AddRange(new List<SupplyCounterPackageSignal>()
                                                        {
                                                            new() {
                                                                Type = SignalType.ReturningPreSignal,
                                                                Time = DateTime.Now
                                                            },
                                                            new()
                                                            {
                                                                Type = SignalType.SendingAssignmentCompleteSignal,
                                                                Time = DateTime.Now
                                                            }
                                                        });
                                                        if (_supplyCounterSettingsDto.IsUseSupplyCounterMode &&
                                                            pair.Guid > _supplyCounterSettingsDto.PrecedingSignalMaxValue) {
                                                            pair.Guid = 0;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    finally {
                                        _createPackageSlim.Release();
                                    }
                                }
                                //取出一个未完成包裹
                                var value = PackageInfoManager.GetPackage(f => f.Value is { IsCompleted: false, BarCodeInfo: not null });
                                if (value is { IsCompleted: false, BarCodeInfo: not null } &&
                                    (!_supplyCounterSettingsDto.IsUseSupplyCounterMode ||
                                     value.SupplyCounterPackageSignalItem.Any(supplyCounterPackageSignal => supplyCounterPackageSignal.Type == SignalType.ReturningPreSignal)
                                      && value.SupplyCounterPackageSignalItem
                                          .Any(supplyCounterPackageSignal => supplyCounterPackageSignal.Type == SignalType.ReturningBindingSignal))) {
                                    //判断填充包裹信息
                                    if (value.VolumeInfo is not null &&
                                        value.WeightInfo is not null &&
                                        value.BarCodeInfo is not null &&
                                        value.IsStackedPackage is not null &&
                                        value.GrayscaleResultInfo is not null &&
                                        (_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector && value.LinkedCarCount > 0 ||
                                         !_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector)) {
                                        //执行输出
                                        _resultOutputService.ExecuteOutput(
                                            value.BarCodeInfo.Barcode, (float)value.WeightInfo.FormattedWeight,
                                            value.BarCodeInfo.ScanTime, (float)value.VolumeInfo.FormattedLength,
                                            (float)value.VolumeInfo.FormattedWidth, (float)value.VolumeInfo.FormattedHeight,
                                            (float)value.VolumeInfo.FormattedVolume, value.BarCodeInfo.CameraSerialNumber,
                                            stoppingToken);
                                        value.IsCompleted = true;
                                        EventAggregator.Instance.Publish(value);
                                    }
                                    else {
                                        //填充体积信息
                                        if (value.VolumeInfo == null && (_cameras.All(camera => camera.BindingType != CameraBindingType.VolumeCamera)
                                                                          && !_externalDataSource.IsVolumeInput ||
                                                                         _volumeSettingsDto.IsUseFusionTimeout &&
                                                                          DateTime.Now.Subtract(value.CreateTime).TotalMilliseconds > _volumeSettingsDto.FusionTimeout)) {
                                            //判断是否开启Tcp体积输入
                                            value.VolumeInfo = new VolumeInfoModel() {
                                                CreateTime = DateTime.Now,
                                                SourceType = SourceType.None
                                            };
                                        }
                                        //填充重量信息
                                        if (value.WeightInfo == null && (_deviceService.ScaleType == ScaleType.None ||
                                                                         _weightSettingsDto.AdditionalWeight.IsUseMergedWeightTimeout &&
                                                                          DateTime.Now.Subtract(value.CreateTime).TotalMilliseconds >
                                                                          _weightSettingsDto.AdditionalWeight.MergedWeightTimeout)) {
                                            value.WeightInfo = new WeightInfoModel() {
                                                CreateTime = DateTime.Now,
                                                SourceType = SourceType.None
                                            };
                                        }
                                        //填充叠包
                                        if (!_stackedPackageDetectionSettingsDto.IsStackedPackageDetection) {
                                            value.IsStackedPackage = false;
                                        }
                                        else if (value.IsStackedPackage is null &&
                                                 DateTime.Now.Subtract(value.CreateTime).TotalMilliseconds >
                                                 _stackedPackageDetectionSettingsDto.Timeout) {
                                            value.IsStackedPackage = false;
                                        }
                                        //填充灰度仪
                                        if (!_grayscaleDeviceSettingsDto.IsUseGrayscaleDetector) {
                                            value.GrayscaleResultInfo = new GrayscaleResult();
                                        }
                                    }
                                }
                                //判断发送前置信号
                                if (_supplyCounterSettingsDto is { IsUseSupplyCounterMode: true, SendPreSequenceNumber: true }) {
                                    try {
                                        await _createPackageSlim.WaitAsync(stoppingToken);

                                        var info = PackageInfoManager.GetPackage(f =>
                                            f.Value is { BarCodeInfo: not null, VolumeInfo: not null, WeightInfo: not null } &&
                                            f.Value.SupplyCounterPackageSignalItem.Any(supplyCounterPackageSignal =>
                                                supplyCounterPackageSignal.Type == SignalType.SendingPreSignal) != true);
                                        if (info is not null &&
                                            info.SupplyCounterPackageSignalItem.Any(supplyCounterPackageSignal => supplyCounterPackageSignal.Type == SignalType.SendingPreSignal) != true) {
                                            EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                                IsSuccess = true,
                                                TriggerPosition = TriggerPositionEnum.SendingPreSignalBefore,
                                            });

                                            //发送前置信号
                                            var instructionsAttach = new InstructionsAttach() {
                                                BarCode = info.BarCodeInfo?.Barcode ?? string.Empty,
                                                Guid = _preSignal,
                                                Timestamp = info.Timestamp,
                                                ScanTime = info.BarCodeInfo?.ScanTime ?? DateTime.Now
                                            };
                                            _sortingService.SendPreSignal(_preSignal, instructionsAttach, stoppingToken);
                                            info.Guid = _preSignal;
                                            info.SupplyCounterPackageSignalItem.Add(new SupplyCounterPackageSignal() {
                                                Time = DateTime.Now,
                                                Type = SignalType.SendingPreSignal
                                            });
                                            //添加到队列
                                            _instructionsAttachItems.Enqueue(instructionsAttach);
                                        }
                                    }
                                    finally {
                                        _createPackageSlim.Release();
                                    }
                                }
                                //匹配全景
                                if (_panoramaImageItems.Count > 0) {
                                    _panoramaImageItems.TryDequeue(out var panoramaImageInfo);
                                    if (panoramaImageInfo is not null) {
                                        var info = PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: not null } &&
                                            f.Value.BarCodeInfo?.Barcode.Equals(panoramaImageInfo.Barcode) == true);
                                        if (info is { WeightInfo: not null, VolumeInfo: not null, BarCodeInfo: not null }) {
                                            //全景图数量+1

                                            var panoramaCameraImageInfo = info.PanoramaCameraImageInfo.FirstOrDefault(f =>
                                                !f.IsExists &&
                                                f.CameraSerialNumber.Equals(panoramaImageInfo.CameraSerialNumber));
                                            if (panoramaCameraImageInfo is not null) {
                                                panoramaCameraImageInfo.IsExists = true;
                                                EventAggregator.Instance.Publish(new ImageMessageInfo {
                                                    BarCode = info.BarCodeInfo.Barcode,
                                                    CameraSerialNumber = panoramaImageInfo.CameraSerialNumber,
                                                    Weight = (float)info.WeightInfo.FormattedWeight,
                                                    Height = (float)info.VolumeInfo.FormattedHeight,
                                                    Image = panoramaImageInfo.Image,
                                                    Length = (float)info.VolumeInfo.FormattedLength,
                                                    Width = (float)info.VolumeInfo.FormattedWidth,
                                                    Volume = (float)info.VolumeInfo.FormattedVolume,
                                                    ScanTime = info.BarCodeInfo.ScanTime,
                                                    Type = SaveImageType.PanoramaImage,
                                                    CameraName = _cameras.FirstOrDefault(f => (bool)f.Info?.SerialNumber.Equals(panoramaImageInfo.CameraSerialNumber))?.Info?.Name ?? string.Empty,
                                                    CameraCustomName = _cameras.FirstOrDefault(f => (bool)f.Info?.SerialNumber.Equals(panoramaImageInfo.CameraSerialNumber))?.Info?.CustomName ?? string.Empty,
                                                });
                                            }
                                            else {
                                                _panoramaImageItems.Enqueue(panoramaImageInfo);
                                            }
                                        }
                                        else {
                                            _panoramaImageItems.Enqueue(panoramaImageInfo);
                                        }
                                    }
                                }
                                //体积图
                                if (_volumeCameraImageItems.Count > 0) {
                                    _volumeCameraImageItems.TryDequeue(out var volumeCameraImageInfo);
                                    if (volumeCameraImageInfo is not null) {
                                        var info = PackageInfoManager.GetPackage(f => f.Value is { BarCodeInfo: not null } &&
                                            f.Value.BarCodeInfo.Barcode.Equals(volumeCameraImageInfo.Barcode));

                                        if (info is { WeightInfo: not null, VolumeInfo: not null, BarCodeInfo: not null }
                                           ) {
                                            EventAggregator.Instance.Publish(new ImageMessageInfo {
                                                BarCode = info.BarCodeInfo.Barcode,
                                                CameraSerialNumber = volumeCameraImageInfo.CameraSerialNumber,
                                                Weight = (float)info.WeightInfo.FormattedWeight,
                                                Height = (float)info.VolumeInfo.FormattedHeight,
                                                Image = volumeCameraImageInfo.Image,
                                                Length = (float)info.VolumeInfo.FormattedLength,
                                                Width = (float)info.VolumeInfo.FormattedWidth,
                                                Volume = (float)info.VolumeInfo.FormattedVolume,
                                                ScanTime = info.BarCodeInfo.ScanTime,
                                                Type = SaveImageType.VolumeImage,
                                                CameraName = _cameras.FirstOrDefault(f => (bool)f.Info?.SerialNumber.Equals(volumeCameraImageInfo.CameraSerialNumber))?.Info?.Name ?? string.Empty,
                                            });
                                        }
                                        else {
                                            _volumeCameraImageItems.Enqueue(volumeCameraImageInfo);
                                        }
                                    }
                                }
                                //扫码图
                                if (PackageInfoManager.GetPackageCount() > 0) {
                                    //判断存图路径等于空
                                    var codeInfo = PackageInfoManager.GetPackage(f => f.Value is {
                                        WeightInfo: not null, VolumeInfo: not null, IsSavedImage: false,
                                        BarCodeInfo: not null, IsCompleted: true
                                    });
                                    if (codeInfo is not null) {
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
                                            CameraName = _cameras.FirstOrDefault(f => (bool)f.Info?.SerialNumber.Equals(codeInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty))?.Info?.Name ?? string.Empty,
                                            CameraCustomName = _cameras.FirstOrDefault(f => (bool)f.Info?.SerialNumber.Equals(codeInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty))?.Info?.CustomName ?? string.Empty,
                                        });
                                        codeInfo.IsSavedImage = true;
                                    }
                                }

                                //移除包裹
                                if (_createPackageSettingsDto.PackageRemoveMethods == PackageRemoveMethodsEnum.FillInformation) {
                                    var packageInfos = PackageInfoManager.GetPackages(w => w.Value is { IsCompleted: true, IsSavedImage: true } &&
                                        (w.Value.PanoramaCameraImageInfo.All(panoramaCameraImageInfo => panoramaCameraImageInfo.IsExists) ||
                                         DateTime.Now.Subtract(w.Value.CreateTime)
                                             .TotalMinutes > 5)) ?? new List<PackageInfo>();

                                    foreach (var kvp in packageInfos) {
                                        PackageInfoManager.RemovePackage(kvp.CreateTime, "填充完整信息移除");
                                    }
                                }
                            }
                        }
                        catch (Exception e) {
                            LogManager.GetCurrentClassLogger().Error($"{e}");
                        }
                    }
                }, stoppingToken);
            }
        }
    }
}