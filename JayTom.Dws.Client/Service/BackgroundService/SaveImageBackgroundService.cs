using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.ImageStorage;
using JayTom.Dws.Domain.Repository.LocalConf;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class SaveImageBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IImageStorageService _imageStorageService;
        private readonly IConfigRepository _configRepository;
        private ConcurrentQueue<ImageMessageInfo> _imageItems = new();
        private SemaphoreSlim _semaphore = new(1);
        public ImageSettingsDto? _imageSettingsDto;

        public SaveImageBackgroundService(IImageStorageService imageStorageService, IConfigRepository configRepository) {
            _imageStorageService = imageStorageService;
            _configRepository = configRepository;
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
            while (!stoppingToken.IsCancellationRequested) {
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

                await Task.Delay(50, stoppingToken);
            }
        }
    }
}