namespace JayTom.Dws.Client.Models.Cameras {

    public class PanoramaCameraItemInfoModel : BaseCameraItemInfoModel {
        private int _captureDelayTime;

        /// <summary>
        /// 延迟时间拍照时间（单位：秒）
        /// </summary>
        public int CaptureDelayTime {
            get => _captureDelayTime;
            set => SetProperty(ref _captureDelayTime, value);
        }
    }
}