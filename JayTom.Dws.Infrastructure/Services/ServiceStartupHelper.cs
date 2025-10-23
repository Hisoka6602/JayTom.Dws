using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JayTom.Dws.Infrastructure.Services {
    /// <summary>
    /// 服务启动助手 - 优化服务启动顺序和性能
    /// 解决启动阻塞问题
    /// </summary>
    public class ServiceStartupHelper {
        private readonly ILogger<ServiceStartupHelper> _logger;

        public ServiceStartupHelper(ILogger<ServiceStartupHelper> logger) {
            _logger = logger;
        }

        /// <summary>
        /// 关键服务列表（需要按顺序启动）
        /// </summary>
        private static readonly HashSet<string> CriticalServiceNames = new() {
            "SingleInstanceBackgroundService",  // 单实例检查
            "ComputerInfoBackgroundService",    // 计算机信息
            "DataProcessingBackgroundService",  // 数据处理
        };

        /// <summary>
        /// 优化的服务启动方法
        /// </summary>
        /// <param name="hostedServices">所有托管服务</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task StartServicesAsync(
            IEnumerable<IHostedService> hostedServices,
            CancellationToken cancellationToken = default) {
            
            var services = hostedServices.ToList();
            _logger.LogInformation("Starting {Count} hosted services", services.Count);

            // 分类服务
            var criticalServices = services
                .Where(s => IsCriticalService(s.GetType().Name))
                .ToList();
            
            var nonCriticalServices = services
                .Except(criticalServices)
                .ToList();

            _logger.LogInformation("Critical services: {Count}, Non-critical services: {Count}",
                criticalServices.Count, nonCriticalServices.Count);

            // 1. 启动关键服务（按顺序，有超时保护）
            foreach (var service in criticalServices) {
                await StartServiceWithTimeoutAsync(
                    service,
                    isCritical: true,
                    timeout: TimeSpan.FromSeconds(30),
                    cancellationToken);
            }

            _logger.LogInformation("All critical services started successfully");

            // 2. 并行启动非关键服务（有超时保护）
            var startTasks = nonCriticalServices.Select(service =>
                StartServiceWithTimeoutAsync(
                    service,
                    isCritical: false,
                    timeout: TimeSpan.FromSeconds(10),
                    cancellationToken));

            await Task.WhenAll(startTasks);

            _logger.LogInformation("All services started successfully");
        }

        /// <summary>
        /// 启动单个服务（带超时保护）
        /// </summary>
        private async Task StartServiceWithTimeoutAsync(
            IHostedService service,
            bool isCritical,
            TimeSpan timeout,
            CancellationToken cancellationToken) {
            
            var serviceName = service.GetType().Name;
            var startTime = DateTime.UtcNow;

            try {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);

                _logger.LogInformation("Starting {ServiceType} service: {ServiceName}",
                    isCritical ? "CRITICAL" : "normal", serviceName);

                await service.StartAsync(cts.Token);

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("Service started successfully: {ServiceName} (took {Duration}ms)",
                    serviceName, duration.TotalMilliseconds);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
                // 超时
                var duration = DateTime.UtcNow - startTime;
                _logger.LogWarning("Service startup timeout: {ServiceName} (timeout: {Timeout}s, elapsed: {Duration}ms)",
                    serviceName, timeout.TotalSeconds, duration.TotalMilliseconds);

                if (isCritical) {
                    throw new TimeoutException(
                        $"Critical service '{serviceName}' failed to start within {timeout.TotalSeconds}s");
                }
            }
            catch (Exception ex) {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Failed to start service: {ServiceName} (elapsed: {Duration}ms)",
                    serviceName, duration.TotalMilliseconds);

                if (isCritical) {
                    throw; // 关键服务失败应该传播异常
                }
            }
        }

        /// <summary>
        /// 判断是否为关键服务
        /// </summary>
        private static bool IsCriticalService(string serviceName) {
            return CriticalServiceNames.Contains(serviceName);
        }

        /// <summary>
        /// 健康检查 - 检查所有服务是否正常运行
        /// </summary>
        public async Task<Dictionary<string, bool>> CheckServicesHealthAsync(
            IEnumerable<IHostedService> hostedServices) {
            
            var healthStatus = new Dictionary<string, bool>();

            foreach (var service in hostedServices) {
                var serviceName = service.GetType().Name;
                try {
                    // 这里可以添加具体的健康检查逻辑
                    // 例如检查服务是否响应，资源是否正常等
                    healthStatus[serviceName] = true;
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Health check failed for service: {ServiceName}", serviceName);
                    healthStatus[serviceName] = false;
                }
            }

            return healthStatus;
        }
    }
}
