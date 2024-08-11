using System;
using JayTom.Dws.Ocr;
using System.Drawing;
using System.Threading;
using JayTom.Dws.Camera;
using System.Threading.Tasks;
using JayTom.Dws.Plugin.Scale;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.Cameras;

namespace JayTom.Dws.Client.Service.Device {

    public interface IDeviceService {

        /// <summary>
        /// 设备运行状态
        /// </summary>
        public bool RunningStatus { get; }

        /// <summary>
        /// 相机列表
        /// </summary>
        public List<CameraInfo> CameraItems { get; }

        /// <summary>
        /// 磅秤类型
        /// </summary>
        public ScaleType ScaleType { get; }

        /// <summary>
        /// 当相机初始化完成时触发的事件，返回初始化的相机列表
        /// </summary>
        event EventHandler<List<ICamera>> CameraInitialized;

        /// <summary>
        /// 当相机断开连接时触发的事件，返回已断开连接的相机列表
        /// </summary>
        event EventHandler<List<ICamera>> CameraDisconnected;

        /// <summary>
        /// 当相机故障发生时触发的事件，返回相机故障列表
        /// </summary>
        event EventHandler<List<ICamera>> CameraFault;

        /// <summary>
        /// 当相机扫到条码时触发的事件
        /// </summary>
        event EventHandler<BarcodeReadEventArgs> BarcodeScanned;

        /// <summary>
        /// 包裹触发但未识别到条码
        /// </summary>
        event EventHandler<BarcodeReadEventArgs> NotBarcodeHitEvent;

        /// <summary>
        /// 相机捕获到全景图片触发事件
        /// </summary>
        event EventHandler<PanoramaCaptureEventArgs> PanoramaCaptured;

        /// <summary>
        /// 当相机捕获到体积信息时触发的事件
        /// </summary>
        event EventHandler<VolumeCapturedEventArgs> VolumeCaptured;

        /// <summary>
        /// 当相机实时画面更新时触发的事件
        /// </summary>
        event EventHandler<RealTimeImageEventArgs> RealTimeImage;

        /// <summary>
        /// 相机枚举事件
        /// </summary>
        event EventHandler<List<CameraFinderItemInfoModel>> CameraEnumerationRefreshed;

        /// <summary>
        /// 相机异常事件
        /// </summary>
        event EventHandler<DeviceExceptionEventArgs> CameraException;

        /// <summary>
        /// 相机枚举刷新
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> OnCameraEnumerationRefreshed(CancellationToken token = default);

        /// <summary>
        /// 相机绑定事件
        /// </summary>
        event EventHandler<CameraFinderItemInfoModel> CameraBound;

        /// <summary>
        /// 相机绑定
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> OnCameraBound(CameraFinderItemInfoModel camera, CancellationToken token = default);

        /// <summary>
        /// 相机解绑事件
        /// </summary>
        event EventHandler<CameraFinderItemInfoModel> CameraUnbound;

        /// <summary>
        /// 相机解绑
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> OnCameraUnbound(CameraFinderItemInfoModel camera, CancellationToken token = default);

        /// <summary>
        /// 相机修改参数事件
        /// </summary>
        event EventHandler<List<CameraParametersModifiedEventArgs>> CameraParametersModified;

        /// <summary>
        /// 相机修改参数
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> OnCameraParametersModified(List<CameraParametersModifiedEventArgs> camera, CancellationToken token = default);

        /// <summary>
        /// 相机释放事件
        /// </summary>
        event EventHandler<string> CameraReleased;

        /// <summary>
        /// 相机启动事件
        /// </summary>
        event EventHandler<CameraStartedEventArgs> CameraStarted;

        /// <summary>
        /// 当磅秤连接时触发的事件
        /// </summary>
        event EventHandler<ScaleConnectedEventArgs> ScaleConnected;

        //Ocr相关

        /// <summary>
        /// 当发生OCR异常时触发的事件
        /// </summary>
        event EventHandler<OcrExceptionEventArgs> OcrExceptionOccurred;

        /// <summary>
        /// 当发生OCR初始化异常时触发的事件
        /// </summary>
        event EventHandler<OcrInitializationExceptionEventArgs> OcrInitializationExceptionOccurred;

        /// <summary>
        /// 当OCR识别到内容时触发的事件
        /// </summary>
        event EventHandler<OcrResult> OcrContentRecognized;

        /// <summary>
        /// 当发生鉴权异常时触发的事件
        /// </summary>
        event EventHandler<AuthenticationExceptionEventArgs> AuthenticationExceptionOccurred;

