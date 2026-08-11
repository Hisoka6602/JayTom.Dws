using System;
using System.Threading;

namespace JayTom.Dws.Client.Service.ResultOutput
{

    public interface IResultOutputService
    {

        /// <summary>
        /// 输出失败回调事件
        /// </summary>
        event EventHandler<Exception> OutputFailed;

        /// <summary>
        /// 执行输出
        /// </summary>
        /// <param name="barCode"></param>
        /// <param name="weight"></param>
        /// <param name="scanTime"></param>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="volume"></param>
        /// <param name="cameraSerialNumber"></param>
        /// <param name="cancellationToken"></param>
        void ExecuteOutput(string barCode, decimal weight,
            DateTime scanTime, decimal length, decimal width, decimal height, decimal volume,
            string cameraSerialNumber, CancellationToken cancellationToken = default);
    }
}