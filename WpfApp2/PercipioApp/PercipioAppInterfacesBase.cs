using System;

namespace WpfApp2.PercipioApp {

    public class PercipioAppInterfacesBase {

        public delegate void TyAppDataCallBack(IntPtr head, IntPtr data, IntPtr userData);

        public static TyAppDataCallBack? AppDataFunc;

        public delegate void TyAppEventCallBack(IntPtr head, IntPtr data, IntPtr userData);

        public static TyAppEventCallBack? AppEventFunc;
    }
}