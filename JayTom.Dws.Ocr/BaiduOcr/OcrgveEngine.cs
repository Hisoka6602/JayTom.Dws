using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Ocr.BaiduOcr {

    public class OcrgveEngine {

        public enum LogLevel : int {
            LOG_DEBUG = 0,
            LOG_INFO,
            LOG_WARNING,
            LOG_ERROR,
            NUM_LOG_LEVELS
        }

        public enum FieldSwitch : int {
            FIELD_OFF = 0,
            FIELD_ON,
            NUM_FIELD_SWITCH
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GeneralVerticalKvRet {
            public string vertical_key; // 垂直字段名
            public string value;        // 字段值
            public float score;         // 字段置信度
            public string zone_id;      // 字段的zone id
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ImageFrame {
            public int width;           // 图像宽度
            public int height;          // 图像高度
            public int stride;          // 图像stride
            public IntPtr data;         // 图像数据
            public int channels;        // 图像通道数
            public int data_type;       // 数据类型：0表示UInt8；1表示Float32；2表示UInt16
        }

        public enum ImageOrientation : int {
            ORIENTATION_UP = 0,
            ORIENTATION_DOWN,
            ORIENTATION_LEFT,
            ORIENTATION_RIGHT
        }

        public enum VISStatus : int {
            VIS_OK = 0,
            VIS_INVALID_INPUT = -1,
            VIS_INVALID_OUTPUT = -2,
            VIS_INVALID_PARAM = -3,
            VIS_INVALID_STATE = -4,
            VIS_NOT_SUPPORTED = -5,
            VIS_LICENSE_ERROR = -6,
            VIS_ENGINE_ERROR = -7,
            VIS_NETWORK_ERROR = -8,
            VIS_UNKNOWN_ERROR = -999
        }

        [DllImport("ocrgve.dll", EntryPoint = "?create@IOcrgveEngine@vis_ocrgve@@SAPEAV12@XZ", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create();

        [DllImport("ocrgve.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void destroy(ref IntPtr engine);

        [DllImport("ocrgve.dll", EntryPoint = "?set_log@IOcrgveEngine@vis_ocrgve@@SAXW4LogLevel@2@AEBV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@_N@Z", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set_log(in LogLevel level, in string path, bool is_console);

        [DllImport("ocrgve.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?init_license@IOcrgveEngine@vis_ocrgve@@SA?AW4VISStatus@2@AEBV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@0_N@Z")]
        private static extern VISStatus init_license(in string license_key, in string license_file, bool is_remote);

        [DllImport("ocrgve.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern VISStatus init(in IntPtr engine, in string resource_path, in FieldSwitch field_switch);

        [DllImport("ocrgve.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern VISStatus uninit();

        [DllImport("ocrgve.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern VISStatus process_ocrgvesdk(
            [MarshalAs(UnmanagedType.Struct)]
            [In] ImageFrame[] frames,
            ImageOrientation orientation,
            [MarshalAs(UnmanagedType.Struct)]
             ref GeneralVerticalKvRet[] response);

        public void Test() {
            LogLevel logLevel = LogLevel.LOG_DEBUG; // 设置日志级别
            string logFile = "log.txt"; // 日志文件名
            bool enableLog = true; // 是否启用日志
            //resource
            var s = Path.Combine($"{AppContext.BaseDirectory}", $"AISEE_OCR_KUAIDIDAN_WIN_1038_0927");
            var initLicense = init_license("AISEE_OCR_KUAIDIDAN_WIN_1038_0927", s, false);
            // 调用C++ DLL中的函数
            set_log(logLevel, logFile, enableLog);

            var nint = create();
            var combine = Path.Combine($"{AppContext.BaseDirectory}", "resource");
            var visStatus = init(nint, combine, FieldSwitch.NUM_FIELD_SWITCH);
            Console.WriteLine(visStatus);
        }
    }
}