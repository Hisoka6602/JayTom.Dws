using System;
using DryIoc;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalLog;
using System.Collections.Generic;
using JayTom.Dws.Legacy.Contracts.Dto.Timer;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Application.Events;
using WindowsAction = JayTom.Dws.Client.Events.WindowsAction;
using WindowsActionType = JayTom.Dws.Client.Events.WindowsActionType;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    public class TimerBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;
        private readonly DateTime _startTime = DateTime.Now;
        private int _isWindowsClose;

        public TimerBackgroundService(
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe<WindowsAction>(item =>
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
                _eventBus.Publish(new TimerDto
                {
                    ElapsedMilliseconds = (long)timeSpan.TotalMilliseconds,
                    FormattedElapsed = $"{timeSpan.Days}->{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}",
                });
            }
        }
    }
}
