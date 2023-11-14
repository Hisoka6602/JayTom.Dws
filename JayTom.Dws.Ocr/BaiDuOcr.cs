using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Ocr {

    public class BaiDuOcr : IOcr {

        #region Dll函数

        /// <summary>
        /// 初始化授权
        /// </summary>
        /// <param name="license_key"></param>
        /// <param name="license_file"></param>
        /// <param name="is_remote"></param>
        /// <returns></returns>

        [DllImport("ocrgve.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?init_license@IOcrgveEngine@vis_ocrgve@@SA?AW4VISStatus@2@AEBV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@0_N@Z")]
        public static extern VISStatus InitLicense(in string license_key, in string license_file, bool isTrial);

        [DllImport("ocrgve.dll", EntryPoint = "?create@IOcrgveEngine@vis_ocrgve@@SAPEAV12@XZ")]
        public static extern IntPtr create();

        [DllImport("ocrgve.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?set_log@IOcrgveEngine@vis_ocrgve@@SAXW4LogLevel@2@AEBV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@_N@Z")]
        public static extern void SetLog(LogLevel logLevel, string logFile, bool enableLog);

        #endregion Dll函数

        public void Dispose() {
            //释放
        }

        public bool ValidateAuthorization() {
            var combine = Path.Combine($"{AppContext.BaseDirectory}", $"BaiduOcr\\Validate");
            if (Directory.Exists(combine)) {
                var strings = Directory.GetFiles(combine);
                if (strings?.Any() == true) {
                    /*init_license(
                       "", strings.FirstOrDefault() ?? string.Empty, false);*/
                    /*var initLicense = init_license(
                        strings.FirstOrDefault() ?? string.Empty);*/

                    LogLevel logLevel = LogLevel.DEBUG; // 设置日志级别
                    string logFile = "log.txt"; // 日志文件名
                    bool enableLog = true; // 是否启用日志
                    //resource
                    var s = Path.Combine($"{AppContext.BaseDirectory}", $"AISEE_OCR_KUAIDIDAN_WIN_1038_0927");
                    var initLicense = InitLicense("AISEE_OCR_KUAIDIDAN_WIN_1038_0927", s, false);
                    // 调用C++ DLL中的函数
                    SetLog(logLevel, logFile, enableLog);

                    var nint = create();
                    Console.WriteLine($"{nint:x8}");
                }
            }

            return false;

            //
        }

        public string RecognizeLocal(string imagePath) {
            throw new NotImplementedException();
        }

        public string RecognizeOnline(string imageUrl) {
            throw new NotImplementedException();
        }

        public void SetParameter(string key, object value) {
            throw new NotImplementedException();
        }

        public event EventHandler<OcrExceptionEventArgs>? OcrExceptionOccurred;

        public event EventHandler<OcrInitializationExceptionEventArgs>? OcrInitializationExceptionOccurred;

        public event EventHandler<OcrContentRecognizedEventArgs>? OcrContentRecognized;

        public event EventHandler<AuthenticationExceptionEventArgs>? AuthenticationExceptionOccurred;

        public OcrStatus Status { get; }

        public void SubmitImage(Bitmap imageBytes) {
            throw new NotImplementedException();
        }

        public void SetOcrParameters(Dictionary<string, object> parameters) {
            throw new NotImplementedException();
        }

        Task<KeyValuePair<bool, string>> IOcr.Initialize() {
            throw new NotImplementedException();
        }

        public void Initialize() {
            throw new NotImplementedException();
        }

        public enum VISStatus {
            SUCCESS = 0,
            // 其他状态值...
        }

        public enum LogLevel {
            DEBUG = 0,
            INFO = 1,
            WARNING = 2,
            ERROR = 3,
            FATAL = 4
        }
    }
}