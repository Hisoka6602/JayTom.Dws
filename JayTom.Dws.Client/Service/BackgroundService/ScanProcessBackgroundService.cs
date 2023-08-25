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

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class ScanProcessBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IDeviceService _deviceService;
        private readonly IResultOutputService _resultOutputService;
        private readonly IImageStorageService _imageStorageService;

        private readonly ConcurrentList<ScanBarCodeInfo> _scanBarCodeItems = new();
        private SemaphoreSlim _semaphore = new(1);

        private List<ICamera> _cameras = new();
        private ConcurrentQueue<CameraImageInfo> _panoramicImageItems = new();
        private ConcurrentQueue<CameraImageInfo> _volumeCameraImageItems = new();
        private ConcurrentQueue<SavedImageInfo> _savedImageItems = new();
        //private Queue<ScanBarCodeInfo> _scanBarCodeItems = new();

        public ScanProcessBackgroundService(IDeviceService deviceService,
            IResultOutputService resultOutputService, IImageStorageService imageStorageService) {
            _deviceService = deviceService;
            _resultOutputService = resultOutputService;
            _imageStorageService = imageStorageService;
            _imageStorageService.ImageSaved += delegate (object? sender, ImageSavedEventArgs args) {
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
                await _semaphore.WaitAsync();
                _scanBarCodeItems.Add(new ScanBarCodeInfo() {
                    BarCode = args.Barcode,
                    CameraSerialNumber = args.CameraSerialNumber,
                    Image = args.Image,
                    ScanTime = args.ScanTime,
                    Timestamp = args.Timestamp,
                });
                _semaphore.Release();
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
                });
            };
            _deviceService.VolumeCaptured += async delegate (object? sender, VolumeCapturedEventArgs args) {
                await Task.Yield();
                _volumeCameraImageItems.Enqueue(new CameraImageInfo() {
                    CameraSerialNumber = args.CameraSerialNumber,
                    Image = args.Image,
                });
                //填充长宽高
                await _semaphore.WaitAsync();
                var scanBarCodeInfo = _scanBarCodeItems.FirstOrDefault(f => f.Length == null ||
                                                                            f.Width == null ||
                                                                            f.Height == null);
                if (scanBarCodeInfo is not null) {
                    scanBarCodeInfo.Length = args.Length;
                    scanBarCodeInfo.Width = args.Width;
                    scanBarCodeInfo.Height = args.Height;
                    scanBarCodeInfo.Volume = args.Volume;
                }
                _semaphore.Release();
            };
            _deviceService.StableWeight += delegate (object? sender, StableWeightEventArgs args) {
                var scanBarCodeInfo = _scanBarCodeItems.FirstOrDefault(f => !f.IsCompleted && f.Weight is null);
                if (scanBarCodeInfo is not null) {
                    scanBarCodeInfo.Weight = args.Weight;
                }
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
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
                            if (_cameras.All(a => a.BindingType != CameraBindingType.VolumeCamera)) {
                                scanBarCodeInfo.Length = 0;
                                scanBarCodeInfo.Width = 0;
                                scanBarCodeInfo.Height = 0;
                            }
                        }
                        //填充重量信息
                        if (_deviceService.ScaleType == ScaleType.None) {
                            scanBarCodeInfo.Weight = 0;
                        }
                    }
                    //判断全景图
                    if (_panoramicImageItems.Count > 0) {
                        _panoramicImageItems.TryDequeue(out var cameraImageInfo);
                        if (cameraImageInfo is not null) {
                            var codeInfo = _scanBarCodeItems.FirstOrDefault(f => f.PanoramaImages?.Count <
                                _cameras.Count(c =>
                                    c.BindingType == CameraBindingType.PanoramicCamera)
                                && !f.PanoramaImages.Any(a => a.CameraSerialNumber.Equals(cameraImageInfo.CameraSerialNumber)));
                            codeInfo?.PanoramaImages?.Add(cameraImageInfo);
                        }
                    }
                    //判断体积图
                    if (_volumeCameraImageItems.Count > 0) {
                        _volumeCameraImageItems.TryDequeue(out var cameraImageInfo);
                        if (cameraImageInfo is not null) {
                            var codeInfo = _scanBarCodeItems.FirstOrDefault(f => f.VolumeImages?.Count <
                                _cameras.Count(c =>
                                    c.BindingType == CameraBindingType.VolumeCamera)
                                && !f.VolumeImages.Any(a => a.CameraSerialNumber.Equals(cameraImageInfo.CameraSerialNumber)));
                            codeInfo?.VolumeImages?.Add(cameraImageInfo);
                        }
                    }
                    //存图判断
                    var barCodeInfo = _scanBarCodeItems.FirstOrDefault(f => !f.IsSavedImage &&
                        f is { IsCompleted: true, PanoramaImages: not null, VolumeImages: not null }
                        && f.PanoramaImages.Count == _cameras.Count(c =>
                            c.BindingType == CameraBindingType.PanoramicCamera) &&
                        f.VolumeImages.Count == _cameras.Count(c =>
                            c.BindingType == CameraBindingType.VolumeCamera));

                    if (barCodeInfo is not null) {
                        //存图
                        //扫码图
                        _imageStorageService.SaveImage(barCodeInfo.Image, SaveImageType.BarcodeImage,
                            barCodeInfo.BarCode, (float)(barCodeInfo.Weight ?? 0),
                            barCodeInfo.ScanTime, (float)(barCodeInfo.Length ?? 0),
                            (float)(barCodeInfo.Width ?? 0), (float)(barCodeInfo.Height ?? 0),
                            (float)(barCodeInfo.Volume ?? 0), barCodeInfo.CameraSerialNumber, stoppingToken);
                        //全景图
                        barCodeInfo.PanoramaImages?.ForEach(f => {
                            _imageStorageService.SaveImage(f.Image, SaveImageType.PanoramaImage,
                                barCodeInfo.BarCode, (float)(barCodeInfo.Weight ?? 0),
                                barCodeInfo.ScanTime, (float)(barCodeInfo.Length ?? 0),
                                (float)(barCodeInfo.Width ?? 0), (float)(barCodeInfo.Height ?? 0),
                                (float)(barCodeInfo.Volume ?? 0), f.CameraSerialNumber, stoppingToken);
                        });
                        //体积图
                        barCodeInfo.VolumeImages?.ForEach(f => {
                            _imageStorageService.SaveImage(f.Image, SaveImageType.VolumeImage,
                                barCodeInfo.BarCode, (float)(barCodeInfo.Weight ?? 0),
                                barCodeInfo.ScanTime, (float)(barCodeInfo.Length ?? 0),
                                (float)(barCodeInfo.Width ?? 0), (float)(barCodeInfo.Height ?? 0),
                                (float)(barCodeInfo.Volume ?? 0), f.CameraSerialNumber, stoppingToken);
                        });
                        barCodeInfo.IsSavedImage = true;
                    }

                    if (_savedImageItems.Count > 0) {
                        _savedImageItems.TryDequeue(out var savedImageInfo);
                        if (savedImageInfo is not null) {
                            var codeInfo = _scanBarCodeItems.FirstOrDefault(f =>
                                f.BarCode.Equals(savedImageInfo.BarCode));
                            if (codeInfo is not null) {
                                if (savedImageInfo.ImageType == SaveImageType.BarcodeImage) {
                                    codeInfo.BarcodeImageFilePath = savedImageInfo.FilePath;
                                }
                                else if (savedImageInfo.ImageType == SaveImageType.PanoramaImage) {
                                    var imageInfo = codeInfo.PanoramaImages?.FirstOrDefault(f =>
                                        f.CameraSerialNumber.Equals(savedImageInfo.CameraSerialNumber));
                                    if (imageInfo is not null) {
                                        imageInfo.ImageFilePath = savedImageInfo.FilePath;
                                    }
                                }
                                else if (savedImageInfo.ImageType == SaveImageType.VolumeImage) {
                                    var imageInfo = codeInfo.VolumeImages?.FirstOrDefault(f =>
                                        f.CameraSerialNumber.Equals(savedImageInfo.CameraSerialNumber));
                                    if (imageInfo is not null) {
                                        imageInfo.ImageFilePath = savedImageInfo.FilePath;
                                    }
                                }
                            }
                        }
                    }

                    var scanBarCodeInfos = _scanBarCodeItems.Where(w => w is { PanoramaImages: not null, VolumeImages: not null, IsCompleted: true, IsSavedImage: true }
                                                                        && w.PanoramaImages.All(a => a.ImageFilePath != null)
                                                                        && w.VolumeImages.All(a => a.ImageFilePath != null)).ToList();

                    //告诉界面这些scanBarCodeInfos已经填充完全部信息，即将移除

                    await _semaphore.WaitAsync(stoppingToken);
                    scanBarCodeInfos.ForEach(f => {
                        _scanBarCodeItems.Remove(f);
                    });
                    _semaphore.Release();
                }

                await Task.Delay(200, stoppingToken);
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
            /// 全景图列表
            /// </summary>
            public List<CameraImageInfo>? PanoramaImages { get; set; } = new();

            /// <summary>
            /// 体积图列表
            /// </summary>
            public List<CameraImageInfo>? VolumeImages { get; set; } = new();

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
            public Image? Image { get; set; }
            public string CameraSerialNumber { get; set; } = string.Empty;
            public string? ImageFilePath { get; set; }
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
    }
}