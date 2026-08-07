using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;

namespace JayTom.Dws.Client.Service.Sorting.Communication.TcpComm
{

    public class PackageDetectionTcp : BaseTcpOperations, IPackageDetectionTcp
    {

        public PackageDetectionTcp(ITcpCommClient tcpCommClient, ITcpCommServer tcpCommServer) : base(tcpCommClient, tcpCommServer)
        {
        }

        public void Dispose()
        {
            Close();
        }
    }
}