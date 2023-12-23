using Microsoft.AspNetCore.SignalR;

namespace JayTom.Dws.Infrastructure.SignalR.VideoApi.SignalRMessageHub {

    public class MessageHub : Hub, IMessageHub {
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly IBarCodeService _barCodeService;
        private List<DataSummary> _dataSummary = new();
        private static SemaphoreSlim _semaphoreSlim = new(1);

        public List<DataSummary> DataSummaries {
            get => _dataSummary;
            private set => _dataSummary = value;
        }

        public MessageHub(IHubContext<MessageHub> hubContext,
            IBarCodeService barCodeService) {
            _hubContext = hubContext;
            _barCodeService = barCodeService;
        }

        public async void SendTotalCount() {
            await _hubContext.Clients.All.SendCoreAsync("DataSummaries", new object?[]
             {
                DataSummaries
             });
        }

        public async void AddTotalCount(DateTime date) {
            try {
                await _semaphoreSlim.WaitAsync();
                var summary = DataSummaries.FirstOrDefault(f => f.Date.Equals(date.Date));
                if (summary is null) {
                    DataSummaries.Add(new DataSummary() {
                        Date = date.Date,
                        TotalCount = 1
                    });
                }
                else {
                    summary.TotalCount += 1;
                }
            }
            finally {
                _semaphoreSlim.Release();
                SendTotalCount();
            }
        }

        public async Task<bool> ResetDataSummaries() {
            try {
                await _semaphoreSlim.WaitAsync();
                DataSummaries.Clear();
                var times = new[]
                {
                    DateTime.Now.AddDays(-1),
                    DateTime.Now
                };
                await Parallel.ForEachAsync(times, async (t, c) => {
                    var (key, value) = await _barCodeService.CodeTotal(t, c);
                    if (key && value is int total) {
                        lock (DataSummaries) {
                            DataSummaries.Add(new DataSummary { Date = t.Date, TotalCount = total });
                        }
                    }
                });
            }
            finally {
                _semaphoreSlim.Release();
            }

            return false;
        }
    }
}