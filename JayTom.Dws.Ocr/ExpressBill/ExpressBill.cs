using System;
using System.IO;
using System.Linq;
using System.Text;
using OpenCvSharp;
using System.Drawing;
using Newtonsoft.Json;
using System.Text.Json;
using System.Threading;
using System.Diagnostics;
using JayTom.Dws.Ocr.Yolo;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Point = System.Drawing.Point;
using System.Security.AccessControl;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Ini;
using System.Text.Json.Serialization.Metadata;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace JayTom.Dws.Ocr.ExpressBill {

    public sealed class ExpressBill : IDisposable {
        private TimeSpan _recognitionTimeout = TimeSpan.FromSeconds(1);
        private const string DllPath = ".\\ExpressBill\\Lib\\Dll\\ExpressBillApi.dll";
        private readonly ExpressBillPool _pool;
        public bool IsExecutingMethod { get; set; }

        private static YoloParser? _yoloParser;

        // sdk初始化
        [DllImport(DllPath, EntryPoint = "init", CharSet = CharSet.Ansi
            , CallingConvention = CallingConvention.Cdecl)]
        public static extern int init(string modelPath);

        // ocr 识别
        [DllImport(DllPath, EntryPoint = "process", CharSet = CharSet.Ansi
            , CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr process(IntPtr mat);

        // sdk销毁
        [DllImport(DllPath, EntryPoint = "uninit", CharSet = CharSet.Ansi
            , CallingConvention = CallingConvention.Cdecl)]
        public static extern void uninit();

        /// <summary>
        /// Onnx模型文件
        /// </summary>
        public string OnnxModel { get; set; } = string.Empty;

        public ExpressBill(ExpressBillPool pool) {
            //释放文件

            lock (pool) {
                _pool = pool;
                var modelFolder = $"{System.AppDomain.CurrentDomain.BaseDirectory}ExpressBill\\Lib";
                var n = init(modelFolder);
                //Ocr初始化异常
                OcrStatus = n != 0 ? OcrStatus.Uninitialized : OcrStatus.Initialized;
                NLog.LogManager.GetCurrentClassLogger().Error($"初始化完成:{n}");
            }
        }

        public void Dispose() {
            //uninit();
            _pool?.ReturnObject(this);
        }

        public OcrStatus OcrStatus { get; private set; } = OcrStatus.Uninitialized;

        public async Task<OcrResult?> ParseOcrResultAsync(Bitmap bitmap) {
            await Task.Yield();
            var submitTimestamp = DateTime.Now;
            if (OcrStatus == OcrStatus.Initialized) {
                try {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    using var mat = CreateMat(bitmap);
                    var ptr = process(mat.CvPtr);

                    var buf = Marshal.PtrToStringAnsi(ptr);

                    var unescape = Regex.Unescape(buf ?? string.Empty);
                    var result = System.Text.Json.JsonSerializer.Deserialize<RootResult>(unescape, new JsonSerializerOptions {
                        ReferenceHandler = ReferenceHandler.Preserve,
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        DefaultBufferSize = 8192,
                        MaxDepth = 4,
                        AllowTrailingCommas = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
                        WriteIndented = false,
                    });
                    var recognitionTime = DateTime.Now;
                    var recognitionTimestamp = new DateTimeOffset(recognitionTime).ToUnixTimeMilliseconds();
                    stopwatch.Stop();
                    if (result is not null) {
                        return GetFilteredResults(new OcrResult {
                            BarCode = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("waybill_number") == true)
                                ?.Str ?? string.Empty,
                            BarcodeArea = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("waybill_number") == true)
                                ?.Coord,
                            ElapsedTime = stopwatch.ElapsedMilliseconds,
                            Image = bitmap,
                            RecipientAddress = result.Data
                                ?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_addr") == true)
                                ?.Str ?? string.Empty,
                            RecipientAddressArea = result.Data
                                ?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_addr") == true)
                                ?.Coord,
                            RecipientName = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_name") == true)
                                ?.Str ?? string.Empty,
                            RecipientPhone = result.Data
                                ?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_phone") == true)
                                ?.Str ?? string.Empty,
                            RecognitionTime = recognitionTime,
                            RecognitionTimestamp = recognitionTimestamp,
                            SenderName = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("sender_name") == true)
                                ?.Str ?? string.Empty,
                            SenderPhone = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("sender_phone") == true)
                                ?.Str ?? string.Empty,
                            SenderAddress = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("sender_addr") == true)
                                ?.Str ?? string.Empty,
                            SenderAddressArea = result.Data
                                ?.FirstOrDefault(f => f.Title?.Key?.Equals("sender_addr") == true)
                                ?.Coord,
                            ThreeSegmentCode = result.Data
                                ?.FirstOrDefault(f => f.Title?.Key?.Equals("three_segment_code") == true)
                                ?.Str ?? string.Empty,
                            ThreeSegmentArea = result.Data
                                ?.FirstOrDefault(f => f.Title?.Key?.Equals("three_segment_code") == true)
                                ?.Coord,
                            VirtualNumber = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("virtual_number") == true)
                                ?.Str ?? string.Empty,
                            VirtualNumberLast4 =
                                result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("virtual_number_last4") == true)
                                    ?.Str ?? string.Empty,
                            SubmitTimestamp = new DateTimeOffset(submitTimestamp).ToUnixTimeMilliseconds(),
                            IsSuccess = true
                        });
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"Ocr识别异常:{e}");
                }
            }

            //识别不成功也需要返回图片
            return new OcrResult() {
                ElapsedTime = long.MinValue,
                Image = bitmap,
                SubmitTimestamp = new DateTimeOffset(submitTimestamp).ToUnixTimeMilliseconds(),
            };
        }

        public OcrResult? ParseOcrResult(Bitmap bitmap, YoloParser? yoloParser, float confidenceThreshold = 0.5f,
            float rectangleScale = 1) {
            var submitTimestamp = DateTime.Now;
            //过滤
            Bitmap? cropImage = null;
            Rectangle? cropRectangle = null;
            yoloParser ??= new YoloParser(OnnxModel);
            var stopwatch = new Stopwatch();
            if (yoloParser.IsLoaded) {
                var yoloInfos = yoloParser.Evaluate(bitmap, confidenceThreshold, rectangleScale);

                if (yoloInfos?.Any() == true) {
                    var yoloInfo = yoloInfos?.MaxBy(o => o.Confidence);
                    if (yoloInfo is not null) {
                        cropRectangle = yoloInfo.Rectangle;
                        var originalTopLeft = new Point(yoloInfo.Rectangle?.X ?? 0, yoloInfo.Rectangle?.Y ?? 0);
                        //裁剪
                        cropImage = CropImage(bitmap, yoloInfo.Rectangle ?? new Rectangle(0, 0, 0, 0));
                        //cropImage.Save($"{System.AppDomain.CurrentDomain.BaseDirectory}CropImage\\{new DateTimeOffset(submitTimestamp).ToUnixTimeMilliseconds()}.jpg");

                        try {
                            if (OcrStatus == OcrStatus.Initialized) {
                                stopwatch.Start();
                                using var mat = CreateMat(cropImage);
                                var ptr = process(mat.CvPtr);

                                var buf = Marshal.PtrToStringAnsi(ptr);

                                var unescape = Regex.Unescape(buf ?? string.Empty);
                                var result = System.Text.Json.JsonSerializer.Deserialize<RootResult>(unescape,
                                    new JsonSerializerOptions {
                                        ReferenceHandler = ReferenceHandler.Preserve,
                                        PropertyNameCaseInsensitive = true,
                                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                                        DefaultBufferSize = 8192,
                                        MaxDepth = 4,
                                        AllowTrailingCommas = true,
                                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                        NumberHandling = JsonNumberHandling.AllowReadingFromString |
                                                         JsonNumberHandling.WriteAsString,
                                        WriteIndented = false,
                                    });
                                var recognitionTime = DateTime.Now;
                                var recognitionTimestamp = new DateTimeOffset(recognitionTime).ToUnixTimeMilliseconds();
                                stopwatch.Stop();
                                if (result is not null) {
                                    return GetFilteredResults(new OcrResult {
                                        BarCode = result.Data?.FirstOrDefault(f =>
                                                f.Title?.Key?.Equals("waybill_number") == true)
                                            ?.Str ?? string.Empty,
                                        BarcodeArea = ConvertToRectangleAndOffset(result.Data
                                            ?.FirstOrDefault(f => f.Title?.Key?.Equals("waybill_number") == true)
                                            ?.Coord ?? new List<double>(), originalTopLeft.X, originalTopLeft.Y),
                                        ElapsedTime = stopwatch.ElapsedMilliseconds,
                                        Image = bitmap,
                                        CropImage = cropImage,
                                        RecipientAddress = result.Data
                                            ?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_addr") == true)
                                            ?.Str ?? string.Empty,
                                        RecipientAddressArea = ConvertToRectangleAndOffset(result.Data
                                            ?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_addr") == true)
                                            ?.Coord ?? new List<double>(), originalTopLeft.X, originalTopLeft.Y),
                                        RecipientName = result.Data?.FirstOrDefault(f =>
                                                f.Title?.Key?.Equals("recipient_name") == true)
                                            ?.Str ?? string.Empty,
                                        RecipientPhone = result.Data
                                            ?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_phone") == true)
                                            ?.Str ?? string.Empty,
                                        RecognitionTime = recognitionTime,
                                        RecognitionTimestamp = recognitionTimestamp,
                                        SenderName = result.Data?.FirstOrDefault(f =>
                                                f.Title?.Key?.Equals("sender_name") == true)
                                            ?.Str ?? string.Empty,
                                        SenderPhone = result.Data?.FirstOrDefault(f =>
                                                f.Title?.Key?.Equals("sender_phone") == true)
                                            ?.Str ?? string.Empty,
                                        SenderAddress = result.Data?.FirstOrDefault(f =>
                                                f.Title?.Key?.Equals("sender_addr") == true)
                                            ?.Str ?? string.Empty,
                                        SenderAddressArea = ConvertToRectangleAndOffset(result.Data
                                            ?.FirstOrDefault(f => f.Title?.Key?.Equals("sender_addr") == true)
                                            ?.Coord ?? new List<double>(), originalTopLeft.X, originalTopLeft.Y),
                                        ThreeSegmentCode = result.Data
                                            ?.FirstOrDefault(f => f.Title?.Key?.Equals("three_segment_code") == true)
                                            ?.Str ?? string.Empty,
                                        ThreeSegmentArea = ConvertToRectangleAndOffset(result.Data
                                            ?.FirstOrDefault(f => f.Title?.Key?.Equals("three_segment_code") == true)
                                            ?.Coord ?? new List<double>(), originalTopLeft.X, originalTopLeft.Y),
                                        VirtualNumber = result.Data?.FirstOrDefault(f =>
                                                f.Title?.Key?.Equals("virtual_number") == true)
                                            ?.Str ?? string.Empty,
                                        VirtualNumberLast4 =
                                            result.Data?.FirstOrDefault(f =>
                                                    f.Title?.Key?.Equals("virtual_number_last4") == true)
                                                ?.Str ?? string.Empty,
                                        SubmitTimestamp = new DateTimeOffset(submitTimestamp).ToUnixTimeMilliseconds(),
                                        IsSuccess = true,
                                        CropRectangle = yoloInfo.Rectangle
                                    });
                                }
                            }
                        }
                        catch (Exception e) {
                            NLog.LogManager.GetCurrentClassLogger().Error($"Ocr识别异常:{e}");
                        }
                    }
                }
            }

            //识别不成功也需要返回图片

            return new OcrResult() {
                RecognitionTime = DateTime.Now,
                RecognitionTimestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                CropRectangle = cropRectangle,
                CropImage = cropImage,
                ElapsedTime = stopwatch.ElapsedMilliseconds,
                Image = bitmap,
                SubmitTimestamp = new DateTimeOffset(submitTimestamp).ToUnixTimeMilliseconds(),
            };
        }

        public OcrResult? ParseOcrResult(Bitmap bitmap, float confidenceThreshold = 0.5f,
            float rectangleScale = 1) {
            _yoloParser ??= new YoloParser(OnnxModel);
            return ParseOcrResult(bitmap, _yoloParser, confidenceThreshold, rectangleScale);
        }

        public async Task<OcrResult?> ParseOcrResult(Bitmap bitmap, string cameraSerialNumber) {
            await Task.Yield();
            var submitTimestamp = DateTime.Now;
            if (OcrStatus == OcrStatus.Initialized) {
                try {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    using var mat = CreateMat(bitmap);
                    var ptr = process(mat.CvPtr);
                    var buf = Marshal.PtrToStringAnsi(ptr);
                    var unescape = Regex.Unescape(buf ?? string.Empty);
                    var result = System.Text.Json.JsonSerializer.Deserialize<RootResult>(unescape, new JsonSerializerOptions {
                        ReferenceHandler = ReferenceHandler.Preserve,
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        DefaultBufferSize = 8192,
                        MaxDepth = 4,
                        AllowTrailingCommas = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
                        WriteIndented = false,
                    });
                    var recognitionTime = DateTime.Now;
                    var recognitionTimestamp = new DateTimeOffset(recognitionTime).ToUnixTimeMilliseconds();
                    stopwatch.Stop();
                    if (result is not null) {
                        return GetFilteredResults(new OcrResult {
                            BarCode = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("waybill_number") == true)
                                ?.Str ?? string.Empty,
                            BarcodeArea = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("waybill_number") == true)
                                ?.Coord,
                            ElapsedTime = stopwatch.ElapsedMilliseconds,
                            Image = bitmap,
                            RecipientAddress = result.Data
                                ?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_addr") == true)
                                ?.Str ?? string.Empty,
                            RecipientAddressArea = result.Data
                                ?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_addr") == true)
                                ?.Coord,
                            RecipientName = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_name") == true)
                                ?.Str ?? string.Empty,
                            RecipientPhone = result.Data
                                ?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_phone") == true)
                                ?.Str ?? string.Empty,
                            RecognitionTime = recognitionTime,
                            RecognitionTimestamp = recognitionTimestamp,
                            SenderName = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("sender_name") == true)
                                ?.Str ?? string.Empty,
                            SenderPhone = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("sender_phone") == true)
                                ?.Str ?? string.Empty,
                            SenderAddress = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("sender_addr") == true)
                                ?.Str ?? string.Empty,
                            SenderAddressArea = result.Data
                                ?.FirstOrDefault(f => f.Title?.Key?.Equals("sender_addr") == true)
                                ?.Coord,
                            ThreeSegmentCode = result.Data
                                ?.FirstOrDefault(f => f.Title?.Key?.Equals("three_segment_code") == true)
                                ?.Str ?? string.Empty,
                            ThreeSegmentArea = result.Data
                                ?.FirstOrDefault(f => f.Title?.Key?.Equals("three_segment_code") == true)
                                ?.Coord,
                            VirtualNumber = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("virtual_number") == true)
                                ?.Str ?? string.Empty,
                            VirtualNumberLast4 =
                                result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("virtual_number_last4") == true)
                                    ?.Str ?? string.Empty,
                            SubmitTimestamp = new DateTimeOffset(submitTimestamp).ToUnixTimeMilliseconds(),
                            CameraSerialNumber = cameraSerialNumber,
                            IsSuccess = true
                        });
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"Ocr识别异常:{e}");
                }
            }

            //识别不成功也需要返回图片
            return new OcrResult() {
                ElapsedTime = long.MinValue,
                Image = bitmap,
                SubmitTimestamp = new DateTimeOffset(submitTimestamp).ToUnixTimeMilliseconds(),
                CameraSerialNumber = cameraSerialNumber
            };
        }

        public OcrResult GetFilteredResults(OcrResult source) {
            try {
                //source.ThreeSegmentCode = Regex.Replace(source.ThreeSegmentCode, @"[^0-9-]", "");
                //source.RecipientPhone = Regex.Replace(source.RecipientPhone, @"[^0-9-]", "");
                //source.SenderPhone = Regex.Replace(source.SenderPhone, @"[^0-9-]", "");
                source.BarCode = Regex.Replace(source.BarCode, @"[^0-9A-Za-z-]", "");
                return source;
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
            return source;
        }

        public async Task<KeyValuePair<bool, string>> SetOcrParameters(Dictionary<string, object> parameters) {
            //设置配置
            //定位文件位置
            await Task.Yield();
            if (!Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}ExpressBill\\Lib\\resource")) {
                return new KeyValuePair<bool, string>(false, "路径不存在");
            }
            List<string> parameterNames = new()
            {
                "three_segment_code",//三段码开关控制参数
                "recipient_name",//收件人姓名开关控制参数
                "recipient_phone",//收件人手机号开关控制参数
                "recipient_addr",//收件人地址开关控制参数
                "sender_name",//寄件人姓名开关控制参数
                "sender_phone",//寄件人手机号开关控制参数
                "sender_addr",//寄件人地址开关控制参数
                "virtual_number",//虚拟面单开关控制参数
                "log_level",//选填，日志级别(TRACE, DEBUG, INFO, WARN, ERR, CRITICAL, OFF)
                "log_path ",//选填，日志输出路径
                "console_log",//选填，是否输出控制台（Android下为logcat）日志
            };
            try {
                var list = parameters?.Where(w => !parameterNames.Contains(w.Key))?
                    .Select(s => s.Key)?.ToList();
                if (list?.Any() == true) {
                    return new KeyValuePair<bool, string>(false, $"参数不存在:{string.Join(",", list)}");
                }

                var lines = (await File.ReadAllLinesAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExpressBill", "Lib", "resource", "configure.ini")))
                    // 过滤注释行和不符合预期格式的行
                    .ToList();
                lines = [.. lines.Select(line => {
                    foreach (var parameter in (parameters ?? new Dictionary<string, object>()).Where(parameter => line.StartsWith(parameter.Key))) {
                        // 修改 log_level 的值
                        line = $"{parameter.Key}{(line.Contains("=") ? "=" : ":")}{parameter.Value?.ToString()?.ToLower()}";
                    }

                    return line;
                })];
                await File.WriteAllLinesAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExpressBill", "Lib", "resource", "configure.ini"), lines);
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public async Task<KeyValuePair<bool, string>> Initialize() {
            /*//限制
            try {
                await _semaphoreSlim.WaitAsync();
                //初始化
                if (OcrStatus == OcrStatus.Initialized) {
                    return new KeyValuePair<bool, string>(true, string.Empty);
                }
                var modelFolder = $"{System.AppDomain.CurrentDomain.BaseDirectory}ExpressBill\\Lib";
                var n = init(modelFolder);
                if (n != 0) {
                    return new KeyValuePair<bool, string>(false, $"sdk init fail and errcode is {n:D}");
                }
                else {
                    OcrStatus = OcrStatus.Initialized;
                    return new KeyValuePair<bool, string>(true, string.Empty);
                }
            }
            finally {
                _semaphoreSlim.Release();
            }*/
            return new KeyValuePair<bool, string>(false, "");
        }

        public Bitmap CropImage(Image image, Rectangle cropArea) {
            var croppedImage = new Bitmap(cropArea.Width, cropArea.Height);
            using var graphics = Graphics.FromImage(croppedImage);
            graphics.DrawImage(image, new Rectangle(0, 0, croppedImage.Width, croppedImage.Height), cropArea, GraphicsUnit.Pixel);
            return croppedImage;
        }

        /// <summary>
        /// 将位图直接复制到 OpenCV 原生内存，避免 JPEG 中转和重复编解码。
        /// </summary>
        private static Mat CreateMat(Bitmap bitmap) {
            Bitmap? convertedBitmap = null;
            var sourceBitmap = bitmap;
            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb) {
                convertedBitmap = new Bitmap(
                    bitmap.Width,
                    bitmap.Height,
                    PixelFormat.Format24bppRgb);
                using (var graphics = Graphics.FromImage(convertedBitmap)) {
                    graphics.DrawImageUnscaled(bitmap, 0, 0);
                }
                sourceBitmap = convertedBitmap;
            }

            BitmapData? bitmapData = null;
            try {
                bitmapData = sourceBitmap.LockBits(
                    new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);
                var stride = Math.Abs(bitmapData.Stride);
                var firstRow = bitmapData.Stride >= 0
                    ? bitmapData.Scan0
                    : IntPtr.Add(
                        bitmapData.Scan0,
                        bitmapData.Stride * (sourceBitmap.Height - 1));
                using var bitmapView = new Mat(
                    sourceBitmap.Height,
                    sourceBitmap.Width,
                    MatType.CV_8UC3,
                    firstRow,
                    stride);
                var mat = bitmapView.Clone();
                if (bitmapData.Stride < 0) {
                    Cv2.Flip(mat, mat, FlipMode.X);
                }
                return mat;
            }
            finally {
                if (bitmapData is not null) {
                    sourceBitmap.UnlockBits(bitmapData);
                }
                convertedBitmap?.Dispose();
            }
        }

        public Bitmap DrawRectangleOnImage(Image image, Rectangle drawArea, Color color, int thickness) {
            var markedImage = new Bitmap(image);
            using (var graphics = Graphics.FromImage(markedImage)) {
                using (var pen = new Pen(color, thickness)) {
                    graphics.DrawRectangle(pen, drawArea);
                }
            }
            return markedImage;
        }

        public static List<double> ConvertToRectangleAndOffset(List<double> rectangleData, int offsetX, int offsetY) {
            if (rectangleData == null || rectangleData.Count % 2 != 0 || rectangleData.Count < 8) {
                return new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0 };
            }

            var result = new List<double>();

            // 计算偏移后的坐标点
            for (var i = 0; i < rectangleData.Count; i += 2) {
                var x = rectangleData[i] + offsetX;
                var y = rectangleData[i + 1] + offsetY;

                result.Add(x);
                result.Add(y);
            }

            return result;
        }
    }

    public class Title {
        public string? Key { get; set; }
        public string? Value { get; set; }
    }

    public class DataItem {
        public List<double>? Coord { get; set; }
        public double Score { get; set; }
        public string? Str { get; set; }
        public Title? Title { get; set; }
    }

    public class RootResult {
        public List<DataItem>? Data { get; set; }
        public int Errno { get; set; }

        public string? Msg { get; set; }
    }
}
