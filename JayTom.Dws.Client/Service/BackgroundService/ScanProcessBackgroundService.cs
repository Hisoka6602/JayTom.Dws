using System;
using DryIoc;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using JayTom.Dws.Camera;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.ImageStorage;
using JayTom.Dws.Client.Service.ResultOutput;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class ScanProcessBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IDeviceService _deviceService;
        private readonly IResultOutputService _resultOutputService;
        private readonly IImageStorageService _imageStorageService;

        private readonly List<ScanBarCodeInfo> _scanBarCodeItems = new();
        private SemaphoreSlim _semaphore = new(1);

        private List<ICamera> _cameras = new();
        private Queue<CameraImageInfo> _panoramicImageItems = new();
        private Queue<CameraImageInfo> _volumeCameraImageItems = new();
        private Queue<SavedImageInfo> _savedImageItems = new();
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
                    Weight = 0.5//先给一个默认重量，以后重量需要从传感器获取
                });
                _semaphore.Release();
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
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                if (_scanBarCodeItems.Count > 0) {
                    var scanBarCodeInfo = _scanBarCodeItems.FirstOrDefault(f => !f.IsCompleted && !string.IsNullOrEmpty(f.BarCode) && f.Width != null);
                    //判断填充包裹信息
                    if (scanBarCodeInfo is not null) {
                        //判断是不是有体积相机、如果有则判断是否已经获取体积
                        if (_cameras.Any(a => a.BindingType == CameraBindingType.VolumeCamera)) {
                            if (scanBarCodeInfo.Length is not null &&
                                scanBarCodeInfo.Width is not null &&
                                scanBarCodeInfo.Height is not null) {
                                //创建另一个对象处理耗时长的内容
                                //上传
                                //输出
                                //条码回调(告诉界面这个条码完成)
                                scanBarCodeInfo.IsCompleted = true;
                            }
                        }
                        else {
                            scanBarCodeInfo.Length = 0;
                            scanBarCodeInfo.Width = 0;
                            scanBarCodeInfo.Height = 0;
                        }
                    }

                    //判断全景图
                    if (_panoramicImageItems.Count > 0) {
                        var cameraImageInfo = _panoramicImageItems.Dequeue();
                        var codeInfo = _scanBarCodeItems.FirstOrDefault(f => f.PanoramaImages?.Count <
                                                                             _cameras.Count(c =>
                                                                                 c.BindingType == CameraBindingType.PanoramicCamera)
                        && !f.PanoramaImages.Any(a => a.CameraSerialNumber.Equals(cameraImageInfo.CameraSerialNumber)));
                        codeInfo?.PanoramaImages?.Add(cameraImageInfo);
                    }
                    //判断体积图
                    if (_volumeCameraImageItems.Count > 0) {
                        var cameraImageInfo = _volumeCameraImageItems.Dequeue();
                        var codeInfo = _scanBarCodeItems.FirstOrDefault(f => f.VolumeImages?.Count <
                                                                             _cameras.Count(c =>
                                                                                 c.BindingType == CameraBindingType.VolumeCamera)
                                                                             && !f.VolumeImages.Any(a => a.CameraSerialNumber.Equals(cameraImageInfo.CameraSerialNumber)));
                        codeInfo?.VolumeImages?.Add(cameraImageInfo);
                    }
                    //如果信息齐全则输出
                    //匹配体积
                    //匹配全景相机图片

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
                        _imageStorageService.SaveImage(barCodeInfo.Image, SaveImageType.PanoramaImage,
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
                            _imageStorageService.SaveImage(f.Image, SaveImageType.PanoramaImage,
                                barCodeInfo.BarCode, (float)(barCodeInfo.Weight ?? 0),
                                barCodeInfo.ScanTime, (float)(barCodeInfo.Length ?? 0),
                                (float)(barCodeInfo.Width ?? 0), (float)(barCodeInfo.Height ?? 0),
                                (float)(barCodeInfo.Volume ?? 0), f.CameraSerialNumber, stoppingToken);
                        });
                        barCodeInfo.IsSavedImage = true;
                    }
                    //匹配存图路径
                    //移除判断
                }
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
        }
    }
}