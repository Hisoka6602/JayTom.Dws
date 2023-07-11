using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Xml.Linq;
using System.Threading;
using WpfApp2.PercipioApp;
using System.Windows.Markup;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static WpfApp2.PercipioCommonTypes;
using Rectangle = System.Drawing.Rectangle;
using static WpfApp2.PercipioApp.PercipioAppCenter;

namespace WpfApp2 {

    public class TyPmLoader {
        private IntPtr _ptr = IntPtr.Zero;
        private AllData _gdata;
        private static object _pmLock = new();
        private static readonly byte PersonStandingPositionMask = 0x00;

        /// <summary>
        /// 是否显示边框
        /// </summary>
        public bool IsShowBorder { get; set; }

        /// <summary>
        /// 实时图片
        /// </summary>
        public event EventHandler<Image>? RealTimeImageEvent;

        /// <summary>
        /// 实时RGB图片(实景图)
        /// </summary>
        public event EventHandler<Image>? RealTimeRgbImageEvent;

        /// <summary>
        /// 捕捉到体积
        /// </summary>
        public event EventHandler<Dimensions>? VolumeDataCaptureEvent;

        /// <summary>
        /// 未检测到物品体积(画面变动)
        /// </summary>
        public event EventHandler<EventArgs>? ItemNotDetected;

        /// <summary>
        /// 物品超出边缘
        /// </summary>
        public event EventHandler<ItemOutOfBoundsEventArgs>? ItemOutOfBounds;

        /// <summary>
        /// 捕抓到3D图
        /// </summary>
        public event EventHandler<byte[]>? Captured3DImage;

        public event EventHandler<Exception>? ExceptionLogged;

        /// <summary>
        /// 初始化
        /// </summary>
        public void InitializeApp() {
            var argv = new IntPtr[1];
            argv[0] = Utils.StringToByteArray(".");
            var status = TYAppInit(1, argv);

            if (status == 0) {
                Connect();
            }
            else {
                OnExceptionLogged(new Exception($"初始化错误:{status}"));
            }
        }

        public void Connect() {
            _gdata = new AllData {
                newData = false,
                newDepth = false,
                newColor = false
            };

            _ptr = Marshal.AllocHGlobal(Marshal.SizeOf(_gdata));
            Marshal.StructureToPtr(_gdata, _ptr, false);

            PercipioAppInterfacesBase.AppDataFunc = delegate (IntPtr head, IntPtr data, IntPtr userData) {
                var ret = Marshal.PtrToStructure(head, typeof(BlockHeader));
                switch (((BlockHeader)ret!).dataType) {
                    case (short)I_DATA_TYPE_LIST.I_DATA_SETUPPER_IMAGE: CsharpCallLocalSetupperCallback(head, 0, userData); break;//3
                    case (short)I_DATA_TYPE_LIST.I_DATA_DEPTH_IMAGE: CsharpCallLocalDepthCallback(head, data, userData); break;//1
                    case (short)I_DATA_TYPE_LIST.I_DATA_COLOR_IMAGE: CsharpCallLocalColorCallback(head, data, userData); break;//2
                    case (short)I_DATA_TYPE_LIST.I_DATA_PACKAGE_MEASURE: CsharpCallLocalPmCallback(head, data, userData); break;//4
                    case (short)I_DATA_TYPE_LIST.I_DATA_P3D: CsharpCallLocalP3DCallback(head, data, userData); break;//19
                    default: break;
                }
                return;

                Console.WriteLine(data);
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

            status = TYAppWriteProperty((int)I_PROPERTY_LIST.I_PROPERTY_string_APP_NAME, initappname, appName.Length + 1);
            if (0 != status) {
                OnExceptionLogged(new Exception($"TYAppWriteProperty I_PROPERTY_string_APP_NAME error : {status}"));
                return;
            }

            Marshal.FreeHGlobal(initappname);

            status = TYAppWriteCmd((int)I_APPCENTER_CMD_LIST.I_CMD_APP_INIT);
            if (0 != status) {
                OnExceptionLogged(new Exception($"TYAppWriteCmd I_CMD_APP_INIT error : {status}"));
                return;
            }

            var b = true;
            unsafe {
                var p = &b;

                var op = Environment.Is64BitProcess ? new IntPtr((long)p) : new IntPtr((int)p);

                status = TYAppWriteProperty((int)I_PROPERTY_LIST.I_PROPERTY_bool_GRAB_DEPTH, op, Marshal.SizeOf(b));
                if (0 != status) {
                    OnExceptionLogged(new Exception($"TYAppWriteProperty I_PROPERTY_bool_GRAB_DEPTH error : {status}"));
                    return;
                }
            }
            unsafe {
                var p = &b;

                var op = Environment.Is64BitProcess ? new IntPtr((long)p) : new IntPtr((int)p);

                status = TYAppWriteProperty((int)I_PROPERTY_LIST.I_PROPERTY_bool_GRAB_COLOR, op, Marshal.SizeOf(b));
                if (0 != status) {
                    OnExceptionLogged(new Exception($"TYAppWriteProperty I_PROPERTY_bool_GRAB_COLOR error : {status}"));
                    return;
                }
            }

            //set depth image output format
            var n = (int)I_IMG_FORMAT_LIST.I_IMG_FORMAT_JPG;
            unsafe {
                var p = &n;

                var op = Environment.Is64BitProcess ? new IntPtr((long)p) : new IntPtr((int)p);

                status = TYAppWriteProperty((int)I_PROPERTY_LIST.I_PROPERTY_int_COLOR_FORMAT, op, Marshal.SizeOf(n));
                if (0 != status) {
                    OnExceptionLogged(new Exception($"TYAppWriteProperty error : {status}"));
                    return;
                }
            }
            unsafe {
                var p = &n;

                var op = Environment.Is64BitProcess ? new IntPtr((long)p) : new IntPtr((int)p);

                status = TYAppWriteProperty((int)I_PROPERTY_LIST.I_PROPERTY_int_DEPTH_FORMAT, op, Marshal.SizeOf(n));
                if (0 != status) {
                    OnExceptionLogged(new Exception($"TYAppWriteProperty error : {status}"));
                    return;
                }
            }

            //get
            var getBgRect = new CvRect();
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

                status = TYAppReadProperty((int)I_PROPERTY_LIST.I_PROPERTY_int4_DEPTH_ROI, op, Marshal.SizeOf(getBgRect), pFilled);
                if (0 != status) {
                    OnExceptionLogged(new Exception($"TYAppReadProperty error : {status}"));
                    return;
                }
            }

            //get
            var getSafeRect = new CvRect();
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
                status = TYAppReadProperty((Int32)I_PROPERTY_LIST.I_PROPERTY_int4_SAFE_RECT, op, Marshal.SizeOf(getSafeRect), pFilled);
                if (0 != status) {
                    OnExceptionLogged(new Exception($"TYAppReadProperty error : {status}"));
                    return;
                }
            }

