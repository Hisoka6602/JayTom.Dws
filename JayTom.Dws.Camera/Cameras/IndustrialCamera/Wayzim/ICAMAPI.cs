using System.Text;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Camera.Cameras.IndustrialCamera.Wayzim {

    //SDK接口的返回值，定义如下
    public enum ICAM_SDK_STATUS {
        ICAM_SDK_STATUS_SUCCESS = 0,   // 操作成功
        ICAM_SDK_STATUS_FAILED = -1,   // 操作失败
        ICAM_SDK_STATUS_intERNAL_ERROR = -2,   // 内部错误
        ICAM_SDK_STATUS_UNKNOW = -3,   // 未知错误
        ICAM_SDK_STATUS_NOT_SUPPORTED = -4,   // 不支持该功能
        ICAM_SDK_STATUS_NOT_INITIALIZED = -5,   // 初始化未完成
        ICAM_SDK_STATUS_PARAMETER_INVALID = -6,   // 参数无效
        ICAM_SDK_STATUS_PARAMETER_OUT_OF_BOUND = -7,   // 参数越界
        ICAM_SDK_STATUS_UNENABLED = -8,   // 未使能
        ICAM_SDK_STATUS_USER_CANCEL = -9,   // 用户手动取消了，比如roi面板点击取消，返回
        ICAM_SDK_STATUS_PATH_NOT_FOUND = -10,  // 注册表中没有找到对应的路径
        ICAM_SDK_STATUS_SIZE_DISMATCH = -11,  // 获得图像数据长度和定义的尺寸不匹配
        ICAM_SDK_STATUS_TIME_OUT = -12,  // 超时错误
        ICAM_SDK_STATUS_IO_ERROR = -13,  // 硬件IO错误
        ICAM_SDK_STATUS_COMM_ERROR = -14,  // 通讯错误
        ICAM_SDK_STATUS_BUS_ERROR = -15,  // 总线错误
        ICAM_SDK_STATUS_NO_DEVICE_FOUND = -16,  // 没有发现设备
        ICAM_SDK_STATUS_NO_LOGIC_DEVICE_FOUND = -17,  // 未找到逻辑设备
        ICAM_SDK_STATUS_DEVICE_IS_OPENED = -18,  // 设备已经打开
        ICAM_SDK_STATUS_DEVICE_IS_CLOSED = -19,  // 设备已经关闭
        ICAM_SDK_STATUS_DEVICE_VEDIO_CLOSED = -20,  // 没有打开设备视频，调用录像相关的函数时，如果相机视频没有打开，则回返回该错误。
        ICAM_SDK_STATUS_NO_MEMORY = -21,  // 没有足够系统内存
        ICAM_SDK_STATUS_FILE_CREATE_FAILED = -22,  // 创建文件失败
        ICAM_SDK_STATUS_FILE_INVALID = -23,  // 文件格式无效
        ICAM_SDK_STATUS_WRITE_PROTECTED = -24,  // 写保护，不可写
        ICAM_SDK_STATUS_GRAB_FAILED = -25,  // 数据采集失败
        ICAM_SDK_STATUS_LOST_DATA = -26,  // 数据丢失，不完整
        ICAM_SDK_STATUS_EOF_ERROR = -27,  // 未接收到帧结束符
        ICAM_SDK_STATUS_BUSY = -28,  // 正忙(上一次操作还在进行中)，此次操作不能进行
        ICAM_SDK_STATUS_WAIT = -29,  // 需要等待(进行操作的条件不成立)，可以再次尝试
        ICAM_SDK_STATUS_IN_PROCESS = -30,  // 正在进行，已经被操作过
        ICAM_SDK_STATUS_IIC_ERROR = -31,  // IIC传输错误
        ICAM_SDK_STATUS_SPI_ERROR = -32,  // SPI传输错误
        ICAM_SDK_STATUS_USB_CONTROL_ERROR = -33,  // USB控制传输错误
        ICAM_SDK_STATUS_USB_BULK_ERROR = -34,  // USB BULK传输错误
        ICAM_SDK_STATUS_SOCKET_INIT_ERROR = -35,  // 网络传输套件初始化失败
        ICAM_SDK_STATUS_GIGE_FILTER_INIT_ERROR = -36,  // 网络相机内核过滤驱动初始化失败，请检查是否正确安装了驱动，或者重新安装。
        ICAM_SDK_STATUS_NET_SEND_ERROR = -37,  // 网络数据发送错误
        ICAM_SDK_STATUS_DEVICE_LOST = -38,  // 与网络相机失去连接，心跳检测超时
        ICAM_SDK_STATUS_DATA_RECV_LESS = -39,  // 接收到的字节数比请求的少
        ICAM_SDK_STATUS_FUNCTION_LOAD_FAILED = -40,  // 从文件中加载程序失败
        ICAM_SDK_STATUS_CRITICAL_FILE_LOST = -41,  // 程序运行所必须的文件丢失。
        ICAM_SDK_STATUS_SENSOR_ID_DISMATCH = -42,  // 固件和程序不匹配，原因是下载了错误的固件。
        ICAM_SDK_STATUS_OUT_OF_RANGE = -43,  // 参数超出有效范围。
        ICAM_SDK_STATUS_REGISTRY_ERROR = -44,  // 安装程序注册错误。请重新安装程序，或者运行安装目录Setup/Installer.exe
        ICAM_SDK_STATUS_ACCESS_DENY = -45,  // 禁止访问。指定相机已经被其他程序占用时，再申请访问该相机，会返回该状态。(一个相机不能被多个程序同时访问)
        ICAM_SDK_STATUS_EXECUTE_ERROR = -255, //操作错误,通常指在进行操作某个不存在的id的相机

        //AIA的标准兼容的错误码
        CAMERA_AIA_PACKET_RESEND = 0x0100, //该帧需要重传

        CAMERA_AIA_NOT_IMPLEMENTED = 0x8001, //设备不支持的命令
        CAMERA_AIA_INVALID_PARAMETER = 0x8002, //命令参数非法
        CAMERA_AIA_INVALID_ADDRESS = 0x8003, //不可访问的地址
        CAMERA_AIA_WRITE_PROTECT = 0x8004, //访问的对象不可写
        CAMERA_AIA_BAD_ALIGNMENT = 0x8005, //访问的地址没有按照要求对齐
        CAMERA_AIA_ACCESS_DENIED = 0x8006, //没有访问权限
        CAMERA_AIA_BUSY = 0x8007, //命令正在处理中
        CAMERA_AIA_DEPRECATED = 0x8008, //0x8008-0x0800B  0x800F  该指令已经废弃
        CAMERA_AIA_PACKET_UNAVAILABLE = 0x800C, //包无效
        CAMERA_AIA_DATA_OVERRUN = 0x800D, //数据溢出，通常是收到的数据比需要的多
        CAMERA_AIA_INVALID_HEADER = 0x800E, //数据包头部中某些区域与协议不匹配
        CAMERA_AIA_PACKET_NOT_YET_AVAILABLE = 0x8010, //图像分包数据还未准备好，多用于触发模式，应用程序访问超时
        CAMERA_AIA_PACKET_AND_PREV_REMOVED_FROM_MEMORY = 0x8011, //需要访问的分包已经不存在。多用于重传时数据已经不在缓冲区中
        CAMERA_AIA_PACKET_REMOVED_FROM_MEMORY = 0x8012, //CAMERA_AIA_PACKET_AND_PREV_REMOVED_FROM_MEMORY
        CAMERA_AIA_NO_REF_TIME = 0x0813, //没有参考时钟源。多用于时间同步的命令执行时
        CAMERA_AIA_PACKET_TEMPORARILY_UNAVAILABLE = 0x0814, //由于信道带宽问题，当前分包暂时不可用，需稍后进行访问
        CAMERA_AIA_OVERFLOW = 0x0815, //设备端数据溢出，通常是队列已满
        CAMERA_AIA_ACTION_LATE = 0x0816, //命令执行已经超过有效的指定时间
        CAMERA_AIA_ERROR = 0x8FFF   //错误
    }

    /// <summary>
    /// 图像类型
    /// </summary>
    public enum ImageType {

        //未知的图像数据类型
        IMAGE_Undefined = 0,

        //Mono8图像
        IMAGE_MONO = 1,

        //Jpg图像
        IMAGE_JPEG = 2,

        //BMP格式数据
        IMAGE_BMP = 3,

        //RGB24图像原始数据
        IMAGE_RGB24 = 4,
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraDefailInfoCpp {
        public int CameraIndex;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
        public byte[] CamFriendlyName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] CamIp;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public byte[] CamMac;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] CamGateWay;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] CamMask;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] EtIp;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] EtMac;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public byte[] EtGateWay;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] EtMask;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] CamSerialNumber;

        public int CamState;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ICAM_CameraInfoCpp {
        public int CameraCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public CameraDefailInfoCpp[] Cameras;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BarCodeModelCpp {

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 120)]
        public byte[] strCode;

        public int enBarType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public int[] stCornerPt;

        public int nAngle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VersionInfoCpp {

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
        public byte[] Version;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
        public byte[] AlgorithmVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageModelCpp {

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public byte[] CameraName;

        public int Width;
        public int Height;
        public int DataLen;
        public uint FrameSequence;
        public IntPtr ImageData;
        public ImageType Type;
        public int BarcodeCount;
        public int CameraIndex;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public BarCodeModelCpp[] CodeModels;

        public double ProcessTime;
        public int RunType;
        public ulong ReceiveTimePoint;
    }

    public delegate void CameraStateCallbackDelegate(IntPtr name, int len, bool state, IntPtr userdata);

    /******************************************************/
    // 函数名   : ICAM_ResolveLibVersion
    // 功能描述 : 获取版本号
    // 参数     : versioninfo  版本信息结构体
    // 返回值   : 是否获取成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_ResolveLibVersion(ref VersionInfoCpp versioninfo);

    /******************************************************/
    // 函数名   : ICAM_EnumerateDevices
    // 功能描述 : 枚举所有相机
    // 参数     : camsinfo  搜索到的所有相机的信息集合
    // 返回值   : 是否搜索成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_EnumerateDevices(ref ICAM_CameraInfoCpp camsinfo);

    /******************************************************/
    // 函数名   : ICAM_RegisterCameraStateCallback
    // 功能描述 : 注册相机状态回调函数
    // 参数     : statecallback  相机状态回调函数
    //            userData       用户自定义数据
    // 返回值   : 是否注册成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_RegisterCameraStateCallback(CameraStateCallbackDelegate statecallback, IntPtr userData);

    /******************************************************/
    // 函数名   : ICAM_StartCamera
    // 功能描述 : 通过id来打开对应的相机
    // 参数     : cameraIndex  相机id
    // 返回值   : 是否打开成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_StartCamera(int cameraIndex);

    /******************************************************/
    // 函数名   : ICAM_StopCamera
    // 功能描述 : 通过id来关闭对应的相机
    // 参数     : cameraIndex  相机id
    // 返回值   : 是否关闭成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_StopCamera(int cameraIndex);

    /******************************************************/
    // 函数名   : ICAM_GetExposureRange
    // 功能描述 : 获取相机的曝光范围值
    // 参数     : cameraIndex  相机id
    //            min 最小值
    //            max 最大值
    //            step 步长
    // 返回值   : 是否获取成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_GetExposureRange(int cameraIndex, ref int min, ref int max, ref int step);

    /******************************************************/
    // 函数名   : ICAM_GetExposure
    // 功能描述 : 获取相机的曝光值
    // 参数     : cameraIndex  相机id
    //            exposure 曝光值
    // 返回值   : 是否获取成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_GetExposure(int cameraIndex, ref int exposure);

    /******************************************************/
    // 函数名   : ICAM_SetExposure
    // 功能描述 : 设置相机的曝光值
    // 参数     : cameraIndex  相机id
    //            exposure 曝光值
    // 返回值   : 是否设置成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_SetExposure(int cameraIndex, int exposure);

    /******************************************************/
    // 函数名   : ICAM_SetExposureMode
    // 功能描述 : 设置相机曝光模式
    // 参数     : cameraIndex  相机id
    //            mode 曝光模式 0:自动曝光 1:手动曝光
    // 返回值   : 是否设置成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_SetExposureMode(int cameraIndex, int mode);

    /******************************************************/
    // 函数名   : ICAM_GetExposureMode
    // 功能描述 : 获取相机曝光模式
    // 参数     : cameraIndex  相机id
    //            mode 曝光模式 0:自动曝光 1:手动曝光
    // 返回值   : 是否获取成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_GetExposureMode(int cameraIndex, ref int mode);

    /******************************************************/
    // 函数名   : ICAM_GetAnaloggainRange
    // 功能描述 : 获取相机模拟增益范围
    // 参数     : cameraIndex  相机id
    //            min 最小值
    //            max 最大值
    //            step 步长
    // 返回值   : 是否获取成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_GetAnaloggainRange(int cameraIndex, ref float min, ref float max, ref float step);

    /******************************************************/
    // 函数名   : ICAM_GetAnaloggain
    // 功能描述 : 获取相机模拟增益值
    // 参数     : cameraIndex  相机id
    //            gain 模拟增益值
    // 返回值   : 是否获取成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_GetAnaloggain(int cameraIndex, ref int gain);

    /******************************************************/
    // 函数名   : ICAM_SetAnaloggain
    // 功能描述 : 设置相机模拟增益值
    // 参数     : cameraIndex  相机id
    //            gain 模拟增益值
    // 返回值   : 是否设置成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_SetAnaloggain(int cameraIndex, int gain);

    /******************************************************/
    // 函数名   : ICAM_SaveCameraParameters
    // 功能描述 : 保存相机参数 曝光增益等参数设置后需要保存参数后断电后才会生效
    // 参数     : cameraIndex  相机id
    // 返回值   : 是否保存成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_SaveCameraParameters(int cameraIndex);

    /******************************************************/
    // 函数名   : ICAM_ImportCameraConfigFile
    // 功能描述 : 导入相机参数文件
    // 参数     : cameraIndex  相机id
    //            filepath 相机参数文件全路径
    // 返回值   : 是否导入成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_ImportCameraConfigFile(int cameraIndex, string filepath);

    /******************************************************/
    // 函数名   : ICAM_ExportCameraConfigFile
    // 功能描述 : 导出相机参数文件
    // 参数     : cameraIndex  相机id
    //            filepath 相机参数文件全路径
    // 返回值   : 是否导出成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_ExportCameraConfigFile(int cameraIndex, string filepath);

    /******************************************************/
    // 函数名   : ICAM_ModifyCameraName
    // 功能描述 : 修改相机名称 需要断电重启后生效
    // 参数     : cameraIndex  相机id
    //            cameraname 修改后的相机名称 不能超过32个字节
    // 返回值   : 是否设置成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_ModifyCameraName(int cameraIndex, string cameraname);

    /******************************************************/
    // 函数名   : ICAM_GetImageMirror
    // 功能描述 : 获取图像镜像信息
    // 参数     : cameraIndex  相机id
    //            hmirror 是否水平镜像 0:未水平镜像 1:水平镜像
    //            vmirror 是否垂直镜像 0:未垂直镜像 1:垂直镜像
    // 返回值   : 是否获取成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_GetImageMirror(int cameraIndex, ref int hmirror, ref int vmirror);

    /******************************************************/
    // 函数名   : ICAM_SetImageMirror
    // 功能描述 : 设置图像镜像信息
    // 参数     : cameraIndex  相机id
    //            hmirror 是否水平镜像 0:未水平镜像 1:水平镜像
    //            vmirror 是否垂直镜像 0:未垂直镜像 1:垂直镜像
    // 返回值   : 是否设置成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_SetImageMirror(int cameraIndex, int hmirror, int vmirror);

    /******************************************************/
    // 函数名   : ICAM_FetchFrame
    // 功能描述 : 获取一帧图像数据
    // 参数     : cameraIndex  相机id
    //            image 图像信息
    // 返回值   : 是否获取成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_FetchFrame(int cameraIndex, ref ImageModelCpp image, int timeout = 300);

    /******************************************************/
    // 函数名   : ICAM_ReleaseFrame
    // 功能描述 : 释放一帧图像
    // 参数     : cameraIndex  相机id
    //            image 图像信息
    // 返回值   : 是否获取成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_ReleaseFrame(ref ImageModelCpp image);

    /******************************************************/
    // 函数名   : ICAM_SetCamBeScanner  默认是不开启读码
    // 功能描述 : 获取一帧图像数据
    // 参数     : cameraIndex  相机id
    //            isscanner 是否需要扫码 0:不需要扫码 1:需要扫码
    // 返回值   : 是否获取成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_SetCamBeScanner(int cameraIndex, int isscanner);

    /******************************************************/
    // 函数名   : ICAM_SetTriggerMode
    // 功能描述 : 设置相机触发模式
    // 参数     : cameraIndex  相机id
    //            model 触发模式 0:连续模式 1:软件触发 2:硬件触发
    // 返回值   : 是否获取成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_SetTriggerMode(int cameraIndex, int mode);

    /******************************************************/
    // 函数名   : ICAM_ExecuteSoftTrigger
    // 功能描述 : 执行一次软件触发
    // 参数     : cameraIndex  相机id
    // 返回值   : 是否触发成功,成功返回 0
    /******************************************************/

    public delegate int pinvoke_ICAM_ExecuteSoftTrigger(int cameraIndex);

    public static class ICAMAPI {

        public static string ICAM_BytesToString(byte[] array) => Encoding.ASCII.GetString(array).TrimEnd('\0');

        public static pinvoke_ICAM_FetchFrame ICAM_FetchFrame;
        public static pinvoke_ICAM_ReleaseFrame ICAM_ReleaseFrame;
        public static pinvoke_ICAM_SetCamBeScanner ICAM_SetCamBeScanner;
        public static pinvoke_ICAM_SetTriggerMode ICAM_SetTriggerMode;
        public static pinvoke_ICAM_ExecuteSoftTrigger ICAM_ExecuteSoftTrigger;
        public static pinvoke_ICAM_ResolveLibVersion ICAM_ResolveLibVersion;
        public static pinvoke_ICAM_EnumerateDevices ICAM_EnumerateDevices;
        public static pinvoke_ICAM_RegisterCameraStateCallback ICAM_RegisterCameraStateCallback;
        public static pinvoke_ICAM_StartCamera ICAM_StartCamera;
        public static pinvoke_ICAM_StopCamera ICAM_StopCamera;
        public static pinvoke_ICAM_GetExposureRange ICAM_GetExposureRange;
        public static pinvoke_ICAM_GetExposure ICAM_GetExposure;
        public static pinvoke_ICAM_SetExposure ICAM_SetExposure;
        public static pinvoke_ICAM_SetExposureMode ICAM_SetExposureMode;
        public static pinvoke_ICAM_GetExposureMode ICAM_GetExposureMode;
        public static pinvoke_ICAM_GetAnaloggainRange ICAM_GetAnaloggainRange;
        public static pinvoke_ICAM_GetAnaloggain ICAM_GetAnaloggain;
        public static pinvoke_ICAM_SetAnaloggain ICAM_SetAnaloggain;
        public static pinvoke_ICAM_SaveCameraParameters ICAM_SaveCameraParameters;
        public static pinvoke_ICAM_ImportCameraConfigFile ICAM_ImportCameraConfigFile;
        public static pinvoke_ICAM_ExportCameraConfigFile ICAM_ExportCameraConfigFile;
        public static pinvoke_ICAM_ModifyCameraName ICAM_ModifyCameraName;
        public static pinvoke_ICAM_GetImageMirror ICAM_GetImageMirror;
        public static pinvoke_ICAM_SetImageMirror ICAM_SetImageMirror;

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32", EntryPoint = "FreeLibrary", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        private static Delegate GetFunctionAddress(IntPtr dllModule, string functionName, Type t) {
            IntPtr address = GetProcAddress(dllModule, functionName);
            if (address == IntPtr.Zero)
                return null;
            else
                return Marshal.GetDelegateForFunctionPointer(address, t);
        }

        static ICAMAPI() {
            IntPtr camSdk = LoadLibrary("ICAM_SDK_CPP.dll");
            ICAM_ResolveLibVersion = (pinvoke_ICAM_ResolveLibVersion)GetFunctionAddress(camSdk, "ICAM_ResolveLibVersion", typeof(pinvoke_ICAM_ResolveLibVersion));
            ICAM_EnumerateDevices = (pinvoke_ICAM_EnumerateDevices)GetFunctionAddress(camSdk, "ICAM_EnumerateDevices", typeof(pinvoke_ICAM_EnumerateDevices));
            ICAM_ExecuteSoftTrigger = (pinvoke_ICAM_ExecuteSoftTrigger)GetFunctionAddress(camSdk, "ICAM_ExecuteSoftTrigger", typeof(pinvoke_ICAM_ExecuteSoftTrigger));
            ICAM_SetTriggerMode = (pinvoke_ICAM_SetTriggerMode)GetFunctionAddress(camSdk, "ICAM_SetTriggerMode", typeof(pinvoke_ICAM_SetTriggerMode));
            ICAM_SetCamBeScanner = (pinvoke_ICAM_SetCamBeScanner)GetFunctionAddress(camSdk, "ICAM_SetCamBeScanner", typeof(pinvoke_ICAM_SetCamBeScanner));
            ICAM_FetchFrame = (pinvoke_ICAM_FetchFrame)GetFunctionAddress(camSdk, "ICAM_FetchFrame", typeof(pinvoke_ICAM_FetchFrame));
            ICAM_SetImageMirror = (pinvoke_ICAM_SetImageMirror)GetFunctionAddress(camSdk, "ICAM_SetImageMirror", typeof(pinvoke_ICAM_SetImageMirror));
            ICAM_GetImageMirror = (pinvoke_ICAM_GetImageMirror)GetFunctionAddress(camSdk, "ICAM_GetImageMirror", typeof(pinvoke_ICAM_GetImageMirror));
            ICAM_ModifyCameraName = (pinvoke_ICAM_ModifyCameraName)GetFunctionAddress(camSdk, "ICAM_ModifyCameraName", typeof(pinvoke_ICAM_ModifyCameraName));
            ICAM_ExportCameraConfigFile = (pinvoke_ICAM_ExportCameraConfigFile)GetFunctionAddress(camSdk, "ICAM_ExportCameraConfigFile", typeof(pinvoke_ICAM_ExportCameraConfigFile));
            ICAM_ImportCameraConfigFile = (pinvoke_ICAM_ImportCameraConfigFile)GetFunctionAddress(camSdk, "ICAM_ImportCameraConfigFile", typeof(pinvoke_ICAM_ImportCameraConfigFile));
            ICAM_SaveCameraParameters = (pinvoke_ICAM_SaveCameraParameters)GetFunctionAddress(camSdk, "ICAM_SaveCameraParameters", typeof(pinvoke_ICAM_SaveCameraParameters));
            ICAM_SetAnaloggain = (pinvoke_ICAM_SetAnaloggain)GetFunctionAddress(camSdk, "ICAM_SetAnaloggain", typeof(pinvoke_ICAM_SetAnaloggain));
            ICAM_GetAnaloggain = (pinvoke_ICAM_GetAnaloggain)GetFunctionAddress(camSdk, "ICAM_GetAnaloggain", typeof(pinvoke_ICAM_GetAnaloggain));
            ICAM_GetAnaloggainRange = (pinvoke_ICAM_GetAnaloggainRange)GetFunctionAddress(camSdk, "ICAM_GetAnaloggainRange", typeof(pinvoke_ICAM_GetAnaloggainRange));
            ICAM_GetExposureMode = (pinvoke_ICAM_GetExposureMode)GetFunctionAddress(camSdk, "ICAM_GetExposureMode", typeof(pinvoke_ICAM_GetExposureMode));
            ICAM_SetExposureMode = (pinvoke_ICAM_SetExposureMode)GetFunctionAddress(camSdk, "ICAM_SetExposureMode", typeof(pinvoke_ICAM_SetExposureMode));
            ICAM_SetExposure = (pinvoke_ICAM_SetExposure)GetFunctionAddress(camSdk, "ICAM_SetExposure", typeof(pinvoke_ICAM_SetExposure));
            ICAM_GetExposure = (pinvoke_ICAM_GetExposure)GetFunctionAddress(camSdk, "ICAM_GetExposure", typeof(pinvoke_ICAM_GetExposure));
            ICAM_GetExposureRange = (pinvoke_ICAM_GetExposureRange)GetFunctionAddress(camSdk, "ICAM_GetExposureRange", typeof(pinvoke_ICAM_GetExposureRange));
            ICAM_StartCamera = (pinvoke_ICAM_StartCamera)GetFunctionAddress(camSdk, "ICAM_StartCamera", typeof(pinvoke_ICAM_StartCamera));
            ICAM_StopCamera = (pinvoke_ICAM_StopCamera)GetFunctionAddress(camSdk, "ICAM_StopCamera", typeof(pinvoke_ICAM_StopCamera));
            ICAM_RegisterCameraStateCallback = (pinvoke_ICAM_RegisterCameraStateCallback)GetFunctionAddress(camSdk, "ICAM_RegisterCameraStateCallback", typeof(pinvoke_ICAM_RegisterCameraStateCallback));
            ICAM_ReleaseFrame = (pinvoke_ICAM_ReleaseFrame)GetFunctionAddress(camSdk, "ICAM_ReleaseFrame", typeof(pinvoke_ICAM_ReleaseFrame));
        }
    }
}