using System;
using HidSharp;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Device.BarcodeScannerDevice {

    public interface IBarcodeScannerDevice : IDisposable {

        public Task<List<HidDevice>> GetListHidDevices();

        public Task<bool> StartListening(HidDevice hidDevice, Action<string> onDataReceived);

        public void StopListening();
    }
}