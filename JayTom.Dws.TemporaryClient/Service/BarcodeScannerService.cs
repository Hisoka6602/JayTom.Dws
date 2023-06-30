using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.TemporaryClient.Service {

    public class BarcodeScannerService : IBarcodeScannerService {

        public event EventHandler<ScanCompletedEventArgs>? ScanCompleted;

        public virtual async void OnScanCompleted(ScanCompletedEventArgs e) {
            await Task.Yield();
            ScanCompleted?.Invoke(this, e);
        }
    }
}