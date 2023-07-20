using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;

namespace JayTom.Dws.TemporaryClient.Service {

    public interface IBarcodeScannerService {

        //扫到条码事件
        event EventHandler<ScanCompletedEventArgs> ScanCompleted;

        void OnScanCompleted(ScanCompletedEventArgs e);
    }

    public class ScanCompletedEventArgs : EventArgs {
        public long TimestampedGuid { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public float Weight { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public DateTime ScanTime { get; set; }
        public UploadStatus RequestStatus { get; set; }
        public DateTime RequestTime { get; set; }
        public string RequestContent { get; set; } = string.Empty;
        public DateTime ResponseTime { get; set; }
        public string ResponseContent { get; set; } = string.Empty;
    }
}