using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using System.Reflection.Emit;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using JayTom.Dws.Device.Camera._3DCamera.Percipio;
using static JayTom.Dws.Device.Camera._3DCamera.Percipio3DCamera;
using static JayTom.Dws.Device.Camera._3DCamera.Percipio.PercipioAppUtils;

namespace JayTom.Dws.Device.Camera._3DCamera {

    public class Percipio3DCamera : I3DCamera {

        #region API Delegate

        // notice the same function parameters and return types here and in the original API class
        public delegate int TYAppInitDelegate(int argc, IntPtr[] argv);

        public delegate int TYAppDeinitDelegate();

        public delegate int TYAppSetDataCallbackDelegate(PercipioAppInterfacesBase.TYAppData_CallBack callback, IntPtr userData);

        public delegate int TYAppSetEventCallbackDelegate(PercipioAppInterfacesBase.TYAppEvent_CallBack callback, IntPtr userData);

        public delegate int TYAppStartDelegate();

        public delegate int TYAppStopDelegate();

        public delegate int TYAppCalcOnceDelegate();

        public delegate int TYAppReadPropertyDelegate(int propId, IntPtr buff, int buflen, IntPtr pfilled);

        public delegate int TYAppWritePropertyDelegate(int propId, IntPtr buff, int buflen);

        public delegate int TYAppWriteCmdDelegate(int cmdId);

        public delegate void TYAppLastErrorDelegate(IntPtr pStatus, IntPtr pMsg, int size);

        #endregion API Delegate

        #region Delegated API Methods

        public TYAppInitDelegate? TYAppInit;
        public TYAppDeinitDelegate? TYAppDeinit;
        public TYAppSetDataCallbackDelegate? TYAppSetDataCallback;
        public TYAppSetEventCallbackDelegate? TYAppSetEventCallback;
        public TYAppStartDelegate? TYAppStart;
        public TYAppStopDelegate? TYAppStop;
        public TYAppCalcOnceDelegate? TYAppCalcOnce;
        public TYAppReadPropertyDelegate? TYAppReadProperty;
        public TYAppWritePropertyDelegate? TYAppWriteProperty;
        public TYAppWriteCmdDelegate? TYAppWriteCmd;
        public TYAppLastErrorDelegate? TYAppLastError;

        #endregion Delegated API Methods

        public Percipio3DCamera() {
            _dynamicType = CreateDynamicType(typeof(PercipioAppCenter), "PercipioAppCenter");

            if (_dynamicType == null) {
                OnExcepted(new Exception($"_dynamicType无法创建"));
            }
            TYAppInit = Delegate.CreateDelegate(
                typeof(TYAppInitDelegate),
                _dynamicType.GetMethod("TYAppInit")) as TYAppInitDelegate;

            TYAppDeinit = Delegate.CreateDelegate(
                typeof(TYAppDeinitDelegate),
                _dynamicType.GetMethod("TYAppDeinit")) as TYAppDeinitDelegate;

            TYAppSetDataCallback = Delegate.CreateDelegate(
                typeof(TYAppSetDataCallbackDelegate),
                _dynamicType.GetMethod("TYAppSetDataCallback")) as TYAppSetDataCallbackDelegate;

            TYAppSetEventCallback = Delegate.CreateDelegate(
                typeof(TYAppSetEventCallbackDelegate),
                _dynamicType.GetMethod("TYAppSetEventCallback")) as TYAppSetEventCallbackDelegate;

            TYAppStart = Delegate.CreateDelegate(
                typeof(TYAppStartDelegate),
                _dynamicType.GetMethod("TYAppStart")) as TYAppStartDelegate;

            TYAppStop = Delegate.CreateDelegate(
                typeof(TYAppStopDelegate),
                _dynamicType.GetMethod("TYAppStop")) as TYAppStopDelegate;

            TYAppCalcOnce = Delegate.CreateDelegate(
                typeof(TYAppCalcOnceDelegate),
                _dynamicType.GetMethod("TYAppCalcOnce")) as TYAppCalcOnceDelegate;

            TYAppReadProperty = Delegate.CreateDelegate(
                typeof(TYAppReadPropertyDelegate),
                _dynamicType.GetMethod("TYAppReadProperty")) as TYAppReadPropertyDelegate;

            TYAppWriteProperty = Delegate.CreateDelegate(
                typeof(TYAppWritePropertyDelegate),
                _dynamicType.GetMethod("TYAppWriteProperty")) as TYAppWritePropertyDelegate;

            TYAppWriteCmd = Delegate.CreateDelegate(
                typeof(TYAppWriteCmdDelegate),
                _dynamicType.GetMethod("TYAppWriteCmd")) as TYAppWriteCmdDelegate;
        }

