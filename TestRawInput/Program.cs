using System;
using System.Runtime.InteropServices;

internal class Program {
    private static IntPtr _hwnd;
    private const int WM_INPUT = 0x00FF;
    private const uint RIDEV_INPUTSINK = 0x00000100;
    private const uint RID_INPUT = 0x10000003;
    private const uint RIM_TYPEKEYBOARD = 1;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

    private const int GWL_WNDPROC = -4;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static WndProcDelegate _wndProcDelegate = WndProc;
    private static IntPtr _wndProc;

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUT {
        public RAWINPUTHEADER header;
        public RAWKEYBOARD data;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWKEYBOARD {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG {
        public IntPtr hWnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct WNDCLASS {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public IntPtr lpszMenuName;
        public string lpszClassName;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private static IntPtr _targetDeviceHandle;

    private static void Main() {
        uint deviceCount = 0;
        uint dwSize = (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));
        GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, dwSize);
        // Register the hidden window

        if (deviceCount > 0) {
            IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
            try {
                uint result = GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, dwSize);
                if (result == uint.MaxValue) {
                    int errorCode = Marshal.GetLastWin32Error();
                    Console.WriteLine($"GetRawInputDeviceList failed with error code: {errorCode}");
                    return;
                }

                for (int i = 0; i < deviceCount; i++) {
                    RAWINPUTDEVICELIST ridl =
                        Marshal.PtrToStructure<RAWINPUTDEVICELIST>((IntPtr)((long)pRawInputDeviceList + (i * dwSize)));
                    if (ridl.dwType == RIM_TYPEKEYBOARD) {
                        // 获取设备名称的大小
                        uint pcbSize = 0;
                        result = GetRawInputDeviceInfo(ridl.hDevice, RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);
                        if (result == uint.MaxValue) {
                            int errorCode = Marshal.GetLastWin32Error();
                            Console.WriteLine($"GetRawInputDeviceInfo (size) failed with error code: {errorCode}");
                            continue;
                        }

                        if (pcbSize > 0) {
                            IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
                            try {
                                result = GetRawInputDeviceInfo(ridl.hDevice, RIDI_DEVICENAME, pData, ref pcbSize);
                                if (result == uint.MaxValue) {
                                    int errorCode = Marshal.GetLastWin32Error();
                                    Console.WriteLine(
                                        $"GetRawInputDeviceInfo (data) failed with error code: {errorCode}");
                                    continue;
                                }

                                string deviceName = Marshal.PtrToStringAuto(pData);

                                // 在这里输出设备信息
                                Console.WriteLine($"Device Handle: {ridl.hDevice}");
                                Console.WriteLine($"Device Name: {deviceName}");

                                // 假设你找到了目标设备
                                // 可以在此处调用 SetTargetDeviceHandle 设置目标设备句柄
                                // SetTargetDeviceHandle(ridl.hDevice);
                            }
                            finally {
                                Marshal.FreeHGlobal(pData);
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
            finally {
                Marshal.FreeHGlobal(pRawInputDeviceList);
            }
        }
        else {
            Console.WriteLine("No devices found.");
        }

        CreateHiddenWindow();
        RegisterRawInputDevices();

        // Message loop
        MSG msg;
        while (true) {
            while (PeekMessage(out msg, IntPtr.Zero, 0, 0, 1)) {
                if (msg.message == WM_INPUT) {
                    ProcessRawInput(msg.lParam);
                }
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
    }

    private static void CreateHiddenWindow() {
        const uint WS_POPUP = 0x80000000;
        const uint WS_EX_TOOLWINDOW = 0x00000080;
        const uint WS_EX_APPWINDOW = 0x00040000;

        // Define a window class
        WNDCLASS wc = new WNDCLASS();
        wc.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
        wc.lpszClassName = "RawInputClass";
        wc.hInstance = GetModuleHandle(null); // 获取当前应用程序的实例句柄

        if (!RegisterClass(ref wc)) {
            throw new Exception("Failed to register window class.");
        }

        // Create the window
        _hwnd = CreateWindowEx(WS_EX_TOOLWINDOW | WS_EX_APPWINDOW, "RawInputClass", "RawInput", WS_POPUP, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, wc.hInstance, IntPtr.Zero);
        if (_hwnd == IntPtr.Zero) {
            throw new Exception("Failed to create window.");
        }

        // Set the window procedure
        _wndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
        SetWindowLong(_hwnd, GWL_WNDPROC, _wndProc);
    }

    private static void RegisterRawInputDevices() {
        RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
        rid[0].UsagePage = 0x01; // Generic desktop controls
        rid[0].Usage = 0x06;     // Keyboard
        rid[0].Flags = RIDEV_INPUTSINK; // Capture input even when application is in the background
        rid[0].Target = _hwnd; // 指定窗口句柄

        if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0]))) {
            throw new Exception("Failed to register raw input devices.");
        }
    }

    private static void ProcessRawInput(IntPtr lParam) {
        uint dwSize = 0;
        GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

        if (dwSize > 0) {
            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try {
                if (GetRawInputData(lParam, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == dwSize) {
                    RAWINPUT rawInput = Marshal.PtrToStructure<RAWINPUT>(buffer);
                    if (rawInput.header.dwType == RIM_TYPEKEYBOARD) {
                        if (_targetDeviceHandle != IntPtr.Zero && rawInput.header.hDevice == _targetDeviceHandle) {
                            Console.WriteLine($"Key: {rawInput.data.VKey}");
                        }
                    }
                }
            }
            finally {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) {
        if (msg == WM_INPUT) {
            ProcessRawInput(lParam);
        }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool RegisterClass(ref WNDCLASS lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int RIDI_DEVICENAME = 0x20000007;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICELIST {
        public IntPtr hDevice;
        public uint dwType;
    }

    private static void SetTargetDeviceHandle(IntPtr deviceHandle) {
        _targetDeviceHandle = deviceHandle;
    }
}