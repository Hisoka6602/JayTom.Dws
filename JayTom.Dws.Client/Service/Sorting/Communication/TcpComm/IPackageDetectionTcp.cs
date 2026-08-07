using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Service.Sorting.Communication.TcpComm
{

    public interface IPackageDetectionTcp : ITcpOperations, IDisposable
    {
    }
}