        /// <summary>
        /// 当磅秤断开连接时触发的事件
        /// </summary>
        event EventHandler<ScaleDisconnectedEventArgs> ScaleDisconnected;

        /// <summary>
        /// 当实时重量信息更新时触发的事件
        /// </summary>
        event EventHandler<RealTimeWeightEventArgs> RealTimeWeight;

        /// <summary>
        /// 当稳定重量信息更新时触发的事件
        /// </summary>
        event EventHandler<StableWeightEventArgs> StableWeight;

        /// <summary>
        /// 稳定重量(含原文)
        /// </summary>
        event EventHandler<WeightChangedEventArgs> WeightStabilized;

        /// <summary>
        /// 重量归0(一般指包裹离开称重台)
        /// </summary>
        event EventHandler<WeightChangedEventArgs> WeightCleared;

        /// <summary>
        /// 当设备发生异常时触发的事件
        /// </summary>
        event EventHandler<DeviceExceptionEventArgs> DeviceException;

        /// <summary>
        /// 启动设备服务
        /// </summary>
        Task<KeyValuePair<bool, string>> Start(CancellationToken token = default);

        /// <summary>
        /// 停止设备服务
        /// </summary>
        Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default);

        /// <summary>
        /// 扫码枪条码返回事件
        /// </summary>
        event EventHandler<string> BarCodeKeyReceived;

        /// <summary>
        /// 扫码枪实时按键事件
        /// </summary>
        event EventHandler<string> RealTimeKeyReceived;

        /// <summary>
        /// 初始化设备服务
        /// </summary>
        Task Initialization();

        /// <summary>
        /// 释放设备注册资源
        /// </summary>
        void Dispose();
    }

    public class StableWeightEventArgs : EventArgs {

        /// <summary>
        /// 磅秤
        /// </summary>
        public IScale? Scale { get; set; }

        /// <summary>
        /// 重量
        /// </summary>
        public float Weight { get; set; }
    }

    public class RealTimeWeightEventArgs : EventArgs {

        /// <summary>
        /// 磅秤
        /// </summary>
        public IScale? Scale { get; set; }

        /// <summary>
        /// 实时重量
        /// </summary>
        public float RealTimeWeight { get; set; }
    }

    public class ScaleDisconnectedEventArgs : EventArgs {

        /// <summary>
        /// 磅秤
        /// </summary>
        public IScale? Scale { get; set; }

        /// <summary>
        /// 断开异常内容
        /// </summary>
        public Exception? Exception { get; set; }
    }

    public class ScaleConnectedEventArgs : EventArgs {

        /// <summary>
        /// 磅秤类型
        /// </summary>
        public ScaleType ScaleType { get; set; } = ScaleType.None;

        /// <summary>
        /// 连接参数
        /// </summary>
        public BaseScaleConnectParam ConnectionParameters { get; set; } = new();
    }

    public enum ScaleType {

        /// <summary>
        /// 静态磅秤
        /// </summary>
        Static,

        /// <summary>
        /// 动态磅秤
        /// </summary>
        Dynamic,

        /// <summary>
        /// 无称重
        /// </summary>
        None
    }

    public class DeviceExceptionEventArgs {
        /*/// <summary>
        /// 设备
        /// </summary>
        public ICamera? Camera;*/

        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception? ExceptionMessage;
    }

    public class RealTimeImageEventArgs {

        /// <summary>
        /// 图像
        /// </summary>
        public Bitmap? Image { get; set; }

        /// <summary>
        /// 相机
        /// </summary>
        public ICamera? Camera { get; set; }

        /// <summary>
        /// 实时帧率
        /// </summary>
        public float RealTimeFrameRate { get; set; }
    }

    public class CameraParametersModifiedEventArgs : EventArgs {

        /// <summary>
        /// 已绑定类型
        /// </summary>
        public CameraBindingType Type { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public object? Parameters { get; set; }
    }

    public class PanoramaCaptureEventArgs : EventArgs {

        /// <summary>
        /// 图片
        /// </summary>
        public Bitmap? Image { get; set; }

        /// <summary>
        /// 缩略图
        /// </summary>
        public Bitmap? ThumbImage { get; set; }

        /// <summary>
        /// 拍照时间
        /// </summary>
        public DateTime PhotoTime { get; set; }

        /// <summary>
        /// 拍照时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 条码时间戳
        /// </summary>
        public long BarcodeTimestamp { get; set; }
    }
}