using System;
using Prism.Mvvm;
using System.ComponentModel;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Excel.Attributes;

namespace JayTom.Dws.Client.Models.DataModels {

    public class PackageItemModel : BindableBase {
        private float _volume;
        private bool _isInserting;
        private bool _isRemoving;
        private int _num;
        private long _timestampedGuid;
        private string _barcode = string.Empty;
        private float _weight;
        private float _length;
        private float _width;
        private float _height;
        private DateTime _scanTime = DateTime.MinValue;
        private UploadStatus _requestStatus = UploadStatus.NotUploaded;
        private string? _barcodeImagePath = string.Empty;
        private bool _isBarcodeImageExists;
        private List<PanoramaImageItemModel> _panoramaImageItems = new();
        private VolumeItemModel _volumeInfo = new();
        private WeightItemModel _weightInfo = new();
        private UploadItemModel _uploadInfo = new();
        private SortingItemModel _sortingInfo = new();
        private OcrItemInfo _ocrInfo = new();
        private string _exitName = string.Empty;
        private bool _isUploadedToCloudVideo;
        private ExitInfoItemModel _exitInfo = new();
        private PackageExitStatus _packageExitStatus = PackageExitStatus.None;
        private List<NodeInfoItemModel> _nodeInfoItems = new();
        private long _packageId;

        [DisplayName("No."), ExcelInfo(Width = 3000)]
        public int Num {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        public long PackageId {
            get => _packageId;
            set => SetProperty(ref _packageId, value);
        }

        /// <summary>
        /// 时间戳Id
        /// </summary>
        [DisplayName("TimestampedGuid"), ExcelInfo(Width = 5000)]
        public long TimestampedGuid {
            get => _timestampedGuid;
            set => SetProperty(ref _timestampedGuid, value);
        }

        /// <summary>
        /// 条码
        /// </summary>
        [DisplayName("Barcode"), ExcelInfo(Width = 4000)]
        public string Barcode {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        /// <summary>
        /// 重量
        /// </summary>
        [DisplayName("Weight"), ExcelInfo(Width = 2000)]
        public float Weight {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        /// <summary>
        /// 长度
        /// </summary>
        [DisplayName("Length"), ExcelInfo(Width = 2000)]
        public float Length {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        /// <summary>
        /// 宽度
        /// </summary>
        [DisplayName("Width"), ExcelInfo(Width = 2000)]
        public float Width {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        /// <summary>
        /// 高度
        /// </summary>
        [DisplayName("Height"), ExcelInfo(Width = 2000)]
        public float Height {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        /// <summary>
        /// 体积
        /// </summary>
        [DisplayName("Volume"), ExcelInfo(Width = 2000)]
        public float Volume {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        /// <summary>
        /// 扫码时间
        /// </summary>
        [DisplayName("ScanTime"), ExcelInfo(Width = 4000)]
        public DateTime ScanTime {
            get => _scanTime;
            set => SetProperty(ref _scanTime, value);
        }

        /// <summary>
        /// 上传状态(1成功、2失败、0未上传)
        /// </summary>
        [DisplayName("RequestStatus"), ExcelInfo(Width = 4000)]
        public UploadStatus RequestStatus {
            get => _requestStatus;
            set => SetProperty(ref _requestStatus, value);
        }

        /// <summary>
        /// 格口名称
        /// </summary>
        [DisplayName("ExitName"), ExcelInfo(Width = 3000)]
        public string ExitName {
            get => _exitName;
            set => SetProperty(ref _exitName, value);
        }

        /// <summary>
        /// 条码图片保存路径
        /// </summary>
        [DisplayName("BarcodeImagePath"), ExcelInfo(Width = 8000)]
        public string? BarcodeImagePath {
            get => _barcodeImagePath;
            set => SetProperty(ref _barcodeImagePath, value);
        }

        /// <summary>
        /// 全景图信息
        /// </summary>
        public List<PanoramaImageItemModel> PanoramaImageItems {
            get => _panoramaImageItems;
            set => SetProperty(ref _panoramaImageItems, value);
        }

        /// <summary>
        /// 其他项
        /// </summary>
        [DisplayName("Other"), ExcelInfo(Width = 4000)]
        public string? Other { get; set; }

        /// <summary>
        /// 体积信息
        /// </summary>
        public VolumeItemModel VolumeInfo {
            get => _volumeInfo;
            set => SetProperty(ref _volumeInfo, value);
        }

        /// <summary>
        /// 重量信息
        /// </summary>
        public WeightItemModel WeightInfo {
            get => _weightInfo;
            set => SetProperty(ref _weightInfo, value);
        }

        /// <summary>
        /// 上传信息
        /// </summary>
        public UploadItemModel UploadInfo {
            get => _uploadInfo;
            set => SetProperty(ref _uploadInfo, value);
        }

        /// <summary>
        /// 分拣信息
        /// </summary>

        public SortingItemModel SortingInfo {
            get => _sortingInfo;
            set => SetProperty(ref _sortingInfo, value);
        }

        /// <summary>
        /// Ocr信息
        /// </summary>
        public OcrItemInfo OcrInfo {
            get => _ocrInfo;
            set => SetProperty(ref _ocrInfo, value);
        }

        /// <summary>
        /// 格口信息
        /// </summary>
        public ExitInfoItemModel ExitInfo {
            get => _exitInfo;
            set => SetProperty(ref _exitInfo, value);
        }

        /// <summary>
        /// 节点信息
        /// </summary>

        public List<NodeInfoItemModel> NodeInfoItems {
            get => _nodeInfoItems;
            set => SetProperty(ref _nodeInfoItems, value);
        }

        /// <summary>
        /// 是否插入
        /// </summary>
        public bool IsInserting {
            get => _isInserting;
            set => SetProperty(ref _isInserting, value);
        }

        /// <summary>
        /// 是否移除
        /// </summary>
        public bool IsRemoving {
            get => _isRemoving;
            set => SetProperty(ref _isRemoving, value);
        }

        /// <summary>
        /// 条码图片是否存在
        /// </summary>
        public bool IsBarcodeImageExists {
            get => _isBarcodeImageExists;
            set => SetProperty(ref _isBarcodeImageExists, value);
        }

        /// <summary>
        /// 是否已上传云视频
        /// </summary>
        public bool IsUploadedToCloudVideo {
            get => _isUploadedToCloudVideo;
            set => SetProperty(ref _isUploadedToCloudVideo, value);
        }

        public PackageExitStatus PackageExitStatus {
            get => _packageExitStatus;
            set => SetProperty(ref _packageExitStatus, value);
        }
    }

    public class PanoramaImageItemModel : BindableBase {
        private bool _isPanoramaImageExists;

        /// <summary>
        /// 全景图片保存路径
        /// </summary>
        public string? PanoramaImagePath { get; set; }

        /// <summary>
        /// 全景图片是否存在
        /// </summary>
        public bool IsPanoramaImageExists {
            get => _isPanoramaImageExists;
            set => SetProperty(ref _isPanoramaImageExists, value);
        }
    }

    public enum PackageExitStatus {

        /// <summary>
        /// 无
        /// </summary>
        [Description("无")]
        None,

        /// <summary>
        /// 正常落格
        /// </summary>
        [Description("正常落格")]
        Normal,

        [Description("异常")]
        Abnormal
    }
}