        public string DeviceCode { get; private set; } = string.Empty;
        public DeviceStatus Status { get; private set; } = DeviceStatus.Uninitialized;

        #region 自有变量

        private IntPtr ptr = IntPtr.Zero;
        private CvRect _bgRoi = new();
        private CvRect _safeRoi = new();
        private AllData _gdata;
        private bool _virtualWorkmode = false;
        private CvAnchor _currentCvAnchor = new();
        private Point _selectPoint = new(); //选中的搜索点
        private int _maxRecordCount = 10000;
        private AutoResetEvent _mPmEvent = new AutoResetEvent(false);
        private Type? _dynamicType;

        /// <summary>
        /// 人员站位
        /// </summary>
        private byte _personStandingPositionMask = 0x15;

        #endregion 自有变量

        public Task<KeyValuePair<bool, string>> Reconnect() {
            throw new NotImplementedException();
        }

        public async Task<KeyValuePair<bool, string>> Connect<T>(T connectParam) {
            //写在这里
            await Task.Yield();
            _gdata = new AllData {
                newData = false,
                newDepth = false,
                newColor = false
            };
            ptr = Marshal.AllocHGlobal(Marshal.SizeOf(_gdata));
            Marshal.StructureToPtr(_gdata, ptr, false);

            PercipioAppInterfacesBase.AppDataFunc = delegate (IntPtr head, IntPtr data, IntPtr userData) {
                //数据事件
            };
            TYAppSetDataCallback(PercipioAppInterfacesBase.AppDataFunc, ptr);

            PercipioAppInterfacesBase.AppEventFunc = delegate (IntPtr head, IntPtr data, IntPtr userData) {
                //不知道是什么触发回调
            };
            TYAppSetEventCallback(PercipioAppInterfacesBase.AppEventFunc, ptr);

            _gdata.running = true;

            var filled = 0;
            var status = 0;
            /*string depthCameraType = ConfigurationManager.AppSettings["DepthCameraType"].Trim();
            var app_name = depthCameraType.ToUpper().Equals("TOF") ? "PackageMeasureTof" : "PackageMeasure";*/
            const string appName = "PackageMeasure";
            var initappname = Marshal.StringToHGlobalAnsi(appName);
            if (TYAppWriteProperty is not null) {
                status = TYAppWriteProperty((int)I_PROPERTY_LIST.I_PROPERTY_string_APP_NAME, initappname, appName.Length + 1);
                if (0 != status) {
                    OnDeviceWarning($"TYAppWriteProperty I_PROPERTY_string_APP_NAME error :{status}");
                }
            }
            else {
                return new KeyValuePair<bool, string>(false,
                    $"TYAppWriteProperty is null");
            }

            Marshal.FreeHGlobal(initappname);
            if (TYAppWriteCmd is not null) {
                status = TYAppWriteCmd((Int32)I_APPCENTER_CMD_LIST.I_CMD_APP_INIT);
                if (0 != status) {
                    OnDeviceWarning($"TYAppWriteCmd I_CMD_APP_INIT error :{status}");
                }
            }
            else {
                return new KeyValuePair<bool, string>(false,
                    $"TYAppWriteCmd is null");
            }
            var b = true;
            unsafe {
                var p = &b;

                var op = Environment.Is64BitProcess ? new IntPtr((long)p) : new IntPtr((int)p);

                status = TYAppWriteProperty((int)I_PROPERTY_LIST.I_PROPERTY_bool_GRAB_DEPTH, op, Marshal.SizeOf(b));
                if (0 != status) {
                    OnDeviceWarning($"TYAppWriteProperty I_PROPERTY_bool_GRAB_DEPTH error : {status}");
                }
            }
            unsafe {
                var p = &b;

                var op = Environment.Is64BitProcess ? new IntPtr((long)p) : new IntPtr((int)p);
                status = TYAppWriteProperty((int)I_PROPERTY_LIST.I_PROPERTY_bool_GRAB_COLOR, op, Marshal.SizeOf(b));
                if (0 != status) {
                    OnDeviceWarning($"TYAppWriteProperty I_PROPERTY_bool_GRAB_COLOR error : {status}");
                }
            }
            var n = (int)I_IMG_FORMAT_LIST.I_IMG_FORMAT_JPG;
            unsafe {
                var p = &n;

                var op = Environment.Is64BitProcess ? new IntPtr((long)p) : new IntPtr((int)p);

                status = TYAppWriteProperty((int)I_PROPERTY_LIST.I_PROPERTY_int_COLOR_FORMAT, op, Marshal.SizeOf(n));
                if (0 != status) {
                    OnDeviceWarning($"TYAppWriteProperty error : {status}");
                }
            }
            unsafe {
                var p = &n;

                var op = Environment.Is64BitProcess ? new IntPtr((Int64)p) : new IntPtr((Int32)p);

                status = TYAppWriteProperty((int)I_PROPERTY_LIST.I_PROPERTY_int_DEPTH_FORMAT, op, Marshal.SizeOf(n));
                if (0 != status) {
                    OnDeviceWarning($"TYAppWriteProperty error : {status}");
                }
            }
            var getBgRect = new CvRect();
            unsafe {
                var p = &getBgRect;
                filled = 0;
                var ppp = &filled;
                var op = IntPtr.Zero;
                var pFilled = IntPtr.Zero;

                if (Environment.Is64BitProcess) {
                    op = new IntPtr((long)p);
                    pFilled = new IntPtr((long)ppp);
                }
                else {
                    op = new IntPtr((int)p);
                    pFilled = new IntPtr((int)ppp);
                }

                if (TYAppReadProperty is not null) {
                    status = TYAppReadProperty((int)I_PROPERTY_LIST.I_PROPERTY_int4_DEPTH_ROI, op, Marshal.SizeOf(getBgRect), pFilled);
                    if (0 != status) {
                        OnDeviceWarning($"TYAppReadProperty error : {status}");
                    }
                }
                else {
                    return new KeyValuePair<bool, string>(false,
                        $"TYAppReadProperty is null");
                }
            }

            //get
            var getSafeRect = new CvRect();
            unsafe {
                var p = &getSafeRect;
                filled = 0;
                int* ppp = &filled;
                var op = IntPtr.Zero;
                var pFilled = IntPtr.Zero;

                if (Environment.Is64BitProcess) {
                    op = new IntPtr((long)p);
                    pFilled = new IntPtr((long)ppp);
                }
                else {
                    op = new IntPtr((int)p);
                    pFilled = new IntPtr((int)ppp);
                }
                status = TYAppReadProperty((int)I_PROPERTY_LIST.I_PROPERTY_int4_SAFE_RECT, op, Marshal.SizeOf(getSafeRect), pFilled);
                if (0 != status) {
                    OnDeviceWarning($"TYAppReadProperty error : {status}");
                }
            }

            if (TYAppStart is not null) {
                status = TYAppStart();
                if (0 != status) {
                    OnDeviceWarning($"TYAppStart error : {status}");
                    return new KeyValuePair<bool, string>(false, $"TYAppStart error : {status}");
                }
            }
            else {
                return new KeyValuePair<bool, string>(false, $"TYAppStart is null");
            }

            OnConnected(this);
            return new KeyValuePair<bool, string>(true, $"LoadPMSuccess");
        }

