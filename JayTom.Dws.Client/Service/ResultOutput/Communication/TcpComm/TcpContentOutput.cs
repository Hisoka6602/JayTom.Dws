using JayTom.Dws.Plugin.Tcp;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;

namespace JayTom.Dws.Client.Service.ResultOutput.Communication.TcpComm {

    public class TcpContentOutput : BaseTcpOperations, ITcpContentOutput {

        public TcpContentOutput(ITcpCommClient tcpCommClient, ITcpCommServer tcpCommServer) : base(tcpCommClient, tcpCommServer) {
        }
    }
}