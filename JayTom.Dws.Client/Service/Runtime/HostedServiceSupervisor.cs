using JayTom.Dws.Application.Resilience;
using Microsoft.Extensions.Hosting;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JayTom.Dws.Client.Service.Runtime;

/// <summary>
/// 观察后台服务执行任务，在异常或意外退出后按指数退避自动重启。
/// </summary>
internal sealed class HostedServiceSupervisor : IHostedServiceSupervisor, IAsyncDisposable {
    /// <summary>后台服务启动允许的最长时间。</summary>
    private static readonly TimeSpan ServiceStartTimeout = TimeSpan.FromSeconds(30);
    /// <summary>单个后台服务停止允许的最长时间。</summary>
    private static readonly TimeSpan ServiceStopTimeout = TimeSpan.FromSeconds(5);
    /// <summary>服务稳定运行达到该时间后重置连续失败次数。</summary>
    private static readonly TimeSpan StableRunDuration = TimeSpan.FromMinutes(5);
    /// <summary>运行心跳写入间隔。</summary>
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);
    /// <summary>心跳写入异常的日志节流间隔。</summary>
    private static readonly TimeSpan HeartbeatErrorLogInterval = TimeSpan.FromMinutes(5);
    /// <summary>后台服务监督日志记录器。</summary>
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    /// <summary>心跳 JSON 序列化配置。</summary>
    private static readonly JsonSerializerOptions HeartbeatJsonOptions = new() {
        WriteIndented = true
    };

    /// <summary>需要统一监督的后台服务。</summary>
    private readonly IReadOnlyList<IHostedService> _services;
    /// <summary>按服务名称保存当前健康状态。</summary>
    private readonly ConcurrentDictionary<string, string> _serviceStates = new(StringComparer.Ordinal);
    /// <summary>按服务名称保存累计重启次数。</summary>
    private readonly ConcurrentDictionary<string, int> _restartCounts = new(StringComparer.Ordinal);
    /// <summary>服务故障重启退避策略。</summary>
    private readonly BoundedExponentialBackoff _restartBackoff = new(
        TimeSpan.FromSeconds(5),
        TimeSpan.FromMinutes(5));
    /// <summary>进程启动时的单调时钟时间戳。</summary>
    private readonly long _processStartedTimestamp = Stopwatch.GetTimestamp();
    /// <summary>监督任务生命周期取消源。</summary>
    private CancellationTokenSource? _lifetimeCancellation;
    /// <summary>每个后台服务对应的监督任务。</summary>
    private Task[] _supervisionTasks = [];
    /// <summary>定期写入运行心跳的任务。</summary>
    private Task? _heartbeatTask;
    /// <summary>最近一次心跳写入异常日志时间戳。</summary>
    private long _lastHeartbeatErrorTimestamp;
    /// <summary>标记监督器是否已经启动。</summary>
    private int _isStarted;

    /// <summary>使用依赖注入容器登记的全部后台服务创建监督器。</summary>
    public HostedServiceSupervisor(IEnumerable<IHostedService> services) {
        ArgumentNullException.ThrowIfNull(services);
        _services = services.ToArray();
    }

    /// <summary>启动并开始监督全部后台服务。</summary>
    public async Task StartAsync(CancellationToken cancellationToken = default) {
        if (Interlocked.CompareExchange(ref _isStarted, 1, 0) != 0) {
            return;
        }

        _lifetimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var lifetimeToken = _lifetimeCancellation.Token;
        var initialStarts = new TaskCompletionSource[_services.Count];
        _supervisionTasks = new Task[_services.Count];

        for (var index = 0; index < _services.Count; index++) {
            var initialStart = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            initialStarts[index] = initialStart;
            _supervisionTasks[index] = SuperviseServiceAsync(
                _services[index],
                initialStart,
                lifetimeToken);
        }

        await Task.WhenAll(initialStarts.Select(source => source.Task)).ConfigureAwait(false);
        _heartbeatTask = RunHeartbeatLoopAsync(lifetimeToken);
        await WriteHeartbeatAsync("Running", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>停止监督并按逆序停止全部后台服务。</summary>
    public async Task StopAsync(CancellationToken cancellationToken = default) {
        if (Interlocked.Exchange(ref _isStarted, 0) == 0) {
            return;
        }

        var lifetimeCancellation = Interlocked.Exchange(ref _lifetimeCancellation, null);
        if (lifetimeCancellation is not null) {
            await lifetimeCancellation.CancelAsync().ConfigureAwait(false);
        }

        foreach (var service in _services.Reverse()) {
            await StopServiceSafelyAsync(service, cancellationToken).ConfigureAwait(false);
        }

        await ObserveCompletionAsync(_supervisionTasks, cancellationToken).ConfigureAwait(false);
        if (_heartbeatTask is not null) {
            await ObserveCompletionAsync([_heartbeatTask], cancellationToken).ConfigureAwait(false);
            _heartbeatTask = null;
        }

        foreach (var service in _services) {
            _serviceStates[service.GetType().Name] = "Stopped";
        }

        try {
            await WriteHeartbeatAsync("Stopped", CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception) {
            Logger.Warn(exception, "写入最终运行心跳失败");
        }

        lifetimeCancellation?.Dispose();
    }

    /// <summary>获取当前后台服务健康状态快照。</summary>
    public IReadOnlyDictionary<string, string> GetHealthSnapshot() =>
        new Dictionary<string, string>(_serviceStates, StringComparer.Ordinal);

    /// <summary>持续监督单个服务并在故障后重新启动。</summary>
    private async Task SuperviseServiceAsync(
        IHostedService service,
        TaskCompletionSource initialStart,
        CancellationToken cancellationToken) {
        var serviceName = service.GetType().Name;
        var consecutiveFailures = 0;
        var isInitialAttempt = true;

        while (!cancellationToken.IsCancellationRequested) {
            var runStartedTimestamp = 0L;
            try {
                _serviceStates[serviceName] = isInitialAttempt ? "Starting" : "Restarting";
                // 启动令牌只约束监督器等待时间；服务自身由 StopAsync 按依赖逆序停止。
                await service.StartAsync(CancellationToken.None)
                    .WaitAsync(ServiceStartTimeout, cancellationToken)
                    .ConfigureAwait(false);
                runStartedTimestamp = Stopwatch.GetTimestamp();
                _serviceStates[serviceName] = "Running";
                if (!isInitialAttempt) {
                    _restartCounts.AddOrUpdate(serviceName, 1, static (_, count) => count + 1);
                    Logger.Info($"后台服务已恢复运行:{serviceName}");
                }

                initialStart.TrySetResult();
                isInitialAttempt = false;

                if (service is Microsoft.Extensions.Hosting.BackgroundService backgroundService &&
                    backgroundService.ExecuteTask is { } executionTask) {
                    await executionTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                    if (!cancellationToken.IsCancellationRequested) {
                        throw new InvalidOperationException($"后台服务意外结束:{serviceName}");
                    }
                }
                else {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                break;
            }
            catch (Exception exception) {
                if (runStartedTimestamp != 0 &&
                    Stopwatch.GetElapsedTime(runStartedTimestamp) >= StableRunDuration) {
                    consecutiveFailures = 0;
                }

                consecutiveFailures++;
                _serviceStates[serviceName] = "Faulted";
                initialStart.TrySetResult();
                isInitialAttempt = false;
                Logger.Error(
                    exception,
                    $"后台服务发生故障，将自动重启。服务:{serviceName}，连续失败:{consecutiveFailures}");
            }

            if (cancellationToken.IsCancellationRequested) {
                break;
            }

            await StopServiceSafelyAsync(service, CancellationToken.None).ConfigureAwait(false);
            var delay = _restartBackoff.GetDelay(Math.Max(1, consecutiveFailures));
            _serviceStates[serviceName] = "Backoff";
            try {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                break;
            }
        }

        initialStart.TrySetResult();
        _serviceStates[serviceName] = "Stopped";
    }

    /// <summary>停止单个服务并隔离超时或释放异常。</summary>
    private static async Task StopServiceSafelyAsync(
        IHostedService service,
        CancellationToken cancellationToken) {
        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(ServiceStopTimeout);
        try {
            await service.StopAsync(timeoutCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCancellation.IsCancellationRequested) {
            Logger.Warn($"后台服务停止超时:{service.GetType().Name}");
        }
        catch (Exception exception) {
            Logger.Error(exception, $"后台服务停止异常:{service.GetType().Name}");
        }
    }

    /// <summary>观察监督或心跳任务结束，避免故障任务成为未观察异常。</summary>
    private static async Task ObserveCompletionAsync(
        IEnumerable<Task> tasks,
        CancellationToken cancellationToken) {
        try {
            await Task.WhenAll(tasks).WaitAsync(ServiceStartTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            // 应用退出超时后由外层继续完成进程关闭。
        }
        catch (TimeoutException) {
            Logger.Warn("等待后台监督任务退出超时");
        }
        catch (Exception exception) {
            Logger.Error(exception, "观察后台监督任务结束时发生异常");
        }
    }

    /// <summary>按固定间隔写入可供外部看门狗检查的运行心跳。</summary>
    private async Task RunHeartbeatLoopAsync(CancellationToken cancellationToken) {
        using var timer = new PeriodicTimer(HeartbeatInterval);
        while (!cancellationToken.IsCancellationRequested) {
            try {
                if (!await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false)) {
                    break;
                }

                await WriteHeartbeatAsync("Running", cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                break;
            }
            catch (Exception exception) {
                LogHeartbeatErrorThrottled(exception);
            }
        }
    }

    /// <summary>以临时文件替换方式原子写入进程及服务健康信息。</summary>
    private async Task WriteHeartbeatAsync(string lifecycleStatus, CancellationToken cancellationToken) {
        var runtimeDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime");
        Directory.CreateDirectory(runtimeDirectory);
        var heartbeatPath = Path.Combine(runtimeDirectory, "runtime-health.json");
        var temporaryPath = Path.Combine(runtimeDirectory, "runtime-health.tmp");
        using var process = Process.GetCurrentProcess();
        var elapsed = Stopwatch.GetElapsedTime(_processStartedTimestamp);
        var serviceStates = _serviceStates
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var restartCounts = _restartCounts
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var overallStatus = serviceStates.Count == _services.Count &&
                            serviceStates.Values.All(state => state == "Running")
            ? "Healthy"
            : "Degraded";
        var heartbeat = new {
            LifecycleStatus = lifecycleStatus,
            OverallStatus = overallStatus,
            LastHeartbeat = DateTimeOffset.Now,
            ProcessId = Environment.ProcessId,
            UptimeSeconds = (long)elapsed.TotalSeconds,
            WorkingSetBytes = process.WorkingSet64,
            ManagedMemoryBytes = GC.GetTotalMemory(false),
            ServiceStates = serviceStates,
            RestartCounts = restartCounts
        };
        var json = JsonSerializer.Serialize(heartbeat, HeartbeatJsonOptions);
        await File.WriteAllTextAsync(
                temporaryPath,
                json,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                cancellationToken)
            .ConfigureAwait(false);
        File.Move(temporaryPath, heartbeatPath, overwrite: true);
    }

    /// <summary>按固定时间窗口记录心跳写入错误，避免磁盘故障引发日志风暴。</summary>
    private void LogHeartbeatErrorThrottled(Exception exception) {
        var now = Stopwatch.GetTimestamp();
        var previous = Interlocked.Read(ref _lastHeartbeatErrorTimestamp);
        if (previous != 0 && Stopwatch.GetElapsedTime(previous, now) < HeartbeatErrorLogInterval) {
            return;
        }

        Interlocked.Exchange(ref _lastHeartbeatErrorTimestamp, now);
        Logger.Error(exception, "写入运行心跳失败");
    }

    /// <summary>释放监督器及其取消源。</summary>
    public async ValueTask DisposeAsync() {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
