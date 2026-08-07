using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.CloudSettingModel
{

    public class NvrCameraBindingItemInfoModel : BindableBase
    {
        private bool _isBinding;
        private string _customCameraName = string.Empty;
        private string _cameraSerialNumber = string.Empty;
        private int _num;

        /// <summary>
        /// 是否已绑定
        /// </summary>
        public bool IsBinding
        {
            get => _isBinding;
            set => SetProperty(ref _isBinding, value);
        }

        /// <summary>
        /// 序号
        /// </summary>
        public int Num
        {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        /// <summary>
        /// 相机自定义名称
        /// </summary>
        public string CustomCameraName
        {
            get => _customCameraName;
            set => SetProperty(ref _customCameraName, value);
        }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber
        {
            get => _cameraSerialNumber;
            set => SetProperty(ref _cameraSerialNumber, value);
        }
    }
}