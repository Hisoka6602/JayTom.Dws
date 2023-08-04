using MVSDK_Net;

internal class Program {
    private static MyCamera camera = new();

    private static void Main(string[] args) {
        Console.WriteLine("Hello, World!");

        int res = IMVDefine.IMV_OK;
        IMVDefine.IMV_DeviceList deviceList = new IMVDefine.IMV_DeviceList();

        //枚举设备
        res = MyCamera.IMV_EnumDevices(ref deviceList,
            (uint)IMVDefine.IMV_EInterfaceType.interfaceTypeAll);

        //res = MyCamera.IMV_EnumDevicesByUnicast(ref deviceList,"10.55.136.253");
        if (res != IMVDefine.IMV_OK) {
            //创建句柄

            camera.IMV_CreateHandle(IMVDefine.IMV_ECreateHandleMode.modeByIndex, 0);

            var imvOpen = camera.IMV_Open();
            if (imvOpen != IMVDefine.IMV_OK) {
            }

            Console.WriteLine("Enumeration devices failed! ErrorCode:[{0}]", res);
            Console.Read();
            return;
        }
        if (deviceList.nDevNum < 1) {
            Console.WriteLine("No device find. ErrorCode:[{0}]", res);
            Console.Read();
            return;
        }
    }
}