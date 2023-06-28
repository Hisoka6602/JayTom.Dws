using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Device.Camera._3DCamera.Percipio {

    public class PercipioAppUtils {

        public struct PmPoint {
            public int X;
            public int Y;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Vec3IBoxSize {
            public int sizeX;
            public int sizeY;
            public int sizeZ;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct AllData {
            public int type;
            public bool running;
            public bool newData;
            public bool newDepth;
            public bool newColor;
            public Vec3IBoxSize boxSize;

            public bool showDataInDepth;
            public int depth_width;
            public int depth_height;
            public byte[] depth_data;

            public bool showDataInColor;
            public int color_width;
            public int color_height;
            public byte[] color_data;

            public bool newP3D;
            public bool showDataInP3D;
            public int p3d_width;
            public int p3d_height;
            public byte[] p3d_data;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public PmPoint[] bounding;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public PmPoint[] boundingRGB;
        };

        public enum I_IMG_PIXEL_TYPE_LIST {
            I_IMG_PIXEL_TYPE_16U = 1, // raw depth
            I_IMG_PIXEL_TYPE_RGB888 = 2,
            I_IMG_PIXEL_TYPE_F32C3 = 3,
            I_IMG_PIXEL_TYPE_TYPE_MAX
        };

        public enum I_IMG_FORMAT_LIST {
            I_IMG_FORMAT_RAW = 1,
            I_IMG_FORMAT_PNG = 2,
            I_IMG_FORMAT_JPG = 3,
            I_IMG_FORMAT_MAX
        };

        public enum I_PROPERTY_LIST {
            I_PROPERTY_string_APP_NAME = 0x10000101,
            I_PROPERTY_string_APP_UID = 0x10000102,
            I_PROPERTY_int_APP_VERSION = 0x10000106,
            I_PROPERTY_bool_AUTO_START = 0x10000109,
            I_PROPERTY_int_LOG_LEVEL = 0x1000010c,
            I_PROPERTY_string_PROTOCOL_CONFIGS = 0x10000113,
            I_PROPERTY_bool_SHOW_DEBUG_IMAGE = 0x10000114,
            I_PROPERTY_string_PROTOCOL_LISTS = 0x10000115,
            I_PROPERTY_string_PROTOCOL_FILE_LISTS = 0x10000116,
            I_PROPERTY_bytes_MEMORY_DATA = 0x10000117,
            I_PROPERTY_string_TAG_PROTOCOL_FILE = 0x10000118,
            I_PROPERTY_string_REMOVE_PROTOCOL_FILE = 0x10000119,
            I_PROPERTY_string_FETCH_PROTOCOL_FILE = 0x1000011a,

            I_PROPERTY_string_DEV_IP = 0x50000101,
            I_PROPERTY_string_DEV_NETMASK = 0x50000102,
            I_PROPERTY_string_DEV_GATEWAY = 0x50000103,
            I_PROPERTY_string_DEV_DNS = 0x50000104,

            I_PROPERTY_bool_ENABLE_P3D = 0x20010105,
            I_PROPERTY_bool_ENABLE_LEFT_IR = 0x20010106,
            I_PROPERTY_bool_TRIGGER_MODE = 0x20010108,
            I_PROPERTY_int_FRAMEPERTRIGGER = 0x20010109,
            I_PROPERTY_int_LASER_POWER = 0x2001010a,
            I_PROPERTY_bool_RGBD_REGISTER = 0x2001010b,
            I_PROPERTY_int_LEFT_IR_GAIN = 0x20010132,
            I_PROPERTY_int_RIGHT_IR_GAIN = 0x20010133,
            I_PROPERTY_int_LEFT_R_GAIN = 0x20010134,
            I_PROPERTY_int_LEFT_G_GAIN = 0x20010135,
            I_PROPERTY_int_LEFT_B_GAIN = 0x20010136,
            I_PROPERTY_bool_LEFT_RGB_AUTOEXPOSURE = 0x20010158,
            I_PROPERTY_bool_LEFT_RGB_AUTOAWB = 0x2001015a,
            I_PROPERTY_bool_LEFT_ENHANCE_FILTER = 0x30000101,
            I_PROPERTY_bool_LEFT_SPECKLE_FILTER = 0x30000102,
            I_PROPERTY_int_LEFT_IR_EXPOSURE = 0x2001013a,
            I_PROPERTY_int_RIGHT_IR_EXPOSURE = 0x2001013b,
            I_PROPERTY_int_LEFT_RGB_EXPOSURE = 0x2001013c,
            I_PROPERTY_bool_LASER_AUTO_CONTROL = 0x20010145,
            I_PROPERTY_int_NFRAMES_FUSION = 0x2001014e,
            I_PROPERTY_int_SPECKLE_FILTERAREA = 0x30001021,

            I_PROPERTY_bool_AGC_RECORDDEPTH = 0x30001030,
            I_PROPERTY_bool_AGC_RECORDCOLOR = 0x30001031,
            I_PROPERTY_bool_PM_RECORDDEPTH = 0x30040200,
            I_PROPERTY_bool_PM_RECORDCOLOR = 0x30040202,
            I_PROPERTY_int_MAXRECORD_COUNT = 0x30000109,

            I_PROPERTY_int_PM_BG_COUNT = 0x30040102,
            I_PROPERTY_int_PM_LEAST_HEIGHT = 0x30040105,
            I_PROPERTY_int_PM_AUTO_THIN = 0x30040121,
            I_PROPERTY_int_PM_THINHEIGHTTHRESH = 0x30040120,
            I_PROPERTY_bool_PM_ONLYSAFERECT = 0x30040117,
            I_PROPERTY_bool_PM_MEASURETOTAL = 0x30040118,
            I_PROPERTY_bool_PM_THINFORCE = 0x3004011e,
            I_PROPERTY_bool_PM_NEVER_BOX = 0x3004011b,
            I_PROPERTY_bool_PM_UNBLANK_IRREG = 0x30040155,
            I_PROPERTY_bool_PM_ALWAYS_BOX = 0x30040123,
            I_PROPERTY_bool_PM_SHRINK_IRREGULAR = 0x30040131,
            I_PROPERTY_int_PM_BGCOLORDIFF = 0x30040133,
            I_PROPERTY_float_PM_EDGECUTCOEF = 0x3004010a,
            I_PROPERTY_float_PM_MAXSHRINKCOEF = 0x3004010b,

            I_PROPERTY_float_PM_BOXFIX = 0x3004010e,
            I_PROPERTY_float_PM_IRREGFIX = 0x3004010f,

            I_PROPERTY_int_DEPTH_FORMAT = 0x30010101,
            I_PROPERTY_int_COLOR_FORMAT = 0x30020101,

            I_PROPERTY_float_SETUPPER_WCOEF = 0x30030101,
            I_PROPERTY_float_SETUPPER_HCOEF = 0x30030102,

            I_PROPERTY_bool_GRAB_DEPTH = 0x40010101,
            I_PROPERTY_bool_GRAB_COLOR = 0x40010102,
            I_PROPERTY_bool_GRAB_SETUPPER = 0x40010103,
            I_PROPERTY_bool_GRAB_PM = 0x40010104,
            I_PROPERTY_bool_GRAB_BC = 0x40010105,
            I_PROPERTY_bool_GRAB_EP = 0x40010106,
            I_PROPERTY_bool_GRAB_BAG_CUTTER = 0x40010107,
            I_PROPERTY_bool_GRAB_MULTI_CALIB = 0x40010108,
            I_PROPERTY_bool_GRAB_P3D = 0x40010113,

            I_PROPERTY_int2_COLORIMAGEMODE = 0x20010114,
            I_PROPERTY_int2_DEPTHIMAGEMODE = 0x20010115,
            I_PROPERTY_int2_DEVRESIZEIMAGESIZE = 0x20010163,
            I_PROPERTY_int_MIN_AREA = 0x30040101,
            I_PROPERTY_int_BG_CNT = 0x30040102,
            I_PROPERTY_int4_DEPTH_ROI = 0x30040103,
            I_PROPERTY_int4_COLOR_ROI = 0x30040104,
            I_PROPERTY_int_LEAST_HEIGHT = 0x30040105,
            I_PROPERTY_int_PIX_CUT_FOR_PLANE = 0x30040106,
            I_PROPERTY_bool_OPTIMIZE_BOX = 0x30040107,
            I_PROPERTY_int2_BOX_CUTTING_UPDOWN = 0x30040108,
            I_PROPERTY_float_MAX_PLANE_SD = 0x30040109,
            I_PROPERTY_float_EDGE_CUTTING_COEF = 0x3004010a,
            I_PROPERTY_float_MAX_SHRINK_COEF = 0x3004010b,
            I_PROPERTY_int4_SAFE_RECT = 0x3004010c,
            I_PROPERTY_bool_MASK_BLACK = 0x3004010d,
            I_PROPERTY_float3_BOX_FIX = 0x3004010e,
            I_PROPERTY_float3_IRREG_FIX = 0x3004010f,
            I_PROPERTY_float3n_XY_FIX_BY_Z = 0x30040110,
            I_PROPERTY_bool_USE_MOVE_FIX = 0x30040111,
            I_PROPERTY_float2_MOVE_FIX = 0x30040112,
            I_PROPERTY_bool_USE_SPEED_FIX = 0x30040113,
            I_PROPERTY_float2_SPEED_FIX = 0x30040114,
            I_PROPERTY_float_ROTATE_FIX = 0x30040115,
            I_PROPERTY_float_BASE_PLANE_OFFSET = 0x30040116,
            I_PROPERTY_bool_ONLY_SAFERECT = 0x30040117,
            I_PROPERTY_bool_MEASURE_TOTAL = 0x30040118,
            I_PROPERTY_int_PROJECTED_BG_Z = 0x30040119,
            I_PROPERTY_bool_REDO_UNBLANK_IRREG = 0x3004011a,
            I_PROPERTY_bool_NEVER_BOX = 0x3004011b,
            I_PROPERTY_bool_THIN_OBJ = 0x3004011e,
            I_PROPERTY_int_THIN_THRED = 0x30040120,
            I_PROPERTY_bool_THIN_OBJ_AUTO = 0x30040121,

            I_PROPERTY_float_MIN_OBJ_DISTANCE = 0x30040202,

            I_PROPERTY_bool_EXTRACT_BG = 0x30040139,
            I_PROPERTY_int_BGG_DIST_THRESH = 0x3004013a,
            I_PROPERTY_int_BGG_MSE_THRESH = 0x3004013b,
            I_PROPERTY_int2_BGG_ANCHOR = 0x3004013c,

            I_PROPERTY_bool_PM_GEN_SAFE_RECT = 0x3004013f,

            I_PROPERTY_int_DEV_PROJ_UNIT_MM = 0x2001010e,

            I_PROPERTY_int_DEV_PROJ_SIZE = 0x2001010f,

            // bg
            I_CMD_RESET_BG_IMAGE = 0x30040001,

            I_PROPERTY_int_BG_COUNT = 0x30040102,
            I_PROPERTY_image_DEPTH_BG = 0x30040137,
            I_PROPERTY_image_COLOR_BG = 0x30040138,
            I_PROPERTY_bool_ENABLE_DEPTH_BG = 0x30040300,
            I_PROPERTY_bool_ENABLE_COLOR_BG = 0x30040301,
            I_PROPERTY_float_BG_ROI_ZOOM_COEF = 0x30040303,
            I_PROPERTY_bool_ENABLE_IRL_BG = 0x30040304,
            I_PROPERTY_int_RESET_BG_STATUS = 0x30040305,
        };

        public enum I_APPCENTER_CMD_LIST {
            I_CMD_APP_INIT = 0x10010001,
            I_CMD_APP_DEINIT = 0x10010002,
            I_PROPERTY_string_APPS_DESC = 0x10010101, // Apps description
            I_PROPERTY_bool_AUTO_RUN_APP = 0x10010102,
            I_PROPERTY_bool_APP_IS_RUNNING = 0x10010103, // current app is running
        };

        public enum I_CMD_LIST {
            I_CMD_START_CAPTURE = 0x20000001,
            I_CMD_STOP_CAPTURE = 0x20000002,
            I_CMD_TRIGGER = 0x20000004,
            I_CMD_CALC = 0x20000005,
            I_CMD_RESET_STATISTICS = 0x10000004,
            I_CMD_HEART_BEAT = 0x100000ff,  ///< heart beat
            I_CMD_REBOOT = 0x50000001,
            I_CMD_RESET_BG_IMAGE = 0x30040001,
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CvRect {
            public Int32 x;
            public Int32 y;
            public Int32 w;
            public Int32 h;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CvAnchor {
            public Int32 xAnchor;
            public Int32 yAnchor;
        }

        #region 方法

        public static IntPtr StringToByteArray(string s) {
            try {
                var b = new byte[s.Length + 1];
                int i;
                for (i = 0; i < s.Length; i++)
                    b[i] = (byte)s.ToCharArray()[i];
                b[s.Length] = 0;
                var p = Marshal.AllocCoTaskMem(s.Length + 1);
                Marshal.Copy(b, 0, p, s.Length + 1);
                return p;
            }
            catch {
                return IntPtr.Zero;
            }
        }

        #endregion 方法
    }
}