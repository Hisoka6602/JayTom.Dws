using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Service.Sorting {

    public class DefaultInventoryManagementService : IInventoryManagementService {

        public event EventHandler<CommunicationMessageInfo>? CommunicationInfoEvent;

        public event EventHandler<Exception>? CommunicationExceptionEvent;

        public void SendInstructions(List<string> instructions, TimeSpan interval, InstructionsAttach attach) {
            throw new NotImplementedException();
        }

        public void SendInstructions(List<SortingInstructionInfoModel> instructions, TimeSpan interval, InstructionsAttach attach) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> Connect(CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> Disconnect(CancellationToken token = default) {
            throw new NotImplementedException();
        }
    }
}