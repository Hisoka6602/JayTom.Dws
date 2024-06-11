using Microsoft.AspNetCore.SignalR.Client;

namespace JayTom.Dws.Infrastructure.SignalR {

    public class RetryPolicy : IRetryPolicy {

        public TimeSpan? NextRetryDelay(RetryContext retryContext) {
            var count = retryContext.PreviousRetryCount / 50;
            return count switch {
                //重试次数<50,间隔1s
                < 1 => new TimeSpan(0, 0, 0),
                < 5 => new TimeSpan(0, 0, 2),
                _ => new TimeSpan(0, 0, 30)
            };
        }
    }
}