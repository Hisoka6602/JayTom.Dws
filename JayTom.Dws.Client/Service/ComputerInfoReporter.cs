using System;
using JayTom.Dws.Client.Models;

namespace JayTom.Dws.Client.Service
{

    public class ComputerInfoReporter : IComputerInfoReporter
    {

        public event EventHandler<ComputerInfoModel>? ComputerInfoReceived;

        public void OnComputerInfoReceived(ComputerInfoModel e)
        {
            ComputerInfoReceived?.Invoke(this, e);
        }
    }
}
