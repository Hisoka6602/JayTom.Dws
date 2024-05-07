using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;

namespace JayTom.Dws.Client.Service.Device {

    /// <summary>
    /// 灰度仪管理器
    /// </summary>
    public interface IGrayscaleSensorManager {

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 启动灰度仪
        /// </summary>
        void StartSensor();

        /// <summary>
        /// 停止灰度仪
        /// </summary>
        void StopSensor();

        /// <summary>
        /// 连接事件
        /// </summary>
        event EventHandler<IGrayscaleSensorManager> Connected;

        /// <summary>
        /// 断开事件
        /// </summary>
        event EventHandler<IGrayscaleSensorManager> Disconnected;

        /// <summary>
        /// 灰度仪结果回调事件
        /// </summary>
        event EventHandler<GrayscaleResult> GrayscaleSensorResultReceived;

        /// <summary>
        /// 获取灰度仪结果(单次)
        /// </summary>
        /// <returns></returns>
        Task<GrayscaleResult> GetSingleGrayscaleSensorResult();

        /// <summary>
        /// 获取灰度仪结果
        /// </summary>
        void ContinuousGrayscaleSensorReading();
    }
}