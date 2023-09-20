using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Camera.Cameras.SmartCamera.Wayzim {

    public class WayzimSmartCamera : ISmartCamera {

        public void Dispose() {
            throw new NotImplementedException();
        }

        public CameraInfo? Info { get; private set; }
        public SdkType SdkType { get; private set; }
        public string SdkName => "GWCamera.dll";
        public bool IsOriginalImageOut { get; set; }
        public CameraStatus Status { get; private set; } = CameraStatus.Uninitialized;
        public CameraBindingType BindingType { get; set; } = CameraBindingType.ScannerCamera;

        public async Task<List<CameraInfo>?> EnumerateCameras() {
            var allCameraNames = GWCamera.CameraBLL.Instance.GetAllCameraNames();
            Console.WriteLine(JsonConvert.SerializeObject(allCameraNames));
            return new List<CameraInfo>();
        }

        public event EventHandler<CameraExceptionEventArgs>? CameraExceptionOccurred;

        public event EventHandler<CameraConnectionEventArgs>? CameraDisconnected;

        public event EventHandler<CameraInitializedEventArgs>? CameraInitialized;

        public event EventHandler<CameraStartedEventArgs>? CameraStarted;

        public event EventHandler<CameraStoppedEventArgs>? CameraStopped;

        public event EventHandler<CameraUnregisteredEventArgs>? CameraUnregistered;

        public event EventHandler<RealtimeImageEventArgs>? RealtimeImage;

        public async Task<KeyValuePair<bool, string>> Initialize(object param) {
            var initCamera = GWCamera.CameraBLL.Instance.InitCamera(false);
            Console.WriteLine(initCamera);
            return new KeyValuePair<bool, string>();
        }

        public Task<KeyValuePair<bool, string>> Start(object param) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> Stop() {
            throw new NotImplementedException();
        }

        public void SetParameters(Dictionary<string, object> parameters) {
            throw new NotImplementedException();
        }

        public bool IsRealtimeImageEnabled { get; }

        public void StartRealTimeImage() {
            throw new NotImplementedException();
        }

        public void StopRealTimeImage() {
            throw new NotImplementedException();
        }

        public event EventHandler<PhotoTakenEventArgs>? PhotoTaken;

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default) {
            throw new NotImplementedException();
        }

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, TimeSpan delay, CancellationToken cancellation = default) {
            throw new NotImplementedException();
        }

        public int TakePhotoDelay { get; set; }
        public int BarcodeBorderSize { get; set; }
        public Color BarcodeBorderColor { get; set; }
        public bool IsShowBarcodeBorder { get; set; }
        public bool IsUseTriggerMode { get; set; }
        public TriggerMode TriggerMode { get; set; }

        public void SoftwareTriggerOnce() {
            throw new NotImplementedException();
        }

        public event EventHandler<BarcodeTriggeredEventArgs>? BarcodeReadTriggered;

        public event EventHandler<BarcodeReadEventArgs>? NotBarcodeHitEvent;

        public void SetScanCodeFilterParams(ScanCodeFilterParams @params) {
            throw new NotImplementedException();
        }
    }
}