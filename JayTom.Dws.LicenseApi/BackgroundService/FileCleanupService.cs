using Microsoft.AspNetCore.Hosting;

namespace JayTom.Dws.LicenseApi.BackgroundService {

    public class FileCleanupService : Microsoft.Extensions.Hosting.BackgroundService {

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                var directoryPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "LicenseFile");
                DeleteOldFiles(directoryPath);
                await Task.Delay(10000, stoppingToken);
            }
        }

        private void DeleteOldFiles(string directoryPath) {
            if (!Directory.Exists(directoryPath)) {
                return;
            }
            var thresholdTime = DateTime.Now.AddHours(-1);
            foreach (var filePath in Directory.GetFiles(directoryPath)) {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.LastWriteTime < thresholdTime) {
                    try {
                        fileInfo.Delete();
                    }
                    catch (Exception ex) {
                        NLog.LogManager.GetCurrentClassLogger().Error($"{ex}");
                    }
                }
            }
        }
    }
}