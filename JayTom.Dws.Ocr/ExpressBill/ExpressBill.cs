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

namespace JayTom.Dws.Ocr.ExpressBill {

    public class ExpressBill : IOcr {
        private SemaphoreSlim _semaphoreSlim = new(1, 1);
        private const string DllPath = ".\\ExpressBill\\Lib\\Dll\\ExpressBillApi.dll";

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

        /*[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetDllDirectory(string lpPathName);*/

        /// <summary>
        /// 是否在线解析
        /// </summary>
        private bool _isOnline = false;

        public ExpressBill() {
            //释放文件
            CopyFiles(".\\ExpressBill\\Lib\\Dll", AppDomain.CurrentDomain.BaseDirectory);
        }

        public void Dispose() {
            uninit();
            OcrStatus = OcrStatus.Uninitialized;
        }

        public event EventHandler<OcrExceptionEventArgs>? OcrExceptionOccurred;

        public event EventHandler<OcrInitializationExceptionEventArgs>? OcrInitializationExceptionOccurred;

        public event EventHandler<OcrContentRecognizedEventArgs>? OcrContentRecognized;

        public event EventHandler<AuthenticationExceptionEventArgs>? AuthenticationExceptionOccurred;

        public OcrStatus OcrStatus { get; private set; }

        public void SubmitImage(Bitmap bitmap, string cameraSerialNumber = "") {
            if (!_isOnline) {
                //本地识别
                try {
                    var submitTimestamp = DateTime.Now;
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    using var stream = new MemoryStream();
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                    var array = stream.ToArray();
                    var mat = Cv2.ImDecode(array, ImreadModes.Color);
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
                        //回调识别到的内容
                        OnOcrContentRecognized(new OcrContentRecognizedEventArgs() {
                            BarCode = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("waybill_number") == true)
                                ?.Str ?? string.Empty,
                            ElapsedTime = stopwatch.ElapsedMilliseconds,
                            Image = bitmap,
                            RecipientAddress = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_addr") == true)
                                ?.Str ?? string.Empty,
                            RecipientName = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_name") == true)
                                ?.Str ?? string.Empty,
                            RecipientPhone = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_phone") == true)
                                ?.Str ?? string.Empty,
                            RecognitionTime = recognitionTime,
                            RecognitionTimestamp = recognitionTimestamp,
                            SenderName = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("sender_name") == true)
                                ?.Str ?? string.Empty,
                            SenderPhone = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("sender_phone") == true)
                                ?.Str ?? string.Empty,
                            ThreeSegmentCode = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("three_segment_code") == true)
                                ?.Str ?? string.Empty,
                            VirtualNumber = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("virtual_number") == true)
                                ?.Str ?? string.Empty,
                            VirtualNumberLast4 = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("virtual_number_last4") == true)?.Str ?? string.Empty,
                            CameraSerialNumber = cameraSerialNumber,
                            SubmitTimestamp = new DateTimeOffset(submitTimestamp).ToUnixTimeMilliseconds()
                        });
                    }
                }
                catch (Exception e) {
                    OnOcrExceptionOccurred(new OcrExceptionEventArgs() {
                        Exception = e
                    });
                }
            }
            else {
                //网络识别
            }
        }

        public OcrResult? ParseOcrResult(Bitmap bitmap) {
            if (!_isOnline) {
                try {
                    var submitTimestamp = DateTime.Now;
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    using var stream = new MemoryStream();
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                    var array = stream.ToArray();
                    var mat = Cv2.ImDecode(array, ImreadModes.Color);
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
                        return new OcrResult {
                            BarCode = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("waybill_number") == true)
                                  ?.Str ?? string.Empty,
                            ElapsedTime = stopwatch.ElapsedMilliseconds,
                            Image = bitmap,
                            RecipientAddress = result.Data
                                  ?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_addr") == true)
                                  ?.Str ?? string.Empty,
                            RecipientName = result.Data
                                  ?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_name") == true)
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
                            ThreeSegmentCode = result.Data
                                  ?.FirstOrDefault(f => f.Title?.Key?.Equals("three_segment_code") == true)
                                  ?.Str ?? string.Empty,
                            VirtualNumber = result.Data
                                  ?.FirstOrDefault(f => f.Title?.Key?.Equals("virtual_number") == true)
                                  ?.Str ?? string.Empty,
                            VirtualNumberLast4 =
                                  result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("virtual_number_last4") == true)
                                      ?.Str ?? string.Empty,
                            SubmitTimestamp = new DateTimeOffset(submitTimestamp).ToUnixTimeMilliseconds()
                        };
                    }
                }
                catch (Exception e) {
                    OnOcrExceptionOccurred(new OcrExceptionEventArgs() {
                        Exception = e,
                        ExceptionTime = DateTime.Now
                    });
                }
            }

            return null;
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
                        line = $"{parameter.Key}{(line.Contains("=") ? "=" : ":")}{parameter.Value.ToString()}";
                    }

                    return line;
                }).ToList();
                await File.WriteAllLinesAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExpressBill", "Lib", "resource", "configure.ini"), lines);
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            catch (Exception e) {
                OnOcrExceptionOccurred(new OcrExceptionEventArgs() {
                    Exception = e,
                    ExceptionTime = DateTime.Now
                });
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public async Task<KeyValuePair<bool, string>> Initialize() {
            //限制
            try {
                await _semaphoreSlim.WaitAsync();
                //初始化
                if (OcrStatus == OcrStatus.Initialized) {
                    return new KeyValuePair<bool, string>(true, string.Empty);
                }
                var modelFolder = $"{System.AppDomain.CurrentDomain.BaseDirectory}ExpressBill\\Lib";
                var n = init(modelFolder);
                if (n != 0) {
                    //Ocr初始化异常
                    OnOcrInitializationExceptionOccurred(new OcrInitializationExceptionEventArgs() {
                        Exception = new Exception($"sdk init fail and errcode is {n:D}"),
                        ExceptionTime = DateTime.Now
                    });
                    return new KeyValuePair<bool, string>(false, $"sdk init fail and errcode is {n:D}");
                }
                else {
                    OcrStatus = OcrStatus.Initialized;
                    return new KeyValuePair<bool, string>(true, string.Empty);
                }
            }
            finally {
                _semaphoreSlim.Release();
            }
        }

        private void CopyFiles(string sourceDirectory, string targetDirectory) {
            try {
                // 获取源目录和目标目录中的所有文件
                var sourceFiles = Directory.GetFiles(sourceDirectory);
                var targetFiles = Directory.GetFiles(targetDirectory);

                // 使用 LINQ 过滤出尚未复制的文件并进行复制
                var filesToCopy = sourceFiles.Except(targetFiles.Select(Path.GetFileName));

                // 复制文件
                foreach (var file in filesToCopy) {
                    File.Copy(file ?? string.Empty, Path.Combine(targetDirectory, Path.GetFileName(file) ?? string.Empty));
                }
            }
            catch (Exception e) {
                OnOcrExceptionOccurred(new OcrExceptionEventArgs() {
                    Exception = e,
                    ExceptionTime = DateTime.Now
                });
            }
        }

        protected virtual async void OnOcrInitializationExceptionOccurred(OcrInitializationExceptionEventArgs e) {
            await Task.Yield();
            OcrInitializationExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnOcrExceptionOccurred(OcrExceptionEventArgs e) {
            await Task.Yield();
            OcrExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnOcrContentRecognized(OcrContentRecognizedEventArgs e) {
            await Task.Yield();
            OcrContentRecognized?.Invoke(this, e);
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