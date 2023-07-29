using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.CompilerServices;
using JayTom.Dws.Device.Camera._3DCamera.Percipio;
using static JayTom.Dws.Device.Camera._3DCamera.Percipio.PercipioAppCenter;

namespace JayTom.Dws.Device.Camera._3DCamera {

    public class Percipio3DCamera : I3DCamera {
        private IntPtr _ptr = IntPtr.Zero;
        private PercipioCommonTypes.AllData _gdata;
        private static object _pmLock = new();
        private static readonly byte PersonStandingPositionMask = 0x00;
        public string DeviceCode { get; private set; } = string.Empty;
        public DeviceStatus Status { get; private set; } = DeviceStatus.Uninitialized;
        public DeviceType Type => DeviceType.Camera;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string path);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        public Task<KeyValuePair<bool, string>> Reconnect() {
            throw new NotImplementedException();
        }

        public async Task<KeyValuePair<bool, string>> Connect<T>(T connectParam) {
            //暂时不设置参数
            await Task.Yield();
            _gdata = new PercipioCommonTypes.AllData {
                newData = false,
                newDepth = false,
                newColor = false
            };

            _ptr = Marshal.AllocHGlobal(Marshal.SizeOf(_gdata));
            Marshal.StructureToPtr(_gdata, _ptr, false);

            PercipioAppInterfacesBase.AppDataFunc = delegate (IntPtr head, IntPtr data, IntPtr userData) {
                var ret = Marshal.PtrToStructure(head, typeof(PercipioCommonTypes.BlockHeader));
                switch (((PercipioCommonTypes.BlockHeader)ret!).dataType) {
                    case (short)PercipioCommonTypes.I_DATA_TYPE_LIST.I_DATA_SETUPPER_IMAGE: CsharpCallLocalSetupperCallback(head, 0, userData); break;//3
                    case (short)PercipioCommonTypes.I_DATA_TYPE_LIST.I_DATA_DEPTH_IMAGE: CsharpCallLocalDepthCallback(head, data, userData); break;//1
                    case (short)PercipioCommonTypes.I_DATA_TYPE_LIST.I_DATA_COLOR_IMAGE: CsharpCallLocalColorCallback(head, data, userData); break;//2
                    case (short)PercipioCommonTypes.I_DATA_TYPE_LIST.I_DATA_PACKAGE_MEASURE: CsharpCallLocalPmCallback(head, data, userData); break;//4
                    case (short)PercipioCommonTypes.I_DATA_TYPE_LIST.I_DATA_P3D: CsharpCallLocalP3DCallback(head, data, userData); break;//19
                    default: break;
                }
            };
            TYAppSetDataCallback(PercipioAppInterfacesBase.AppDataFunc, _ptr);

            PercipioAppInterfacesBase.AppEventFunc = delegate (IntPtr head, IntPtr data, IntPtr userData) {
                try {
                    var ret = Marshal.PtrToStructure(head, typeof(PercipioCommonTypes.XData));
                    var xdata = (PercipioCommonTypes.XData)ret!;
                    if (0 != xdata.error_id) {
                        var msgString = Marshal.PtrToStringAnsi(data);
                        OnExceptionLogged(new Exception($"####MSG:{xdata.error_id} {msgString}"));
                    }
                    else {
                        OnItemNotDetected(EventArgs.Empty);
                    }
                }
                catch (Exception e) {
                    OnExceptionLogged(e);
                }
            };
            TYAppSetEventCallback(PercipioAppInterfacesBase.AppEventFunc, _ptr);

            _gdata.running = true;

            int filled;
            int status;

            /*String depthCameraType = ConfigurationManager.AppSettings["DepthCameraType"].Trim();
            String app_name = depthCameraType.ToUpper().Equals("TOF") ? "PackageMeasureTof" : "PackageMeasure";*/
            const string appName = "PackageMeasure";
            var initappname = Marshal.StringToHGlobalAnsi(appName);

            status = TYAppWriteProperty((int)PercipioCommonTypes.I_PROPERTY_LIST.I_PROPERTY_string_APP_NAME, initappname, appName.Length + 1);
            if (0 != status) {
                OnExceptionLogged(new Exception($"TYAppWriteProperty I_PROPERTY_string_APP_NAME error : {status}"));
                return new KeyValuePair<bool, string>(false, $"TYAppWriteProperty I_PROPERTY_string_APP_NAME error : {status}");
            }

            Marshal.FreeHGlobal(initappname);

            status = TYAppWriteCmd((int)PercipioCommonTypes.I_APPCENTER_CMD_LIST.I_CMD_APP_INIT);
            if (0 != status) {
                OnExceptionLogged(new Exception($"TYAppWriteCmd I_CMD_APP_INIT error : {status}"));
                return new KeyValuePair<bool, string>(false, $"TYAppWriteCmd I_CMD_APP_INIT error : {status}");
            }

            var b = true;
            unsafe {
                var p = &b;

                var op = Environment.Is64BitProcess ? new IntPtr((long)p) : new IntPtr((int)p);

                status = TYAppWriteProperty((int)PercipioCommonTypes.I_PROPERTY_LIST.I_PROPERTY_bool_GRAB_DEPTH, op, Marshal.SizeOf(b));
                if (0 != status) {
                    OnExceptionLogged(new Exception($"TYAppWriteProperty I_PROPERTY_bool_GRAB_DEPTH error : {status}"));

                    return new KeyValuePair<bool, string>(false, $"TYAppWriteProperty I_PROPERTY_bool_GRAB_DEPTH error : {status}");
                }
            }
            unsafe {
                var p = &b;

                var op = Environment.Is64BitProcess ? new IntPtr((long)p) : new IntPtr((int)p);

                status = TYAppWriteProperty((int)PercipioCommonTypes.I_PROPERTY_LIST.I_PROPERTY_bool_GRAB_COLOR, op, Marshal.SizeOf(b));
                if (0 != status) {
                    OnExceptionLogged(new Exception($"TYAppWriteProperty I_PROPERTY_bool_GRAB_COLOR error : {status}"));
                    return new KeyValuePair<bool, string>(false, $"TYAppWriteProperty I_PROPERTY_bool_GRAB_COLOR error : {status}");
                }
            }

            //set depth image output format
            var n = (int)PercipioCommonTypes.I_IMG_FORMAT_LIST.I_IMG_FORMAT_JPG;
            unsafe {
                var p = &n;

                var op = Environment.Is64BitProcess ? new IntPtr((long)p) : new IntPtr((int)p);

                status = TYAppWriteProperty((int)PercipioCommonTypes.I_PROPERTY_LIST.I_PROPERTY_int_COLOR_FORMAT, op, Marshal.SizeOf(n));
                if (0 != status) {
                    OnExceptionLogged(new Exception($"TYAppWriteProperty error : {status}"));
                    return new KeyValuePair<bool, string>(false, $"TYAppWriteProperty error : {status}");
                }
            }
            unsafe {
                var p = &n;

                var op = Environment.Is64BitProcess ? new IntPtr((long)p) : new IntPtr((int)p);

                status = TYAppWriteProperty((int)PercipioCommonTypes.I_PROPERTY_LIST.I_PROPERTY_int_DEPTH_FORMAT, op, Marshal.SizeOf(n));
                if (0 != status) {
                    OnExceptionLogged(new Exception($"TYAppWriteProperty error : {status}"));
                    return new KeyValuePair<bool, string>(false, $"TYAppWriteProperty error : {status}");
                }
            }

            //get
            var getBgRect = new PercipioCommonTypes.CvRect();
            unsafe {
                var p = &getBgRect;
                filled = 0;
                var ppp = &filled;
                IntPtr op;
                IntPtr pFilled;

                if (Environment.Is64BitProcess) {
                    op = new IntPtr((long)p);
                    pFilled = new IntPtr((long)ppp);
                }
                else {
                    op = new IntPtr((int)p);
                    pFilled = new IntPtr((int)ppp);
                }

                status = TYAppReadProperty((int)PercipioCommonTypes.I_PROPERTY_LIST.I_PROPERTY_int4_DEPTH_ROI, op, Marshal.SizeOf(getBgRect), pFilled);
                if (0 != status) {
                    OnExceptionLogged(new Exception($"TYAppReadProperty error : {status}"));
                    return new KeyValuePair<bool, string>(false, $"TYAppWriteProperty error : {status}");
                }
            }

            //get
            var getSafeRect = new PercipioCommonTypes.CvRect();
            unsafe {
                var p = &getSafeRect;
                filled = 0;
                var ppp = &filled;
                IntPtr op;
                IntPtr pFilled;

                if (Environment.Is64BitProcess) {
                    op = new IntPtr((Int64)p);
                    pFilled = new IntPtr((Int64)ppp);
                }
                else {
                    op = new IntPtr((Int32)p);
                    pFilled = new IntPtr((Int32)ppp);
                }
                status = TYAppReadProperty((Int32)PercipioCommonTypes.I_PROPERTY_LIST.I_PROPERTY_int4_SAFE_RECT, op, Marshal.SizeOf(getSafeRect), pFilled);
                if (0 != status) {
                    OnExceptionLogged(new Exception($"TYAppReadProperty error : {status}"));
                    return new KeyValuePair<bool, string>(false, $"TYAppWriteProperty error : {status}");
                }
            }

            status = TYAppStart();
            if (0 != status) {
                OnExceptionLogged(new Exception($"TYAppStart error : {status}"));
            }
            else {
                Status = DeviceStatus.Connected;
                OnConnected(this);
                return new KeyValuePair<bool, string>(true, "相机连接成功");
            }

            return new KeyValuePair<bool, string>(true, "相机连接失败!");
        }

