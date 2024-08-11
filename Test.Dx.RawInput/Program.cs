using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Linearstar.Windows.RawInput;
using System.Runtime.InteropServices;
using Linearstar.Windows.RawInput.Native;
using static System.Net.Mime.MediaTypeNames;
using JayTom.Dws.Plugin.Device.KeyboardDevice;

internal class Program {

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder receivingBuffer, int bufferSize, uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    // Required external methods and constants
    private const int VK_CAPITAL = 0x14;

    private static List<string> _keyList = new();
    private static DateTime firstKeyTime = DateTime.Now;

    private static void Main(string[] args) {
        // Get the devices that can be handled with Raw Input.
        var devices = RawInputDevice.GetDevices();
        // Keyboards will be returned as a RawInputKeyboard.
        var keyboards = devices.OfType<RawInputKeyboard>();

        // List them up.
        foreach (var device in keyboards) {
            /*Console.WriteLine(
                $"{device.DeviceType} {device.VendorId:X4}:{device.ProductId:X4} {device.ProductName}, {device.ManufacturerName}");*/

            Console.WriteLine(JsonConvert.SerializeObject(device));
        }

        // To begin catching inputs, first make a window that listens WM_INPUT.
        // 创建 RawInputReceiverWindow 实例
        Task.Run(() => {
            try {
                var window = RawInputReceiverWindow.Instance;

                window.Input += (sender, e) => {
                    // 处理输入数据
                    var data = e.Data;
                    if (data is RawInputKeyboardData { Keyboard.Flags: RawKeyboardFlags.None } keyboardData &&
                        keyboardData.Device?.ProductName?.Contains("USB HID Keyboard",
                            StringComparison.CurrentCultureIgnoreCase) == true) {
                        var keyString = VirtualKeyToString(keyboardData.Keyboard.VirutalKey);
                        //Console.WriteLine($"VirtualKey转换后的字符是: {keyString}");
                        if (!string.IsNullOrEmpty(keyString)) {
                            //全大写
                            AddKeyToList(keyString.ToUpper());
                        }

                        if (keyboardData.Keyboard.VirutalKey == 13 && _keyList.Any()) {
                            Console.WriteLine(string.Join(string.Empty, _keyList));
                            _keyList.Clear();
                        }
                    }
                };
                // 注册设备
                RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard,
                    RawInputDeviceFlags.ExInputSink | RawInputDeviceFlags.NoLegacy, window.Handle);

                // 运行消息循环
                RawInputReceiverWindow.MessageLoop();
            }
            catch (Exception e) {
                Console.WriteLine($"Exception: {e.Message}");
            }
        });
        /*var messageLoopThread = new Thread(() => {
        });
        messageLoopThread.SetApartmentState(ApartmentState.STA);
        messageLoopThread.Start();*/

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }

    private static void AddKeyToList(string key) {
        // 如果这是第一个键，记录时间
        if (_keyList.Count == 0) {
            firstKeyTime = DateTime.Now;
        }

        // 检查是否超过1秒
        if ((DateTime.Now - firstKeyTime).TotalSeconds > 1) {
            _keyList.Clear();
        }

        _keyList.Add(key);
    }

    private static string VirtualKeyToString(int virtualKey) {
        uint scanCode = MapVirtualKey((uint)virtualKey, 0);
        byte[] keyboardState = new byte[256];
        GetKeyboardState(keyboardState);

        StringBuilder buffer = new StringBuilder(256);
        int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, buffer, buffer.Capacity, 0);

        return result > 0 ? buffer.ToString() : string.Empty;
    }

    private static string ScanCodeToString(int scanCode) {
        uint virtualKeyCode = MapVirtualKey((uint)scanCode, 1);
        byte[] keyboardState = new byte[256];
        GetKeyboardState(keyboardState);

        StringBuilder buffer = new StringBuilder(256);
        int result = ToUnicode(virtualKeyCode, (uint)scanCode, keyboardState, buffer, buffer.Capacity, 0);

        return result > 0 ? buffer.ToString() : string.Empty;
    }
}