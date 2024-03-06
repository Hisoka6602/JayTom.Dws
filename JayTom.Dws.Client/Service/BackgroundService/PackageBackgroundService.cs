using System;
using DryIoc;
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
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalConf;
using NPOI.SS.Formula.Functions;
using System.Threading.Channels;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Client.Service.ImageStorage;
using JayTom.Dws.Client.Service.ResultOutput;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class PackageBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IDeviceService _deviceService;
        private readonly IResultOutputService _resultOutputService;
        private readonly IImageStorageService _imageStorageService;
        private readonly IExternalDataService _externalDataService;
        private readonly IConfigRepository _configRepository;
        private readonly ISortingService _sortingService;
        private readonly IStackedPackageService _stackedPackageService;
        private ExternalDataSourceEventArgs _externalDataSource = new();
        private List<ConfigInfoModel> _configInfoModels = new();

        //private CommunicationsSettingsDto _communicationsSettingsDto = new();
        private VolumeSettingsDto _volumeSettingsDto = new();

        private WeightSettingsDto _weightSettingsDto = new();
        private CreatePackageSettingsDto _createPackageSettingsDto = new();
        private List<ICamera> _cameras = new();
        private List<PanoramaCameraConfigInfoModel> _panoramaCameras = new();
        private ConcurrentQueue<CameraImageInfo> _panoramaImageItems = new();
        private ConcurrentQueue<CameraImageInfo> _volumeCameraImageItems = new();
        private ConcurrentDictionary<DateTime, PackageInfo> _packageInfos = new();
        private static bool _isWindowsClose;
        private SemaphoreSlim _createPackageSlim = new(1);

        public PackageBackgroundService(IDeviceService deviceService,
            IResultOutputService resultOutputService,
            IImageStorageService imageStorageService,
            IExternalDataService externalDataService, IConfigRepository configRepository,
            ISortingService sortingService, IPanoramaCameraConfigRepository panoramaCameraConfigRepository,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            IStackedPackageService stackedPackageService) {
            _deviceService = deviceService;
            _resultOutputService = resultOutputService;
            _imageStorageService = imageStorageService;
            _externalDataService = externalDataService;
            _configRepository = configRepository;
            _sortingService = sortingService;
            _stackedPackageService = stackedPackageService;

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

                    var info = _packageInfos.OrderBy(o => o.Key).FirstOrDefault(f => f.Value.BarCodeInfo != null &&
                    f.Value.BarCodeInfo.ScanTime.Equals(
                        args.ScanTime) &&
                    f.Value.BarCodeInfo.CameraSerialNumber.Equals(
                        args.CameraSerialNumber));
                    if (info.Value.BarCodeInfo != null && _createPackageSettingsDto.BarcodeHandlingMethod != BarcodeHandlingMethodEnum.UseMultipleBarcodes) {
                        if (_createPackageSettingsDto.BarcodeHandlingMethod == BarcodeHandlingMethodEnum.MergeBarcodes) {
                            info.Value.BarCodeInfo.Barcode += $"_{args.Barcode}";
                        }
                        return;
                    }
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            _packageInfos.OrderBy(o => o.Key).FirstOrDefault(f => f.Value.BarCodeInfo == null).Value :
                            _packageInfos.OrderBy(o => o.Key).LastOrDefault(f => f.Value.BarCodeInfo == null).Value;
                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.ScanBarcodeCamera)
                        == PackageCreationMethodsEnum.ScanBarcodeCamera && packageInfo is null) {
                        //支持扫码创建
                        packageInfo = new PackageInfo() {
                            Guid = args.Timestamp,
                            BarCodeInfo = new BarCodeInfoModel() {
                                Barcode = args.Barcode,
                                CameraSerialNumber = args.CameraSerialNumber,
                                ScanTime = args.ScanTime,
                                Source = SourceType.Camera
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
                                Source = SourceType.Camera
                            };
                            packageInfo.Image = args.Image;
                        }
                    }
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            //空包裹
            _deviceService.NotBarcodeHitEvent += async delegate (object? sender, BarcodeReadEventArgs args) {
                if (!_createPackageSettingsDto.IsUseNoRead) {
                    return;
                }

                try {
                    await _createPackageSlim.WaitAsync();
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            _packageInfos.OrderBy(o => o.Key).FirstOrDefault(f => f.Value.BarCodeInfo == null).Value :
                            _packageInfos.OrderBy(o => o.Key).LastOrDefault(f => f.Value.BarCodeInfo == null).Value;
                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.ScanBarcodeCamera) ==
                        PackageCreationMethodsEnum.ScanBarcodeCamera && packageInfo is null) {
                        //扫码相机创建

                        packageInfo = new PackageInfo() {
                            Guid = args.Timestamp,
                            BarCodeInfo = new BarCodeInfoModel() {
                                Barcode = args.Barcode,
                                CameraSerialNumber = args.CameraSerialNumber,
                                ScanTime = args.ScanTime,
                                Source = SourceType.Camera
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
                                Source = SourceType.Camera
                            };
                            packageInfo.Image = args.Image;
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
                       _packageInfos.OrderBy(o => o.Key).FirstOrDefault(f => f.Value.VolumeInfo == null).Value :
                       _packageInfos.OrderBy(o => o.Key).LastOrDefault(f => f.Value.VolumeInfo == null).Value;
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
                            packageInfo.VolumeInfo = new VolumeInfoModel() {
                                CreateTime = args.Timestamp,
                                FormattedHeight = args.Height - packageInfo.LengthToDeduct,
                                FormattedWidth = args.Width - packageInfo.WidthToDeduct,
                                FormattedLength = args.Length - packageInfo.LengthToDeduct,
                                FormattedVolume = args.Volume - packageInfo.VolumeToDeduct,
                                SourceType = SourceType.Camera,
                            };
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
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            _packageInfos.OrderBy(o => o.Key).FirstOrDefault(f => f.Value is { IsCompleted: false, WeightInfo: null }).Value :
                            _packageInfos.OrderBy(o => o.Key).LastOrDefault(f => f.Value is { IsCompleted: false, WeightInfo: null }).Value;
                    if ((_createPackageSettingsDto.PackageCreationMethods & PackageCreationMethodsEnum.StableWeight) ==
                        PackageCreationMethodsEnum.StableWeight && packageInfo is null) {
                        //稳定重量创建
                        packageInfo = new PackageInfo() {
                            Guid = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                            WeightInfo = new WeightInfoModel {
                                CreateTime = DateTime.Now,
                                FormattedWeight = args.Weight,
                                SourceType = SourceType.SerialPort,
                                WeighingMode = WeighingMode.Static
                            }
                        };

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
            //外部数据源
            _externalDataService.DataSourceEnabled += delegate (object? sender, ExternalDataSourceEventArgs args) {
                _externalDataSource = args;
            };
            //外部全量数据
            _externalDataService.ContentInputReceived += async (sender, args) => {
                try {
                    await _createPackageSlim.WaitAsync();
                    var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            _packageInfos.OrderBy(o => o.Key).FirstOrDefault(f => f.Value.BarCodeInfo == null).Value :
                            _packageInfos.OrderBy(o => o.Key).LastOrDefault(f => f.Value.BarCodeInfo == null).Value;
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
                            _packageInfos.OrderBy(o => o.Key).FirstOrDefault(f => f.Value.VolumeInfo == null).Value :
                            _packageInfos.OrderBy(o => o.Key).LastOrDefault(f => f.Value.VolumeInfo == null).Value;

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
                                CreateTime = DateTime.Now
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
                                        InstructionGeneratedTime = DateTime.Now,
                                        InstructionType = InstructionTypeType.CreatePackage
                                    }
                                }
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
                if (_createPackageSettingsDto.PackageRemoveMethods == PackageRemoveMethodsEnum.LowerMachineRemoval) {
                    try {
                        await _createPackageSlim.WaitAsync();
                        var tryParse = int.TryParse(args.Keyword, out var num);
                        if (tryParse) {
                            var keyValuePair = _packageInfos.FirstOrDefault(f => f.Value.Guid.Equals(num));
                            if (keyValuePair.Value is not null) {
                                _packageInfos.TryRemove(keyValuePair);

                                EventAggregator.Instance.Publish(new InstructionReceived() {
                                    Timestamp = new DateTimeOffset(keyValuePair.Value.CreateTime).ToUnixTimeMilliseconds(),
                                    IsCreatedByLowerMachine = true,
                                    InstructionInfos = new List<InstructionInfoModel>()
                                    {
                                        new()
                                        {
                                            InstructionContent = args.Instruction,
                                            InstructionGeneratedTime = DateTime.Now,
                                            InstructionType = InstructionTypeType.SignalCallback
                                        }
                                    }
                                });
                            }
                        }
                    }
                    finally {
                        _createPackageSlim.Release();
                    }
                }

                //其他协议
            };

            //下位机(清空异常)
            _sortingService.ClearExceptionEvent += async delegate (object? sender, string o) {
                try {
                    await _createPackageSlim.WaitAsync();
                    _packageInfos.Clear();
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            //Ocr算法
            _deviceService.OcrContentRecognized += async delegate (object? sender, OcrResult args) {
                try {
                    await _createPackageSlim.WaitAsync();
                    //创建条码
                    var packageInfo =
                        _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                            _packageInfos.OrderBy(o => o.Key).FirstOrDefault(f => f.Value.BarCodeInfo == null).Value :
                            _packageInfos.OrderBy(o => o.Key).LastOrDefault(f => f.Value.BarCodeInfo == null).Value;
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
                if (barcodeInfo is BarcodeTypeProviderEvent args) {
                    try {
                        await _createPackageSlim.WaitAsync();
                        var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        var packageInfo =
                            _createPackageSettingsDto.BarcodeQueueOrder == BarcodeQueueOrderEnum.TimeAscending ?
                                _packageInfos.OrderBy(o => o.Key).FirstOrDefault(f => f.Value.BarCodeInfo == null).Value :
                                _packageInfos.OrderBy(o => o.Key).LastOrDefault(f => f.Value.BarCodeInfo == null).Value;
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
                if (item is SettingsChangedEvent model) {
                    switch (model.SettingsName) {
                        case "VolumeSettings":
                            try {
                                var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("VolumeSettings"));
                                if (configInfoModel != null) {
                                    _volumeSettingsDto = JsonConvert.DeserializeObject<VolumeSettingsDto>(configInfoModel.Value) ?? new VolumeSettingsDto();
                                }
                            }
                            catch (Exception e) {
                                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                            }

                            break;

                        case "WeightSettings":
                            try {
                                var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("WeightSettings"));
                                if (configInfoModel != null) {
                                    _weightSettingsDto = JsonConvert.DeserializeObject<WeightSettingsDto>(configInfoModel.Value) ?? new WeightSettingsDto();
                                }
                            }
                            catch (Exception e) {
                                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                            }

                            break;

                        case "CreatePackageSettings":
                            try {
                                var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("CreatePackageSettings"));
                                if (configInfoModel != null) {
                                    _createPackageSettingsDto = JsonConvert.DeserializeObject<CreatePackageSettingsDto>(configInfoModel.Value) ?? new CreatePackageSettingsDto();
                                }
                            }
                            catch (Exception e) {
                                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                            }
                            break;
                    }
                    //其他设置
                }
            });
            //创建包裹后触发
            EventAggregator.Instance.Subscribe<TriggerPositionEvent>(async item => {
                if (item is TriggerPositionEvent { TriggerPosition: TriggerPositionEnum.PackageTrigger, PackageInfo: { } packageInfo }) {
                    _packageInfos.TryAdd(packageInfo.CreateTime, packageInfo);

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
                        //体积
                        if (packageInfo.VolumeInfo is null) {
                            //获取外部数据
                            //体积
                            if (_externalDataSource.IsVolumeInput) {
                                await _externalDataService.GetVolume(packageInfo.BarCodeInfo.Barcode);
                            }
                            else {
                                //触发体积测量
                                var volumeCameras = _cameras?.Where(w => w.BindingType == CameraBindingType.VolumeCamera)?.ToList();
                                if (volumeCameras?.Any() == true) {
                                    foreach (var volumeCamera in volumeCameras) {
                                        if (volumeCamera is IVolumeCamera vCamera) {
                                            await vCamera.TriggerMeasurementPhotoAsync(packageInfo.BarCodeInfo.Barcode, packageInfo.Guid, 100);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                if (item is WindowsAction { Type: WindowsActionType.Close }) {
                    _isWindowsClose = true;
                }
            });
            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(item => {
                if (item is ApplicationStatusChanged info) {
                    if (info.Status == ApplicationStatus.Stop &&
                        _createPackageSettingsDto.ClearPackageQueueOnStop) {
                        _packageInfos.Clear();
                    }
                }
            });
            //叠包事件
            _stackedPackageService.StackedPackageReturned += (sender, args) => {
                var packageInfo = _packageInfos.FirstOrDefault(f => f.Key.Equals(args.PackageTime)).Value;
                if (packageInfo is not null) {
                    packageInfo.IsStackedPackage = true;
                }
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                //读配置
                _configInfoModels = await _configRepository.Select(s => s.Id > 0,
                    o => o.Id, stoppingToken);
                var configInfoModel = _configInfoModels?.FirstOrDefault(f => f.ConfigName.Equals("CommunicationsSettings"));
                configInfoModel = _configInfoModels?.FirstOrDefault(f => f.ConfigName.Equals("VolumeSettings"));
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
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }

            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                try {
                    if (_packageInfos.Count > 0 && _deviceService.RunningStatus) {
                        //判断空包裹过期
                        if (_createPackageSettingsDto.IsUseEmptyPackageExpiry) {
                            try {
                                await _createPackageSlim.WaitAsync(stoppingToken);

                                var keyValuePairs = _packageInfos.Where(w => w.Value.BarCodeInfo == null &&
                                                                             DateTime.Now.Subtract(w.Key).TotalMilliseconds >
                                                                             _createPackageSettingsDto.EmptyPackageExpiryTime)
                                    ?.ToList();
                                if (keyValuePairs?.Any() == true) {
                                    Parallel.ForEach(keyValuePairs, key => {
                                        _packageInfos.TryRemove(key);
                                    });
                                }
                            }
                            finally {
                                _createPackageSlim.Release();
                            }
                        }
                        //判断包裹过期
                        if (_createPackageSettingsDto.IsUsePackageExpiry) {
                            try {
                                await _createPackageSlim.WaitAsync(stoppingToken);

                                var keyValuePairs = _packageInfos.Where(w => w.Value.BarCodeInfo == null &&
                                                                             DateTime.Now.Subtract(w.Key).TotalMilliseconds >
                                                                             _createPackageSettingsDto.PackageExpiryTime)
                                    ?.ToList();
                                if (keyValuePairs?.Any() == true) {
                                    Parallel.ForEach(keyValuePairs, key => {
                                        _packageInfos.TryRemove(key);
                                    });
                                }
                            }
                            finally {
                                _createPackageSlim.Release();
                            }
                        }
                        //取出一个未完成包裹
                        var packageInfo = _packageInfos.OrderBy(o => o.Key).FirstOrDefault(f => f.Value is { IsCompleted: false, BarCodeInfo: not null }).Value;
                        if (packageInfo != null) {
                            //判断填充包裹信息
                            if (packageInfo.VolumeInfo is not null &&
                                packageInfo.WeightInfo is not null &&
                                packageInfo.BarCodeInfo is not null) {
                                //执行输出
                                _resultOutputService.ExecuteOutput(
                                    packageInfo.BarCodeInfo.Barcode, (float)(packageInfo.WeightInfo.FormattedWeight),
                                    packageInfo.BarCodeInfo.ScanTime, (float)(packageInfo.VolumeInfo.FormattedLength),
                                    (float)(packageInfo.VolumeInfo.FormattedWidth), (float)(packageInfo.VolumeInfo.FormattedHeight),
                                    (float)(packageInfo.VolumeInfo.FormattedVolume), packageInfo.BarCodeInfo.CameraSerialNumber,
                                    stoppingToken);
                                packageInfo.IsCompleted = true;
                                EventAggregator.Instance.Publish(packageInfo);
                            }
                            else {
                                //填充体积信息
                                if ((_cameras.All(a => a.BindingType != CameraBindingType.VolumeCamera)
                                    && !_externalDataSource.IsVolumeInput) ||
                                    (_volumeSettingsDto.IsUseFusionTimeout &&
                                     DateTime.Now.Subtract(packageInfo.CreateTime).TotalMilliseconds > _volumeSettingsDto.FusionTimeout)) {
                                    //判断是否开启Tcp体积输入
                                    packageInfo.VolumeInfo = new VolumeInfoModel() {
                                        CreateTime = DateTime.Now,
                                        SourceType = SourceType.None
                                    };
                                }
                                //填充重量信息
                                if (_deviceService.ScaleType == ScaleType.None ||
                                    (_weightSettingsDto.AdditionalWeight.IsUseMergedWeightTimeout &&
                                     DateTime.Now.Subtract(packageInfo.CreateTime).TotalMilliseconds >
                                     _weightSettingsDto.AdditionalWeight.MergedWeightTimeout)) {
                                    packageInfo.WeightInfo = new WeightInfoModel() {
                                        CreateTime = DateTime.Now,
                                        SourceType = SourceType.None
                                    };
                                }
                            }
                        }
                        //匹配全景
                        if (_panoramaImageItems.Count > 0) {
                            _panoramaImageItems.TryDequeue(out var panoramaImageInfo);
                            if (panoramaImageInfo is not null) {
                                var info = _packageInfos.FirstOrDefault(f => f.Value.BarCodeInfo != null && f.Value.BarCodeInfo?.Barcode.Equals(panoramaImageInfo.Barcode) == true).Value;
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
                                var info = _packageInfos.FirstOrDefault(f => f.Value.BarCodeInfo != null && f.Value.BarCodeInfo.Barcode.Equals(volumeCameraImageInfo.Barcode)).Value;
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
                        if (_packageInfos.Count > 0) {
                            //判断存图路径等于空
                            var codeInfo = _packageInfos.FirstOrDefault(f => f.Value is {
                                WeightInfo: not null, VolumeInfo: not null, IsSavedImage: false,
                                BarCodeInfo: not null
                            }).Value;
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

                        if (_createPackageSettingsDto.PackageRemoveMethods == PackageRemoveMethodsEnum.FillInformation) {
                            //告诉界面这些scanBarCodeInfos已经填充完全部信息，即将移除
                            try {
                                await _createPackageSlim.WaitAsync(stoppingToken);
                                var packageInfos = _packageInfos.Where(w => w.Value is { IsCompleted: true, IsSavedImage: true } &&
                                                                            (w.Value.PanoramaCameraImageInfo.All(a => a.IsExists)
                                                                             || DateTime.Now.Subtract(w.Value.CreateTime).TotalMinutes > 5)
                                    )
                                    .ToList();
                                Parallel.ForEach(packageInfos, key => {
                                    _packageInfos.TryRemove(key);
                                });
                            }
                            finally {
                                _createPackageSlim.Release();
                            }
                        }
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }

                await Task.Delay(10, stoppingToken);
            }
        }
    }

    public class PackageInfo {

        /// <summary>
        /// 包裹创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Guid
        /// </summary>
        public long Guid { get; set; }

        /*

        /// <summary>
        /// 条码
        /// </summary>
        public string? BarCode { get; set; }
        */

        /// <summary>
        /// 条码图片
        /// </summary>
        public Image? Image { get; set; }

        /*/// <summary>
        /// 重量
        /// </summary>
        public double? Weight { get; set; }

        /// <summary>
        /// 长度
        /// </summary>
        public double? Length { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public double? Width { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public double? Height { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        public double? Volume { get; set; }*/

        /// <summary>
        /// 条码信息
        /// </summary>
        public BarCodeInfoModel? BarCodeInfo { get; set; }

        /// <summary>
        /// 体积信息
        /// </summary>
        public VolumeInfoModel? VolumeInfo { get; set; }

        /// <summary>
        /// 称重信息
        /// </summary>
        public WeightInfoModel? WeightInfo { get; set; }

        /*/// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;*/

        /// <summary>
        /// 是否已完成(完成输出、上传、但未从集合删除)
        /// </summary>
        public bool IsCompleted;

        /// <summary>
        /// 是否完成存图
        /// </summary>
        public bool IsSavedImage;

        /*/// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime;

        /// <summary>
        /// 条码时间戳
        /// </summary>
        public long Timestamp { get; set; }*/
        /*/// <summary>
        /// /需要扣除的重量
        /// </summary>
        public float WeightToDeduct { get; set; }*/

        /// <summary>
        /// 需要扣除的长度
        /// </summary>
        public float LengthToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的宽度
        /// </summary>
        public float WidthToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的高度
        /// </summary>
        public float HeightToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的体积
        /// </summary>
        public float VolumeToDeduct { get; set; }

        /// <summary>
        /// 创建包裹指令
        /// </summary>
        public string PackageCreationInstruction { get; set; } = string.Empty;

        /// <summary>
        /// 是否由下位机创建
        /// </summary>
        public bool IsCreatedByLowerMachine { get; set; }

        /// <summary>
        /// 全景图信息
        /// </summary>
        public List<PanoramaCameraImageInfo> PanoramaCameraImageInfo { get; set; } = new();

        /// <summary>
        /// 是否叠包
        /// </summary>
        public bool IsStackedPackage { get; set; }
    }

    public class CameraImageInfo {

        /// <summary>
        /// 图片
        /// </summary>
        public Image? Image { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 图片路径
        /// </summary>
        public string? ImageFilePath { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 条码时间戳
        /// </summary>
        public long BarcodeTimestamp { get; set; }
    }

    public class SavedImageInfo {

        /// <summary>
        /// 文件路径
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        public string? BarCode { get; set; }

        /// <summary>
        /// 图片类型
        /// </summary>
        public SaveImageType? ImageType { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime { get; set; }
    }

    /// <summary>
    /// 存图参数
    /// </summary>
    public class ImageMessageInfo {
        public Image? Image { get; set; }

        public SaveImageType Type { get; set; }
        public string BarCode { get; set; } = string.Empty;
        public float Weight { get; set; }
        public DateTime ScanTime { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Volume { get; set; }
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 相机名称
        /// </summary>
        public string CameraName { get; set; } = string.Empty;

        /// <summary>
        /// 相机自定义名称
        /// </summary>
        public string CameraCustomName { get; set; } = string.Empty;
    }

    public class PackageOcrInfo {

        /// <summary>
        /// 条码
        /// </summary>
        public string BarCode { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置虚拟号码。
        /// </summary>
        public string VirtualNumber { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置收件人地址。
        /// </summary>
        public string RecipientAddress { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置收件人姓名。
        /// </summary>
        public string RecipientName { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置收件人电话。
        /// </summary>
        public string RecipientPhone { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置寄件人姓名。
        /// </summary>
        public string SenderName { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置寄件人电话。
        /// </summary>
        public string SenderPhone { get; set; } = string.Empty;

        /// <summary>
        /// 发件人地址
        /// </summary>
        public string SenderAddress { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置三段码。
        /// </summary>
        public string ThreeSegmentCode { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置虚拟号码后四位。
        /// </summary>
        public string VirtualNumberLast4 { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置识别时间。
        /// </summary>
        public DateTime RecognitionTime { get; set; }

        /// <summary>
        /// 获取或设置耗时(ms)
        /// </summary>
        public long ElapsedTime { get; set; }

        /// <summary>
        /// 获取或设置识别时间戳。
        /// </summary>
        public long RecognitionTimestamp { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 提交图时间
        /// </summary>
        public long SubmitTimestamp { get; set; }

        /// <summary>
        /// 是否叠包
        /// </summary>
        public bool IsStackedPackage { get; set; }
    }

    public class PanoramaCameraImageInfo {

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 是否已存在
        /// </summary>
        public bool IsExists { get; set; }
    }
}