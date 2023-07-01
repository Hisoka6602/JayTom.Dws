using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Device.Camera._3DCamera.Percipio;

namespace JayTom.Dws.Device.Camera._3DCamera {

    public class Percipio3DCamera2 : PercipioAppCenter, I3DCamera {
        public string DeviceCode { get; private set; } = string.Empty;
        public DeviceStatus Status { get; private set; } = DeviceStatus.Uninitialized;

        public Task<KeyValuePair<bool, string>> Reconnect() {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> Connect<T>(T connectParam) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> Initialization() {
            var argv = new IntPtr[1];
            argv[0] = PercipioAppUtils.StringToByteArray(".");
            var tyAppInit = TYAppInit(1, argv);
            Console.WriteLine(tyAppInit);

            throw new NotImplementedException();
        }

        public event EventHandler<IDevice>? Initialized;

        public event EventHandler<IDevice>? Connected;

        public event EventHandler<IDevice>? Disconnected;

        public event EventHandler<IDevice>? Reconnected;

        public event EventHandler<Exception>? Excepted;

        public string CameraName { get; private set; } = string.Empty;
        public string CameraId { get; private set; } = string.Empty;
        public float Framerate { get; private set; }
        public int DetectionBorderSize { get; set; }
        public Color DetectionBorderColor { get; set; }
        public bool IsShowDetectionBorder { get; set; }
        public bool IsUseImageWatermark { get; set; }

        public event EventHandler<Bitmap>? RealtimeImageEvent;

        public event EventHandler<VolumeCapturedEventArgs>? VolumeCapturedEvent;

        public event EventHandler<Bitmap>? LiveMappingEvent;

        public event EventHandler<string>? DeviceWarning;

        public KeyValuePair<bool, string> Pause() {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> Resume() {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> SetConfiguration<T>(T configData) {
            throw new NotImplementedException();
        }
    }
}