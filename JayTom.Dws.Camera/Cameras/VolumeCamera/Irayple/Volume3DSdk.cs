using System.Runtime.InteropServices;

namespace JayTom.Dws.Camera.Cameras.VolumeCamera.Irayple {

    /// <summary>
    /// 3D动态测体积
    /// </summary>
    public static class Volume3DSdk {
        private const string DllPath = ".\\Cameras\\VolumeCamera\\Irayple\\Dll\\Volume3DSdkmd.dll";

        /// <summary>
        /// 体积测量运行结果回调函数定义
        /// </summary>
        /// <param name="param0">参数1, 长（单位mm）</param>
        /// <param name="param1">参数2, 宽（单位mm）</param>
        /// <param name="param2">参数1, 高（单位mm）</param>
        /// <param name="param3">参数2, 体积（单位mm3）</param>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void VslbVolumeResultCB(double param0, double param1, double param2, double param3);

        /// <summary>
        /// 三维坐标点
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct S3DPointf {

            /// <summary>
            /// x坐标
            /// </summary>
            public float x;

            /// <summary>
            /// y坐标
            /// </summary>
            public float y;

            /// <summary>
            /// z坐标
            /// </summary>
            public float z;
        }

        /// <summary>
        /// 测量结果
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SVolumeResult {

            /// <summary>
            /// 测量状态, 0-物体进入, 1-物体离开, 2-定时输出
            /// </summary>
            public int state;

            /// <summary>
            /// 本次测量检测到的物体总数
            /// </summary>
            public UInt16 packCount;

            /// <summary>
            /// 本次测量检测到的物体序号
            /// </summary>
            public UInt16 packIndex;

            /// <summary>
            /// 长度, 单位mm
            /// </summary>
            public double length;

            /// <summary>
            /// 宽度, 单位mm
            /// </summary>
            public double width;

            /// <summary>
            /// 宽度, 单位mm
            /// </summary>
            public double height;

            /// <summary>
            /// 体积, 单位mm3
            /// </summary>
            public double volume;

            /// <summary>
            /// 顶面4个点
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public S3DPointf[] vertices;

            /// <summary>
            /// 物体进入时间(单位ms), 以相机时间戳为基准
            /// </summary>
            public UInt64 objEnterTime;

            /// <summary>
            /// 物体离开时间(单位ms), 以相机时间戳为基准
            /// </summary>
            public UInt64 objLeaveTime;

            /// <summary>
            /// 点云图片路径
            /// </summary>
            public UInt64 pointCloudImagePath; // const char* in cpp. string s = Marshal.PtrToStringAnsi(pointCloudImagePath)

            /// <summary>
            /// 点云数据指针
            /// </summary>
            public UInt64 pointCloud;

            /// <summary>
            /// 点云中点的数量
            /// </summary>
            public int pointCloudNum;

            /// <summary>
            /// 倾斜角度, 物体长边与运动方向的夹角
            /// </summary>
            public float slantAngle;

            /// <summary>
            /// 顶面中心点
            /// </summary>
            public S3DPointf topCenter;

            /// <summary>
            /// 对齐填充, 无意义
            /// </summary>
            public int _padding4;

            /// <summary>
            /// 物体进入时间, UTC系统时间(单位ms)
            /// </summary>
            public UInt64 objEnterSysTime;

            /// <summary>
            /// 物体离开时间, UTC系统时间(单位ms)
            /// </summary>
            public UInt64 objLeaveSysTime;

            /// <summary>
            /// 完成计算的时间, UTC系统时间(单位ms)
            /// </summary>
            public UInt64 retOutputSysTime;

            /// <summary>
            /// xyz坐标轴方向
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public S3DPointf[] coordinateAxes;

            /// <summary>
            /// 保留
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 348)]
            public byte[] reserved;
        };

        /// <summary>
        /// 体积测量运行结果回调函数定义
        /// </summary>
        /// <param name="resultPtr">测量结果, 对应结构体 SVolumeResult</param>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void VslbVolumeResultCBEx(IntPtr resultPtr);

        /// <summary>
        /// 创建体积测量句柄
        /// </summary>
        /// <returns>Volume3D句柄</returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr vslbVolume3DCreate();

        /// <summary>
        /// 初始化，主要进行标定参数读取
        /// </summary>
        /// <param name="handle">Volume3D句柄</param>
        /// <param name="cfgDir">已废弃, 可以传null. 在老版本中指定标定文件所在目录</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int vslbVolume3DInit(IntPtr handle, [In()][MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] string cfgDir);

        /// <summary>
        /// 反初始化
        /// </summary>
        /// <param name="handle">Volume3D句柄</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int vslbVolume3DFini(IntPtr handle);

        /// <summary>
        /// 启动测量
        /// </summary>
        /// <param name="handle">Volume3D句柄</param>
        /// <param name="procRunCB">结果回调函数</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int vslbVolume3DRun(IntPtr handle, VslbVolumeResultCB procRunCB);

        /// <summary>
        /// 启动测量
        /// </summary>
        /// <param name="handle">Volume3D句柄</param>
        /// <param name="procRunCB">结果回调函数</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int vslbVolume3DRunEx(IntPtr handle, VslbVolumeResultCBEx procRunCB);

        /// <summary>
        /// 停止测量
        /// </summary>
        /// <param name="handle">Volume3D句柄</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int vslbVolume3DStop(IntPtr handle);

        /// <summary>
        /// 销毁体积测量句柄
        /// </summary>
        /// <param name="handle">Volume3D句柄</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int vslbVolume3DDestroy(IntPtr handle);
    }
}