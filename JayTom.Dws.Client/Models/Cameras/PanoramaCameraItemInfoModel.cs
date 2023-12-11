namespace JayTom.Dws.Client.Models.Cameras {

    public class PanoramaCameraItemInfoModel : BaseCameraItemInfoModel {
        private int _captureDelayTime;
        private string _selectedCameraSerialNumber = string.Empty;

        /// <summary>
        /// 延迟时间拍照时间（单位：秒）
        /// </summary>
        public int CaptureDelayTime {
            get => _captureDelayTime;
            set => SetProperty(ref _captureDelayTime, value);
        }

        /// <summary>
        /// 指定的拍照相机
        /// </summary>
        public string SelectedCameraSerialNumber {
            get => _selectedCameraSerialNumber;
            set => SetProperty(ref _selectedCameraSerialNumber, value);
        }
    }
}