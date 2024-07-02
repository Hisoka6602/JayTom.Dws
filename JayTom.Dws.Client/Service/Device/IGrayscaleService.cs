using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;

namespace JayTom.Dws.Client.Service.Device {

    /// <summary>
    /// 灰度仪管理器
    /// </summary>
    public interface IGrayscaleService {

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 启动灰度仪
        /// </summary>
        Task<KeyValuePair<bool, string>> StartSensor();

        /// <summary>
        /// 停止灰度仪
        /// </summary>
        Task<KeyValuePair<bool, string>> StopSensor();

        /// <summary>
        /// 连接事件
        /// </summary>
        event EventHandler<IGrayscaleService> Connected;

        /// <summary>
        /// 断开事件
        /// </summary>
        event EventHandler<IGrayscaleService> Disconnected;

        /// <summary>
        /// 灰度仪结果回调事件
        /// </summary>
        event EventHandler<GrayscaleResult> GrayscaleSensorResultReceived;

        /// <summary>
        /// 触发但未识别到包裹
        /// </summary>
        event EventHandler ParcelLocationNotReceived;

        /// <summary>
        /// 获取灰度仪结果(单次)
        /// </summary>
        /// <returns></returns>
        Task<GrayscaleResult?> GetSingleGrayscaleSensorResult(object param, int timeOut = 500, CancellationToken token = default);

        /// <summary>
        /// 获取灰度仪结果
        /// </summary>
        void ContinuousGrayscaleSensorReading(object param, CancellationToken token);

        /// <summary>
        /// 增加小车数量计算
        /// </summary>
        /// <param name="carNum"></param>
        /// <param name="additionalCarCount"></param>
        int IncreaseCarCount(int carNum, int additionalCarCount);
    }
}