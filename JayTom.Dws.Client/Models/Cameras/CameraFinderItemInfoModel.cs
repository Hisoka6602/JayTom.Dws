using JayTom.Dws.Camera;

namespace JayTom.Dws.Client.Models.Cameras {

    public class CameraFinderItemInfoModel : BaseCameraItemInfoModel {
        private bool _hasBinding;
        private CameraBindingType _boundType;
        private bool _isOcrSupported;

        /// <summary>
        /// 是否已被选择
        /// </summary>
        public bool HasBinding {
            get => _hasBinding;
            set => SetProperty(ref _hasBinding, value);
        }

        /// <summary>
        /// 已绑定相机
        /// </summary>
        public CameraBindingType BoundType {
            get => _boundType;
            set => SetProperty(ref _boundType, value);
        }

        /// <summary>
        /// 是否支持Ocr算法
        /// </summary>
        public bool IsOcrSupported {
            get => _isOcrSupported;
            set => SetProperty(ref _isOcrSupported, value);
        }
    }

    /*public enum BoundCameraType {
        /// <summary>
        /// 全景相机
        /// </summary>

        PanoramaCamera,

        /// <summary>
        /// 扫码相机
        /// </summary>
        BarcodeScannerCamera,

        /// <summary>
        /// 体积相机
        /// </summary>
        VolumeCamera,
        /// <summary>
        /// Ocr相机(算法)
        /// </summary>
        OcrCamera,
    }*/
}