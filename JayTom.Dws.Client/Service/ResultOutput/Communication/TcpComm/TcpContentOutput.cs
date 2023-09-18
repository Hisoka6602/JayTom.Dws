using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;

namespace JayTom.Dws.Client.Service.ResultOutput.Communication.TcpComm {

    public class TcpContentOutput : BaseTcpOperations, ITcpContentOutput {

        public TcpContentOutput(ITcpCommClient tcpCommClient, ITcpCommServer tcpCommServer) : base(tcpCommClient, tcpCommServer) {
        }
    }
}