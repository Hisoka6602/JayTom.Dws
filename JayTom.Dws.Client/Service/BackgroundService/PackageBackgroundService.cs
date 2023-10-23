using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Camera;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Client.Service.ImageStorage;
using JayTom.Dws.Client.Service.ResultOutput;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Service.ExternalDataService;
using static JayTom.Dws.Client.Service.BackgroundService.ScanProcessBackgroundService;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class PackageBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IDeviceService _deviceService;
        private readonly IResultOutputService _resultOutputService;
        private readonly IImageStorageService _imageStorageService;
        private readonly IExternalDataService _externalDataService;
        private readonly IConfigRepository _configRepository;
        private readonly ISortingService _sortingService;
        private ExternalDataSourceEventArgs _externalDataSource = new();
        private List<ConfigInfoModel> _configInfoModels = new();
        private CommunicationsSettingsDto _communicationsSettingsDto = new();
        private List<ICamera> _cameras = new();
        private ConcurrentQueue<CameraImageInfo> _panoramicImageItems = new();
        private ConcurrentQueue<CameraImageInfo> _volumeCameraImageItems = new();
        private ConcurrentQueue<PackageInfo> _packageInfos = new();

        public PackageBackgroundService(IDeviceService deviceService,
            IResultOutputService resultOutputService,
            IImageStorageService imageStorageService,
            IExternalDataService externalDataService, IConfigRepository configRepository,
            ISortingService sortingService) {
            _deviceService = deviceService;
            _resultOutputService = resultOutputService;
            _imageStorageService = imageStorageService;
            _externalDataService = externalDataService;
            _configRepository = configRepository;
            _sortingService = sortingService;
            //相机
            _deviceService.CameraInitialized += delegate (object? sender, List<ICamera> list) {
                _cameras = list;
            };
            //扫码
            _deviceService.BarcodeScanned += async delegate (object? sender, BarcodeReadEventArgs args) {
                if (_communicationsSettingsDto.Type == CommunicationsType.None ||
                    !_communicationsSettingsDto.DeviceControlSettingsInfo.IsUseCreatePackageByDevice) {
                    var packageInfo = new PackageInfo() {
                        Guid = args.Timestamp,
                        BarCode = args.Barcode,
                        CameraSerialNumber = args.CameraSerialNumber,
                        Image = args.Image,
                        ScanTime = args.ScanTime,
                        Timestamp = args.Timestamp,
                    };
                    _packageInfos.Enqueue(packageInfo);
                    //获取外部数据
                    //体积
                    if (_externalDataSource.IsVolumeInput) {
                        await _externalDataService.GetVolume(args.Barcode);
                    }
                    EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                        IsSuccess = true,
                        TriggerPosition = TriggerPositionEnum.PackageTrigger
                    });
                }
                else {
                    var info = _packageInfos.FirstOrDefault(f => f.BarCode == null);
                    if (info != null) {
                        info.BarCode = args.Barcode;
                    }
                }
            };
            //空包裹
            _deviceService.NotBarcodeHitEvent += delegate (object? sender, BarcodeReadEventArgs args) {
                EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                    IsSuccess = false,
                    TriggerPosition = TriggerPositionEnum.PackageTrigger
                });
            };
            //全景相机
            _deviceService.PanoramaCaptured += async delegate (object? sender, PanoramaCaptureEventArgs args) {
                await Task.Yield();
                _panoramicImageItems.Enqueue(new CameraImageInfo() {
                    CameraSerialNumber = args.CameraSerialNumber,
                    Image = args.Image,
                    Barcode = args.Barcode,
                    BarcodeTimestamp = args.BarcodeTimestamp
                });
            };
            //体积相机
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
                //增加体积单位转换
                if (scanBarCodeInfo is not null) {
                    scanBarCodeInfo.Length = args.Length - scanBarCodeInfo.LengthToDeduct;
                    scanBarCodeInfo.Width = args.Width - scanBarCodeInfo.WidthToDeduct;
                    scanBarCodeInfo.Height = args.Height - scanBarCodeInfo.HeightToDeduct;
                    scanBarCodeInfo.Volume = args.Volume - scanBarCodeInfo.VolumeToDeduct;
                }
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                //读配置
                _configInfoModels = await _configRepository.Select(s => s.Id > 0,
                    o => o.Id, stoppingToken);
                var configInfoModel = _configInfoModels?.FirstOrDefault(f => f.ConfigName.Equals("CommunicationsSettings"));
                if (configInfoModel is not null) {
                    _communicationsSettingsDto = JsonConvert.DeserializeObject<CommunicationsSettingsDto>(configInfoModel.Value) ?? new CommunicationsSettingsDto();
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
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

        /// <summary>
        /// 条码
        /// </summary>
        public string? BarCode { get; set; }

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

        /// <summary>
        /// 需要扣除的重量
        /// </summary>
        public float LengthToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的宽度
        /// </summary>
        public float WidthToDeduct { get; set; }

        /// <summary>
        /// /需要扣除的重量
        /// </summary>
        public float WeightToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的高度
        /// </summary>
        public float HeightToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的体积
        /// </summary>
        public float VolumeToDeduct { get; set; }
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
}