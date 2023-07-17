using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Service {

    public class ComputerInfoReporter : IComputerInfoReporter {

        public event EventHandler<ComputerInfoModel>? ComputerInfoReceived;

        public async void OnComputerInfoReceived(ComputerInfoModel e) {
            await Task.Yield();
            ComputerInfoReceived?.Invoke(this, e);
        }
    }
}