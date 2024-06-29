using System;
using DryIoc;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.Timer;
using JayTom.Dws.Domain.EventMediators;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class TimerBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private DateTime _startTime = DateTime.Now;
        private static bool _isWindowsClose;

        public TimerBackgroundService() {
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                if (item is WindowsAction { Type: WindowsActionType.Close }) {
                    _isWindowsClose = true;
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                await Task.Delay(1000, stoppingToken);
                var timeSpan = DateTime.Now.Subtract(_startTime);
                EventAggregator.Instance.Publish(new TimerDto {
                    ElapsedMilliseconds = (long)timeSpan.TotalMilliseconds,
                    FormattedElapsed = $"{timeSpan.Days}->{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}",
                });
            }
        }
    }
}