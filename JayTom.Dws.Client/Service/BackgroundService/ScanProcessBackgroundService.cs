using System;
using DryIoc;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading;
using TouchSocket.Core;
using JayTom.Dws.Camera;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.ImageStorage;
using JayTom.Dws.Client.Service.ResultOutput;
using JayTom.Dws.Client.Service.ExternalDataService;
using static JayTom.Dws.Client.Service.BackgroundService.ScanProcessBackgroundService;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class ScanProcessBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IDeviceService _deviceService;
        private readonly IResultOutputService _resultOutputService;
        private readonly IImageStorageService _imageStorageService;
        private readonly IExternalDataService _externalDataService;
        private readonly ConcurrentQueue<ScanBarCodeInfo> _scanBarCodeItems = new();

        //private SemaphoreSlim _semaphore = new(1);
        private ExternalDataSourceEventArgs _externalDataSource = new();

        private List<ICamera> _cameras = new();
        private ConcurrentQueue<CameraImageInfo> _panoramicImageItems = new();
        private ConcurrentQueue<CameraImageInfo> _volumeCameraImageItems = new();
        private ConcurrentQueue<SavedImageInfo> _savedImageItems = new();
        //private Queue<ScanBarCodeInfo> _scanBarCodeItems = new();

        public ScanProcessBackgroundService(IDeviceService deviceService,
            IResultOutputService resultOutputService,
            IImageStorageService imageStorageService,
            IExternalDataService externalDataService) {
            _deviceService = deviceService;
            _resultOutputService = resultOutputService;
            _imageStorageService = imageStorageService;
            _externalDataService = externalDataService;
            _imageStorageService.ImageSaved += delegate (object? sender, ImageSavedEventArgs args) {
                //保存后触发
                _savedImageItems.Enqueue(new SavedImageInfo() {
                    BarCode = args.BarCode,
                    FilePath = args.FilePath,
                    ImageType = args.ImageType,
                    CameraSerialNumber = args.CameraSerialNumber ?? string.Empty
                });
            };
            _deviceService.CameraInitialized += delegate (object? sender, List<ICamera> list) {
                _cameras = list;
            };

            _deviceService.BarcodeScanned += async delegate (object? sender, BarcodeReadEventArgs args) {
                var scanBarCodeInfo = new ScanBarCodeInfo() {
                    BarCode = args.Barcode,
                    CameraSerialNumber = args.CameraSerialNumber,
                    Image = args.Image,
                    ScanTime = args.ScanTime,
                    Timestamp = args.Timestamp,
                };
                //填充重量信息
                if (_deviceService.ScaleType == ScaleType.None) {
                    scanBarCodeInfo.Weight = 0;
                }
                _scanBarCodeItems.Enqueue(scanBarCodeInfo);
                //获取外部数据
                //体积
                if (_externalDataSource.IsVolumeInput) {
                    await _externalDataService.GetVolume(args.Barcode);
                }
                EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                    IsSuccess = true,
                    TriggerPosition = TriggerPositionEnum.PackageTrigger
                });
            };
            _deviceService.NotBarcodeHitEvent += delegate (object? sender, BarcodeReadEventArgs args) {
                EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                    IsSuccess = false,
                    TriggerPosition = TriggerPositionEnum.PackageTrigger
                });
            };
            _deviceService.PanoramaCaptured += async delegate (object? sender, PanoramaCaptureEventArgs args) {
                await Task.Yield();
                _panoramicImageItems.Enqueue(new CameraImageInfo() {
                    CameraSerialNumber = args.CameraSerialNumber,
                    Image = args.Image,
                    Barcode = args.Barcode,
                    BarcodeTimestamp = args.BarcodeTimestamp
                });
            };
            _deviceService.VolumeCaptured += async delegate (object? sender, VolumeCapturedEventArgs args) {
                await Task.Yield();
                _volumeCameraImageItems.Enqueue(new CameraImageInfo() {
                    CameraSerialNumber = args.CameraSerialNumber,
                    Image = args.Image,
                    Barcode = args.Barcode,
                    BarcodeTimestamp = args.BarcodeTimestamp
                });
                //填充长宽高
                var scanBarCodeInfo = _scanBarCodeItems.FirstOrDefault(f => f.Length == null ||
                                                                            f.Width == null ||
                                                                            f.Height == null);
                if (scanBarCodeInfo is not null) {
                    scanBarCodeInfo.Length = args.Length;
                    scanBarCodeInfo.Width = args.Width;
                    scanBarCodeInfo.Height = args.Height;
                    scanBarCodeInfo.Volume = args.Volume;
                }
            };
            _deviceService.StableWeight += delegate (object? sender, StableWeightEventArgs args) {
                var scanBarCodeInfo = _scanBarCodeItems.FirstOrDefault(f => !f.IsCompleted && f.Weight is null);
                if (scanBarCodeInfo is not null) {
                    scanBarCodeInfo.Weight = args.Weight;
                }
            };
            //外部数据源
            _externalDataService.DataSourceEnabled += delegate (object? sender, ExternalDataSourceEventArgs args) {
                _externalDataSource = args;
            };
            //输入体积
            _externalDataService.VolumeReceived += delegate (object? sender, ExternalVolumeInputEventArgs args) {
                var scanBarCodeInfo = _scanBarCodeItems.FirstOrDefault(f => f.BarCode.Equals(args.BarCode));
                if (scanBarCodeInfo is not null) {
                    scanBarCodeInfo.Length = args.Length;
                    scanBarCodeInfo.Width = args.Width;
                    scanBarCodeInfo.Height = args.Height;
                    scanBarCodeInfo.Volume = args.Volume;
                }
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    if (_scanBarCodeItems.Count > 0) {
                        var scanBarCodeInfo = _scanBarCodeItems.FirstOrDefault(f => !f.IsCompleted && !string.IsNullOrEmpty(f.BarCode) && f.Weight != null);
                        //判断填充包裹信息
                        if (scanBarCodeInfo is not null) {
                            //判断是不是有体积相机、如果有则判断是否已经获取体积
                            if (scanBarCodeInfo.Length is not null &&
                                scanBarCodeInfo.Width is not null &&
                                scanBarCodeInfo.Height is not null) {
                                //创建另一个对象处理耗时长的内容
                                //上传
                                //条码回调(告诉界面这个条码完成)
                                //保存到数据库
                                //输出
                                _resultOutputService.ExecuteOutput(
                                    scanBarCodeInfo.BarCode, (float)(scanBarCodeInfo.Weight ?? 0),
                                    scanBarCodeInfo.ScanTime, (float)(scanBarCodeInfo.Length ?? 0),
                                    (float)(scanBarCodeInfo.Width ?? 0), (float)(scanBarCodeInfo.Height ?? 0),
                                    (float)(scanBarCodeInfo.Volume ?? 0), scanBarCodeInfo.CameraSerialNumber,
                                    stoppingToken);
                                scanBarCodeInfo.IsCompleted = true;
                                EventAggregator.Instance.Publish(scanBarCodeInfo);
                            }
                            else {
                                if (_cameras.All(a => a.BindingType != CameraBindingType.VolumeCamera)
                                    && !_externalDataSource.IsVolumeInput) {
                                    //判断是否开启Tcp体积输入
                                    scanBarCodeInfo.Length = 0;
                                    scanBarCodeInfo.Width = 0;
                                    scanBarCodeInfo.Height = 0;
                                    scanBarCodeInfo.Volume = 0;
                                }
                            }
                        }

                        //全景图
                        if (_panoramicImageItems.Count > 0) {
                            _panoramicImageItems.TryDequeue(out var panoramicImageInfo);
                            if (panoramicImageInfo is not null) {
                                var info = _scanBarCodeItems.FirstOrDefault(f => f.BarCode.Equals(panoramicImageInfo.Barcode));
                                if (info is { Weight: not null, Length: not null, Width: not null, Height: not null, Volume: not null }
                                   ) {
                                    EventAggregator.Instance.Publish(new ImageMessageInfo {
                                        BarCode = info.BarCode,
                                        CameraSerialNumber = panoramicImageInfo.CameraSerialNumber,
                                        Weight = (float)info.Weight,
                                        Height = (float)info.Height,
                                        Image = panoramicImageInfo.Image,
                                        Length = (float)info.Length,
                                        Width = (float)info.Width,
                                        Volume = (float)info.Volume,
                                        ScanTime = info.ScanTime,
                                        Type = SaveImageType.PanoramaImage
                                    });
                                }
                                else {
                                    _panoramicImageItems.Enqueue(panoramicImageInfo);
                                }
                            }
                        }
                        //体积图
                        if (_volumeCameraImageItems.Count > 0) {
                            _volumeCameraImageItems.TryDequeue(out var volumeCameraImageInfo);
                            if (volumeCameraImageInfo is not null) {
                                var info = _scanBarCodeItems.FirstOrDefault(f => f.BarCode.Equals(volumeCameraImageInfo.Barcode));
                                if (info is { Weight: not null, Length: not null, Width: not null, Height: not null, Volume: not null }
                                   ) {
                                    EventAggregator.Instance.Publish(new ImageMessageInfo {
                                        BarCode = info.BarCode,
                                        CameraSerialNumber = volumeCameraImageInfo.CameraSerialNumber,
                                        Weight = (float)info.Weight,
                                        Height = (float)info.Height,
                                        Image = volumeCameraImageInfo.Image,
                                        Length = (float)info.Length,
                                        Width = (float)info.Width,
                                        Volume = (float)info.Volume,
                                        ScanTime = info.ScanTime,
                                        Type = SaveImageType.VolumeImage
                                    });
                                }
                            }
                            else {
                                _volumeCameraImageItems.Enqueue(volumeCameraImageInfo);
                            }
                        }
                        //扫码图
                        if (_scanBarCodeItems.Count > 0) {
                            //判断存图路径等于空
                            var codeInfo = _scanBarCodeItems.FirstOrDefault(f => f is {
                                Weight: not null, Length: not null, Width: not null,
                                Height: not null, Volume: not null, IsSavedImage: false
                            });
                            if (codeInfo is not null) {
                                EventAggregator.Instance.Publish(new ImageMessageInfo {
                                    BarCode = codeInfo.BarCode,
                                    CameraSerialNumber = codeInfo.CameraSerialNumber,
                                    Weight = (float)codeInfo.Weight,
                                    Height = (float)codeInfo.Height,
                                    Image = codeInfo.Image,
                                    Length = (float)codeInfo.Length,
                                    Width = (float)codeInfo.Width,
                                    Volume = (float)codeInfo.Volume,
                                    ScanTime = codeInfo.ScanTime,
                                    Type = SaveImageType.BarcodeImage
                                });
                                codeInfo.IsSavedImage = true;
                            }
                        }
                        //填充路径
                        if (_savedImageItems.Count > 0) {
                            _savedImageItems.TryDequeue(out var savedImageInfo);
                            if (savedImageInfo?.FilePath != null) {
                                var codeInfo = _scanBarCodeItems.FirstOrDefault(f =>
                                    f.BarCode.Equals(savedImageInfo.BarCode));
                                if (codeInfo is not null) {
                                    if (savedImageInfo.ImageType == SaveImageType.BarcodeImage) {
                                        codeInfo.BarcodeImageFilePath = savedImageInfo.FilePath;
                                    }
                                    else if (savedImageInfo.ImageType == SaveImageType.PanoramaImage) {
                                        codeInfo.PanoramaImageFilePaths?.Add(savedImageInfo.FilePath);
                                    }
                                    else if (savedImageInfo.ImageType == SaveImageType.VolumeImage) {
                                        codeInfo.VolumeImageFilePaths?.Add(savedImageInfo.FilePath);
                                    }
                                }
                            }
                        }

                        //告诉界面这些scanBarCodeInfos已经填充完全部信息，即将移除

                        var scanBarCodeInfos = _scanBarCodeItems.Where(w => w is { IsCompleted: true, IsSavedImage: true } &&
                                                                            w.PanoramaImageFilePaths?.Count ==
                                                                            _cameras.Count(c => c.BindingType == CameraBindingType.PanoramicCamera) &&
                                                                            w.VolumeImageFilePaths?.Count == _cameras.Count(c => c.BindingType == CameraBindingType.VolumeCamera))
                            .ToList();

                        var count = _scanBarCodeItems.Count;
                        while (count-- > 0) {
                            _scanBarCodeItems.TryDequeue(out var info);
                            if (info is null) continue;
                            if (!scanBarCodeInfos.Any(a => a.BarCode.Equals(info.BarCode))) {
                                _scanBarCodeItems.Enqueue(info);
                            }
                        }
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }

                await Task.Delay(50, stoppingToken);
            }
        }

        public class ScanBarCodeInfo {

            /// <summary>
            /// 条码
            /// </summary>
            public string BarCode { get; set; } = string.Empty;

            /// <summary>
            /// 条码图片
            /// </summary>
            public Image? Image { get; set; }

            /// <summary>
            /// 条码存图路径
            /// </summary>
            public string? BarcodeImageFilePath { get; set; }

            /// <summary>
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
            public double? Volume { get; set; }

            /// <summary>
            /// 全景图存图路径列表
            /// </summary>
            public List<string>? PanoramaImageFilePaths { get; set; } = new();

            /// <summary>
            /// 体积图存图路径列表
            /// </summary>
            public List<string>? VolumeImageFilePaths { get; set; } = new();

            /// <summary>
            /// 相机序列号
            /// </summary>
            public string CameraSerialNumber { get; set; } = string.Empty;

            /// <summary>
            /// 是否已完成(完成输出、上传、但未从集合删除)
            /// </summary>
            public bool IsCompleted;

            /// <summary>
            /// 是否完成存图
            /// </summary>
            public bool IsSavedImage;

            /// <summary>
            /// 扫码时间
            /// </summary>
            public DateTime ScanTime;

            /// <summary>
            /// 条码时间戳
            /// </summary>
            public long Timestamp { get; set; }

            //上传内容
            //上传返回内容
            //上传时间
            //Plc发送内容
            //Plc发送时间
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
        }
    }
}