using System;
using System.IO;
using System.Linq;
using System.Text;
using OpenCvSharp;
using System.Drawing;
using Newtonsoft.Json;
using System.Text.Json;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace JayTom.Dws.Ocr.ExpressBill {

    public class ExpressBill : IOcr {
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
        }

        public event EventHandler<OcrExceptionEventArgs>? OcrExceptionOccurred;

        public event EventHandler<OcrInitializationExceptionEventArgs>? OcrInitializationExceptionOccurred;

        public event EventHandler<OcrContentRecognizedEventArgs>? OcrContentRecognized;

        public event EventHandler<AuthenticationExceptionEventArgs>? AuthenticationExceptionOccurred;

        public OcrStatus Status { get; private set; }

        public void SubmitImage(Bitmap bitmap) {
            if (!_isOnline) {
                //本地识别
                try {
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

                    /*var result = JsonConvert.DeserializeObject<RootResult>(unescape, new JsonSerializerSettings() {
                        MaxDepth = 5, // 设置为适当的深度
                        TypeNameHandling = TypeNameHandling.None,
                        CheckAdditionalContent = false,
                        NullValueHandling = NullValueHandling.Ignore, // 忽略 null 值
                        DefaultValueHandling = DefaultValueHandling.Ignore, // 忽略默认值
                        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple, // 使用简单格式
                    });*/

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
                            VirtualNumberLast4 = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("virtual_number_last4") == true)?.Str ?? string.Empty
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

        public void SetOcrParameters(Dictionary<string, object> parameters) {
            throw new NotImplementedException();
        }

        public async Task<KeyValuePair<bool, string>> Initialize() {
            await Task.Yield();
            //初始化

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
                return new KeyValuePair<bool, string>(true, string.Empty);
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