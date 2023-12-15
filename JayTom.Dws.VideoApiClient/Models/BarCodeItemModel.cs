using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JayTom.Dws.VideoApiClient.Models {

    public class BarCodeItemModel : BindableBase {
        private int _num;
        private string? _barCode;
        private string? _nodeName;
        private DateTime _scanTime;
        private string? _cameraSerialNumber;
        private string? _cameraCustomName;
        private string? _scanImageUrl;
        private bool _scanImageVisible;
        private ObservableCollection<PanoramaImageItemModel> _panoramaImageItems = new();

        public int Num {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        /// <summary>
        /// 条码
        /// </summary>
        public string? BarCode {
            get => _barCode;
            set => SetProperty(ref _barCode, value);
        }

        /// <summary>
        /// 节点名称
        /// </summary>
        public string? NodeName {
            get => _nodeName;
            set => SetProperty(ref _nodeName, value);
        }

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime {
            get => _scanTime;
            set => SetProperty(ref _scanTime, value);
        }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string? CameraSerialNumber {
            get => _cameraSerialNumber;
            set => SetProperty(ref _cameraSerialNumber, value);
        }

        /// <summary>
        /// 相机自定义名称
        /// </summary>
        public string? CameraCustomName {
            get => _cameraCustomName;
            set => SetProperty(ref _cameraCustomName, value);
        }

        /// <summary>
        /// 扫码图片地址
        /// </summary>
        public string? ScanImageUrl {
            get => _scanImageUrl;
            set => SetProperty(ref _scanImageUrl, value);
        }

        /// <summary>
        /// 是否显示扫码图片
        /// </summary>
        public bool ScanImageVisible {
            get => _scanImageVisible;
            set => SetProperty(ref _scanImageVisible, value);
        }

        /// <summary>
        /// 全景图
        /// </summary>
        public ObservableCollection<PanoramaImageItemModel> PanoramaImageItems {
            get => _panoramaImageItems;
            set => SetProperty(ref _panoramaImageItems, value);
        }
    }
}