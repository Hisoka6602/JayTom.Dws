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
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Reflection.Metadata;
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

        public ExpressBill(ExpressBillPool pool) {
            //释放文件

            lock (pool) {
                //初始化

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

        public async Task<OcrResult?> ParseOcrResult(Bitmap bitmap) {
            await Task.Yield();
            if (OcrStatus == OcrStatus.Initialized) {
                try {
                    var matBgr = new Mat();
                    var submitTimestamp = DateTime.Now;
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    using var stream = new MemoryStream();
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    //stream.Position = 0;
                    var array = stream.ToArray();
                    var mat = Cv2.ImDecode(array, ImreadModes.Unchanged);
                    //Cv2.CvtColor(mat, matBgr, ColorConversionCodes.RGB2BGR);
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

            return null;
        }

        public async Task<OcrResult?> ParseOcrResult(Bitmap bitmap, string cameraSerialNumber) {
            await Task.Yield();
            var submitTimestamp = DateTime.Now;
            if (OcrStatus == OcrStatus.Initialized) {
                try {
                    //var matBgr = new Mat();
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    using var stream = new MemoryStream();
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    //stream.Position = 0;
                    var array = stream.ToArray();
                    var mat = Cv2.ImDecode(array, ImreadModes.Unchanged);
                    //Cv2.CvtColor(mat, matBgr, ColorConversionCodes.RGB2BGR);
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
                source.ThreeSegmentCode = Regex.Replace(source.ThreeSegmentCode, @"[^0-9-]", "");
                source.RecipientPhone = Regex.Replace(source.RecipientPhone, @"[^0-9-]", "");
                source.SenderPhone = Regex.Replace(source.SenderPhone, @"[^0-9-]", "");
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
                lines = lines.Select(line => {
                    foreach (var parameter in (parameters ?? new Dictionary<string, object>()).Where(parameter => line.StartsWith(parameter.Key))) {
                        // 修改 log_level 的值
                        line = $"{parameter.Key}{(line.Contains("=") ? "=" : ":")}{parameter.Value?.ToString()?.ToLower()}";
                    }

                    return line;
                }).ToList();
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