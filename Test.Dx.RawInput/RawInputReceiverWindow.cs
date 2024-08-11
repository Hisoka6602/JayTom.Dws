using Linearstar.Windows.RawInput;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Plugin.Device.KeyboardDevice {
    internal sealed class RawInputReceiverWindow : IDisposable {
        private const int WM_INPUT = 0x00FF;
        public IntPtr Handle;
        private WndProcDelegate _wndProcDelegate;

        private static RawInputReceiverWindow? _instance;
        private static readonly object _lock = new();

        private static bool _isMessageLoopRunning;

        // 定义 WNDPROC 委托
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public event EventHandler<RawInputEventArgs>? Input;

        public static CancellationTokenSource CancellationToken = new();

        // 私有构造函数，防止外部实例化
        private RawInputReceiverWindow() {
            _wndProcDelegate = WindowProcedure;
            Handle = CreateHiddenWindow();
        }

        // 单例实例的公开访问点
        public static RawInputReceiverWindow Instance {
            get {
                lock (_lock) {
                    return _instance ??= new RawInputReceiverWindow();
                }
            }
        }

        private IntPtr CreateHiddenWindow() {
            const uint WS_POPUP = 0x80000000;
            const uint WS_EX_TOOLWINDOW = 0x00000080;
            const uint WS_EX_APPWINDOW = 0x00040000;

            var wc = new WNDCLASS {
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                lpszClassName = "RawInputClass",
                hInstance = GetModuleHandle(null)
            };

            if (!RegisterClass(ref wc)) {
                return IntPtr.Zero;
            }

            var hwnd = CreateWindowEx(WS_EX_TOOLWINDOW | WS_EX_APPWINDOW, "RawInputClass", "RawInput", WS_POPUP, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, wc.hInstance, IntPtr.Zero);
            if (hwnd != IntPtr.Zero) {
                SetWindowLong(hwnd, -4, wc.lpfnWndProc);
            }

            return hwnd;
        }

        public static void MessageLoop() {
            lock (_lock) {
                if (_isMessageLoopRunning) {
                    return;
                }
                _isMessageLoopRunning = true;
            }
            CancellationToken = new();
            try {
                while (!CancellationToken.Token.IsCancellationRequested) {
                    if (PeekMessage(out var msg, IntPtr.Zero, 0, 0, 0)) {
                        GetMessage(out msg, IntPtr.Zero, 0, 0);
                        TranslateMessage(ref msg);
                        DispatchMessage(ref msg);
                    }
                    else {
                        // 你可以在这里插入一些非阻塞的代码，比如记录日志，或者做其他处理
                    }
                }
            }
            catch (Exception e) {
                //NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            finally {
                _isMessageLoopRunning = false;
            }
        }

        private IntPtr WindowProcedure(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam) {
            if (msg == WM_INPUT) {
                var data = RawInputData.FromHandle(lParam);
                Input?.Invoke(this, new RawInputEventArgs(data));
            }

            return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterClass(ref WNDCLASS lpWndClass);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct WNDCLASS {
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG {
            public IntPtr hwnd;
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

        [DllImport("user32.dll")]
        private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        public async void Dispose() {
            if (_instance == null) return;

            CancellationToken.Cancel();
            await Task.Delay(100);

            if (Handle != IntPtr.Zero) {
                DestroyWindow(Handle);
                Handle = IntPtr.Zero;
            }

            _instance = null;
            GC.SuppressFinalize(this);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);
    }

    public class RawInputEventArgs : EventArgs {

        public RawInputEventArgs(RawInputData data) {
            Data = data;
        }

        public RawInputData Data { get; }
    }
}