        public void Dispose() {
            var status = TYAppStop();
            if (0 != status) {
                OnExceptionLogged(new Exception($"TYAppStop error : {status}"));
                return;
            }

            Marshal.FreeHGlobal(_ptr); //free the memory

            _gdata.running = false;
        }

        public async Task<KeyValuePair<bool, string>> Initialization() {
            await Task.Yield();
            var argv = new IntPtr[1];
            argv[0] = PercipioAppUtils.StringToByteArray(".");
            var status = TYAppInit(1, argv);

            if (status == 0) {
                OnInitialized(this);
                return new KeyValuePair<bool, string>(true, "初始化完成");
            }
            else {
                OnExceptionLogged(new Exception($"初始化错误:{status}"));
                return new KeyValuePair<bool, string>(false, $"错误状态码:{status}");
            }
        }

        private void CsharpCallLocalSetupperCallback(IntPtr result, int blockSize, IntPtr userData) {
            var ret = Marshal.PtrToStructure(result, typeof(PercipioCommonTypes.ImageHeader));
            var img = (PercipioCommonTypes.ImageHeader)ret!;

            if (img.blk.dataType != (short)PercipioCommonTypes.I_DATA_TYPE_LIST.I_DATA_SETUPPER_IMAGE) return;
            if ((img.format != (char)PercipioCommonTypes.I_IMG_FORMAT_LIST.I_IMG_FORMAT_RAW) &&
                (img.format != (char)PercipioCommonTypes.I_IMG_FORMAT_LIST.I_IMG_FORMAT_JPG)) return;
            _gdata.depth_width = img.width;
            _gdata.depth_height = img.height;
            if (_gdata.depth_data == null)
                _gdata.depth_data = new byte[img.size];
            else if (_gdata.depth_data.Length != img.size)
                Array.Resize(ref _gdata.depth_data, img.size);

            result = IntPtr.Add(result, Marshal.SizeOf(img));
            Marshal.Copy(result, _gdata.depth_data, 0, img.size);
            var byteToImage = PercipioAppUtils.ByteToImage(_gdata.depth_data);
            if (byteToImage is not null) {
                if (IsShowDetectionBorder && _gdata.bounding?.Length >= 4) {
                    //画边框
                    using (var graphics = Graphics.FromImage(byteToImage)) {
                        // 绘制矩形
                        using (var pen = new Pen(DetectionBorderColor, DetectionBorderSize) {
                            DashStyle = DashStyle.Solid,
                            DashPattern = new float[] { 1f, 2f }
                        }) {
                            var tmpPixelPointsRgb = new System.Drawing.PointF[4];
                            for (var i = 0; i < 4; i++) {
                                tmpPixelPointsRgb[i].X = _gdata.bounding[i].x;
                                tmpPixelPointsRgb[i].Y = _gdata.bounding[i].y;
                            }
                            var myPath = new GraphicsPath();
                            myPath.AddPolygon(tmpPixelPointsRgb);
                            graphics.DrawPath(pen, myPath);
                        }
                    }
                    OnLiveMappingEvent((Bitmap)byteToImage);
                }
                else {
                    OnLiveMappingEvent((Bitmap)byteToImage);
                }
            }
        }

