using System;
using System.Buffers;
using Dynamsoft;
using System.Linq;
using System.Text;
using Dynamsoft.DBR;
using System.Drawing;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Camera.BarCodeReader {

    public class DynamsoftBarCodeReader : IBarCodeReader {
        private static string dbrLicenseKeys = "t0075oQAAAIvhAJJ+Mv2OHC+ZyzvrkkYyqMuHRgLktAwWHPtBRExDoEyZOSN3p9eHQ0csZBILJK+DKrBs2QaXyzJtmx0k+YgeciYvcCOd";

        //private static string dntLicenseKeys = "t0071WQAAAIP64uktmNbWzB4BpR9uN81ZcXDga6MZQlXA+n8nb0L8q3jVDPpYvMlRHU7VP2eQUIYACdUYZhZd1ZqZ5cuIySHQErA=";
        private BarcodeReader? _mBarcodeReader;

        private PublicRuntimeSettings? _mNormalRuntimeSettings;
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

        //跳帧
        private int _recognitionSkipFrames = 2;

        private int _framenum = 0;
        private CancellationTokenSource _stopCancellationTokenSource = new();
        private readonly ConcurrentQueue<Bitmap> _bitmapQueue = new();
        private readonly SemaphoreSlim _frameSignal = new(0, 1);
        private Task? _readerThread;

        //图片缩放百分比
        private int _scalePercentage = 50;

        public void Dispose() {
            _stopCancellationTokenSource.Cancel();
            try {
                _frameSignal.Release();
            }
            catch (SemaphoreFullException) {
            }
            if (_readerThread != null) {
                try {
                    _readerThread.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) {
                }
                _readerThread?.Dispose();
            }

            _readerThread = null;
            while (_bitmapQueue.TryDequeue(out var bitmap)) {
                bitmap.Dispose();
            }
            _mBarcodeReader?.Dispose();
        }

        public async Task<BarcodeResult> ReadFromFrame(Bitmap bitmap, CancellationToken token = default) {
            long elapsedMilliseconds = 0;
            TextResult[]? bars = null;

            Bitmap? scaledBitmap = null;
            var decodeBitmap = bitmap;
            if (_scalePercentage is > 0 and < 100) {
                scaledBitmap = GenerateThumbnail(
                    bitmap,
                    Math.Max(1, bitmap.Width * _scalePercentage / 100),
                    Math.Max(1, bitmap.Height * _scalePercentage / 100));
                decodeBitmap = scaledBitmap ?? bitmap;
            }
            var lockTaken = false;
            try {
                await _semaphoreSlim.WaitAsync(token);
                lockTaken = true;
                if (_mBarcodeReader is not null) {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    bars = DecodeBitmap(_mBarcodeReader, decodeBitmap);
                    stopwatch.Stop();
                    elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            finally {
                if (lockTaken) {
                    _semaphoreSlim.Release();
                }
                scaledBitmap?.Dispose();
            }

            //解析条码
            var barcodeResult = new BarcodeResult() {
                ScanTime = DateTime.Now,
                Image = bitmap
            };
            if (bars is not null && bars.Length > 0) {
                //识别到条码
                barcodeResult.BarCodes = bars.Select(s => new BarcodeInfo {
                    Barcode = s.BarcodeText,
                    BarcodeRegion = s.LocalizationResult.ResultPoints?.ToList(),
                    BarcodeType = s.LocalizationResult.BarcodeFormatString,
                })?.ToList();

                barcodeResult.RecognitionTime = elapsedMilliseconds;

            }
            return barcodeResult;
        }

        /// <summary>
        /// 使用池化缓冲区将位图提交给读码引擎。
        /// </summary>
        private static TextResult[]? DecodeBitmap(BarcodeReader reader, Bitmap bitmap) {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat);
            var stride = Math.Abs(bitmapData.Stride);
            var bufferLength = checked(bitmapData.Height * stride);
            var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
            try {
                for (var row = 0; row < bitmapData.Height; row++) {
                    Marshal.Copy(
                        IntPtr.Add(bitmapData.Scan0, row * bitmapData.Stride),
                        buffer,
                        row * stride,
                        stride);
                }
                return reader.DecodeBuffer(
                    buffer,
                    bitmap.Width,
                    bitmap.Height,
                    stride,
                    GetImagePixelFormat(bitmap.PixelFormat),
                    string.Empty);
            }
            finally {
                bitmap.UnlockBits(bitmapData);
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public event EventHandler<BarcodeResult>? BarcodeRead;

        public void EnqueueFrame(Bitmap bitmap) {
            while (_bitmapQueue.Count >= 3 && _bitmapQueue.TryDequeue(out var staleBitmap)) {
                staleBitmap.Dispose();
            }
            _bitmapQueue.Enqueue(bitmap);
            try {
                _frameSignal.Release();
            }
            catch (SemaphoreFullException) {
            }
        }

        public async Task<KeyValuePair<bool, string>> SetBarcodeReaderParameter(Dictionary<BarcodeReaderParameter, object> parameters) {
            await Task.Yield();
            if (_mBarcodeReader is not null) {
                try {
                    _mBarcodeReader.ResetRuntimeSettings();
                    var runtimeSettings = _mBarcodeReader.GetRuntimeSettings();

                    var recognitionSkipFrames = parameters.FirstOrDefault(f =>
                            f.Key == BarcodeReaderParameter.RecognitionSkipFrames)
                        .Value;
                    if (recognitionSkipFrames is int skipFrames) {
                        _recognitionSkipFrames = skipFrames;
                    }

                    //条码类型
                    var enumBarcodeFormat = parameters.FirstOrDefault(f =>
                            f.Key == BarcodeReaderParameter.EnumBarcodeFormat)
                        .Value;
                    if (enumBarcodeFormat is EnumBarcodeFormat format) {
                        runtimeSettings.BarcodeFormatIds = (int)format;
                    }

                    var enumBarcodeFormat2 = parameters.FirstOrDefault(f =>
                            f.Key == BarcodeReaderParameter.EnumBarcodeFormat2)
                        .Value;
                    if (enumBarcodeFormat2 is EnumBarcodeFormat_2 format2) {
                        runtimeSettings.BarcodeFormatIds = (int)format2;
                    }

                    var scalePercentage = parameters.FirstOrDefault(f =>
                            f.Key == BarcodeReaderParameter.ScalePercentage)
                        .Value;
                    if (scalePercentage is int percentage) {
                        _scalePercentage = percentage == 0 ? 100 : percentage * 10;
                    }
                    var recognitionMode = parameters.FirstOrDefault(f =>
                            f.Key == BarcodeReaderParameter.RecognitionMode)
                        .Value ?? ScanMode.Speed;
                    if (recognitionMode is ScanMode scanMode) {
                        switch (scanMode) {
                            case ScanMode.Speed: {
                                    //runtimeSettings.BarcodeFormatIds = (int)(EnumBarcodeFormat.BF_CODE_128 | EnumBarcodeFormat.BF_CODE_39 | EnumBarcodeFormat.BF_QR_CODE);

                                    runtimeSettings.LocalizationModes[0] = EnumLocalizationMode.LM_SCAN_DIRECTLY;
                                    for (var i = 1; i < runtimeSettings.LocalizationModes.Length; i++)
                                        runtimeSettings.LocalizationModes[i] = EnumLocalizationMode.LM_SKIP;
                                    runtimeSettings.DeblurLevel = 3;
                                    runtimeSettings.ExpectedBarcodesCount = 1;
                                    runtimeSettings.ScaleDownThreshold = 2300;
                                    runtimeSettings.FurtherModes.GrayscaleTransformationModes[0] = EnumGrayscaleTransformationMode.GTM_ORIGINAL;
                                    runtimeSettings.FurtherModes.GrayscaleTransformationModes[1] = EnumGrayscaleTransformationMode.GTM_SKIP;
                                    runtimeSettings.FurtherModes.ImagePreprocessingModes[0] =
                                        EnumImagePreprocessingMode.IPM_GENERAL;
                                    runtimeSettings.MinResultConfidence = 30;
                                    for (var i = 0; i < runtimeSettings.FurtherModes.TextFilterModes.Length; i++)
                                        runtimeSettings.FurtherModes.TextFilterModes[i] = EnumTextFilterMode.TFM_SKIP;
                                    //runtimeSettings.FurtherModes.TextFilterModes[0] = EnumTextFilterMode.TFM_GENERAL_CONTOUR;
                                    _mBarcodeReader.UpdateRuntimeSettings(runtimeSettings);

                                    break;
                                }
                            case ScanMode.Balance: {
                                    //runtimeSettings.LocalizationModes = _mNormalRuntimeSettings?.LocalizationModes;
                                    runtimeSettings.LocalizationModes[0] = EnumLocalizationMode.LM_CONNECTED_BLOCKS;
                                    runtimeSettings.LocalizationModes[1] = EnumLocalizationMode.LM_SCAN_DIRECTLY;
                                    for (var i = 2; i < runtimeSettings.LocalizationModes.Length; i++)
                                        runtimeSettings.LocalizationModes[i] = EnumLocalizationMode.LM_SKIP;
                                    /*runtimeSettings.FurtherModes.RegionPredetectionModes[0] =
                                        EnumRegionPredetectionMode.RPM_GENERAL_RGB_CONTRAST;*/
                                    runtimeSettings.DeblurLevel = 5;
                                    runtimeSettings.ExpectedBarcodesCount = 512;
                                    runtimeSettings.ScaleDownThreshold = 2300;
                                    runtimeSettings.FurtherModes.TextFilterModes[0] = EnumTextFilterMode.TFM_GENERAL_CONTOUR;
                                    for (var i = 1; i < runtimeSettings.FurtherModes.TextFilterModes.Length; i++)
                                        runtimeSettings.FurtherModes.TextFilterModes[i] = EnumTextFilterMode.TFM_SKIP;
                                    //后面补充
                                    runtimeSettings.FurtherModes.GrayscaleTransformationModes[0] = EnumGrayscaleTransformationMode.GTM_ORIGINAL;
                                    runtimeSettings.FurtherModes.GrayscaleTransformationModes[1] = EnumGrayscaleTransformationMode.GTM_INVERTED;
                                    for (var i = 2; i < runtimeSettings.FurtherModes.GrayscaleTransformationModes.Length; i++)
                                        runtimeSettings.FurtherModes.GrayscaleTransformationModes[i] = EnumGrayscaleTransformationMode.GTM_SKIP;

                                    break;
                                }
                            case ScanMode.Coverage: {
                                    runtimeSettings.DeblurLevel = 9;
                                    runtimeSettings.ExpectedBarcodesCount = 512;
                                    runtimeSettings.ScaleDownThreshold = 214748347;
                                    runtimeSettings.FurtherModes.TextFilterModes[0] = EnumTextFilterMode.TFM_GENERAL_CONTOUR;
                                    for (var i = 1; i < runtimeSettings.FurtherModes.TextFilterModes.Length; i++)
                                        runtimeSettings.FurtherModes.TextFilterModes[i] = EnumTextFilterMode.TFM_SKIP;
                                    runtimeSettings.FurtherModes.GrayscaleTransformationModes[0] = EnumGrayscaleTransformationMode.GTM_ORIGINAL;
                                    runtimeSettings.FurtherModes.GrayscaleTransformationModes[1] = EnumGrayscaleTransformationMode.GTM_INVERTED;
                                    for (var i = 2; i < runtimeSettings.FurtherModes.GrayscaleTransformationModes.Length; i++)
                                        runtimeSettings.FurtherModes.GrayscaleTransformationModes[i] = EnumGrayscaleTransformationMode.GTM_SKIP;
                                    //_mBarcodeReader.UpdateRuntimeSettings(runtimeSettings);
                                    break;
                                }
                            case ScanMode.Custom: {
                                    var expectedBarcodesCount = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.ExpectedBarcodesCount)
                                        .Value;
                                    if (expectedBarcodesCount is int count) {
                                        runtimeSettings.ExpectedBarcodesCount = count;
                                    }

                                    var deblurLevel = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.DeblurLevel)
                                        .Value;
                                    if (deblurLevel is int level) {
                                        runtimeSettings.DeblurLevel = level;
                                    }
                                    for (var i = 0; i < runtimeSettings.LocalizationModes.Length; i++)
                                        runtimeSettings.LocalizationModes[i] = EnumLocalizationMode.LM_SKIP;

                                    var localizationMode = (LocalizationMode)(parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.LocalizationMode)
                                        .Value ?? 0);
                                    if (localizationMode is var mode) {
                                        switch (mode) {
                                            case LocalizationMode.Default:
                                                runtimeSettings.LocalizationModes = _mNormalRuntimeSettings?.LocalizationModes;
                                                break;

                                            case LocalizationMode.ConnectedBlocks:
                                                runtimeSettings.LocalizationModes[0] = EnumLocalizationMode.LM_CONNECTED_BLOCKS;
                                                break;

                                            case LocalizationMode.Statistics:
                                                runtimeSettings.LocalizationModes[0] = EnumLocalizationMode.LM_STATISTICS;
                                                break;

                                            case LocalizationMode.Lines:
                                                runtimeSettings.LocalizationModes[0] = EnumLocalizationMode.LM_LINES;
                                                break;

                                            case LocalizationMode.ScanDirectly:
                                                runtimeSettings.LocalizationModes[0] = EnumLocalizationMode.LM_SCAN_DIRECTLY;
                                                break;

                                            case LocalizationMode.ConnectedBlocksAndScanDirectly:
                                                runtimeSettings.LocalizationModes[0] = EnumLocalizationMode.LM_CONNECTED_BLOCKS;
                                                runtimeSettings.LocalizationModes[1] = EnumLocalizationMode.LM_SCAN_DIRECTLY;
                                                break;
                                        }
                                    }

                                    var isUseTextFilterMode = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.IsUseTextFilterMode)
                                        .Value;
                                    if (isUseTextFilterMode is bool filterMode) {
                                        runtimeSettings.FurtherModes.TextFilterModes[0] = filterMode ? EnumTextFilterMode.TFM_GENERAL_CONTOUR : EnumTextFilterMode.TFM_SKIP;
                                        for (var i = 1; i < runtimeSettings.FurtherModes.TextFilterModes.Length; i++)
                                            runtimeSettings.FurtherModes.TextFilterModes[i] = EnumTextFilterMode.TFM_SKIP;
                                    }

                                    var isUseRegionPredetectionMode = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.IsUseRegionPredetectionMode)
                                        .Value;
                                    if (isUseRegionPredetectionMode is bool predetectionMode) {
                                        runtimeSettings.FurtherModes.RegionPredetectionModes[0] = predetectionMode ? EnumRegionPredetectionMode.RPM_GENERAL_RGB_CONTRAST : EnumRegionPredetectionMode.RPM_SKIP;
                                    }

                                    var scaleDownThreshold = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.ScaleDownThreshold)
                                        .Value;
                                    if (scaleDownThreshold is int threshold) {
                                        runtimeSettings.ScaleDownThreshold = threshold < 512 ? 512 : threshold;
                                    }

                                    var grayscaleTransformationMode = (GrayscaleTransformationMode)(parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.GrayscaleTransformationMode)
                                        .Value ?? 0);
                                    if (grayscaleTransformationMode is var formationMode) {
                                        switch (formationMode) {
                                            case GrayscaleTransformationMode.Original:
                                                runtimeSettings.FurtherModes.GrayscaleTransformationModes[0] = EnumGrayscaleTransformationMode.GTM_ORIGINAL;
                                                runtimeSettings.FurtherModes.GrayscaleTransformationModes[1] = EnumGrayscaleTransformationMode.GTM_INVERTED;
                                                for (var i = 2; i < runtimeSettings.FurtherModes.GrayscaleTransformationModes.Length; i++)
                                                    runtimeSettings.FurtherModes.GrayscaleTransformationModes[i] = EnumGrayscaleTransformationMode.GTM_SKIP;
                                                break;

                                            case GrayscaleTransformationMode.Inverted:
                                                runtimeSettings.FurtherModes.GrayscaleTransformationModes[0] = EnumGrayscaleTransformationMode.GTM_INVERTED;
                                                runtimeSettings.FurtherModes.GrayscaleTransformationModes[1] = EnumGrayscaleTransformationMode.GTM_SKIP;
                                                for (var i = 2; i < runtimeSettings.FurtherModes.GrayscaleTransformationModes.Length; i++)
                                                    runtimeSettings.FurtherModes.GrayscaleTransformationModes[i] = EnumGrayscaleTransformationMode.GTM_SKIP;
                                                break;

                                            case GrayscaleTransformationMode.OriginalAndInverted:
                                                runtimeSettings.FurtherModes.GrayscaleTransformationModes[0] = EnumGrayscaleTransformationMode.GTM_ORIGINAL;
                                                runtimeSettings.FurtherModes.GrayscaleTransformationModes[1] = EnumGrayscaleTransformationMode.GTM_SKIP;
                                                for (var i = 2; i < runtimeSettings.FurtherModes.GrayscaleTransformationModes.Length; i++)
                                                    runtimeSettings.FurtherModes.GrayscaleTransformationModes[i] = EnumGrayscaleTransformationMode.GTM_SKIP;
                                                break;
                                        }
                                    }

                                    var imagePreprocessingMode = (ImagePreprocessingMode)(parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.ImagePreprocessingMode)
                                        .Value ?? 0);
                                    if (imagePreprocessingMode is var preprocessingMode) {
                                        runtimeSettings.FurtherModes.ImagePreprocessingModes[0] = preprocessingMode switch {
                                            ImagePreprocessingMode.General => EnumImagePreprocessingMode.IPM_GENERAL,
                                            ImagePreprocessingMode.GrayEqualization => EnumImagePreprocessingMode.IPM_GRAY_EQUALIZE,
                                            ImagePreprocessingMode.GraySmoothing => EnumImagePreprocessingMode.IPM_GRAY_SMOOTH,
                                            ImagePreprocessingMode.SharpeningAndSmoothing => EnumImagePreprocessingMode
                                                .IPM_SHARPEN_SMOOTH,
                                            _ => runtimeSettings.FurtherModes.ImagePreprocessingModes[0]
                                        };
                                    }

                                    var minResultConfidence = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.MinResultConfidence)
                                        .Value;
                                    if (minResultConfidence is int confidence) {
                                        runtimeSettings.MinResultConfidence = confidence * 10;
                                    }

                                    break;
                                }
                        }
                        var textureDetectionSensitivity = parameters.FirstOrDefault(f =>
                                f.Key == BarcodeReaderParameter.TextureDetectionSensitivity)
                            .Value;
                        if (textureDetectionSensitivity is int sensitivity) {
                            runtimeSettings.FurtherModes.TextureDetectionModes[0] = sensitivity == 0 ? EnumTextureDetectionMode.TDM_SKIP : EnumTextureDetectionMode.TDM_GENERAL_WIDTH_CONCENTRATION;
                            if (sensitivity > 0) {
                                _mBarcodeReader.SetModeArgument("TextureDetectionModes", 0, "Sensitivity", sensitivity.ToString(), out var strErrorMessage);
                            }
                        }

                        var binarizationBlockSize = parameters.FirstOrDefault(f =>
                                f.Key == BarcodeReaderParameter.BinarizationBlockSize)
                            .Value;
                        if (binarizationBlockSize is int) {
                            _mBarcodeReader.SetModeArgument("BinarizationModes", 0, "BlockSizeX", binarizationBlockSize.ToString(), out var strErrorMessage);
                        }
                    }
                    _mBarcodeReader.UpdateRuntimeSettings(runtimeSettings);
                    return new KeyValuePair<bool, string>(true, "读码器设置成功");
                }
                catch (Exception e) {
                    OnExceptionOccurred(e);
                    return new KeyValuePair<bool, string>(false, "读码器设置失败");
                }
            }
            else {
                return new KeyValuePair<bool, string>(false, "读码器未初始化");
            }
        }

        public event EventHandler<Exception>? ExceptionOccurred;

        public async Task<bool> Initialize() {
            var ret = BarcodeReader.InitLicense(dbrLicenseKeys, out var errorMsg);
            IsInitialized = (ret == EnumErrorCode.DBR_SUCCESS);
            if (!IsInitialized) {
                NLog.LogManager.GetCurrentClassLogger().Error($"InitLicense Failed:{errorMsg}");
            }
            else {
                _mBarcodeReader = BarcodeReader.GetInstance();
                _mNormalRuntimeSettings = _mBarcodeReader?.GetRuntimeSettings();

                await SetBarcodeReaderParameter(new Dictionary<BarcodeReaderParameter, object>()
                {
                   {BarcodeReaderParameter.RecognitionMode, ScanMode.Custom },
                    {BarcodeReaderParameter.IsUseTextFilterMode,true},
                    {BarcodeReaderParameter.IsUseRegionPredetectionMode,true},
                    {BarcodeReaderParameter.DeblurLevel,3},
                    {BarcodeReaderParameter.ExpectedBarcodesCount,1},
                    {BarcodeReaderParameter.EnumBarcodeFormat, EnumBarcodeFormat.BF_QR_CODE|EnumBarcodeFormat.BF_MICRO_QR|EnumBarcodeFormat.BF_CODE_128|EnumBarcodeFormat.BF_CODE_39|EnumBarcodeFormat.BF_CODE_93|EnumBarcodeFormat.BF_CODABAR },
                });
            }

            if (_readerThread is null) {
                _stopCancellationTokenSource = new CancellationTokenSource();
                _readerThread = Task.Run(async () => {
                    var token = _stopCancellationTokenSource.Token;
                    while (!token.IsCancellationRequested) {
                        try {
                            await _frameSignal.WaitAsync(token).ConfigureAwait(false);
                            Bitmap? image = null;
                            while (_bitmapQueue.TryDequeue(out var queuedImage)) {
                                image?.Dispose();
                                image = queuedImage;
                            }

                            if (image is not null) {
                                var barcodeResult = new BarcodeResult() {
                                    Image = image,
                                    ScanTime = DateTime.Now
                                };

                                if (_framenum >= _recognitionSkipFrames) {
                                    _framenum = 0;

                                    barcodeResult = await ReadFromFrame(image);
                                }
                                OnBarcodeRead(barcodeResult);
                                _framenum++;
                            }
                        }
                        catch (Exception e) {
                            OnExceptionOccurred(e);
                        }
                    }
                });
            }
            return IsInitialized;
        }

        public bool IsInitialized { get; private set; }

        protected virtual void OnExceptionOccurred(Exception e) {
            ExceptionOccurred?.Invoke(this, e);
        }

        /*public static (byte[] buffer, int stride, EnumImagePixelFormat pixelFormat) GetBitmapData(Bitmap bitmap) {
            // 锁定Bitmap对象的内存区域，并获取其指针
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var ptr = bitmapData.Scan0;

            // 计算每行像素需要的字节数
            var bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            var stride = bitmapData.Stride;

            // 创建缓冲区，并将Bitmap对象的数据复制到缓冲区
            var bufferSize = bitmapData.Height * Math.Abs(bitmapData.Stride);
            var buffer = new byte[bufferSize];
            Marshal.Copy(ptr, buffer, 0, bufferSize);

            // 解锁Bitmap对象的内存区域
            bitmap.UnlockBits(bitmapData);

            // 获取像素格式
            var pixelFormat = GetImagePixelFormat(bitmap.PixelFormat);

            return (buffer, stride, pixelFormat);
        }*/

        public static (byte[] buffer, int stride, EnumImagePixelFormat pixelFormat) GetBitmapData(Bitmap bitmap) {
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                             ImageLockMode.ReadOnly, bitmap.PixelFormat);
            try {
                var stride = Math.Abs(bitmapData.Stride);
                var bufferSize = bitmapData.Height * stride;
                var buffer = new byte[bufferSize];
                for (var row = 0; row < bitmapData.Height; row++) {
                    var sourceRow = IntPtr.Add(bitmapData.Scan0, row * bitmapData.Stride);
                    Marshal.Copy(sourceRow, buffer, row * stride, stride);
                }

                return (buffer, stride, GetImagePixelFormat(bitmap.PixelFormat));
            }
            finally {
                bitmap.UnlockBits(bitmapData);
            }
        }

        public Bitmap? GenerateThumbnail(Bitmap? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            return CameraImageProcessing.CreateThumbnail(sourceImage, thumbnailWidth, thumbnailHeight);
        }

        public static Bitmap FastClone(Bitmap sourceBitmap) {
            return sourceBitmap.Clone(
                new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height),
                sourceBitmap.PixelFormat);
        }

        private static EnumImagePixelFormat GetImagePixelFormat(PixelFormat pixelFormat) {
            return pixelFormat switch {
                PixelFormat.Format24bppRgb => EnumImagePixelFormat.IPF_RGB_888,
                PixelFormat.Format32bppArgb => EnumImagePixelFormat.IPF_ARGB_8888,
                _ => EnumImagePixelFormat.IPF_ABGR_8888
            };
        }

        protected virtual void OnBarcodeRead(BarcodeResult e) {
            var handler = BarcodeRead;
            if (handler is null) {
                e.Image?.Dispose();
                return;
            }
            handler.Invoke(this, e);
        }
    }
}
