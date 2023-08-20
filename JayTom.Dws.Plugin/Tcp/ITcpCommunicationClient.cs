using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Tcp {

    public interface ITcpCommunicationClient {

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接异常事件(直接返回异常信息的Json)
        /// </summary>
        event EventHandler<string> ConnectionException;

        /// <summary>
        /// 异常事件
        /// </summary>
        event EventHandler<Exception> Exception;

        /// <summary>
        /// 用户完成连接事件
        /// </summary>
        event EventHandler<string> Connected;

        /// <summary>
        /// 通讯消息事件
        /// </summary>
        event EventHandler<CommunicationInfo> Communication;

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        Task<bool> Connect();

        /// <summary>
        /// 重新连接
        /// </summary>
        /// <returns></returns>
        Task<bool> Reconnect(int count);

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        bool SetParameter(object par);

        /// <summary>
        /// 发送信息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<bool> SendMessage(string message);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<bool> SendMessage(byte[] message);

        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns></returns>
        void Close();
    }
}