        public void Dispose() {
            int status;
            if (TYAppStop is not null) {
                status = TYAppStop();
                if (0 != status) {
                    OnDeviceWarning($"TYAppStop error : {status}");
                    return;
                }

                Marshal.FreeHGlobal(ptr); //free the memory
            }
            else {
                OnExcepted(new Exception($"TYAppStop is null"));
            }
            _gdata.running = false;
            _mPmEvent.Set();
        }

        public async Task<KeyValuePair<bool, string>> Initialization() {
            await Task.Yield();
            _gdata = new AllData {
                running = false
            };
            var status = 0;

            if (_virtualWorkmode == true) {
                var argv = new IntPtr[1];
                argv[0] = StringToByteArray(".");
                //argv[1] = StringToByteArray("-logL");
                //argv[2] = StringToByteArray("2");
                //argv[3] = StringToByteArray("-log2file");
                argv[1] = StringToByteArray("-deviceType");
                argv[2] = StringToByteArray("Virtual");
                if (TYAppInit is not null) {
                    status = TYAppInit(3, argv);
                    //return new KeyValuePair<bool, string>(false, "init virtual device mode");
                }
            }
            else {
                var argv = new IntPtr[1];
                argv[0] = StringToByteArray(".");
                //argv[1] = StringToByteArray("-logL");
                //argv[2] = StringToByteArray("2");
                //argv[3] = StringToByteArray("-log2file");
                //argv[4] = StringToByteArray("-deviceType");
                //argv[5] = StringToByteArray("Virtual");
                if (TYAppInit is not null) {
                    status = TYAppInit(1, argv);
                    //return new KeyValuePair<bool, string>(false, "init real device mode");
                }
                else {
                    return new KeyValuePair<bool, string>(false, "TYAppInit is null");
                }
            }

            if (0 != status) {
                return new KeyValuePair<bool, string>(false, $"TYAppInit error : {status}");
            }
            ReadRoiRect((int)I_PROPERTY_LIST.I_PROPERTY_int4_DEPTH_ROI, ref _bgRoi);
            ReadRoiRect((int)I_PROPERTY_LIST.I_PROPERTY_int4_SAFE_RECT, ref _safeRoi);

            var cvAnchor = new CvAnchor();
            if (ReadCvAnchor((int)I_PROPERTY_LIST.I_PROPERTY_int2_BGG_ANCHOR, ref cvAnchor)) {
                _currentCvAnchor.xAnchor = cvAnchor.xAnchor;
                _currentCvAnchor.yAnchor = cvAnchor.yAnchor;
                if (_currentCvAnchor is { xAnchor: > 0, yAnchor: > 0 }) {
                    _selectPoint.X = _currentCvAnchor.xAnchor;
                    _selectPoint.Y = _currentCvAnchor.yAnchor;
                }
            }

            //originalImage = pictureBox1.Image;
            if (ReadInt32Property((Int32)I_PROPERTY_LIST.I_PROPERTY_int_MAXRECORD_COUNT, ref _maxRecordCount)) {
                //Logs.WriteLogs("INFO", String.Format("read max record count = {0}", _maxRecordCount));
            }

            return new KeyValuePair<bool, string>(true, string.Empty);
        }

