using System;
using System.Drawing;
using System.Threading;
using JayTom.Dws.Camera;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.Cameras;

namespace JayTom.Dws.Client.Service.Device {

    public interface IDeviceService {

        /// <summary>
        /// 设备运行状态
        /// </summary>
        public bool RunningStatus { get; }

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

        /*/// <summary>
        /// 当磅秤连接时触发的事件
        /// </summary>
        event EventHandler<ScaleConnectedEventArgs> ScaleConnected;

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
        /// 当设备重新连接成功时触发的事件
        /// </summary>
        event EventHandler<DeviceReconnectedEventArgs> DeviceReconnected;*/

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
        /// 初始化设备服务
        /// </summary>
        Task Initialization();

        /// <summary>
        /// 释放设备注册资源
        /// </summary>
        void Dispose();
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
        public BoundCameraType Type { get; set; }

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
    }

    public class VolumeCapturedEventArgs : EventArgs {

        /// <summary>
        /// 测量图片
        /// </summary>
        public Bitmap? Image { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 长度
        /// </summary>
        public double Length { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        public double Volume { get; set; }
    }
}