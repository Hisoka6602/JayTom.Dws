using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Device.Camera._3DCamera.Percipio {

    public class PercipioAppCenter : PercipioAppInterfacesBase {

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppInit", CharSet = CharSet.Unicode, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 TYAppInit(Int32 argc, IntPtr[] argv);// defaut is "./" const char* argv[]

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppDeinit", CharSet = CharSet.Unicode, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 TYAppDeinit();

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppSetDataCallback", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 TYAppSetDataCallback(TYAppData_CallBack callback, IntPtr user_data);

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppSetEventCallback", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 TYAppSetEventCallback(TYAppEvent_CallBack callback, IntPtr user_data);

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppStart", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 TYAppStart();

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppStop", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 TYAppStop();

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppCalcOnce", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 TYAppCalcOnce();

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppReadProperty", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 TYAppReadProperty(Int32 prop_id, IntPtr buff, Int32 buflen, IntPtr pfilled);

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppWriteProperty", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 TYAppWriteProperty(Int32 prop_id, IntPtr buff, Int32 buflen);

        [DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppWriteCmd", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 TYAppWriteCmd(Int32 cmd_id);

        //[DllImport("PercipioAppCentermt.dll", EntryPoint = "TYAppLastError", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        //public static extern char* TYAppLastError();
    }
}