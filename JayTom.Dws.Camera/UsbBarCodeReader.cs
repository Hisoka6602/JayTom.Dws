using System;
using Dynamsoft;
using System.Linq;
using System.Text;
using Dynamsoft.UVC;
using Dynamsoft.PDF;
using Dynamsoft.DBR;
using Dynamsoft.Core;
using System.Drawing;
using Dynamsoft.TWAIN;
using System.Xml.Linq;
using Dynamsoft.Common;
using System.Management;
using static System.String;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace JayTom.Dws.Camera {

    public class UsbBarCodeReader : IDisposable {
        private string dbrLicenseKeys = "t0075oQAAAIvhAJJ+Mv2OHC+ZyzvrkkYyqMuHRgLktAwWHPtBRExDoEyZOSN3p9eHQ0csZBILJK+DKrBs2QaXyzJtmx0k+YgeciYvcCOd";
        private string dntLicenseKeys = "t0071WQAAAIP64uktmNbWzB4BpR9uN81ZcXDga6MZQlXA+n8nb0L8q3jVDPpYvMlRHU7VP2eQUIYACdUYZhZd1ZqZ5cuIySHQErA=";
        private static ConcurrentDictionary<string, UsbCameraInfo> _cameraDictionary = new();
        public UsbCameraStatus UsbCameraStatus { get; private set; } = UsbCameraStatus.Uninitialized;
        public UsbCameraInfo UsbCameraInfo { get; private set; } = new();
        private Dynamsoft.UVC.Camera? _selectCamera;
        private EnumBarcodeFormat mEmBarcodeFormat = 0;
        private EnumBarcodeFormat_2 mEmBarcodeFormat_2 = 0;
        private readonly BarcodeReader mBarcodeReader;
        private PublicRuntimeSettings mCustomRuntimeSettings;

        public event EventHandler<BarcodeScannedEventArgs> BarcodeScanned;

        public event EventHandler<Bitmap> ImageDataReceived;

        /// <summary>
        /// 相机管理
        /// </summary>
        private static CameraManager? _cameraManager;

        /// <summary>
        /// 图像
        /// </summary>
        private ImageCore? _imageCore;

        /// <summary>
        /// 协议管理
        /// </summary>
        private TwainManager? _twainManager;

        public UsbBarCodeReader() {
            _twainManager = new TwainManager(dntLicenseKeys);
            _cameraManager = new CameraManager(dntLicenseKeys);
            //mPDFRasterizer = new PDFRasterizer(dntLicenseKeys);
            _imageCore = new ImageCore();
        }

        /// <summary>
        /// 枚举相机
        /// </summary>
        /// <returns></returns>
        public static List<UsbCameraInfo> EnumerateCameras() {
            //枚举相机
            //之后需要加一个过滤
            var usbCameraInfos = new List<UsbCameraInfo>();
            _cameraDictionary = new();
            var cameraNames = _cameraManager?.GetCameraNames() ?? new List<string>();
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Camera'");
            var devices = searcher.Get();

            foreach (var device in devices) {
                var select = cameraNames?.Select((s, i) => new {
                    Id = i,
                    Name = s
                });
                var orDefault = select?.FirstOrDefault(f => f.Name.Equals(device["Caption"]?.ToString()));
                if (orDefault is not null && !string.IsNullOrEmpty(device["ClassGuid"]?.ToString())) {
                    //取出序列号ClassGuid
                    var usbCameraInfo = new UsbCameraInfo() {
                        CameraName = orDefault.Name,
                        CameraId = orDefault.Id,
                        CameraDescription = device["Description"]?.ToString(),
                        CameraManufacturer = device["Manufacturer"]?.ToString(),
                        CameraSerialNumber = device["ClassGuid"]?.ToString(),
                        CameraResolutions = _cameraManager?.SelectCamera(orDefault.Name)?.SupportedResolutions?.Select(s =>
                            new Size(s.Width, s.Height))?.ToList()
                    };
                    usbCameraInfos.Add(usbCameraInfo);
                    _cameraDictionary.AddOrUpdate(device["ClassGuid"].ToString() ?? string.Empty, value => usbCameraInfo,
                        (key, oldValue) => usbCameraInfo);
                }
            }
            return usbCameraInfos;
        }

        /// <summary>
        /// 设置Usb相机参数
        /// </summary>
        /// <param name="parameters"></param>
        public async Task<KeyValuePair<bool, string>> SetUsbCameraParameter(Dictionary<UsbCameraParameter, object> parameters) {
            var (key, value) = _cameraDictionary.FirstOrDefault(f => f.Key.Equals(UsbCameraInfo.CameraSerialNumber));
            if (!string.IsNullOrEmpty(key)) {
                try {
                    _selectCamera = _cameraManager?.SelectCamera(value.CameraName);
                    if (_selectCamera is not null) {
                        //设置参数
                        var exposure = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Exposure)
                            .Value;
                        if (exposure is int i) {
                            _selectCamera.Exposure.IfAuto = false;
                            _selectCamera.Exposure.Value = i;
                            //设置曝光度
                        }
                        else {
                            _selectCamera.Exposure.IfAuto = true;
                        }

                        //设置分辨率(如果没有指定则使用最大分辨率)
                        var resolution = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Resolution)
                            .Value;
                        if (resolution is Size size) {
                            _selectCamera.CurrentResolution = new CamResolution(size.Width, size.Height);
                        }
                        else {
                            var orDefault = UsbCameraInfo.CameraResolutions?.OrderByDescending(o => o.Width * o.Height)?.FirstOrDefault();
                            if (orDefault is not null) {
                                _selectCamera.CurrentResolution = new CamResolution(orDefault.Value.Width, orDefault.Value.Height);
                            }
                        }
                        //设置其他属性

                        //设置识别码种
                    }
                    else {
                        return new KeyValuePair<bool, string>(false, "选择相机失败");
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            }
            else {
                return new KeyValuePair<bool, string>(false, "相机未绑定");
            }
            return new KeyValuePair<bool, string>(false, "未知错误");
        }

        /// <summary>
        /// 设置读码参数
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> SetBarcodeReaderParameter(
            Dictionary<BarcodeReaderParameter, object> parameters) {
            return new KeyValuePair<bool, string>(false, "未知错误");
        }

        /// <summary>
        /// 绑定
        /// </summary>
        /// <param name="info"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void BindCamera(UsbCameraInfo info) {
            // 实现绑定相机的逻辑
            // 根据相机序列号绑定相机设备
            // _selectCamera = _cameraManager?.SelectCamera(value.CameraName);
            throw new NotImplementedException();
        }

        /// <summary>
        /// 开始
        /// </summary>
        public async Task<KeyValuePair<bool, string>> Start() {
            //判断相机是否存在

            return new KeyValuePair<bool, string>(false, "未知错误");
        }

        /// <summary>
        /// 停止
        /// </summary>
        public async Task<KeyValuePair<bool, string>> Stop() {
            return new KeyValuePair<bool, string>(false, "未知错误");
        }

        //绑定相机
        //开始
        //停止

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() {
        }

        protected virtual async void OnBarcodeScanned(BarcodeScannedEventArgs e) {
            Task.Yield();
            BarcodeScanned?.Invoke(this, e);
        }
    }

    public class UsbCameraInfo {

        /// <summary>
        /// 相机名称
        /// </summary>
        public string? CameraName { get; set; }

        /// <summary>
        /// 相机Id
        /// </summary>
        public int? CameraId { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string? CameraSerialNumber { get; set; }

        /// <summary>
        /// 相机制造商
        /// </summary>
        public string? CameraManufacturer { get; set; }

        /// <summary>
        /// 相机版本
        /// </summary>
        public string? CameraVersion { get; set; }

        /// <summary>
        /// 相机型号
        /// </summary>
        public string? CameraModel { get; set; }

        /// <summary>
        /// 相机描述
        /// </summary>
        public string? CameraDescription { get; set; }

        /// <summary>
        /// 支持的分辨率
        /// </summary>
        public List<Size>? CameraResolutions { get; set; }
    }

    public class BarcodeScannedEventArgs : EventArgs {

        /// <summary>
        /// 图片
        /// </summary>
        public Bitmap? Image { get; set; }

        /// <summary>
        /// 条码集合
        /// </summary>
        public List<BarcodeInfo>? BarCodes { get; set; }

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string? CameraSerialNumber { get; set; }
    }

    public class BarcodeInfo {

        /// <summary>
        /// 条码
        /// </summary>
        public string? Barcode { get; set; }

        /// <summary>
        /// 条码区域
        /// </summary>
        public Region? BarcodeRegion { get; set; }

        /// <summary>
        /// 条码类型
        /// </summary>
        public string? BarcodeType { get; set; }
    }

    //增益
    /*tempCamera.Exposure.IfAuto = false;
    tempCamera.Exposure.Value = exposure;*/

    //Exposure.IfAuto->是否自动增益
    //Exposure.Value->增益值

    //EnumBarcodeFormat->条码类型
    //EnumBarcodeFormat_2->未知类型

    //--------算法参数----------

    // LocalizationModes->本地化模式()
    // DeblurLevel->去模糊级别(0-9)
    //ExpectedBarcodesCount->期望的条形码数量
    //ScaleDownThreshold->缩放阈值
    //TextFilterMode->是否使用文本过滤模式
    //RegionPredetectionMode->是否使用区域预检测模式
    //GrayscaleTransformationModes->灰度转换模式
    //MinResultConfidence->最小结果置信度(0-9,乘以10)
    //TextureDetectionSensitivity->纹理检测敏感度(0-9)
    //BinarizationBlockSize->二值化块大小(0-999)
    //RecognitionMode->识别模式
    public enum UsbCameraStatus {

        /// <summary>
        /// 未初始化
        /// </summary>
        Uninitialized,

        /// <summary>
        /// 已开始
        /// </summary>
        Started,

        /// <summary>
        /// 已停止
        /// </summary>
        Stopped,

        /// <summary>
        /// 异常
        /// </summary>
        Error
    }

    /// <summary>
    /// 本地化模式
    /// </summary>
    public enum LocalizationMode {

        /// <summary>
        /// 默认
        /// </summary>
        Default = 0,

        /// <summary>
        /// 连通块
        /// </summary>
        ConnectedBlocks = 1,

        /// <summary>
        /// 统计
        /// </summary>
        Statistics = 2,

        /// <summary>
        /// 线条
        /// </summary>
        Lines = 3,

        /// <summary>
        /// 直接扫描
        /// </summary>
        ScanDirectly = 4,

        /// <summary>
        /// 连通块 + 直接扫描
        /// </summary>
        ConnectedBlocksAndScanDirectly = 5
    }

    /// <summary>
    /// 灰度转换模式
    /// </summary>
    public enum GrayscaleTransformationMode {

        /// <summary>
        /// 原图
        /// </summary>
        Original,

        /// <summary>
        /// 反色
        /// </summary>
        Inverted,

        /// <summary>
        /// 原图+反色
        /// </summary>
        OriginalAndInverted
    }

    /// <summary>
    /// 图像预处理模式
    /// </summary>
    public enum ImagePreprocessingMode {

        /// <summary>
        /// 通用
        /// </summary>
        General,

        /// <summary>
        /// 灰度均衡化
        /// </summary>
        GrayEqualization,

        /// <summary>
        /// 灰度平滑
        /// </summary>
        GraySmoothing,

        /// <summary>
        /// 锐化和平滑
        /// </summary>
        SharpeningAndSmoothing
    }

    /// <summary>
    /// 扫码模式
    /// </summary>
    public enum ScanMode {

        /// <summary>
        /// 速度
        /// </summary>
        Speed,

        /// <summary>
        /// 平衡
        /// </summary>
        Balance,

        /// <summary>
        /// 覆盖
        /// </summary>
        Coverage
    }

    /// <summary>
    /// 读码参数
    /// </summary>
    public enum BarcodeReaderParameter {

        /// <summary>
        /// 条码类型
        /// </summary>
        EnumBarcodeFormat,

        /// <summary>
        /// 本地化模式
        /// </summary>
        LocalizationMode,

        /// <summary>
        /// 去模糊级别(0-9)
        /// </summary>
        DeblurLevel,

        /// <summary>
        /// 期望的条形码数量
        /// </summary>
        ExpectedBarcodesCount,

        /// <summary>
        /// 缩放阈值
        /// </summary>
        ScaleDownThreshold,

        /// <summary>
        /// 是否使用文本过滤模式
        /// </summary>
        IsUseTextFilterMode,

        /// <summary>
        /// 是否使用区域预检测模式
        /// </summary>
        IsUseRegionPredetectionMode,

        /// <summary>
        /// 灰度转换模式
        /// </summary>
        GrayscaleTransformationMode,

        /// <summary>
        /// 最小结果置信度(0-9,乘以10)
        /// </summary>
        MinResultConfidence,

        /// <summary>
        /// 纹理检测敏感度(0-9)
        /// </summary>
        TextureDetectionSensitivity,

        /// <summary>
        /// 二值化块大小(0-999)
        /// </summary>
        BinarizationBlockSize,

        /// <summary>
        /// 识别模式
        /// </summary>
        RecognitionMode,
    }

    /// <summary>
    /// Usb相机参数
    /// </summary>
    public enum UsbCameraParameter {

        /// <summary>
        /// 曝光度
        /// </summary>
        Exposure,

        /// <summary>
        /// 分辨率
        /// </summary>
        Resolution,

        /// <summary>
        /// 亮度
        /// </summary>
        Brightness,

        /// <summary>
        /// 对比度
        /// </summary>
        Contrast,

        /// <summary>
        /// 色调
        /// </summary>
        Hue,

        /// <summary>
        /// 饱和度
        /// </summary>
        Saturation,

        /// <summary>
        /// 锐度
        /// </summary>
        Sharpness,

        /// <summary>
        /// 伽马值
        /// </summary>
        Gamma,

        /// <summary>
        /// 白平衡
        /// </summary>
        WhiteBalance,

        /// <summary>
        /// 背光补偿
        /// </summary>
        BklightComp,

        /// <summary>
        /// 增益
        /// </summary>
        Gain,

        /// <summary>
        /// 变焦
        /// </summary>
        Zoom,

        /// <summary>
        /// 对焦
        /// </summary>
        Focus,

        /// <summary>
        /// 光圈
        /// </summary>
        Iris,

        /// <summary>
        /// 水平旋转
        /// </summary>
        Pan,

        /// <summary>
        /// 垂直旋转
        /// </summary>
        Tilt,

        /// <summary>
        /// 翻转
        /// </summary>
        Roll
    }
}