        private void CsharpCallLocalDepthCallback(IntPtr head, IntPtr body, IntPtr userData) {
            var ret = Marshal.PtrToStructure(head, typeof(PercipioCommonTypes.ImageHeader));
            var img = (PercipioCommonTypes.ImageHeader)ret!;
            if (img.blk.dataType != (short)PercipioCommonTypes.I_DATA_TYPE_LIST.I_DATA_DEPTH_IMAGE) return;
            if (img.format != (char)PercipioCommonTypes.I_IMG_FORMAT_LIST.I_IMG_FORMAT_RAW &&
                img.format != (char)PercipioCommonTypes.I_IMG_FORMAT_LIST.I_IMG_FORMAT_JPG) return;
            //ret = Marshal.PtrToStructure(user_data, typeof(AllData));
            //var data = (AllData)ret;

            _gdata.depth_width = img.width;
            _gdata.depth_height = img.height;
            if (_gdata.depth_data == null)
                _gdata.depth_data = new byte[img.size];
            else if (_gdata.depth_data.Length != img.size)
                Array.Resize(ref _gdata.depth_data, img.size);

            //result = IntPtr.Add(result, Marshal.SizeOf(img));
            Marshal.Copy(body, _gdata.depth_data, 0, img.size);
            var byteToImage = PercipioAppUtils.ByteToImage(_gdata.depth_data);

            if (byteToImage is not null) {
                if (IsShowDetectionBorder && _gdata.bounding?.Length >= 4) {
                    //画边框
                    using (var graphics = Graphics.FromImage(byteToImage)) {
                        // 绘制矩形
                        using (var pen = new Pen(DetectionBorderColor, DetectionBorderSize) {
                            DashStyle = DashStyle.Solid,
                            DashPattern = new float[] { 1f, 2f }
                        }) {
                            var tmpPixelPointsRgb = new System.Drawing.PointF[4];
                            for (var i = 0; i < 4; i++) {
                                tmpPixelPointsRgb[i].X = _gdata.bounding[i].x;
                                tmpPixelPointsRgb[i].Y = _gdata.bounding[i].y;
                            }
                            var myPath = new GraphicsPath();
                            myPath.AddPolygon(tmpPixelPointsRgb);
                            graphics.DrawPath(pen, myPath);
                        }
                    }
                    OnLiveMappingEvent((Bitmap)byteToImage);
                }
                else {
                    OnLiveMappingEvent((Bitmap)byteToImage);
                }
            }
        }

