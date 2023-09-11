using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class CleanupService : Microsoft.Extensions.Hosting.BackgroundService {
        //获取设置

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                //
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}