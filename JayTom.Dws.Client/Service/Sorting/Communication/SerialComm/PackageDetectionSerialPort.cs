using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.SerialPort;

namespace JayTom.Dws.Client.Service.Sorting.Communication.SerialComm {

    public class PackageDetectionSerialPort : BaseSerialPort, IPackageDetectionSerialPort {

        public PackageDetectionSerialPort(SerialPort serialPort) : base(serialPort) {
        }
    }
}