using System;
using System.IO;
using System.Linq;
using System.Text;
using OpenCvSharp;
using System.Drawing;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Ocr.ExpressBill {

    public class ExpressBillPool : IDisposable {
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

        private Stack<ExpressBillPool> pool = new Stack<ExpressBillPool>();

        public OcrResult? ParseOcrResult(Bitmap bitmap) {
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
                        RecipientAddress = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_addr") == true)
                            ?.Str ?? string.Empty,
                        RecipientAddressArea = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("recipient_addr") == true)
                        ?.Coord,
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
                        SenderAddress = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("sender_addr") == true)
                            ?.Str ?? string.Empty,
                        SenderAddressArea = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("sender_addr") == true)
                            ?.Coord,
                        ThreeSegmentCode = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("three_segment_code") == true)
                            ?.Str ?? string.Empty,
                        ThreeSegmentArea = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("three_segment_code") == true)
                            ?.Coord,
                        VirtualNumber = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("virtual_number") == true)
                            ?.Str ?? string.Empty,
                        VirtualNumberLast4 = result.Data?.FirstOrDefault(f => f.Title?.Key?.Equals("virtual_number_last4") == true)?.Str ?? string.Empty,
                        SubmitTimestamp = new DateTimeOffset(submitTimestamp).ToUnixTimeMilliseconds(),
                    });
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"Ocr识别异常:{e}");
            }

            return null;
        }

        public OcrContentRecognizedEventArgs GetFilteredResults(OcrContentRecognizedEventArgs source) {
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

        private void ReleaseUnmanagedResources() {
            // TODO release unmanaged resources here
        }

        protected virtual void Dispose(bool disposing) {
            ReleaseUnmanagedResources();
            if (disposing) {
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ExpressBillPool() {
            Dispose(false);
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