using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading;
using System.Globalization;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Ftp;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Plugin.SaveImage;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using WatermarkPosition = JayTom.Dws.Plugin.SaveImage.WatermarkPosition;

namespace JayTom.Dws.Client.Service.ImageStorage {

    public class DefaultImageStorageService : IImageStorageService {
        private readonly ISaveImage _saveImage;
        private readonly IConfigRepository _configRepository;
        private readonly IFtp _ftp;
        private ImageSettingsDto? _imageSettingsDto;
        private SemaphoreSlim _semaphore = new(1);

        public DefaultImageStorageService(ISaveImage saveImage, IConfigRepository configRepository,
            IFtp ftp) {
            _saveImage = saveImage;
            _configRepository = configRepository;
            _ftp = ftp;
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async settings => {
                if (settings is SettingsChangedEvent { SettingsName: "SaveImageSettings" }) {
                    await _semaphore.WaitAsync();
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("SaveImageSettings"));
                    if (configInfoModel is not null) {
                        try {
                            _imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel.Value);
                        }
                        catch (Exception e) {
                            OnImageSaveFailed(e);
                        }
                    }
                    _imageSettingsDto ??= new ImageSettingsDto();
                    if (_imageSettingsDto.IsFtpUploadEnabled) {
                        var (key, value) = await _ftp.Connect(_imageSettingsDto.FtpInfo.IpAddress, _imageSettingsDto.FtpInfo.Username,
                            _imageSettingsDto.FtpInfo.Password);
                        if (!key) {
                            OnImageSaveFailed(new Exception(value));
                        }
                    }
                    _semaphore.Release();
                }
            });
        }

        public event EventHandler<Exception>? ImageSaveFailed;

        public event EventHandler<ImageSavedEventArgs>? ImageSaved;

        public async void SaveImage(Image image, SaveImageType type, string barCode, float weight, DateTime scanTime, float length,
            float width, float height, float volume, string cameraSerialNumber, CancellationToken cancellationToken = default) {
            if (_imageSettingsDto is null) {
                await _semaphore.WaitAsync(cancellationToken);
                var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("SaveImageSettings"), cancellationToken);
                if (configInfoModel is not null) {
                    try {
                        _imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel.Value);
                    }
                    catch (Exception e) {
                        OnImageSaveFailed(e);
                    }
                }

                _imageSettingsDto ??= new ImageSettingsDto();
                if (_imageSettingsDto.IsFtpUploadEnabled) {
                    var (key, value) = await _ftp.Connect(_imageSettingsDto.FtpInfo.IpAddress, _imageSettingsDto.FtpInfo.Username,
                        _imageSettingsDto.FtpInfo.Password, cancellationToken);
                    if (!key) {
                        OnImageSaveFailed(new Exception(value));
                    }
                }
                _semaphore.Release();
            }
            //判断不需要保存的图即刻返回
            Task.Run(async () => {
                //开始保存
                //获取存图目录(根目录+模板子目录)
                var pathList = _imageSettingsDto.SubDirectoryTemplate?
                    .Where(w => w is { ApplicationType: ItemApplicationType.SubDirectory, Type: 0 })?
                    .Select(s => ParseTemplate(s.Content, type, barCode, weight, scanTime, length, width, height,
                        volume, cameraSerialNumber))?
                    .ToList();
                if (pathList?.Any() != true) {
                    OnImageSaveFailed(new Exception("存图路径解析错误,未找到模板内容!"));
                    return;
                }
                var fullPath = $"{_imageSettingsDto.ImageRootDirectory}\\{string.Join("\\", pathList)}";
                //解析图片命名模板
                var imageNaminglist = _imageSettingsDto.ImageNamingTemplate
                    ?.Where(w => w.ApplicationType == ItemApplicationType.ImageNaming)?
                    .Select(s => ParseTemplate(s.Content, type, barCode, weight, scanTime, length, width, height,
                        volume, cameraSerialNumber))
                    ?.ToList();
                if (imageNaminglist?.Any() != true) {
                    OnImageSaveFailed(new Exception("图片命名解析错误,未找到模板内容!"));
                    return;
                }
                var imageName = string.Join("_", imageNaminglist);
                WatermarkParams? watermarkParams = null;
                //判断是否需要水印
                if (_imageSettingsDto.IsUseWatermark) {
                    //解析水印模板(使用图片命名解析)
                    var watermarkList = _imageSettingsDto.WatermarkInfo.ItemTemplate?.Where(w => w.ApplicationType == ItemApplicationType.ImageNaming)?
                        .Select(s => ParseTemplate(s.Content, type, barCode, weight, scanTime, length, width, height,
                            volume, cameraSerialNumber, true))
                        ?.ToList();
                    if (watermarkList?.Any() != true) {
                        OnImageSaveFailed(new Exception("图片命名解析错误,未找到模板内容!"));
                        return;
                    }

                    watermarkParams = new WatermarkParams() {
                        FontSize = _imageSettingsDto.WatermarkInfo.WatermarkFontSize,
                        WatermarkColor = _imageSettingsDto.WatermarkInfo.WatermarkColor,
                        WatermarkPosition = (WatermarkPosition)_imageSettingsDto.WatermarkInfo.WatermarkPosition,
                        WatermarkContent = watermarkList
                    };
                }
                //判断是否保存原图
                if (_imageSettingsDto.IsSaveOriginalImage) {
                    var (key, value) = await _saveImage.SaveOriginalImage(image, imageName, fullPath, watermarkParams, cancellationToken);
                    if (!key) {
                        OnImageSaveFailed(new Exception(value));
                    }
                    else {
                        OnImageSaved(new ImageSavedEventArgs() {
                            BarCode = barCode,
                            CameraSerialNumber = cameraSerialNumber,
                            FilePath = $"{fullPath}\\{imageName}.{(_imageSettingsDto.IsSaveOriginalImage ? "bmp" : "jpg")}",
                            ImageType = type,
                            SaveDateTime = DateTime.Now
                        });
                    }
                }
                else {
                    var (key, value) = await _saveImage.SaveCompressedImage(image, imageName, fullPath, watermarkParams, cancellationToken);
                    if (!key) {
                        OnImageSaveFailed(new Exception(value));
                    }
                    else {
                        OnImageSaved(new ImageSavedEventArgs() {
                            BarCode = barCode,
                            CameraSerialNumber = cameraSerialNumber,
                            FilePath = $"{fullPath}\\{imageName}.{(_imageSettingsDto.IsSaveOriginalImage ? "bmp" : "jpg")}",
                            ImageType = type,
                            SaveDateTime = DateTime.Now
                        });
                    }
                }
                //判断是否需要上传Ftp
                if (_imageSettingsDto.IsFtpUploadEnabled) {
                    var path = $"{fullPath}\\{imageName}.{(_imageSettingsDto.IsSaveOriginalImage ? "bmp" : "jpg")}";
                    if (File.Exists(path)) {
                        var (key, value) = await _ftp.UploadFile(path, path.Replace(_imageSettingsDto.ImageRootDirectory, string.Empty),
                            cancellationToken);
                        if (!key) {
                            OnImageSaveFailed(new Exception(value));
                        }
                    }
                }
            });
        }

        public string ParseTemplate(string source, SaveImageType type, string barCode, float weight, DateTime scanTime, float length,
            float width, float height, float volume, string cameraSerialNumber, bool isWatermark = false) {
            return source switch {
                "{BarCode}" => barCode,
                "{Weight}" => weight.ToString(CultureInfo.InvariantCulture),
                "{Volume}" => volume.ToString(CultureInfo.InvariantCulture),
                "{Length}" => length.ToString(CultureInfo.InvariantCulture),
                "{Width}" => width.ToString(CultureInfo.InvariantCulture),
                "{Height}" => height.ToString(CultureInfo.InvariantCulture),
                "{ScanTime}" => isWatermark ? $"{scanTime:yyyy-MM-dd HH:mm:ss.fff}" : $"{scanTime:yyyyMMddHHmmssfff}",
                "{TimestampedGuid}" => new DateTimeOffset(scanTime).ToUnixTimeMilliseconds().ToString(),
                "{CameraSerialNumber}" => cameraSerialNumber,
                "{ImageType}" => type.ToString(),
                "{Year}" => $"{scanTime:yyyy}",
                "{Month}" => $"{scanTime:MM}",
                "{Day}" => $"{scanTime:dd}",
                "{Hour}" => $"{scanTime:hh}",
                _ => "null"
            };
        }

        protected virtual async void OnImageSaveFailed(Exception e) {
            await Task.Yield();
            ImageSaveFailed?.Invoke(this, e);
        }

        protected virtual async void OnImageSaved(ImageSavedEventArgs e) {
            await Task.Yield();
            ImageSaved?.Invoke(this, e);
        }
    }
}