            status = TYAppStart();
            if (0 != status) {
                OnExceptionLogged(new Exception($"TYAppStart error : {status}"));
            }
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

        private void CsharpCallLocalSetupperCallback(IntPtr result, int blockSize, IntPtr userData) {
            var ret = Marshal.PtrToStructure(result, typeof(ImageHeader));
            var img = (ImageHeader)ret!;

            if (img.blk.dataType != (short)I_DATA_TYPE_LIST.I_DATA_SETUPPER_IMAGE) return;
            if ((img.format != (char)I_IMG_FORMAT_LIST.I_IMG_FORMAT_RAW) &&
                (img.format != (char)I_IMG_FORMAT_LIST.I_IMG_FORMAT_JPG)) return;
            _gdata.depth_width = img.width;
            _gdata.depth_height = img.height;
            if (_gdata.depth_data == null)
                _gdata.depth_data = new byte[img.size];
            else if (_gdata.depth_data.Length != img.size)
                Array.Resize(ref _gdata.depth_data, img.size);

            result = IntPtr.Add(result, Marshal.SizeOf(img));
            Marshal.Copy(result, _gdata.depth_data, 0, img.size);
            var byteToImage = Utils.ByteToImage(_gdata.depth_data);
            if (IsShowBorder && byteToImage is not null && _gdata.bounding?.Length >= 4) {
                //画边框
                using (var graphics = Graphics.FromImage(byteToImage)) {
                    // 绘制矩形
                    using (var pen = new Pen(Color.Yellow, 3) {
                        DashStyle = DashStyle.Solid,
                        DashPattern = new float[] { 1f, 2f }
                    }) {
                        var tmpPixelPointsRgb = new PointF[4];
                        for (var i = 0; i < 4; i++) {
                            tmpPixelPointsRgb[i].X = _gdata.bounding[i].x;
                            tmpPixelPointsRgb[i].Y = _gdata.bounding[i].y;
                        }
                        var myPath = new GraphicsPath();
                        myPath.AddPolygon(tmpPixelPointsRgb);
                        graphics.DrawPath(pen, myPath);
                    }
                }
                OnRealTimeImageEvent(byteToImage);
            }
            else {
                OnRealTimeImageEvent(byteToImage);
            }
        }

