using System;
using System.Drawing;
using JayTom.Dws.Device;
using System.Threading.Tasks;
using JayTom.Dws.Device.Camera;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Service.Device {

    public interface IDeviceService {

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
        event EventHandler<BarcodeHitEventArgs> BarcodeScanned;

        /// <summary>
        /// 当相机捕获到体积信息时触发的事件
        /// </summary>
        event EventHandler<VolumeCapturedEventArgs> VolumeCaptured;

        /// <summary>
        /// 当相机实时画面更新时触发的事件
        /// </summary>
        event EventHandler<RealTimeImageEventArgs> RealTimeImage;

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
        Task<KeyValuePair<bool, string>> Start();

        /// <summary>
        /// 停止设备服务
        /// </summary>
        Task<KeyValuePair<bool, string>> Stop();
    }

    public class DeviceExceptionEventArgs {

        /// <summary>
        /// 设备
        /// </summary>
        public IDevice? Device;

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
}