using JayTom.Dws.Application.Configuration;
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
using System.Collections.Generic;
using JayTom.Dws.Plugin.SaveImage;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.ImageService;
using WindowsAction = JayTom.Dws.Client.EventMediators.WindowsAction;
using WindowsActionType = JayTom.Dws.Client.EventMediators.WindowsActionType;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;
using static JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.DaHuatechSecurityCamera;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    public class SaveImageBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IImageStorageService _imageStorageService;
        private readonly ISaveImage _saveImage;
        private readonly ISettingsStore _settingsStore;
        private readonly IDeviceService _deviceService;
        /// <summary>
        /// 原图待保存队列。
        /// </summary>
        private readonly Queue<(ImageMessageInfo Message, long EstimatedBytes)> _imageItems =
            new(MaxPendingImages);
        /// <summary>
        /// 原图队列同步锁。
        /// </summary>
        private readonly System.Threading.Lock _imageQueueLock = new();
        private readonly SemaphoreSlim _semaphore = new(1);
        private ImageSettingsDto? _imageSettingsDto;
        private OcrSettingsDto? _ocrSettingsDto;
        /// <summary>
        /// OCR 裁剪图待保存队列。
        /// </summary>
        private readonly Queue<(Bitmap Image, long EstimatedBytes)> _cropImageQueue =
            new(MaxPendingCropImages);
        /// <summary>
        /// OCR 裁剪图队列同步锁。
        /// </summary>
        private readonly System.Threading.Lock _cropImageQueueLock = new();
        /// <summary>
        /// 后台存图工作信号。
        /// </summary>
        private readonly SemaphoreSlim _workSignal = new(0, 1);
        private int _isWindowsClose;
        /// <summary>
        /// 工作信号是否已经置位。
        /// </summary>
        private int _workSignalArmed;
        /// <summary>
        /// 上次报告后丢弃的原图数量。
        /// </summary>
        private long _droppedImageCount;
        /// <summary>
        /// 上次报告后丢弃的 OCR 裁剪图数量。
        /// </summary>
        private long _droppedCropImageCount;
        /// <summary>
        /// 用于避免同毫秒裁剪图文件名冲突的序号。
        /// </summary>
        private long _cropImageSequence;
        /// <summary>
        /// 原图队列当前估算的解码后内存占用。
        /// </summary>
        private long _pendingImageBytes;
        /// <summary>
        /// OCR 裁剪图队列当前估算的解码后内存占用。
        /// </summary>
        private long _pendingCropImageBytes;
        /// <summary>
        /// 当前后台工作器的停止令牌。
        /// </summary>
        private CancellationToken _stoppingToken;
        /// <summary>
        /// 原图待保存队列容量上限。
        /// </summary>
        private const int MaxPendingImages = 32;
        /// <summary>
        /// 裁剪图待保存队列容量上限。
        /// </summary>
        private const int MaxPendingCropImages = 64;
        /// <summary>
        /// 原图队列最多占用约 256 MiB 解码内存；单张超大图仍允许独占队列。
        /// </summary>
        private const long MaxPendingImageBytes = 256L * 1024L * 1024L;
        /// <summary>
        /// OCR 裁剪图队列最多占用约 64 MiB 解码内存。
        /// </summary>
        private const long MaxPendingCropImageBytes = 64L * 1024L * 1024L;

        public SaveImageBackgroundService(IImageStorageService imageStorageService, ISaveImage saveImage,
            ISettingsStore settingsStore, IDeviceService deviceService)
        {
            _imageStorageService = imageStorageService;
            _saveImage = saveImage;
            _settingsStore = settingsStore;
            _deviceService = deviceService;
            EventAggregator.Instance.Subscribe<ImageMessageInfo>(info =>
            {
                //判断是否需要存图
                if (info is ImageMessageInfo imageInfo)
                {
                    if (imageInfo.Image is null)
                    {
                        return;
                    }
                    var imageSettings = Volatile.Read(ref _imageSettingsDto);
                    if (imageSettings is not null)
                    {
                        if ((imageSettings.IsSaveBarcodeImage && imageInfo.Type == SaveImageType.BarcodeImage) ||
                            (imageSettings.IsSavePanoramaImage && imageInfo.Type == SaveImageType.PanoramaImage) ||
                            (imageSettings.IsSaveVolumeImage && imageInfo.Type == SaveImageType.VolumeImage))
                        {
                            EnqueueImage(imageInfo);
                            return;
                        }
                    }
                    imageInfo?.Image?.Dispose();
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(OnSettingsChanged);
            _deviceService.OcrContentRecognized += delegate (object? sender, OcrResult result)
            {
                var ocrSettings = Volatile.Read(ref _ocrSettingsDto);
                if (ocrSettings?.IsSaveCropImage == true && !string.IsNullOrEmpty(ocrSettings.CropImagePath))
                {
                    if (result?.CropImage is not null)
                    {
                        try
                        {
                            EnqueueCropImage(new Bitmap(result.CropImage));
                        }
                        catch (Exception exception)
                        {
                            LogManager.GetCurrentClassLogger()
                                .Error(exception, "复制 OCR 裁剪图失败");
                        }
                    }
                }
            };
            EventAggregator.Instance.Subscribe<WindowsAction>(item =>
            {
                if (item is WindowsAction { Type: WindowsActionType.Close })
                {
                    Interlocked.Exchange(ref _isWindowsClose, 1);
                    SignalWork();
                }
            });
        }

        /// <summary>
        /// 在生产者启动前加载存图配置，避免启动窗口期丢弃首批图片。
        /// </summary>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await ReloadSettingsAsync("SaveImageSettings", cancellationToken).ConfigureAwait(false);
            await ReloadSettingsAsync("OcrSettings", cancellationToken).ConfigureAwait(false);
            await base.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _stoppingToken = stoppingToken;

            try
            {
                while (!stoppingToken.IsCancellationRequested &&
                       Volatile.Read(ref _isWindowsClose) == 0)
                {
                    try
                    {
                        await _workSignal.WaitAsync(stoppingToken).ConfigureAwait(false);
                        Interlocked.Exchange(ref _workSignalArmed, 0);

                        if (TryDequeueImage(out var messageInfo) && messageInfo.Image is not null)
                        {
                            await _imageStorageService.SaveImage(messageInfo.PackageTimestamped, messageInfo.Image,
                                messageInfo.Type, messageInfo.BarCode, messageInfo.Weight,
                                messageInfo.ScanTime, messageInfo.Length, messageInfo.Width,
                                messageInfo.Height, messageInfo.Volume, messageInfo.CameraSerialNumber,
                                stoppingToken).ConfigureAwait(false);
                        }

                        if (TryDequeueCropImage(out var cropImage))
                        {
                            using (cropImage)
                            {
                                var cropImagePath = Volatile.Read(ref _ocrSettingsDto)?.CropImagePath;
                                if (!string.IsNullOrWhiteSpace(cropImagePath))
                                {
                                    var now = DateTime.Now;
                                    var directory = Path.Combine(cropImagePath, now.ToString("MM"),
                                        now.ToString("dd"), now.ToString("HH"));
                                    var imageName =
                                        $"{new DateTimeOffset(now).ToUnixTimeMilliseconds()}_{Interlocked.Increment(ref _cropImageSequence)}";
                                    var (saved, message) = await _saveImage.SaveOriginalImage(
                                        cropImage,
                                        imageName,
                                        directory,
                                        cancellationToken: stoppingToken).ConfigureAwait(false);
                                    if (!saved)
                                    {
                                        LogManager.GetCurrentClassLogger()
                                            .Error($"OCR裁剪图保存失败:{message}");
                                    }
                                }
                            }
                        }

                        ReportDroppedImages();
                        if (HasPendingWork())
                        {
                            SignalWork();
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception exception)
                    {
                        LogManager.GetCurrentClassLogger().Error(exception, "存图异常");
                        if (HasPendingWork())
                        {
                            SignalWork();
                        }
                    }
                }
            }
            finally
            {
                // 服务停止后不再处理排队图片，显式释放以避免 GDI 句柄泄漏。
                while (TryDequeueImage(out var pendingImage))
                {
                    pendingImage.Image?.Dispose();
                }

                while (TryDequeueCropImage(out var pendingCropImage))
                {
                    pendingCropImage.Dispose();
                }
            }
        }

        private void OnSettingsChanged(SettingsChangedEvent settings)
        {
            if (settings.SettingsName is "SaveImageSettings" or "OcrSettings")
            {
                _ = ReloadSettingsAsync(settings.SettingsName, _stoppingToken);
            }
        }

        /// <summary>
        /// 将原图加入有界待保存队列，超限时释放最旧图像。
        /// </summary>
        private void EnqueueImage(ImageMessageInfo imageInfo)
        {
            var estimatedBytes = EstimateDecodedBytes(imageInfo.Image);
            List<ImageMessageInfo>? discarded = null;
            lock (_imageQueueLock)
            {
                while (_imageItems.Count > 0 &&
                       (_imageItems.Count >= MaxPendingImages ||
                        WouldExceedBudget(_pendingImageBytes, estimatedBytes, MaxPendingImageBytes)))
                {
                    discarded ??= [];
                    var removed = _imageItems.Dequeue();
                    _pendingImageBytes = Math.Max(0, _pendingImageBytes - removed.EstimatedBytes);
                    discarded.Add(removed.Message);
                }

                _imageItems.Enqueue((imageInfo, estimatedBytes));
                _pendingImageBytes += estimatedBytes;
            }

            if (discarded is not null)
            {
                foreach (var discardedImage in discarded)
                {
                    discardedImage.Image?.Dispose();
                }
                Interlocked.Add(ref _droppedImageCount, discarded.Count);
            }

            SignalWork();
        }

        /// <summary>
        /// 将裁剪图加入有界待保存队列，超限时释放最旧图像。
        /// </summary>
        private void EnqueueCropImage(Bitmap cropImage)
        {
            var estimatedBytes = EstimateDecodedBytes(cropImage);
            List<Bitmap>? discarded = null;
            lock (_cropImageQueueLock)
            {
                while (_cropImageQueue.Count > 0 &&
                       (_cropImageQueue.Count >= MaxPendingCropImages ||
                        WouldExceedBudget(
                            _pendingCropImageBytes,
                            estimatedBytes,
                            MaxPendingCropImageBytes)))
                {
                    discarded ??= [];
                    var removed = _cropImageQueue.Dequeue();
                    _pendingCropImageBytes = Math.Max(
                        0,
                        _pendingCropImageBytes - removed.EstimatedBytes);
                    discarded.Add(removed.Image);
                }

                _cropImageQueue.Enqueue((cropImage, estimatedBytes));
                _pendingCropImageBytes += estimatedBytes;
            }

            if (discarded is not null)
            {
                foreach (var discardedImage in discarded)
                {
                    discardedImage.Dispose();
                }
                Interlocked.Add(ref _droppedCropImageCount, discarded.Count);
            }

            SignalWork();
        }

        /// <summary>
        /// 尝试取出一张待保存原图。
        /// </summary>
        /// <param name="imageInfo">取出的原图消息。</param>
        /// <returns>成功取出时返回 <see langword="true"/>。</returns>
        private bool TryDequeueImage(out ImageMessageInfo imageInfo)
        {
            lock (_imageQueueLock)
            {
                if (_imageItems.Count == 0)
                {
                    imageInfo = null!;
                    return false;
                }

                var item = _imageItems.Dequeue();
                _pendingImageBytes = Math.Max(0, _pendingImageBytes - item.EstimatedBytes);
                imageInfo = item.Message;
                return true;
            }
        }

        /// <summary>
        /// 尝试取出一张待保存 OCR 裁剪图。
        /// </summary>
        /// <param name="cropImage">取出的裁剪图。</param>
        /// <returns>成功取出时返回 <see langword="true"/>。</returns>
        private bool TryDequeueCropImage(out Bitmap cropImage)
        {
            lock (_cropImageQueueLock)
            {
                if (_cropImageQueue.Count == 0)
                {
                    cropImage = null!;
                    return false;
                }

                var item = _cropImageQueue.Dequeue();
                _pendingCropImageBytes = Math.Max(
                    0,
                    _pendingCropImageBytes - item.EstimatedBytes);
                cropImage = item.Image;
                return true;
            }
        }

        /// <summary>
        /// 判断是否仍有待处理图片。
        /// </summary>
        /// <returns>任一队列非空时返回 <see langword="true"/>。</returns>
        private bool HasPendingWork()
        {
            lock (_imageQueueLock)
            {
                if (_imageItems.Count > 0)
                {
                    return true;
                }
            }

            lock (_cropImageQueueLock)
            {
                return _cropImageQueue.Count > 0;
            }
        }

        /// <summary>
        /// 置位后台工作信号，合并重复通知以降低高频事件开销。
        /// </summary>
        private void SignalWork()
        {
            if (Interlocked.Exchange(ref _workSignalArmed, 1) == 0)
            {
                _workSignal.Release();
            }
        }

        /// <summary>
        /// 在后台线程汇总报告队列溢出，避免相机热路径直接写日志。
        /// </summary>
        private void ReportDroppedImages()
        {
            var droppedImages = Interlocked.Exchange(ref _droppedImageCount, 0);
            if (droppedImages > 0)
            {
                LogManager.GetCurrentClassLogger()
                    .Error($"待保存图片队列已满，已释放最旧图片 {droppedImages} 张以保护进程内存");
            }

            var droppedCropImages = Interlocked.Exchange(ref _droppedCropImageCount, 0);
            if (droppedCropImages > 0)
            {
                LogManager.GetCurrentClassLogger()
                    .Error($"OCR裁剪图片队列已满，已释放最旧图片 {droppedCropImages} 张以保护进程内存");
            }
        }

        /// <summary>
        /// 估算图片解码后占用的像素内存字节数。
        /// </summary>
        private static long EstimateDecodedBytes(Image? image)
        {
            if (image is null || image.Width <= 0 || image.Height <= 0)
            {
                return 0;
            }

            var bitsPerPixel = Image.GetPixelFormatSize(image.PixelFormat);
            if (bitsPerPixel <= 0)
            {
                bitsPerPixel = 32;
            }

            var bytesPerPixel = Math.Max(1, (bitsPerPixel + 7) / 8);
            return (long)image.Width * image.Height * bytesPerPixel;
        }

        /// <summary>
        /// 判断加入图片后是否会超过队列的内存预算。
        /// </summary>
        private static bool WouldExceedBudget(long currentBytes, long incomingBytes, long budgetBytes)
        {
            return incomingBytes > budgetBytes - Math.Min(currentBytes, budgetBytes);
        }

        private async Task ReloadSettingsAsync(string settingsName, CancellationToken cancellationToken)
        {
            var lockTaken = false;
            try
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                lockTaken = true;
                if (settingsName == "SaveImageSettings")
                {
                    var imageSettings = await _settingsStore
                        .GetAsync<ImageSettingsDto>(settingsName, cancellationToken)
                        .ConfigureAwait(false);
                    Volatile.Write(ref _imageSettingsDto, imageSettings ?? new ImageSettingsDto());
                }
                else
                {
                    var ocrSettings = await _settingsStore
                        .GetAsync<OcrSettingsDto>(settingsName, cancellationToken)
                        .ConfigureAwait(false);
                    Volatile.Write(ref _ocrSettingsDto, ocrSettings ?? new OcrSettingsDto());
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 服务停止时无需继续加载配置。
            }
            catch (Exception exception)
            {
                LogManager.GetCurrentClassLogger().Error(exception, $"加载{settingsName}配置失败");
                if (settingsName == "SaveImageSettings")
                {
                    Interlocked.CompareExchange(ref _imageSettingsDto, new ImageSettingsDto(), null);
                }
                else
                {
                    Interlocked.CompareExchange(ref _ocrSettingsDto, new OcrSettingsDto(), null);
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _semaphore.Release();
                }
            }
        }
    }
}