        private void CsharpCallLocalColorCallback(IntPtr head, IntPtr body, IntPtr userData) {
            var ret = Marshal.PtrToStructure(head, typeof(PercipioCommonTypes.ImageHeader));
            var img = (PercipioCommonTypes.ImageHeader)ret!;
            if (img.blk.dataType != (short)PercipioCommonTypes.I_DATA_TYPE_LIST.I_DATA_COLOR_IMAGE) return;
            if (img.format != (char)PercipioCommonTypes.I_IMG_FORMAT_LIST.I_IMG_FORMAT_JPG) return;
            ret = Marshal.PtrToStructure(userData, typeof(PercipioCommonTypes.AllData));
            //var data = (AllData)ret;

            _gdata.color_width = img.width;
            _gdata.color_height = img.height;
            if (_gdata.color_data == null)
                _gdata.color_data = new byte[img.size];
            else if (_gdata.color_data.Length != img.size)
                Array.Resize(ref _gdata.color_data, img.size);

            var size = Marshal.SizeOf(img);
            //result = IntPtr.Add(result, Marshal.SizeOf(img));
            Marshal.Copy(body, _gdata.color_data, 0, img.size);
            var byteToImage = PercipioAppUtils.ByteToImage(_gdata.color_data);
            if (byteToImage is not null) {
                if (IsShowDetectionBorder && _gdata.boundingRGB?.Length >= 4) {
                    //画边框
                    using (var graphics = Graphics.FromImage(byteToImage)) {
                        using (var pen = new Pen(DetectionBorderColor, DetectionBorderSize)) {
                            var pointFs = new System.Drawing.PointF[4];
                            for (var i = 0; i < 4; i++) {
                                pointFs[i].X = _gdata.boundingRGB[i].x;
                                pointFs[i].Y = _gdata.boundingRGB[i].y;
                            }

                            var graphicsPath = new GraphicsPath();
                            graphicsPath.AddPolygon(pointFs);
                            graphics.DrawPath(pen, graphicsPath);
                        }
                    }

                    OnRealtimeImageEvent((Bitmap)byteToImage);
                }
                else {
                    OnRealtimeImageEvent((Bitmap)byteToImage);
                }
            }
        }

