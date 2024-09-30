using System.Management;

namespace JayTom.Dws.Plugin.UsbDevice {

    public class UsbManager {
        private static readonly Lazy<UsbManager> _instance = new(() => new UsbManager());
        private ManagementEventWatcher _insertWatcher;
        private ManagementEventWatcher _removeWatcher;
        private DateTime _lastTriggerTime = DateTime.MinValue;

        // 定义事件
        public event EventHandler<EventArgs>? UsbDeviceInserted;

        public event EventHandler<EventArgs>? UsbDeviceRemoved;

        private UsbManager() {
            // 监控 USB 插入事件
            var insertQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
            _insertWatcher = new ManagementEventWatcher(insertQuery);
            _insertWatcher.EventArrived += DeviceInserted;
            _insertWatcher.Start();

            // 监控 USB 移除事件
            var removeQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");
            _removeWatcher = new ManagementEventWatcher(removeQuery);
            _removeWatcher.EventArrived += DeviceRemoved;
            _removeWatcher.Start();
        }

        public static UsbManager Instance => _instance.Value;

        private void DeviceInserted(object sender, EventArrivedEventArgs e) {
            var currentTime = DateTime.Now;
            if ((currentTime - _lastTriggerTime).TotalMilliseconds < 500) {
                return; // 在 500ms 内不处理
            }
            _lastTriggerTime = currentTime;
            UsbDeviceInserted?.Invoke(this, EventArgs.Empty);
        }

        private void DeviceRemoved(object sender, EventArrivedEventArgs e) {
            var currentTime = DateTime.Now;
            if ((currentTime - _lastTriggerTime).TotalMilliseconds < 500) {
                return; // 在 500ms 内不处理
            }
            _lastTriggerTime = currentTime;
            UsbDeviceRemoved?.Invoke(this, EventArgs.Empty);
        }

        public void StopMonitoring() {
            _insertWatcher?.Stop();
            _removeWatcher?.Stop();
        }
    }
}