using System;
using DryIoc;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.Timer;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using WindowsAction = JayTom.Dws.Client.EventMediators.WindowsAction;
using WindowsActionType = JayTom.Dws.Client.EventMediators.WindowsActionType;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    public class TimerBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly DateTime _startTime = DateTime.Now;
        private int _isWindowsClose;

        public TimerBackgroundService()
        {
            EventAggregator.Instance.Subscribe<WindowsAction>(item =>
            {
                if (item is { Type: WindowsActionType.Close })
                {
                    Interlocked.Exchange(ref _isWindowsClose, 1);
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (Volatile.Read(ref _isWindowsClose) == 0 &&
                   await timer.WaitForNextTickAsync(stoppingToken))
            {
                var timeSpan = DateTime.Now.Subtract(_startTime);
                EventAggregator.Instance.Publish(new TimerDto
                {
                    ElapsedMilliseconds = (long)timeSpan.TotalMilliseconds,
                    FormattedElapsed = $"{timeSpan.Days}->{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}",
                });
            }
        }
    }
}