        private void CsharpCallLocalDepthCallback(IntPtr head, IntPtr body, IntPtr userData) {
            var ret = Marshal.PtrToStructure(head, typeof(ImageHeader));
            var img = (ImageHeader)ret!;
            if (img.blk.dataType != (short)I_DATA_TYPE_LIST.I_DATA_DEPTH_IMAGE) return;
            if (img.format != (char)I_IMG_FORMAT_LIST.I_IMG_FORMAT_RAW &&
                img.format != (char)I_IMG_FORMAT_LIST.I_IMG_FORMAT_JPG) return;
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
            var byteToImage = Utils.ByteToImage(_gdata.depth_data);

            if (IsShowBorder && byteToImage is not null && _gdata.bounding?.Length >= 4) {
                //画边框
                using (var graphics = Graphics.FromImage(byteToImage)) {
                    // 绘制矩形
                    using (var pen = new Pen(Color.Yellow, 3) {
                        DashStyle = DashStyle.Solid,
                        DashPattern = new float[] { 1f, 2f }
                    }) {
                        var tmpPixelPointsRgb = new PointF[4];
                        for (var i = 0; i < 4; i++) {
                            tmpPixelPointsRgb[i].X = _gdata.bounding[i].x;
                            tmpPixelPointsRgb[i].Y = _gdata.bounding[i].y;
                        }
                        var myPath = new GraphicsPath();
                        myPath.AddPolygon(tmpPixelPointsRgb);
                        graphics.DrawPath(pen, myPath);
                    }
                }
                OnRealTimeImageEvent(byteToImage);
            }
            else {
                OnRealTimeImageEvent(byteToImage);
            }
        }

        private void CsharpCallLocalColorCallback(IntPtr head, IntPtr body, IntPtr userData) {
            var ret = Marshal.PtrToStructure(head, typeof(ImageHeader));
            var img = (ImageHeader)ret!;
            if (img.blk.dataType != (short)I_DATA_TYPE_LIST.I_DATA_COLOR_IMAGE) return;
            if (img.format != (char)I_IMG_FORMAT_LIST.I_IMG_FORMAT_JPG) return;
            ret = Marshal.PtrToStructure(userData, typeof(AllData));
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
            var byteToImage = Utils.ByteToImage(_gdata.color_data);
            if (IsShowBorder && byteToImage is not null && _gdata.boundingRGB?.Length >= 4) {
                //画边框
                using (var graphics = Graphics.FromImage(byteToImage)) {
                    using (var pen = new Pen(Color.Yellow, 3)) {
                        var pointFs = new PointF[4];
                        for (var i = 0; i < 4; i++) {
                            pointFs[i].X = _gdata.boundingRGB[i].x;
                            pointFs[i].Y = _gdata.boundingRGB[i].y;
                        }

                        var graphicsPath = new GraphicsPath();
                        graphicsPath.AddPolygon(pointFs);
                        graphics.DrawPath(pen, graphicsPath);
                    }
                }
                OnRealTimeRgbImageEvent(byteToImage);
            }
            else {
                OnRealTimeRgbImageEvent(byteToImage);
            }
        }

        private void CsharpCallLocalPmCallback(IntPtr head, IntPtr body, IntPtr userData)//BlockHeader*
        {
            var ret = Marshal.PtrToStructure(head, typeof(PackageData));
            var pmData = (PackageData)ret!;
            if (pmData.blk.dataType != (short)I_DATA_TYPE_LIST.I_DATA_PACKAGE_MEASURE) {
                return;
            }

            if (pmData.count <= 0) {
                _gdata.boxSize.sizeX = 0;
                _gdata.boxSize.sizeY = 0;
                _gdata.boxSize.sizeZ = 0;
                _gdata.type = 0;
                _gdata.bounding ??= new PmPoint[4];
                _gdata.boundingRGB ??= new PmPoint[4];
                _gdata.newData = false;
                OnItemNotDetected(null);
                return;
            }

            var obj = Marshal.PtrToStructure(body, typeof(SinglePackageInfo));
            var package = (SinglePackageInfo)obj!;
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

                _gdata.bounding ??= new PmPoint[4];
                _gdata.boundingRGB ??= new PmPoint[4];

                _gdata.newData = false;

                TYAppCalcOnce();

                return;
            }

