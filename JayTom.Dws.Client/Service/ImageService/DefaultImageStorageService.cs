using JayTom.Dws.Application.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Globalization;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Abstractions.Integrations.Ftp;
using JayTom.Dws.Abstractions.Imaging;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Plugin.SaveImage;
using JayTom.Dws.Domain.Converters;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Legacy.Contracts.Dto.BaseInfoModels;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Legacy.Contracts.Services.ImageService;
using JayTom.Dws.Application.Workflows;
using WatermarkPosition = JayTom.Dws.Plugin.SaveImage.WatermarkPosition;
using SettingsChangedEvent = JayTom.Dws.Application.Events.SettingsChangedEvent;

namespace JayTom.Dws.Client.Service.ImageService
{

    public class DefaultImageStorageService : IImageStorageService, IAsyncDisposable
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;
        /// <summary>
        /// 用于清除水印和文件名中控制字符的复用正则。
        /// </summary>
        private static readonly Regex ControlCharactersRegex =
            new(@"[\u0000-\u001D\b]", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private readonly ISaveImage _saveImage;
        private readonly ISettingsStore _settingsStore;
        private readonly IFtp _ftp;
        /// <summary>限制长时间 FTP 故障期间保留在内存中的远程上传任务数量。</summary>
        private const long MaximumPendingFtpUploads = 4_096;
        /// <summary>隔离本地存图与远程 FTP 网络延迟的顺序上传器。</summary>
        private readonly AsyncOrderedDispatcher<FtpUploadWork> _ftpUploadDispatcher;
        /// <summary>停止 FTP 上传工作器时使用的取消源。</summary>
        private readonly CancellationTokenSource _ftpShutdown = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly SemaphoreSlim _saveSemaphore = new(1, 1);
        private VolumeSettingsDto? _volumeSettingsDto;

        public DefaultImageStorageService(ISaveImage saveImage, ISettingsStore settingsStore,
            IFtp ftp,
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            _saveImage = saveImage;
            _settingsStore = settingsStore;
            _ftp = ftp;
            _ftpUploadDispatcher = new AsyncOrderedDispatcher<FtpUploadWork>(
                ProcessFtpUploadAsync,
                (_, exception) => OnImageSaveFailed(exception));
            _eventBus.SubscribeAsync<SettingsChangedEvent>(async settings =>
            {
                switch (settings)
                {
                    case SettingsChangedEvent { SettingsName: "SaveImageSettings" } model:
                        {
                            await _semaphore.WaitAsync();
                            try
                            {
                                ImageSettingsDto = await _settingsStore.GetAsync<ImageSettingsDto>(model.SettingsName) ?? new ImageSettingsDto();
                            }
                            catch (Exception exception)
                            {
                                NLog.LogManager.GetCurrentClassLogger()
                                    .Error(exception, "重新加载存图配置失败");
                            }
                            finally
                            {
                                _semaphore.Release();
                            }
                            break;
                        }
                    case SettingsChangedEvent { SettingsName: "VolumeSettings" } volumeSettings:
                        try
                        {
                            _volumeSettingsDto = await _settingsStore
                                .GetAsync<VolumeSettingsDto>(volumeSettings.SettingsName)
                                ?? new VolumeSettingsDto();
                        }
                        catch (Exception exception)
                        {
                            NLog.LogManager.GetCurrentClassLogger()
                                .Error(exception, "重新加载体积配置失败");
                        }
                        break;
                }
            });
        }

        /// <summary>保存可原子替换的存图配置，单次保存全程使用同一版本。</summary>
        private ImageSettingsDto? _imageSettingsDto;

        public ImageSettingsDto? ImageSettingsDto
        {
            get => Volatile.Read(ref _imageSettingsDto);
            private set => Volatile.Write(ref _imageSettingsDto, value);
        }

        public event EventHandler<Exception>? ImageSaveFailed;

        public event EventHandler<ImageSavedEventArgs>? ImageSaved;

        public async Task SaveAndDisposeImageAsync(ImageHandle image, SaveImageType type, string barCode, decimal weight, DateTime scanTime, decimal length,
            decimal width, decimal height, decimal volume, string cameraSerialNumber, CancellationToken cancellationToken = default)
        {
            await SaveAndDisposeImageAsync(
                0,
                image,
                type,
                barCode,
                weight,
                scanTime,
                length,
                width,
                height,
                volume,
                cameraSerialNumber,
                cancellationToken);
        }

        public async Task SaveAndDisposeImageAsync(long packageTimestamped, ImageHandle image, SaveImageType type, string barCode, decimal weight, DateTime scanTime,
            decimal length, decimal width, decimal height, decimal volume, string cameraSerialNumber,
            CancellationToken cancellationToken = default)
        {
            var drawingImage = image.As<Image>();
            try
            {
                var imageSettings = ImageSettingsDto;
                if (imageSettings is null)
                {
                    imageSettings = await _settingsStore
                        .GetAsync<ImageSettingsDto>("SaveImageSettings", cancellationToken)
                        ?? new ImageSettingsDto();
                    ImageSettingsDto = imageSettings;
                }
                var configurationLockTaken = false;
                try
                {
                    await _semaphore.WaitAsync(cancellationToken);
                    configurationLockTaken = true;
                    _volumeSettingsDto ??= await _settingsStore
                        .GetAsync<VolumeSettingsDto>("VolumeSettings", cancellationToken)
                        ?? new VolumeSettingsDto();
                }
                finally
                {
                    if (configurationLockTaken)
                    {
                        _semaphore.Release();
                    }
                }

                //开始保存
                //获取存图目录(根目录+模板子目录)
                var saveLockTaken = false;
                try
                {
                    await _saveSemaphore.WaitAsync(cancellationToken);
                    saveLockTaken = true;
                var pathList = imageSettings.SubDirectoryTemplate?
                    .Where(w => w is { ApplicationType: ItemApplicationType.SubDirectory, Type: 1 })?
                    .Select(s => ParseTemplate(s.Content, type, barCode, weight, scanTime, length, width, height,
                        volume, cameraSerialNumber))?
                    .ToList();
                if (pathList?.Any() != true)
                {
                    OnImageSaveFailed(new Exception("存图路径解析错误,未找到模板内容"));
                    return;
                }

                var fullPath = $"{imageSettings.ImageRootDirectory}\\{string.Join("\\", pathList)}";
                //解析图片命名模板
                var imageNaminglist = imageSettings.ImageNamingTemplate
                    ?.Where(w => w.ApplicationType == ItemApplicationType.ImageNaming)?
                    .Select(s => ParseTemplate(s.Content, type, barCode, weight, scanTime, length, width, height,
                        volume, cameraSerialNumber))
                    ?.ToList();
                if (imageNaminglist?.Any() != true)
                {
                    OnImageSaveFailed(new Exception("图片命名解析错误,未找到模板内容"));
                    return;
                }

                var imageName = string.Join("_", imageNaminglist);
                WatermarkParams? watermarkParams = null;
                //判断是否需要水印
                if (imageSettings.IsUseWatermark)
                {
                    //解析水印模板(使用图片命名解析)
                    var watermarkList = imageSettings.WatermarkInfo.ItemTemplate
                        ?.Where(w => w.ApplicationType == ItemApplicationType.Watermark)?
                        .Select(s => WatermarkParseTemplate(s.Content, type, barCode, weight, scanTime, length, width, height,
                            volume, cameraSerialNumber, true))
                        ?.ToList();
                    if (watermarkList?.Any() != true)
                    {
                        OnImageSaveFailed(new Exception("图片命名解析错误,未找到模板内容"));
                        return;
                    }
                    watermarkParams = new WatermarkParams()
                    {
                        FontSize = imageSettings.WatermarkInfo.WatermarkFontSize,
                        WatermarkColor = Color.FromArgb(imageSettings.WatermarkInfo.WatermarkColor.A,
                            imageSettings.WatermarkInfo.WatermarkColor.R,
                            imageSettings.WatermarkInfo.WatermarkColor.G,
                            imageSettings.WatermarkInfo.WatermarkColor.B),
                        WatermarkPosition = (WatermarkPosition)imageSettings.WatermarkInfo.WatermarkPosition,
                        WatermarkContent = watermarkList
                    };
                }

                //判断是否保存原图
                if (imageSettings.IsSaveOriginalImage)
                {
                    var (key, value) = await _saveImage.SaveOriginalImage(drawingImage, imageName, fullPath, watermarkParams,
                        cancellationToken);
                    if (!key)
                    {
                        throw new InvalidOperationException(value);
                    }
                    else
                    {
                        OnImageSaved(new ImageSavedEventArgs()
                        {
                            BarCode = barCode,
                            CameraSerialNumber = cameraSerialNumber,
                            FilePath =
                                $"{fullPath}\\{imageName}.{"jpg"}",
                            ImageType = type,
                            SaveDateTime = DateTime.Now,
                            ScanTime = scanTime,
                            PackageTimestamp = packageTimestamped
                        });
                    }
                }
                else
                {
                    var (key, value) = await _saveImage.SaveCompressedImage(drawingImage, imageName, fullPath, watermarkParams,
                        cancellationToken);
                    if (!key)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error(value);
                        throw new InvalidOperationException(value);
                    }
                    else
                    {
                        OnImageSaved(new ImageSavedEventArgs()
                        {
                            BarCode = barCode,
                            CameraSerialNumber = cameraSerialNumber,
                            FilePath =
                                $"{fullPath}\\{imageName}.{"jpg"}",
                            ImageType = type,
                            SaveDateTime = DateTime.Now,
                            ScanTime = scanTime,
                            PackageTimestamp = packageTimestamped
                        });
                    }
                }

                //判断是否需要上传Ftp
                if (imageSettings.IsFtpUploadEnabled)
                {
                    var path = $"{fullPath}\\{imageName}.{"jpg"}";
                    if (File.Exists(path))
                    {
                        var ftpInfo = imageSettings.FtpInfo;
                        if (_ftpUploadDispatcher.PendingCount >= MaximumPendingFtpUploads)
                        {
                            OnImageSaveFailed(new InvalidOperationException(
                                $"FTP 上传积压已达到保护上限 {MaximumPendingFtpUploads}，" +
                                $"本地图片已保留:{path}"));
                        }
                        else if (!_ftpUploadDispatcher.TryEnqueue(new FtpUploadWork(
                                path,
                                path.Replace(
                                    imageSettings.ImageRootDirectory,
                                    string.Empty),
                                ftpInfo.IpAddress,
                                ftpInfo.Port,
                                ftpInfo.Username,
                                ftpInfo.Password)))
                        {
                            OnImageSaveFailed(new InvalidOperationException(
                                "FTP 上传队列已经停止"));
                        }
                    }
                    else
                    {
                        OnImageSaveFailed(new Exception($"图片不存在"));
                    }
                }
                }
                catch (Exception e)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                    OnImageSaveFailed(e);
                    throw;
                }
                finally
                {
                    if (saveLockTaken)
                    {
                        _saveSemaphore.Release();
                    }
                }
            }
            finally
            {
                image.Dispose();
            }
        }

        /// <summary>在低优先级专用线程中连接 FTP 并上传已落盘文件。</summary>
        private async Task ProcessFtpUploadAsync(FtpUploadWork work)
        {
            var token = _ftpShutdown.Token;
            token.ThrowIfCancellationRequested();
            if (!_ftp.IsConnected)
            {
                var (connected, message) = await _ftp.Connect(
                        work.Server,
                        work.Port,
                        work.Username,
                        work.Password,
                        token);
                if (!connected)
                {
                    throw new InvalidOperationException(message);
                }
            }

            var (uploaded, uploadMessage) = await _ftp.UploadFile(
                    work.LocalPath,
                    work.RemotePath,
                    token);
            if (!uploaded)
            {
                throw new InvalidOperationException(uploadMessage);
            }

            _eventBus.Publish(new FtpLogInfoModel
            {
                Type = LogType.Information,
                CreateTime = DateTime.Now,
                Message = $"FTP上传:{work.RemotePath}",
                FtpCommunicationType = FtpCommunicationType.Upload
            });
        }

        /// <summary>停止远程上传器并释放图像服务持有的同步资源。</summary>
        public async ValueTask DisposeAsync()
        {
            _ftpShutdown.Cancel();
            await _ftpUploadDispatcher.DisposeAsync().ConfigureAwait(false);
            _ftpShutdown.Dispose();
            _saveSemaphore.Dispose();
            _semaphore.Dispose();
        }

        public string ParseTemplate(string source, SaveImageType type, string barCode, decimal weight, DateTime scanTime, decimal length,
            decimal width, decimal height, decimal volume, string cameraSerialNumber, bool isWatermark = false)
        {
            return source switch
            {
                "{BarCode}" => $"{(isWatermark ? "BarCode:" : string.Empty)}{(isWatermark ? RemoveControlCharacters(barCode) : SanitizeFileSystemSegment(barCode))}",
                "{Weight}" => $"{(isWatermark ? "Weight:" : string.Empty)}{weight.ToString(CultureInfo.InvariantCulture)}",
                "{Volume}" => $"{(isWatermark ? "Volume:" : string.Empty)}{(volume / _volumeSettingsDto?.Unit switch
                {
                    VolumeUnit.Millimeter => 1,
                    VolumeUnit.Centimeter => 1_000m,
                    VolumeUnit.Meter => 1_000_000_000m,
                    _ => 1
                })
                    .ToString(CultureInfo.InvariantCulture)}",
                "{Length}" => $"{(isWatermark ? "Length:" : string.Empty)}{(length / _volumeSettingsDto?.Unit switch
                {
                    VolumeUnit.Millimeter => 1,
                    VolumeUnit.Centimeter => 10,
                    VolumeUnit.Meter => 1000,
                    _ => 1
                })
                    .ToString(CultureInfo.InvariantCulture)}",
                "{Width}" => $"{(isWatermark ? "Width:" : string.Empty)}{(width / _volumeSettingsDto?.Unit switch
                {
                    VolumeUnit.Millimeter => 1,
                    VolumeUnit.Centimeter => 10,
                    VolumeUnit.Meter => 1000,
                    _ => 1
                })
                    .ToString(CultureInfo.InvariantCulture)}",
                "{Height}" => $"{(isWatermark ? "Height:" : string.Empty)}{(height / _volumeSettingsDto?.Unit switch
                {
                    VolumeUnit.Millimeter => 1,
                    VolumeUnit.Centimeter => 10,
                    VolumeUnit.Meter => 1000,
                    _ => 1
                })
                    .ToString(CultureInfo.InvariantCulture)}",
                "{ScanTime}" => $"{(isWatermark ? "ScanTime:" : string.Empty)}{(isWatermark ? $"{scanTime:yyyy-MM-dd HH:mm:ss.fff}" : $"{scanTime:yyyyMMddHHmmssfff}")}",
                "{TimestampedGuid}" => $"{(isWatermark ? "TimestampMilliseconds:" : string.Empty)}{new DateTimeOffset(scanTime).ToUnixTimeMilliseconds().ToString()}",
                "{CameraSerialNumber}" => $"{(isWatermark ? "CameraSerialNumber:" : string.Empty)}{(isWatermark ? RemoveControlCharacters(cameraSerialNumber) : SanitizeFileSystemSegment(cameraSerialNumber))}",
                "{ImageType}" => $"{(isWatermark ? "ImageType:" : string.Empty)}{type}",
                "{Year}" => $"{(isWatermark ? "Year:" : string.Empty)}{scanTime:yyyy}",
                "{Month}" => $"{(isWatermark ? "Month:" : string.Empty)}{scanTime:MM}",
                "{Day}" => $"{(isWatermark ? "Day:" : string.Empty)}{scanTime:dd}",
                "{Hour}" => $"{(isWatermark ? "Hour:" : string.Empty)}{scanTime:HH}",
                _ => "null"
            };
        }

        public string WatermarkParseTemplate(string source, SaveImageType type, string barCode, decimal weight,
            DateTime scanTime, decimal length,
            decimal width, decimal height, decimal volume, string cameraSerialNumber, bool isWatermark = false, string? language = default)
        {
            //默认中文
            var vUnit = _volumeSettingsDto?.Unit switch
            {
                VolumeUnit.Millimeter => "mm",
                VolumeUnit.Centimeter => "cm",
                VolumeUnit.Meter => "m",
                _ => "mm"
            };
            return source switch
            {
                "{BarCode}" => $"{(isWatermark ? "条码:" : string.Empty)}{RemoveControlCharacters(barCode)}",
                "{Weight}" => $"{(isWatermark ? "重量:" : string.Empty)}{weight.ToString(CultureInfo.InvariantCulture)} kg",
                "{Volume}" => $"{(isWatermark ? "体积:" : string.Empty)}{Math.Round(volume / _volumeSettingsDto?.Unit switch
                {
                    VolumeUnit.Millimeter => 1,
                    VolumeUnit.Centimeter => 1_000m,
                    VolumeUnit.Meter => 1_000_000_000m,
                    _ => 1
                }, 2).ToString("#.##", CultureInfo.InvariantCulture)} {vUnit}³",

                "{Length}" => $"{(isWatermark ? "长度:" : string.Empty)}{Math.Round(length / _volumeSettingsDto?.Unit switch
                {
                    VolumeUnit.Millimeter => 1,
                    VolumeUnit.Centimeter => 10,
                    VolumeUnit.Meter => 1000,
                    _ => 1
                }, 2).ToString("#.##", CultureInfo.InvariantCulture)} {vUnit}",

                "{Width}" => $"{(isWatermark ? "宽度:" : string.Empty)}{Math.Round(width / _volumeSettingsDto?.Unit switch
                {
                    VolumeUnit.Millimeter => 1,
                    VolumeUnit.Centimeter => 10,
                    VolumeUnit.Meter => 1000,
                    _ => 1
                }, 2).ToString("#.##", CultureInfo.InvariantCulture)} {vUnit}",

                "{Height}" => $"{(isWatermark ? "高度:" : string.Empty)}{Math.Round(height / _volumeSettingsDto?.Unit switch
                {
                    VolumeUnit.Millimeter => 1,
                    VolumeUnit.Centimeter => 10,
                    VolumeUnit.Meter => 1000,
                    _ => 1
                }, 2).ToString("#.##", CultureInfo.InvariantCulture)} {vUnit}",

                "{ScanTime}" => $"{(isWatermark ? "扫码时间:" : string.Empty)}{(isWatermark ? $"{scanTime:yyyy-MM-dd HH:mm:ss}" : $"{scanTime:yyyyMMddHHmmssfff}")}",
                "{TimestampedGuid}" => $"{(isWatermark ? "时间戳:" : string.Empty)}{new DateTimeOffset(scanTime).ToUnixTimeMilliseconds().ToString()}",
                "{CameraSerialNumber}" => $"{(isWatermark ? "相机序列号:" : string.Empty)}{RemoveControlCharacters(cameraSerialNumber)}",
                "{ImageType}" => $"{(isWatermark ? "图片类型:" : string.Empty)}{type}",
                "{Year}" => $"{(isWatermark ? "年:" : string.Empty)}{scanTime:yyyy}",
                "{Month}" => $"{(isWatermark ? "月:" : string.Empty)}{scanTime:MM}",
                "{Day}" => $"{(isWatermark ? "日:" : string.Empty)}{scanTime:dd}",
                "{Hour}" => $"{(isWatermark ? "时:" : string.Empty)}{scanTime:HH}",
                _ => ""
            };
        }

        protected virtual void OnImageSaveFailed(Exception e)
        {
            ImageSaveFailed?.Invoke(this, e);
        }

        protected virtual void OnImageSaved(ImageSavedEventArgs e)
        {
            ImageSaved?.Invoke(this, e);
        }

        /// <summary>
        /// 清除文本中的控制字符。
        /// </summary>
        private static string RemoveControlCharacters(string value)
        {
            return ControlCharactersRegex.Replace(value ?? string.Empty, string.Empty);
        }

        /// <summary>
        /// 将外部文本转换为可安全用作文件系统路径段的内容。
        /// </summary>
        private static string SanitizeFileSystemSegment(string value)
        {
            var sanitized = RemoveControlCharacters(value);
            var invalidCharacters = Path.GetInvalidFileNameChars();
            var characters = sanitized.ToCharArray();
            for (var index = 0; index < characters.Length; index++)
            {
                if (Array.IndexOf(invalidCharacters, characters[index]) >= 0)
                {
                    characters[index] = '_';
                }
            }

            var result = new string(characters).Trim().TrimEnd('.');
            return string.IsNullOrEmpty(result) ? "_" : result;
        }
    }
}
