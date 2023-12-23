using Microsoft.AspNetCore.SignalR.Client;

namespace JayTom.Dws.Infrastructure.SignalR {

    public class RetryPolicy : IRetryPolicy {

        public TimeSpan? NextRetryDelay(RetryContext retryContext) {
            var count = retryContext.PreviousRetryCount / 50;
            if (count < 1)//重试次数<50,间隔1s
            {
                return new TimeSpan(0, 0, 0);
            }
            else if (count < 5) {
                return new TimeSpan(0, 0, 2);
            }
            else {
                return new TimeSpan(0, 0, 30);
            }
        }
    }
}