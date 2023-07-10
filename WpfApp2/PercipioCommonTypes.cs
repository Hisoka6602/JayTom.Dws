using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WpfApp2 {

    public class PercipioCommonTypes {

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Vec3IBoxSize {
            public int sizeX;
            public int sizeY;
            public int sizeZ;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PmPoint {
            public int x;
            public int y;
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
            public byte[]? depth_data;

            public bool showDataInColor;
            public int color_width;
            public int color_height;
            public byte[]? color_data;

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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CvRect {
            public Int32 x;
            public Int32 y;
            public Int32 w;
            public Int32 h;
        }

        [System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BlockHeader {
            public Int64 timestamp;
            public Int32 headSize;
            public Int32 bodySize;
            public Int16 err;
            public Int16 dataType;
            public Int16 channelId;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public Int16[] __rsvd;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ImageHeader {  //32 + 12 + 20 = 64
            public BlockHeader blk;
            public char format;     ///< see I_IMG_FORMAT_LIST
            public char pixelType;    ///< see I_IMG_PIXEL_TYPE_LIST
            public char pixelSize;
            public char __rsvd0;
            public Int16 width;
            public Int16 height;
            public Int32 size;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public char[] devSN;

            public Int32 __rsvd1;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PackageData {
            public BlockHeader blk;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] devID;//[32];      ///< User defined device ID

            public Int32 count;
            public byte detectSource;       ///< see PACKAGE_DETECT_SOURCE_LIST

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 35)]
            public byte[] __rsvd;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SinglePackageInfo {
            public char type;           ///< see PACKAGE_TYPE_LIST

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public char[] __rsvd0;//[3];

            public float sizeX;
            public float sizeY;
            public float sizeZ;
            public float boundingVolume; ///< accumulated vol
            public float integeralVolume;///< accumulated vol
            public float centerPosX;
            public float centerPosY;
            public float posAngle;
            public float distanceLeft;   ///< positive means safe
            public float distanceRight;
            public float distanceTop;
            public float distanceBottom;
            public float frontSd;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public float[] pixelPoints;//[8]; ///< vertex of bounding box, {x,y}[4]

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public float[] pixelPointsRGB;//[8]; ///< vertex of bounding box in color, {x,y}[4]

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public float[] pixelPointsProj;//[8]; ///< vertex of bounding box proj, {x,y}[4]

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public float[] realPoints;//[8]; ///< vertex of bounding box in mm, {x,y}[4]

            public float topCenterX;
            public float topCenterY;
            public float topCenterZ;
            public float avgZ; ///< accumulated vol

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 52)]
            public Int32[] __rsvd1;//[52];
        };

        public enum I_DATA_TYPE_LIST {
            I_DATA_DEPTH_IMAGE = 1,
            I_DATA_COLOR_IMAGE = 2,
            I_DATA_SETUPPER_IMAGE = 3,
            I_DATA_PACKAGE_MEASURE = 4,
            I_DATA_BOX_CUTTER = 5,
            I_DATA_EMPTY_PALLET = 6,
            I_DATA_BAG_CUTTER = 7,
            I_DATA_CALIB_IMAGE = 8,
            I_DATA_CALIB_DATA = 9,
            I_DATA_PACKAGE_MEASURE_EX = 10,
            I_DATA_PACKAGE_LOCATION = 11,
            I_DATA_DEPTH_MEASURE = 12,
            I_DATA_COLOR_MEASURE = 13,
            I_DATA_HEAD_COUNTER = 14,
            I_DATA_FORKLIFT_LOCATION = 15,
            I_DATA_CARDBOARD_LOCATION = 16,
            I_DATA_TROLLEY_VOLUME = 17,
            I_DATA_SEPARATOR_DRIVER = 18,
            I_DATA_P3D = 19,
            I_DATA_OVEN_DETECTION = 20,
            I_DATA_STEEL_PIPE_LOCATION = 21,
            I_DATA_SOLID_FLOW_METER = 22,
            I_DATA_WAGON_LOADING = 23,
            I_DATA_PACKAGE_PICKING = 24,
            I_DATA_SINGULATOR_CHECKER = 25,
            I_DATA_IMAGE = 100,
            I_DATA_FRAME = 101,
            I_DATA_APP_INFO = 1000,
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

        public enum I_IMG_FORMAT_LIST {
            I_IMG_FORMAT_RAW = 1,
            I_IMG_FORMAT_PNG = 2,
            I_IMG_FORMAT_JPG = 3,
            I_IMG_FORMAT_MAX
        };

        public enum PACKAGE_TYPE_LIST {
            I_PACKAGE_TYPE_NONE = 0,
            I_PACKAGE_TYPE_BOX = 1,
            I_PACKAGE_TYPE_BAG = 2,
        };
    }
}