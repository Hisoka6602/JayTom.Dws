using JayTom.Dws.Application.Service.CloudApi;

namespace JayTom.Dws.CloudApi.BackgroundService {

    public class DataCleanupService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IConfiguration _configuration;
        private readonly ICloudAppService _cloudAppService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<DataCleanupService> _logger;
        private int _daysAgo = 0;
        private long _minFreeSpaceInMb = 100;

        public DataCleanupService(IConfiguration configuration,
            ICloudAppService cloudAppService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<DataCleanupService> logger) {
            _configuration = configuration;
            _cloudAppService = cloudAppService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            //读配置
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //读配置
            _daysAgo = _configuration.GetValue<int>("CleanupDataDaysAgo", 0);
            _minFreeSpaceInMb = _configuration.GetValue<long>("MinFreeSpaceInMb", 100);
            while (!stoppingToken.IsCancellationRequested) {
                //判断最低空间保障
                await _cloudAppService.CleanEarliestImageFiles(_webHostEnvironment.WebRootPath, _minFreeSpaceInMb);
                if (_daysAgo > 0) {
                    var (key, value) = await _cloudAppService.CleanupDataDaysAgo(_daysAgo, _webHostEnvironment.WebRootPath, stoppingToken);
                }
                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}