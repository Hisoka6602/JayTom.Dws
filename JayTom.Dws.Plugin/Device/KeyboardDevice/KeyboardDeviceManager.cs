using System;
using System.Linq;
using System.Text;
using MathNet.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Linearstar.Windows.RawInput;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Linearstar.Windows.RawInput.Native;

namespace JayTom.Dws.Plugin.Device.KeyboardDevice {

    public class KeyboardDeviceManager : IKeyboardDeviceManager {

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int ToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder receivingBuffer, int bufferSize, uint flags);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private const int VK_CAPITAL = 0x14;

        private static List<string> _keyList = new();
        private static DateTime _firstKeyTime = DateTime.Now;
        private static RawInputReceiverWindow? _window;
        private static string _regexPattern = string.Empty;
        private static List<KeyboardDevice> _keyboardDevices = new();

        public void Dispose() {
            StopListening();
        }

        public event EventHandler<KeyboardBarCodeReceivedEventArgs>? BarCodeReceived;

        public event EventHandler<KeyboardRealTimeKeyEventArgs>? RealTimeKeyReceived;

        public bool IsListening { get; private set; }
        public KeyboardDevice ListeningDevice { get; private set; } = new();

        public async Task<List<KeyboardDevice>> EnumerateKeyboardDevices() {
            await Task.Yield();
            var devices = RawInputDevice.GetDevices();

            _keyboardDevices = devices?.OfType<RawInputKeyboard>()?.Select(s => new KeyboardDevice {
                ProductId = s.ProductId,
                VendorId = s.VendorId,
                DeviceName = s.ProductName,
                DevicePath = s.DevicePath,
                IsConnected = s.IsConnected,
                ManufacturerName = s.ManufacturerName
            })?.ToList() ?? new List<KeyboardDevice>();
            return _keyboardDevices;
        }

        public async Task<bool> StartListening(KeyboardDevice device) {
            await Task.Yield();

            if (IsListening || _keyboardDevices.Any(a => a.ProductId.Equals(device.ProductId) &&
                                                      a.VendorId.Equals(device.VendorId) &&
                                                      a.DeviceName?.Equals(device.DeviceName) == true) != true) {
                return false;
            }
            IsListening = true;
            ListeningDevice = device;

            Task.Run(() => {
                try {
                    _window = RawInputReceiverWindow.Instance;

                    _window.Input += (sender, e) => {
                        // 处理输入数据

                        if (e.Data is RawInputKeyboardData { Keyboard.Flags: RawKeyboardFlags.None } keyboardData && e.Data.Device?.ProductId.Equals(ListeningDevice.ProductId) == true &&
                            e.Data.Device?.VendorId.Equals(ListeningDevice.VendorId) == true &&
                            e.Data.Device?.ProductName?.Equals(ListeningDevice.DeviceName) == true) {
                            var keyString = VirtualKeyToString(keyboardData.Keyboard.VirutalKey);
                            //全大写
                            var upper = keyString.Replace("\n", "").Replace("\r", "").ToUpper();
                            if (!string.IsNullOrEmpty(upper)) {
                                AddKeyToList(upper);
                            }

                            if (keyboardData.Keyboard.VirutalKey == 13 && _keyList.Any()) {
                                //过滤
                                var data = string.Join(string.Empty, _keyList);
                                if (string.IsNullOrEmpty(_regexPattern) || Regex.IsMatch(data, _regexPattern)) {
                                    OnBarCodeReceived(new KeyboardBarCodeReceivedEventArgs() {
                                        Barcode = data,
                                        Device = ListeningDevice,
                                        ScanTime = DateTime.Now,
                                        Timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds()
                                    });
                                }

                                _keyList.Clear();
                            }
                            OnRealTimeKeyReceived(new KeyboardRealTimeKeyEventArgs() {
                                Data = upper,
                                Device = ListeningDevice,
                                ScanTime = DateTime.Now,
                                ScanCode = keyboardData.Keyboard.ScanCode,
                                VirutalKey = keyboardData.Keyboard.VirutalKey
                            });
                        }
                    };
                    RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard,
                        RawInputDeviceFlags.ExInputSink | RawInputDeviceFlags.NoLegacy, _window.Handle);
                    _window.MessageLoop();
                }
                catch (Exception e) {
                    IsListening = false;
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            });
            return true;
        }

        public void StopListening() {
            _window?.Dispose();
            RawInputDevice.UnregisterDevice(HidUsageAndPage.Keyboard);
            IsListening = false;
        }

        public void SetFilterRule(string regexPattern) {
            _regexPattern = regexPattern;
        }

        private static void AddKeyToList(string key) {
            // 如果这是第一个键，记录时间
            if (_keyList.Count == 0) {
                _firstKeyTime = DateTime.Now;
            }

            // 检查是否超过1秒
            if ((DateTime.Now - _firstKeyTime).TotalSeconds > 1) {
                _keyList.Clear();
                _firstKeyTime = DateTime.Now;
            }

            _keyList.Add(key);
        }

        private static string VirtualKeyToString(int virtualKey) {
            try {
                var scanCode = MapVirtualKey((uint)virtualKey, 0);
                var keyboardState = new byte[256];
                GetKeyboardState(keyboardState);

                var buffer = new StringBuilder(256);
                var result = ToUnicode((uint)virtualKey, scanCode, keyboardState, buffer, buffer.Capacity, 0);

                return result > 0 ? buffer.ToString() : string.Empty;
            }
            catch (Exception e) {
                return string.Empty;
            }
        }

        protected virtual void OnBarCodeReceived(KeyboardBarCodeReceivedEventArgs e) {
            BarCodeReceived?.Invoke(this, e);
        }

        protected virtual void OnRealTimeKeyReceived(KeyboardRealTimeKeyEventArgs e) {
            RealTimeKeyReceived?.Invoke(this, e);
        }
    }
}