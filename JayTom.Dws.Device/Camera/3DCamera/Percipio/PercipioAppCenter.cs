using System.Runtime.InteropServices;

namespace JayTom.Dws.Device.Camera._3DCamera.Percipio {

    public class PercipioAppCenter : PercipioAppInterfacesBase {

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppInit", CharSet = CharSet.Unicode, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern int TYAppInit(int argc, IntPtr[] argv);// defaut is "./" const char* argv[]

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppDeinit", CharSet = CharSet.Unicode, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern int TYAppDeinit();

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppSetDataCallback", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern int TYAppSetDataCallback(TyAppDataCallBack callback, IntPtr userData);

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppSetEventCallback", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern int TYAppSetEventCallback(TyAppEventCallBack callback, IntPtr userData);

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppStart", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern int TYAppStart();

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppStop", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern int TYAppStop();

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppCalcOnce", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern int TYAppCalcOnce();

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppReadProperty", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern int TYAppReadProperty(int propId, IntPtr buff, int buflen, IntPtr pfilled);

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppWriteProperty", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern int TYAppWriteProperty(int propId, IntPtr buff, int buflen);

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppWriteCmd", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern int TYAppWriteCmd(int cmdId);

        //[DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppLastError", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        //public static extern char* TYAppLastError();
    }
}