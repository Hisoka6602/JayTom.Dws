using System;
using System.IO;
using System.Linq;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading;
using System.Globalization;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Ftp;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Plugin.SaveImage;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using WatermarkPosition = JayTom.Dws.Plugin.SaveImage.WatermarkPosition;

namespace JayTom.Dws.Client.Service.ImageStorage {

    public class DefaultImageStorageService : IImageStorageService {
        private readonly ISaveImage _saveImage;
        private readonly IConfigRepository _configRepository;
        private readonly IFtp _ftp;
        private SemaphoreSlim _semaphore = new(1);
        private SemaphoreSlim _saveSemaphore = new(10);

        public DefaultImageStorageService(ISaveImage saveImage, IConfigRepository configRepository,
            IFtp ftp) {
            _saveImage = saveImage;
            _configRepository = configRepository;
            _ftp = ftp;
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async settings => {
                if (settings is SettingsChangedEvent { SettingsName: "SaveImageSettings" }) {
                    await _semaphore.WaitAsync();
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("SaveImageSettings"));
                    try {
                        ImageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel.Value);
                    }
                    catch (Exception e) {
                        OnImageSaveFailed(e);
                    }
                    ImageSettingsDto ??= new ImageSettingsDto();
                    if (ImageSettingsDto.IsFtpUploadEnabled) {
                        var (key, value) = await _ftp.Connect(ImageSettingsDto.FtpInfo.IpAddress,
                            ImageSettingsDto.FtpInfo.Port,
                            ImageSettingsDto.FtpInfo.Username,
                            ImageSettingsDto.FtpInfo.Password);
                        if (!key) {
                            OnImageSaveFailed(new Exception(value));
                        }
                    }
                    _semaphore.Release();
                }
            });
        }

        public ImageSettingsDto? ImageSettingsDto { get; private set; }

        public event EventHandler<Exception>? ImageSaveFailed;

        public event EventHandler<ImageSavedEventArgs>? ImageSaved;

        public async Task SaveImage(Image? image, SaveImageType type, string barCode, float weight, DateTime scanTime, float length,
            float width, float height, float volume, string cameraSerialNumber, CancellationToken cancellationToken = default) {
            if (image is null) return;
            if (ImageSettingsDto is null) {
                await _semaphore.WaitAsync(cancellationToken);
                var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("SaveImageSettings"), cancellationToken);
                try {
                    ImageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel.Value);
                }
                catch (Exception e) {
                    OnImageSaveFailed(e);
                }
                ImageSettingsDto ??= new ImageSettingsDto();
                if (ImageSettingsDto.IsFtpUploadEnabled) {
                    var (key, value) = await _ftp.Connect(ImageSettingsDto.FtpInfo.IpAddress,
                        ImageSettingsDto.FtpInfo.Port,
                        ImageSettingsDto.FtpInfo.Username,
                        ImageSettingsDto.FtpInfo.Password, cancellationToken);
                    if (!key) {
                        OnImageSaveFailed(new Exception(value));
                    }
                }
                _semaphore.Release();
            }

            //开始保存
            //获取存图目录(根目录+模板子目录)
            try {
                await _saveSemaphore.WaitAsync(cancellationToken);
                var pathList = ImageSettingsDto.SubDirectoryTemplate?
                    .Where(w => w is { ApplicationType: ItemApplicationType.SubDirectory, Type: 1 })?
                    .Select(s => ParseTemplate(s.Content, type, barCode, weight, scanTime, length, width, height,
                        volume, cameraSerialNumber))?
                    .ToList();
                if (pathList?.Any() != true) {
                    OnImageSaveFailed(new Exception(Languages.Language.ResourceManager.GetString("存图路径解析错误,未找到模板内容") ?? string.Empty));
                    return;
                }

                var fullPath = $"{ImageSettingsDto.ImageRootDirectory}\\{string.Join("\\", pathList)}";
                //解析图片命名模板
                var imageNaminglist = ImageSettingsDto.ImageNamingTemplate
                    ?.Where(w => w.ApplicationType == ItemApplicationType.ImageNaming)?
                    .Select(s => ParseTemplate(s.Content, type, barCode, weight, scanTime, length, width, height,
                        volume, cameraSerialNumber))
                    ?.ToList();
                if (imageNaminglist?.Any() != true) {
                    OnImageSaveFailed(new Exception(Languages.Language.ResourceManager.GetString("图片命名解析错误,未找到模板内容") ?? string.Empty));
                    return;
                }

                var imageName = string.Join("_", imageNaminglist);
                WatermarkParams? watermarkParams = null;
                //判断是否需要水印
                if (ImageSettingsDto.IsUseWatermark) {
                    //解析水印模板(使用图片命名解析)
                    var watermarkList = ImageSettingsDto.WatermarkInfo.ItemTemplate
                        ?.Where(w => w.ApplicationType == ItemApplicationType.Watermark)?
                        .Select(s => WatermarkParseTemplate(s.Content, type, barCode, weight, scanTime, length, width, height,
                            volume, cameraSerialNumber, true))
                        ?.ToList();
                    if (watermarkList?.Any() != true) {
                        OnImageSaveFailed(new Exception(Languages.Language.ResourceManager.GetString("图片命名解析错误,未找到模板内容") ?? string.Empty));
                        return;
                    }
                    watermarkParams = new WatermarkParams() {
                        FontSize = ImageSettingsDto.WatermarkInfo.WatermarkFontSize,
                        WatermarkColor = ImageSettingsDto.WatermarkInfo.WatermarkColor,
                        WatermarkPosition = (WatermarkPosition)ImageSettingsDto.WatermarkInfo.WatermarkPosition,
                        WatermarkContent = watermarkList
                    };
                }

                //判断是否保存原图
                if (ImageSettingsDto.IsSaveOriginalImage) {
                    var (key, value) = await _saveImage.SaveOriginalImage(image, imageName, fullPath, watermarkParams,
                        cancellationToken);
                    if (!key) {
                        OnImageSaveFailed(new Exception(value));
                    }
                    else {
                        OnImageSaved(new ImageSavedEventArgs() {
                            BarCode = barCode,
                            CameraSerialNumber = cameraSerialNumber,
                            FilePath =
                                $"{fullPath}\\{imageName}.{"jpg"}",
                            ImageType = type,
                            SaveDateTime = DateTime.Now,
                            ScanTime = scanTime
                        });
                    }
                }
                else {
                    var (key, value) = await _saveImage.SaveCompressedImage(image, imageName, fullPath, watermarkParams,
                        cancellationToken);
                    if (!key) {
                        NLog.LogManager.GetCurrentClassLogger().Error(value);
                        OnImageSaveFailed(new Exception(value));
                    }
                    else {
                        OnImageSaved(new ImageSavedEventArgs() {
                            BarCode = barCode,
                            CameraSerialNumber = cameraSerialNumber,
                            FilePath =
                                $"{fullPath}\\{imageName}.{"jpg"}",
                            ImageType = type,
                            SaveDateTime = DateTime.Now,
                            ScanTime = scanTime
                        });
                    }
                }

                //判断是否需要上传Ftp
                if (ImageSettingsDto.IsFtpUploadEnabled) {
                    var path = $"{fullPath}\\{imageName}.{"jpg"}";
                    if (File.Exists(path)) {
                        var (key, value) = await _ftp.UploadFile(path,
                            path.Replace(ImageSettingsDto.ImageRootDirectory, string.Empty),
                            cancellationToken);
                        if (!key) {
                            OnImageSaveFailed(new Exception(value));
                        }
                        else {
                            EventAggregator.Instance.Publish(new FtpLogInfoModel() {
                                Type = LogType.Information,
                                CreateTime = DateTime.Now,
                                Message = $"FTP上传:{path.Replace(ImageSettingsDto.ImageRootDirectory, string.Empty)}",
                                FtpCommunicationType = FtpCommunicationType.Upload
                            });
                        }
                    }
                    else {
                        OnImageSaveFailed(new Exception($"{Languages.Language.ResourceManager.GetString("FTPUpload") ?? string.Empty}{Languages.Language.ResourceManager.GetString("图片不存在") ?? string.Empty}"));
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                OnImageSaveFailed(new Exception($"{Languages.Language.ResourceManager.GetString("存图异常") ?? string.Empty}:{e.Message}"));
            }
            finally {
                image?.Dispose();
                _saveSemaphore.Release();
            }
        }

        public string ParseTemplate(string source, SaveImageType type, string barCode, float weight, DateTime scanTime, float length,
            float width, float height, float volume, string cameraSerialNumber, bool isWatermark = false) {
            return source switch {
                "{BarCode}" => $"{(isWatermark ? "BarCode:" : string.Empty)}{barCode}",
                "{Weight}" => $"{(isWatermark ? "Weight:" : string.Empty)}{weight.ToString(CultureInfo.InvariantCulture)}",
                "{Volume}" => $"{(isWatermark ? "Volume:" : string.Empty)}{volume.ToString(CultureInfo.InvariantCulture)}",
                "{Length}" => $"{(isWatermark ? "Length:" : string.Empty)}{length.ToString(CultureInfo.InvariantCulture)}",
                "{Width}" => $"{(isWatermark ? "Width:" : string.Empty)}{width.ToString(CultureInfo.InvariantCulture)}",
                "{Height}" => $"{(isWatermark ? "Height:" : string.Empty)}{height.ToString(CultureInfo.InvariantCulture)}",
                "{ScanTime}" => $"{(isWatermark ? "ScanTime:" : string.Empty)}{(isWatermark ? $"{scanTime:yyyy-MM-dd HH:mm:ss.fff}" : $"{scanTime:yyyyMMddHHmmssfff}")}",
                "{TimestampedGuid}" => $"{(isWatermark ? "TimestampedGuid:" : string.Empty)}{new DateTimeOffset(scanTime).ToUnixTimeMilliseconds().ToString()}",
                "{CameraSerialNumber}" => $"{(isWatermark ? "CameraSerialNumber:" : string.Empty)}{cameraSerialNumber}",
                "{ImageType}" => $"{(isWatermark ? "ImageType:" : string.Empty)}{type}",
                "{Year}" => $"{(isWatermark ? "Year:" : string.Empty)}{scanTime:yyyy}",
                "{Month}" => $"{(isWatermark ? "Month:" : string.Empty)}{scanTime:MM}",
                "{Day}" => $"{(isWatermark ? "Day:" : string.Empty)}{scanTime:dd}",
                "{Hour}" => $"{(isWatermark ? "Hour:" : string.Empty)}{scanTime:HH}",
                _ => "null"
            };
        }

        public string WatermarkParseTemplate(string source, SaveImageType type, string barCode, float weight,
            DateTime scanTime, float length,
            float width, float height, float volume, string cameraSerialNumber, bool isWatermark = false, string? language = default) {
            //默认中文
            return source switch {
                "{BarCode}" => $"{(isWatermark ? "条码:" : string.Empty)}{barCode}",
                "{Weight}" => $"{(isWatermark ? "重量:" : string.Empty)}{weight.ToString(CultureInfo.InvariantCulture)}",
                "{Volume}" => $"{(isWatermark ? "体积:" : string.Empty)}{volume.ToString(CultureInfo.InvariantCulture)}",
                "{Length}" => $"{(isWatermark ? "长度:" : string.Empty)}{length.ToString(CultureInfo.InvariantCulture)}",
                "{Width}" => $"{(isWatermark ? "宽度:" : string.Empty)}{width.ToString(CultureInfo.InvariantCulture)}",
                "{Height}" => $"{(isWatermark ? "高度:" : string.Empty)}{height.ToString(CultureInfo.InvariantCulture)}",
                "{ScanTime}" => $"{(isWatermark ? "扫码时间:" : string.Empty)}{(isWatermark ? $"{scanTime:yyyy-MM-dd HH:mm:ss.fff}" : $"{scanTime:yyyyMMddHHmmssfff}")}",
                "{TimestampedGuid}" => $"{(isWatermark ? "时间戳:" : string.Empty)}{new DateTimeOffset(scanTime).ToUnixTimeMilliseconds().ToString()}",
                "{CameraSerialNumber}" => $"{(isWatermark ? "相机序列号:" : string.Empty)}{cameraSerialNumber}",
                "{ImageType}" => $"{(isWatermark ? "图片类型:" : string.Empty)}{type}",
                "{Year}" => $"{(isWatermark ? "年:" : string.Empty)}{scanTime:yyyy}",
                "{Month}" => $"{(isWatermark ? "月:" : string.Empty)}{scanTime:MM}",
                "{Day}" => $"{(isWatermark ? "日:" : string.Empty)}{scanTime:dd}",
                "{Hour}" => $"{(isWatermark ? "时:" : string.Empty)}{scanTime:HH}",
                _ => ""
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