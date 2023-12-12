using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Repository.LocalConf;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class CloudBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IConfigRepository _configRepository;

        public CloudBackgroundService(IConfigRepository configRepository) {
            _configRepository = configRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                //提交到云端

                await Task.Delay(50, stoppingToken);
            }
        }
    }
}