using JayTom.Dws.Plugin.Tcp;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;

namespace JayTom.Dws.Client.Service.ExternalDataService.Communication.TcpComm {

    public class TcpVolumeInput : BaseTcpOperations, ITcpVolumeInput {

        public TcpVolumeInput(ITcpCommClient tcpCommClient, ITcpCommServer tcpCommServer) : base(tcpCommClient, tcpCommServer) {
        }
    }
}