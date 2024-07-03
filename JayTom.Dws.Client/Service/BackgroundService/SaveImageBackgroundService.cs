using System;
using System.IO;
using System.Drawing;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Domain.Model;
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.ImageService;
using WindowsAction = JayTom.Dws.Client.EventMediators.WindowsAction;
using WindowsActionType = JayTom.Dws.Client.EventMediators.WindowsActionType;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;
using static JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.DaHuatechSecurityCamera;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class SaveImageBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IImageStorageService _imageStorageService;
        private readonly IConfigRepository _configRepository;
        private readonly IDeviceService _deviceService;
        private ConcurrentQueue<ImageMessageInfo> _imageItems = new();
        private SemaphoreSlim _semaphore = new(1);
        private ImageSettingsDto? _imageSettingsDto;
        private OcrSettingsDto? _ocrSettingsDto;
        private ConcurrentQueue<Bitmap> _cropImageQueue = new();
        private static bool _isWindowsClose;

        public SaveImageBackgroundService(IImageStorageService imageStorageService,
            IConfigRepository configRepository, IDeviceService deviceService) {
            _imageStorageService = imageStorageService;
            _configRepository = configRepository;
            _deviceService = deviceService;
            EventAggregator.Instance.Subscribe<ImageMessageInfo>(async info => {
                //判断是否需要存图
                if (info is ImageMessageInfo imageInfo) {
                    if (_imageSettingsDto is not null) {
                        if ((_imageSettingsDto.IsSaveBarcodeImage && imageInfo.Type == SaveImageType.BarcodeImage) ||
                            (_imageSettingsDto.IsSavePanoramaImage && imageInfo.Type == SaveImageType.PanoramaImage) ||
                            (_imageSettingsDto.IsSaveVolumeImage && imageInfo.Type == SaveImageType.VolumeImage)) {
                            _imageItems.Enqueue(imageInfo);
                            return;
                        }
                    }
                    imageInfo?.Image?.Dispose();
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async settings => {
                if (settings is SettingsChangedEvent { SettingsName: "SaveImageSettings" }) {
                    await _semaphore.WaitAsync();
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("SaveImageSettings"));
                    try {
                        _imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel.Value);
                    }
                    catch (Exception e) {
                        _imageSettingsDto ??= new ImageSettingsDto();
                    }
                    _semaphore.Release();
                }
                else if (settings is SettingsChangedEvent { SettingsName: "OcrSettings" }) {
                    await _semaphore.WaitAsync();
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("OcrSettings"));
                    try {
                        _ocrSettingsDto = JsonConvert.DeserializeObject<OcrSettingsDto>(configInfoModel?.Value ?? string.Empty);
                    }
                    catch (Exception e) {
                        _ocrSettingsDto ??= new OcrSettingsDto();
                    }
                    _semaphore.Release();
                }
            });
            _deviceService.OcrContentRecognized += delegate (object? sender, OcrResult result) {
                if (_ocrSettingsDto?.IsSaveCropImage == true && !string.IsNullOrEmpty(_ocrSettingsDto.CropImagePath)) {
                    if (result?.CropImage is not null) {
                        _cropImageQueue.Enqueue(result.CropImage);
                    }
                }
            };
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                if (item is WindowsAction { Type: WindowsActionType.Close }) {
                    _isWindowsClose = true;
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            if (_imageSettingsDto is null) {
                await _semaphore.WaitAsync(stoppingToken);
                var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("SaveImageSettings"), stoppingToken);
                try {
                    _imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel?.Value ?? string.Empty);
                }
                catch (Exception e) {
                    _imageSettingsDto ??= new ImageSettingsDto();
                }
                _semaphore.Release();
            }

            if (_ocrSettingsDto is null) {
                await _semaphore.WaitAsync(stoppingToken);
                var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("OcrSettings"), stoppingToken);
                try {
                    _ocrSettingsDto = JsonConvert.DeserializeObject<OcrSettingsDto>(configInfoModel?.Value ?? string.Empty);
                }
                catch (Exception e) {
                    _ocrSettingsDto ??= new OcrSettingsDto();
                }
                _semaphore.Release();
            }
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                await Task.Delay(100, stoppingToken).ContinueWith(async a => {
                    try {
                        var tryDequeue = _imageItems.TryDequeue(out var messageInfo);
                        if (tryDequeue && messageInfo is not null) {
                            if (messageInfo.Image is not null) {
                                await _imageStorageService.SaveImage(messageInfo.Image,
                                    messageInfo.Type, messageInfo.BarCode, messageInfo.Weight,
                                    messageInfo.ScanTime, messageInfo.Length, messageInfo.Width,
                                    messageInfo.Height, messageInfo.Volume, messageInfo.CameraSerialNumber
                                    , stoppingToken);
                            }
                        }
                        //存截图(按单号+时间戳)
                        var dequeue = _cropImageQueue.TryDequeue(out var cropImage);
                        if (dequeue && cropImage is not null) {
                            if (!string.IsNullOrEmpty(_ocrSettingsDto?.CropImagePath)) {
                                var directory =
                                    $"{_ocrSettingsDto.CropImagePath}\\{DateTime.Now:MM}\\{DateTime.Now:dd}\\{DateTime.Now:HH}";
                                if (!Directory.Exists(directory)) {
                                    Directory.CreateDirectory(directory);
                                }

                                var fileName =
                                    $"{directory}\\{new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds()}.jpg";
                                cropImage.Save(fileName);
                                cropImage?.Dispose();
                            }
                        }
                    }
                    catch (Exception e) {
                        NLog.LogManager.GetCurrentClassLogger().Error($"存图异常:{e}");
                    }
                }, stoppingToken);
            }
        }
    }
}