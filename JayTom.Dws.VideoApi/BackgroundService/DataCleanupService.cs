using JayTom.Dws.Application.Service.VideoApi;

namespace JayTom.Dws.VideoApi.BackgroundService {

    public class DataCleanupService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IConfiguration _configuration;
        private readonly IVideoBarCodeAppService _videoBarCodeAppService;
        private readonly ILogger<DataCleanupService> _logger;
        private int _daysAgo = 0;

        public DataCleanupService(IConfiguration configuration,
            IVideoBarCodeAppService videoBarCodeAppService,
            ILogger<DataCleanupService> logger) {
            _configuration = configuration;
            _videoBarCodeAppService = videoBarCodeAppService;
            _logger = logger;
            //读配置
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //读配置
            _daysAgo = _configuration.GetValue<int>("CleanupDataDaysAgo", 0);
            while (!stoppingToken.IsCancellationRequested && _daysAgo > 0) {
                await Task.Delay(30000, stoppingToken).ContinueWith(async a => {
                    var (key, value) = await _videoBarCodeAppService.CleanupDataDaysAgo(_daysAgo, stoppingToken);
                    if (!key && value is string errorMessage) {
                        _logger.LogError(errorMessage);
                    }
                }, stoppingToken).Unwrap();
            }
        }
    }
}