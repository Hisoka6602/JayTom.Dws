using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Device.Camera._3DCamera.Percipio {

    public class PercipioAppInterfacesBase {

        public delegate void TYAppDataCallBack(IntPtr head, IntPtr data, IntPtr userData);

        public static TYAppDataCallBack? AppDataFunc;

        public delegate void TYAppEventCallBack(IntPtr head, IntPtr data, IntPtr userData);

        public static TYAppEventCallBack? AppEventFunc;
    }
}