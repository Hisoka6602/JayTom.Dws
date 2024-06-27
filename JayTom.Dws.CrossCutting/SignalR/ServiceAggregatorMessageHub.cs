namespace JayTom.Dws.CrossCutting.SignalR {

    public class ServiceAggregatorMessageHub : BaseClientMessageHub, IServiceAggregatorMessageHub {

        public Task PushSystemInfo(string message, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public Task PushSystemWarning(string message, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }
    }
}