        public event EventHandler<IDevice>? Initialized;

        public event EventHandler<IDevice>? Connected;

        public event EventHandler<IDevice>? Disconnected;

        public event EventHandler<IDevice>? Reconnected;

        public event EventHandler<Exception>? Excepted;

        public string CameraName { get; private set; } = string.Empty;
        public string CameraId { get; private set; } = string.Empty;
        public float Framerate { get; private set; }
        public int DetectionBorderSize { get; set; }
        public Color DetectionBorderColor { get; set; }
        public bool IsShowDetectionBorder { get; set; }
        public bool IsUseImageWatermark { get; set; }

        public event EventHandler<Bitmap>? RealtimeImageEvent;

        public event EventHandler<VolumeCapturedEventArgs>? VolumeCapturedEvent;

        public event EventHandler<Bitmap>? LiveMappingEvent;

        public event EventHandler<string>? DeviceWarning;

        public KeyValuePair<bool, string> Pause() {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> Resume() {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> SetConfiguration<T>(T configData) {
            throw new NotImplementedException();
        }

        private async void OnDeviceWarning(string e) {
            await Task.Yield();
            DeviceWarning?.Invoke(this, e);
        }

        private async void OnConnected(IDevice e) {
            await Task.Yield();
            Connected?.Invoke(this, e);
        }

        private async void OnExcepted(Exception e) {
            await Task.Yield();
            Excepted?.Invoke(this, e);
        }

        #region 本身源码方法(暂不整理)

        public bool ReadRoiRect(int propertyId, ref CvRect rect) {
            if (!_gdata.running)
                return false;

            var tmpRect = new CvRect();
            unsafe {
                var p = &tmpRect;
                var filled = 0;
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

                if (TYAppReadProperty is not null) {
                    var status = TYAppReadProperty((Int32)propertyId, op, Marshal.SizeOf(tmpRect), pFilled);
                    if (0 != status) {
                        OnDeviceWarning($"TYAppReadProperty {propertyId} error : {status}");
                        return false;
                    }

                    rect = tmpRect;
                }
                else {
                    return false;
                }
            }
            return true;
        }

        public bool ReadCvAnchor(int propertyID, ref CvAnchor cvAnchor) {
            var tmpCvAnchor = new CvAnchor();
            unsafe {
                var p = &tmpCvAnchor;
                var filled = 0;
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

                if (TYAppReadProperty is not null) {
                    var status = TYAppReadProperty((int)propertyID, op, Marshal.SizeOf(tmpCvAnchor), pFilled);
                    if (0 != status) {
                        OnDeviceWarning($"TYAppReadProperty {propertyID} error : {status}");
                        return false;
                    }

                    cvAnchor = tmpCvAnchor;
                }
                else {
                    return false;
                }
            }
            return true;
        }

        public bool ReadInt32Property(int propertyId, ref int value) {
            if (!_gdata.running)
                return false;

            var tmpValue = 0x00;
            unsafe {
                var p = &tmpValue;
                var filled = 0;
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

                if (TYAppReadProperty is not null) {
                    var status = TYAppReadProperty((Int32)propertyId, op, Marshal.SizeOf(tmpValue), pFilled);
                    if (0 != status) {
                        OnDeviceWarning($"TYAppReadProperty {propertyId} error : {status}");
                        return false;
                    }

                    value = tmpValue;
                }
                else {
                    return false;
                }
            }
            return true;
        }

        private Type? CreateDynamicType(Type originalType, string dynamicBaseName) {
            // Create dynamic assembly
            var assemblyName = new AssemblyName {
                Name = dynamicBaseName + "Assembly" // nothing fancy, "...Asembly", could be anything
            };

            // The AssemblyBuilderAccess.RunAndSave attribute allows me to save this assmebly later on so I can inspect it.
            // In the release version it will be sufficient to use AssemblyBuilderAccess.Run, as there is no need to save it.
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            // Add module to assembly
            ModuleBuilder moduleBuilder;

            // I'm using an overloaded constructor here that creates a persistent module
            // which can be saved to the disk
            moduleBuilder = assemblyBuilder.DefineDynamicModule(dynamicBaseName + "Module");

            // Add class to module
            var typeBuilder = moduleBuilder.DefineType(dynamicBaseName + "Type", TypeAttributes.Class);

            // retrieve all the methods that are public and static in the originalType
            var methodInfos = originalType.GetMethods(BindingFlags.Public | BindingFlags.Static);

            // loop through those methods
            for (var i = 0; i < methodInfos.GetLength(0); i++) {
                // mi holds the info for an api method
                var mi = methodInfos[i];

                // get all method parameters so we can save thier types
                var methodParameters = mi.GetParameters();
                var parameterCount = methodParameters.GetLength(0);

                // stores for parameter types and attributes
                var parameterTypes = new Type[parameterCount];
                var parameterAttributes = new ParameterAttributes[parameterCount];

                // save method parameter types and attributes
                for (var j = 0; j < parameterCount; j++) {
                    parameterTypes[j] = methodParameters[j].ParameterType;
                    parameterAttributes[j] = methodParameters[j].Attributes;
                }

                //create a MethodBuilder for a PInvoke method
                var methodBuilder = typeBuilder.DefinePInvokeMethod(
                    mi.Name, // use same name as original
                    "PercipioAppCentermt.dll", // here we change the dynamic path of the dll's
                    mi.Attributes,
                    mi.CallingConvention, // default calling convention
                    mi.ReturnType, // original method return type
                    parameterTypes, // the method parameter types we collected
                    CallingConvention.StdCall, // StdCall interop calling convention (possible problem)
                    CharSet.Auto);

                // we have to additionally define the parameter Attributes
                // set them the same as the original parameter attributes
                for (var j = 0; j < parameterCount; j++)
                    methodBuilder.DefineParameter(j + 1, parameterAttributes[j], methodParameters[j].Name);

                // We set the implementation flags the same as the original method
                methodBuilder.SetImplementationFlags(mi.GetMethodImplementationFlags());
            }

            // create the defined type
            var retval = typeBuilder.CreateType();

            // save the dll in the bin directory, not necessary but informative
            // (ex. you can use Lutz Roeder's .NET Reflector to open it)
            //assemblyBuilder.Save(dynamicBaseName + ".dll");
            // finally return the dynamic type!
            return retval;
        }

        private Type? ReadDynamicType(Type originalType, string dynamicBaseName) {
            string assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dynamicBaseName + ".dll");
            Assembly assembly = Assembly.LoadFrom(assemblyPath);

            Type? dynamicType = assembly.GetType(dynamicBaseName + "Type");
            /*if (dynamicType != null) {
                // 使用动态类型进行操作
                // ...
            }*/

            return dynamicType;
        }

        #endregion 本身源码方法(暂不整理)
    }
}