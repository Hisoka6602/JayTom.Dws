using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Tcp {

    public interface ITcpCommunication {

        /// <summary>
        /// 连接异常事件(直接返回异常信息的Json)
        /// </summary>
        event EventHandler<string> ConnectionException;

        /// <summary>
        /// 断开事件(直接返回断开信息的Json)
        /// </summary>
        event EventHandler<string> Disconnected;

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
        bool Connect();

        /// <summary>
        /// 重新连接
        /// </summary>
        /// <returns></returns>
        bool Reconnect(int count);

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        bool SetParameter(object par);

        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns></returns>
        void Close();
    }

    public class CommunicationInfo {
        public DateTime Time { get; set; }
        public string Content { get; set; } = string.Empty;
        public CommunicationType Type { get; set; }
    }

    public enum CommunicationType {
        Send, Receive
    }
}