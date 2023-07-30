using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Client.Models.Cameras {

    public class BarcodeScannerCameraItemInfoModel : BaseCameraItemInfoModel {
        private bool _isShowRealTimeImage;

        /// <summary>
        /// 是否显示实时图像
        /// </summary>
        public bool IsShowRealTimeImage {
            get => _isShowRealTimeImage;
            set => SetProperty(ref _isShowRealTimeImage, value);
        }
    }
}