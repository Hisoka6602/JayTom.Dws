using System;
using HidSharp;
using System.IO;
using System.Linq;
using System.Text;
using HidSharp.Reports;
using System.Threading.Tasks;
using HidSharp.Reports.Input;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Device.BarcodeScannerDevice {

    public class BarcodeScannerDevice : IBarcodeScannerDevice {
        private Action<string> _onDataReceived;
        private Task _listenTask;
        private bool _isListening;
        private byte[] inputReportBuffer = new byte[1024];
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private HidDeviceInputReceiver hidDeviceInputReceiver;
        private HidStream _hidStream;

        public async Task<List<HidDevice>> GetListHidDevices() {
            await Task.Yield();

            return DeviceList.Local.GetHidDevices().ToList();
        }

        public async Task<bool> StartListening(HidDevice barcodeScanner, Action<string> onDataReceived) {
            // 确保设备是有效的

            if (barcodeScanner != null) {
                // 打开设备

                // 打开设备
                _hidStream = barcodeScanner.Open();
                Console.WriteLine("Device opened.");

                var reportDescriptor = barcodeScanner.GetReportDescriptor();

                var reportDescriptorReports = reportDescriptor.Reports;

                hidDeviceInputReceiver = reportDescriptor.CreateHidDeviceInputReceiver();
                var firstOrDefault = reportDescriptor.OutputReports.FirstOrDefault();
                if (firstOrDefault is not null) {
                    inputReportBuffer = new byte[firstOrDefault.Length];
                    hidDeviceInputReceiver.Received += HidDeviceInputReceiver_Received;
                    hidDeviceInputReceiver.Started += HidDeviceInputReceiverOnStarted;
                    hidDeviceInputReceiver.Stopped += HidDeviceInputReceiverOnStopped;
                    hidDeviceInputReceiver.Start(_hidStream);
                }
            }
            else {
                Console.WriteLine("Barcode scanner not found.");
            }

            return false;
        }

        private void HidDeviceInputReceiverOnStopped(object? sender, EventArgs e) {
            Console.WriteLine(11);
        }

        private void HidDeviceInputReceiverOnStarted(object? sender, EventArgs e) {
            Console.WriteLine(11);
        }

        private void HidDeviceInputReceiver_Received(object? sender, EventArgs e) {
            hidDeviceInputReceiver.TryRead(inputReportBuffer, 0, out var report);
        }

        /*private void ListenForData() {
            while ((ec = reader.Read(readBuffer, 5000, out int bytesRead)) == ErrorCode.None) {
                if (bytesRead > 0) {
                    // 将读取的数据转换为字符串（根据扫描枪的具体编码方式进行处理）
                    string barcode = System.Text.Encoding.ASCII.GetString(readBuffer, 0, bytesRead);
                    _onDataReceived?.Invoke(barcode);
                }
                Task.Delay(100).Wait();
            }
        }*/

        public void StopListening() {
            _isListening = false;
            cancellationTokenSource.Cancel();
            //_device?.Dispose();
        }

        private void DeviceInsertedHandler() {
            // Handle device inserted event if necessary
        }

        private void DeviceRemovedHandler() {
            // Handle device removed event if necessary
            StopListening();
        }

        public void Dispose() {
            StopListening();
        }
    }
}