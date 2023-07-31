using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.Cameras {

    public class CameraFinderItemInfoModel : BaseCameraItemInfoModel {
        private bool _hasBinding;
        private BoundCameraType _boundType;

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
        public BoundCameraType BoundType {
            get => _boundType;
            set => SetProperty(ref _boundType, value);
        }
    }

    public enum BoundCameraType {
        /// <summary>
        /// 全景相机
        /// </summary>

        PanoramicCamera,

        /// <summary>
        /// 扫码相机
        /// </summary>
        BarcodeScannerCamera,

        /// <summary>
        /// 体积相机
        /// </summary>
        VolumeCamera
    }
}