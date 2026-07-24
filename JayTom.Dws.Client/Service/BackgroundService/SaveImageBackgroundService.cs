using NLog;
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
        private readonly ConcurrentQueue<ImageMessageInfo> _imageItems = new();
        private readonly SemaphoreSlim _semaphore = new(1);
        private ImageSettingsDto? _imageSettingsDto;
        private OcrSettingsDto? _ocrSettingsDto;
        private readonly ConcurrentQueue<Bitmap> _cropImageQueue = new();
        private bool _isWindowsClose;

        public SaveImageBackgroundService(IImageStorageService imageStorageService,
            IConfigRepository configRepository, IDeviceService deviceService) {
            _imageStorageService = imageStorageService;
            _configRepository = configRepository;
            _deviceService = deviceService;
            EventAggregator.Instance.Subscribe<ImageMessageInfo>(info => {
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
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(OnSettingsChanged);
            _deviceService.OcrContentRecognized += delegate (object? sender, OcrResult result) {
                if (_ocrSettingsDto?.IsSaveCropImage == true && !string.IsNullOrEmpty(_ocrSettingsDto.CropImagePath)) {
                    if (result?.CropImage is not null) {
                        _cropImageQueue.Enqueue(result.CropImage);
                    }
                }
            };
            EventAggregator.Instance.Subscribe<WindowsAction>(item => {
                if (item is WindowsAction { Type: WindowsActionType.Close }) {
                    _isWindowsClose = true;
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            await ReloadSettingsAsync("SaveImageSettings", stoppingToken).ConfigureAwait(false);
            await ReloadSettingsAsync("OcrSettings", stoppingToken).ConfigureAwait(false);

            try {
                while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                    var itemProcessed = false;
                    try {
                        if (_imageItems.TryDequeue(out var messageInfo) && messageInfo?.Image is not null) {
                            itemProcessed = true;
                            try {
                                await _imageStorageService.SaveImage(messageInfo.PackageTimestamped, messageInfo.Image,
                                    messageInfo.Type, messageInfo.BarCode, messageInfo.Weight,
                                    messageInfo.ScanTime, messageInfo.Length, messageInfo.Width,
                                    messageInfo.Height, messageInfo.Volume, messageInfo.CameraSerialNumber,
                                    stoppingToken).ConfigureAwait(false);
                            }
                            finally {
                                // 队列接管图片所有权；即使存储实现异常，也必须释放非托管图像资源。
                                messageInfo.Image.Dispose();
                            }
                        }

                        if (_cropImageQueue.TryDequeue(out var cropImage) && cropImage is not null) {
                            itemProcessed = true;
                            using (cropImage) {
                                var cropImagePath = _ocrSettingsDto?.CropImagePath;
                                if (!string.IsNullOrWhiteSpace(cropImagePath)) {
                                    var now = DateTime.Now;
                                    var directory = Path.Combine(cropImagePath, now.ToString("MM"),
                                        now.ToString("dd"), now.ToString("HH"));
                                    Directory.CreateDirectory(directory);
                                    cropImage.Save(Path.Combine(directory,
                                        $"{new DateTimeOffset(now).ToUnixTimeMilliseconds()}.jpg"));
                                }
                            }
                        }

                        if (!itemProcessed) {
                            await Task.Delay(100, stoppingToken).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                        break;
                    }
                    catch (Exception exception) {
                        LogManager.GetCurrentClassLogger().Error(exception, "存图异常");
                    }
                }
            }
            finally {
                // 服务停止后不再处理排队图片，显式释放以避免 GDI 句柄泄漏。
                while (_imageItems.TryDequeue(out var pendingImage)) {
                    pendingImage?.Image?.Dispose();
                }

                while (_cropImageQueue.TryDequeue(out var pendingCropImage)) {
                    pendingCropImage?.Dispose();
                }
            }
        }

        private void OnSettingsChanged(SettingsChangedEvent settings) {
            if (settings.SettingsName is "SaveImageSettings" or "OcrSettings") {
                _ = ReloadSettingsAsync(settings.SettingsName, CancellationToken.None);
            }
        }

        private async Task ReloadSettingsAsync(string settingsName, CancellationToken cancellationToken) {
            var lockTaken = false;
            try {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                lockTaken = true;
                var configInfoModel = await _configRepository
                    .FirstOrDefault(w => w.ConfigName.Equals(settingsName), cancellationToken)
                    .ConfigureAwait(false);

                if (settingsName == "SaveImageSettings") {
                    _imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(
                        configInfoModel?.Value ?? string.Empty) ?? new ImageSettingsDto();
                }
                else {
                    _ocrSettingsDto = JsonConvert.DeserializeObject<OcrSettingsDto>(
                        configInfoModel?.Value ?? string.Empty) ?? new OcrSettingsDto();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                // 服务停止时无需继续加载配置。
            }
            catch (Exception exception) {
                LogManager.GetCurrentClassLogger().Error(exception, $"加载{settingsName}配置失败");
                if (settingsName == "SaveImageSettings") {
                    _imageSettingsDto ??= new ImageSettingsDto();
                }
                else {
                    _ocrSettingsDto ??= new OcrSettingsDto();
                }
            }
            finally {
                if (lockTaken) {
                    _semaphore.Release();
                }
            }
        }
    }
}
