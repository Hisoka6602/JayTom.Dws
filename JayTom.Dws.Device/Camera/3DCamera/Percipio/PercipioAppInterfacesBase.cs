using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Device.Camera._3DCamera.Percipio {

    public class PercipioAppInterfacesBase {

        public delegate void TYAppData_CallBack(IntPtr head, IntPtr data, IntPtr user_data);

        public static TYAppData_CallBack AppDataFunc;

        public delegate void TYAppEvent_CallBack(IntPtr head, IntPtr data, IntPtr user_data);

        public static TYAppEvent_CallBack AppEventFunc;
    }
}