        private void CsharpCallLocalPmCallback(IntPtr head, IntPtr body, IntPtr userData)//BlockHeader*
        {
            var ret = Marshal.PtrToStructure(head, typeof(PercipioCommonTypes.PackageData));
            var pmData = (PercipioCommonTypes.PackageData)ret!;
            if (pmData.blk.dataType != (short)PercipioCommonTypes.I_DATA_TYPE_LIST.I_DATA_PACKAGE_MEASURE) {
                return;
            }

            if (pmData.count <= 0) {
                _gdata.boxSize.sizeX = 0;
                _gdata.boxSize.sizeY = 0;
                _gdata.boxSize.sizeZ = 0;
                _gdata.type = 0;
                _gdata.bounding ??= new PercipioCommonTypes.PmPoint[4];
                _gdata.boundingRGB ??= new PercipioCommonTypes.PmPoint[4];
                _gdata.newData = false;
                OnItemNotDetected(null);
                return;
            }

            var obj = Marshal.PtrToStructure(body, typeof(PercipioCommonTypes.SinglePackageInfo));
            var package = (PercipioCommonTypes.SinglePackageInfo)obj!;
            int objsize = Marshal.SizeOf(package);

            if (((PersonStandingPositionMask & 0x01) != 0x00 && package.distanceLeft < 0) ||
                ((PersonStandingPositionMask & 0x02) != 0x00 && package.distanceTop < 0) ||
                ((PersonStandingPositionMask & 0x04) != 0x00 && package.distanceRight < 0) ||
                ((PersonStandingPositionMask & 0x08) != 0x00 && package.distanceBottom < 0)) {
                OutOfBoundsDirection direction = OutOfBoundsDirection.Left;
                if ((PersonStandingPositionMask & 0x01) != 0x00 && (package.distanceLeft < 0))
                    direction = OutOfBoundsDirection.Left;
                else if ((PersonStandingPositionMask & 0x02) != 0x00 && (package.distanceTop < 0))
                    direction = OutOfBoundsDirection.Up;
                else if ((PersonStandingPositionMask & 0x04) != 0x00 && (package.distanceRight < 0))
                    direction = OutOfBoundsDirection.Right;
                else if ((PersonStandingPositionMask & 0x08) != 0x00 && (package.distanceBottom < 0))
                    direction = OutOfBoundsDirection.Down;
                OnItemOutOfBounds(new ItemOutOfBoundsEventArgs() {
                    Direction = direction
                });

                _gdata.boxSize.sizeX = 0;
                _gdata.boxSize.sizeY = 0;
                _gdata.boxSize.sizeZ = 0;

                _gdata.bounding ??= new PercipioCommonTypes.PmPoint[4];
                _gdata.boundingRGB ??= new PercipioCommonTypes.PmPoint[4];

                _gdata.newData = false;

                TYAppCalcOnce();

                return;
            }

            _gdata.boxSize.sizeX = (int)package.sizeX;
            _gdata.boxSize.sizeY = (int)package.sizeY;
            _gdata.boxSize.sizeZ = (int)package.sizeZ;

            _gdata.type = (int)package.type;
            _gdata.bounding ??= new PercipioCommonTypes.PmPoint[4];
            _gdata.boundingRGB ??= new PercipioCommonTypes.PmPoint[4];

            _gdata.bounding[0].x = (int)package.pixelPoints[0];
            _gdata.bounding[0].y = (int)package.pixelPoints[1];

            _gdata.bounding[1].x = (int)package.pixelPoints[2];
            _gdata.bounding[1].y = (int)package.pixelPoints[3];

            _gdata.bounding[2].x = (int)package.pixelPoints[4];
            _gdata.bounding[2].y = (int)package.pixelPoints[5];

            _gdata.bounding[3].x = (int)package.pixelPoints[6];
            _gdata.bounding[3].y = (int)package.pixelPoints[7];

            _gdata.boundingRGB[0].x = (int)package.pixelPointsRGB[0];
            _gdata.boundingRGB[0].y = (int)package.pixelPointsRGB[1];

            _gdata.boundingRGB[1].x = (int)package.pixelPointsRGB[2];
            _gdata.boundingRGB[1].y = (int)package.pixelPointsRGB[3];

            _gdata.boundingRGB[2].x = (int)package.pixelPointsRGB[4];
            _gdata.boundingRGB[2].y = (int)package.pixelPointsRGB[5];

            _gdata.boundingRGB[3].x = (int)package.pixelPointsRGB[6];
            _gdata.boundingRGB[3].y = (int)package.pixelPointsRGB[7];
            _gdata.newData = true;

            if (_gdata.boxSize is { sizeX: 0, sizeY: 0, sizeZ: 0 })
                OnItemNotDetected(null);
            else
                ConvertDimensions(_gdata);
        }

