using System;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;

namespace JayTom.Dws.Client.Service {

    public class ComputerInfoReporter : IComputerInfoReporter {

        public event EventHandler<ComputerInfoModel>? ComputerInfoReceived;

        public async void OnComputerInfoReceived(ComputerInfoModel e) {
            await Task.Yield();
            ComputerInfoReceived?.Invoke(this, e);
        }
    }
}