            _gdata.boxSize.sizeX = (int)package.sizeX;
            _gdata.boxSize.sizeY = (int)package.sizeY;
            _gdata.boxSize.sizeZ = (int)package.sizeZ;

            _gdata.type = (int)package.type;
            _gdata.bounding ??= new PmPoint[4];
            _gdata.boundingRGB ??= new PmPoint[4];

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
            var ret = Marshal.PtrToStructure(result, typeof(ImageHeader));
            var img = (ImageHeader)ret!;
            if (img.blk.dataType == (short)I_DATA_TYPE_LIST.I_DATA_P3D) {
                if (img.format == (char)I_IMG_FORMAT_LIST.I_IMG_FORMAT_RAW && img.pixelType == (char)I_IMG_PIXEL_TYPE_LIST.I_IMG_PIXEL_TYPE_F32C3) {
                    _gdata.p3d_width = img.width;
                    _gdata.p3d_height = img.height;
                    if (_gdata.p3d_data.Length != img.blk.bodySize)
                        Array.Resize(ref _gdata.p3d_data, img.blk.bodySize);

                    result = IntPtr.Add(result, Marshal.SizeOf(img));
                    Marshal.Copy(result, _gdata.p3d_data, 0, img.blk.bodySize);
                    OnCaptured3DImage(_gdata.p3d_data);
                }
            }
        }

        private void ConvertDimensions(AllData pmResults) {
            lock (_pmLock) {
                var pmAllData = new AllData();
                pmAllData.boxSize.sizeX = pmResults.boxSize.sizeX;
                pmAllData.boxSize.sizeY = pmResults.boxSize.sizeY;
                pmAllData.boxSize.sizeZ = pmResults.boxSize.sizeZ;
                pmAllData.boundingRGB = new PmPoint[pmResults.boundingRGB.Length];
                for (var i = 0; i < pmResults.boundingRGB.Length; i++) {
                    pmAllData.boundingRGB[i].x = pmResults.boundingRGB[i].x;
                    pmAllData.boundingRGB[i].y = pmResults.boundingRGB[i].y;
                }

                pmAllData.bounding = new PmPoint[pmResults.bounding.Length];
                for (var i = 0; i < pmResults.bounding.Length; i++) {
                    pmAllData.bounding[i].x = pmResults.bounding[i].x;
                    pmAllData.bounding[i].y = pmResults.bounding[i].y;
                }

                pmAllData.newColor = pmResults.newColor;
                pmAllData.newDepth = pmResults.newDepth;
                pmAllData.newData = pmResults.newData;

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
                OnVolumeDataCaptureEvent(new Dimensions() {
                    Length = pmAllData.boxSize.sizeX,
                    Width = pmAllData.boxSize.sizeY,
                    Height = pmAllData.boxSize.sizeZ
                });
            }
        }

        protected async void OnRealTimeImageEvent(Image? e) {
            await Task.Yield();
            if (e is not null) {
                RealTimeImageEvent?.Invoke(this, e);
            }
        }

        protected async void OnRealTimeRgbImageEvent(Image? e) {
            await Task.Yield();
            if (e is not null) {
                RealTimeRgbImageEvent?.Invoke(this, e);
            }
        }

        protected virtual async void OnVolumeDataCaptureEvent(Dimensions? e) {
            await Task.Yield();
            if (e is not null) {
                VolumeDataCaptureEvent?.Invoke(this, e);
            }
        }

        protected virtual async void OnItemNotDetected(EventArgs? e) {
            lock (_pmLock) {
                _gdata = new AllData();
            }
            await Task.Yield();
            if (e is not null) {
                ItemNotDetected?.Invoke(this, e);
            }
        }

        protected virtual async void OnItemOutOfBounds(ItemOutOfBoundsEventArgs? e) {
            lock (_pmLock) {
                _gdata = new AllData();
            }
            await Task.Yield();
            if (e is not null) {
                ItemOutOfBounds?.Invoke(this, e);
            }
        }

        protected virtual async void OnCaptured3DImage(byte[]? e) {
            await Task.Yield();
            if (e is not null) {
                Captured3DImage?.Invoke(this, e);
            }
        }

        protected virtual async void OnExceptionLogged(Exception? e) {
            await Task.Yield();
            if (e is not null) {
                ExceptionLogged?.Invoke(this, e);
            }
        }
    }

    public class Dimensions {
        public double Length { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public class ItemOutOfBoundsEventArgs : EventArgs {
        public OutOfBoundsDirection Direction { get; set; } // 超出边缘的方位
    }

    public enum OutOfBoundsDirection {
        Up,
        Down,
        Left,
        Right
    }
}