        private void CsharpCallLocalP3DCallback(IntPtr result, IntPtr blockSize, IntPtr userData) {
            var ret = Marshal.PtrToStructure(result, typeof(PercipioCommonTypes.ImageHeader));
            var img = (PercipioCommonTypes.ImageHeader)ret!;
            if (img.blk.dataType == (short)PercipioCommonTypes.I_DATA_TYPE_LIST.I_DATA_P3D) {
                if (img.format == (char)PercipioCommonTypes.I_IMG_FORMAT_LIST.I_IMG_FORMAT_RAW && img.pixelType == (char)PercipioCommonTypes.I_IMG_PIXEL_TYPE_LIST.I_IMG_PIXEL_TYPE_F32C3) {
                    _gdata.p3d_width = img.width;
                    _gdata.p3d_height = img.height;
                    if (_gdata.p3d_data.Length != img.blk.bodySize)
                        Array.Resize(ref _gdata.p3d_data, img.blk.bodySize);

                    result = IntPtr.Add(result, Marshal.SizeOf(img));
                    Marshal.Copy(result, _gdata.p3d_data, 0, img.blk.bodySize);
                    //OnCaptured3DImage(_gdata.p3d_data);
                    //暂时不需要3D图
                }
            }
        }

        private void ConvertDimensions(PercipioCommonTypes.AllData pmResults) {
            lock (_pmLock) {
                var pmAllData = new PercipioCommonTypes.AllData();
                pmAllData.boxSize.sizeX = pmResults.boxSize.sizeX;
                pmAllData.boxSize.sizeY = pmResults.boxSize.sizeY;
                pmAllData.boxSize.sizeZ = pmResults.boxSize.sizeZ;
                var volumeCapturedEventArgs = new VolumeCapturedEventArgs() {
                    //暂时不返回图像
                    Length = pmAllData.boxSize.sizeX,
                    Width = pmAllData.boxSize.sizeY,
                    Height = pmAllData.boxSize.sizeZ,
                    AreaCoords = new System.Drawing.Point[4],
                    Timestamp = DateTime.Now
                };

                if (pmResults.boundingRGB is not null && pmResults.bounding is not null) {
                    pmAllData.boundingRGB = new PercipioCommonTypes.PmPoint[pmResults.boundingRGB.Length];
                    for (var i = 0; i < pmResults.boundingRGB.Length; i++) {
                        pmAllData.boundingRGB[i].x = pmResults.boundingRGB[i].x;
                        pmAllData.boundingRGB[i].y = pmResults.boundingRGB[i].y;
                    }

                    pmAllData.bounding = new PercipioCommonTypes.PmPoint[pmResults.bounding.Length];
                    for (var i = 0; i < pmResults.bounding.Length; i++) {
                        pmAllData.bounding[i].x = pmResults.bounding[i].x;
                        pmAllData.bounding[i].y = pmResults.bounding[i].y;
                        volumeCapturedEventArgs.AreaCoords[i].X = pmResults.bounding[i].x;
                        volumeCapturedEventArgs.AreaCoords[i].Y = pmResults.bounding[i].y;
                    }

                    pmAllData.newColor = pmResults.newColor;
                    pmAllData.newDepth = pmResults.newDepth;
                    pmAllData.newData = pmResults.newData;
                }

                /*switch (pmResults.type) {
                    case (int)PACKAGE_TYPE_LIST.I_PACKAGE_TYPE_NONE:
                        packgeType = "NONE";
                        break;

                    case (int)PACKAGE_TYPE_LIST.I_PACKAGE_TYPE_BOX:
                        packgeType = "Box";
                        break;

                    case (int)PACKAGE_TYPE_LIST.I_PACKAGE_TYPE_BAG:
                        packgeType = "Bag";
                        break;

                    default:
                        break;
                }*/
                OnVolumeCapturedEvent(volumeCapturedEventArgs);
            }
        }

