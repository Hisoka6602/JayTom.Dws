using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Tcp.TcpServer;

namespace JayTom.Dws.Plugin.Tcp.TcpClient {

    public interface ITcpCommClient : ITcpBase {

        /// <summary>
        /// Ip地址
        /// </summary>
        string IpAddress { get; }

        /// <summary>
        /// 端口
        /// </summary>
        int Port { get; }

        /// <summary>
        /// 数据长度
        /// </summary>
        int DataLen { get; set; }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="dataLen"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> Connect(FormatType dataType = FormatType.Ascii, int dataLen = 0, CancellationToken token = default);

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        bool SetParameter(object par);
    }
}