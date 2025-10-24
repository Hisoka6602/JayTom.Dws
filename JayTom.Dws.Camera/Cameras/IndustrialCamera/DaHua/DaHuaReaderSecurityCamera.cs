using System;
using System.Linq;
using System.Text;
using System.Drawing;
using JayTom.Dws.Ocr;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Camera.Attributes.CameraAttributes;

namespace JayTom.Dws.Camera.Cameras.IndustrialCamera.DaHua {

    /// <summary>
    /// 大华读码安防相机
    /// </summary>
    public class DaHuaReaderSecurityCamera : IIndustrialCamera {

        public void Dispose() {
            throw new NotImplementedException();
        }

        public CameraInfo? Info { get; }
        public SdkType SdkType => SdkType.IndustrialCameraSdk;
        public string SdkName { get; }
        public bool IsOriginalImageOut { get; set; }
        public CameraStatus Status { get; }
        public CameraBindingType BindingType { get; set; }

        // public CameraBindingType SupportedBindingType =>
        //     CameraBindingType.PanoramaCamera | CameraBindingType.OcrCamera;

        public Task<List<CameraInfo>?> EnumerateCameras() {
            throw new NotImplementedException();
        }

        public event EventHandler<CameraExceptionEventArgs>? CameraExceptionOccurred;

        public event EventHandler<CameraConnectionEventArgs>? CameraDisconnected;

        public event EventHandler<CameraInitializedEventArgs>? CameraInitialized;

        public event EventHandler<CameraStartedEventArgs>? CameraStarted;

        public event EventHandler<CameraStoppedEventArgs>? CameraStopped;

        public event EventHandler<CameraUnregisteredEventArgs>? CameraUnregistered;

        public event EventHandler<RealtimeImageEventArgs>? RealtimeImage;

        public Task<KeyValuePair<bool, string>> Initialize(object param) {
            throw new NotImplementedException();
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
        public IOcr? Ocr { get; set; }
        public int BarcodeBorderSize { get; set; }
        public Color BarcodeBorderColor { get; set; }
        public bool IsShowBarcodeBorder { get; set; }

        public event EventHandler<BarcodeReadEventArgs>? BarcodeRead;

        public event EventHandler<OcrResult>? OcrContentRecognized;

        public event EventHandler<BarcodeReadEventArgs>? FilteredBarcodeReturned;

        public void SetScanCodeFilterParams([NotNull] ScanCodeFilterParams @params) {
            throw new NotImplementedException();
        }
    }
}