        public event EventHandler<IDevice>? Initialized;

        public event EventHandler<IDevice>? Connected;

        public event EventHandler<IDevice>? Disconnected;

        public event EventHandler<IDevice>? Reconnected;

        public event EventHandler<Exception>? Excepted;

        public string CameraName { get; private set; } = string.Empty;
        public string CameraId { get; private set; } = string.Empty;
        public float Framerate { get; private set; }
        public int BarcodeBorderSize { get; set; }
        public System.Drawing.Color BarcodeBorderColor { get; set; }
        public bool IsShowBarcodeBorder { get; set; }
        public string Brand => "图漾";
        public CameraStatus CameraStatus { get; } = CameraStatus.Disconnected;
        public CameraType CameraType { get; } = CameraType.ThreeDCamera;
        public ConnectionType ConnectionType { get; } = ConnectionType.Usb;

        public event EventHandler<BarcodeHitEventArgs>? BarcodeHitEvent;

        public event EventHandler<BarcodeHitEventArgs>? NotBarcodeHitEvent;

        public int DetectionBorderSize { get; set; } = 3;
        public System.Drawing.Color DetectionBorderColor { get; set; } = System.Drawing.Color.Yellow;
        public bool IsShowDetectionBorder { get; set; } = true;
        public bool IsUseImageWatermark { get; set; }

        public event EventHandler<Bitmap>? RealtimeImageEvent;

        public event EventHandler<VolumeCapturedEventArgs>? VolumeCapturedEvent;

        public event EventHandler<Bitmap>? LiveMappingEvent;

        public event EventHandler<string>? DeviceWarning;

        public event EventHandler<ItemOutOfBoundsEventArgs>? ItemOutOfBounds;

        public event EventHandler<EventArgs>? ItemNotDetected;

        public KeyValuePair<bool, string> SetFilterCondition<T>(T condition) {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> SetBarcodeType(BarcodeType type) {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> Pause() {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> Resume() {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> SetConfiguration<T>(T configData) {
            throw new NotImplementedException();
        }

        protected virtual async void OnExceptionLogged(Exception e) {
            await Task.Yield();
            OnDisconnected(this);
            Excepted?.Invoke(this, e);
        }

        protected virtual async void OnInitialized(IDevice e) {
            await Task.Yield();
            Initialized?.Invoke(this, e);
        }

        protected virtual async void OnConnected(IDevice e) {
            await Task.Yield();
            Connected?.Invoke(this, e);
        }

        protected virtual async void OnDisconnected(IDevice e) {
            await Task.Yield();
            Disconnected?.Invoke(this, e);
        }

        protected virtual async void OnRealtimeImageEvent(Bitmap? e) {
            await Task.Yield();
            if (e is not null) {
                RealtimeImageEvent?.Invoke(this, e);
            }
        }

        protected virtual async void OnLiveMappingEvent(Bitmap e) {
            await Task.Yield();
            LiveMappingEvent?.Invoke(this, e);
        }

        protected virtual async void OnVolumeCapturedEvent(VolumeCapturedEventArgs e) {
            await Task.Yield();
            VolumeCapturedEvent?.Invoke(this, e);
        }

        protected virtual async void OnItemOutOfBounds(ItemOutOfBoundsEventArgs e) {
            lock (_pmLock) {
                _gdata = new PercipioCommonTypes.AllData();
            }
            await Task.Yield();
            ItemOutOfBounds?.Invoke(this, e);
        }

        protected virtual async void OnDeviceWarning(string e) {
            await Task.Yield();
            DeviceWarning?.Invoke(this, e);
        }

        protected virtual async void OnItemNotDetected(EventArgs e) {
            lock (_pmLock) {
                _gdata = new PercipioCommonTypes.AllData();
            }
            await Task.Yield();
            ItemNotDetected?.Invoke(this, e);
        }
    }
}