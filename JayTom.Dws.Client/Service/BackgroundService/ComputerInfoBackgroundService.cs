using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Models.LocalLog;
using System.Collections.Generic;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Infrastructure.IComputer;
using NetworkType = JayTom.Dws.Client.Models.NetworkType;
using WindowsAction = JayTom.Dws.Client.Events.WindowsAction;
using WindowsActionType = JayTom.Dws.Client.Events.WindowsActionType;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    public class ComputerInfoBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;
        private readonly IComputerInfoReporter _computerInfoReporter;
        private readonly IComputer _computer;
        private static readonly TimeSpan WarningInterval = TimeSpan.FromMinutes(5);
        private bool _isWindowsClose;
        private long _lastCpuUsageWarning;
        private long _lastCpuTemperatureWarning;
        private long _lastMemoryWarning;
        private long _lastCollectionError;

        public ComputerInfoBackgroundService(IComputerInfoReporter computerInfoReporter, IComputer computer,
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            _computerInfoReporter = computerInfoReporter;
            _computer = computer;
            _eventBus.Subscribe<WindowsAction>(item =>
            {
                if (item is { Type: WindowsActionType.Close })
                {
                    _isWindowsClose = true;
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var counter = new PerformanceCounter("System", "System Up Time");
            var systemInfo = _computer.GetSystemInfo();
            var systemInfoString = $"{systemInfo.OsVersion}-{systemInfo.SystemType}";
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose)
            {
                try
                {
                    if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                    {
                        break;
                    }

                    // 这些采集操作彼此独立，并发执行即可，不需要再包一层 Task.Run。
                    var cpuInfoTask = _computer.GetCpuInfoAsync();
                    var fanSpeedTask = _computer.GetFanSpeedAsync();
                    var memoryInfoTask = _computer.GetMemoryInfoAsync();
                    var gpuInfosTask = _computer.GetGpuInformationAsync();
                    var networkInfoTask = _computer.GetNetworkInfoAsync();
                    var localNetworkInfosTask = _computer.GetLocalNetworkConnectionInfosAsync();
                    await Task.WhenAll(cpuInfoTask, fanSpeedTask, memoryInfoTask, gpuInfosTask,
                        networkInfoTask, localNetworkInfosTask).ConfigureAwait(false);

                    var cpuInfo = await cpuInfoTask.ConfigureAwait(false);
                    var fanSpeed = await fanSpeedTask.ConfigureAwait(false);
                    var memoryInfo = await memoryInfoTask.ConfigureAwait(false);
                    var gpuInfos = await gpuInfosTask.ConfigureAwait(false);
                    var networkInfo = await networkInfoTask.ConfigureAwait(false);
                    var localNetworkInfos = await localNetworkInfosTask.ConfigureAwait(false);
                    var primaryGpu = gpuInfos?.FirstOrDefault();

                    var computerInfoModel = new ComputerInfoModel
                    {
                        CpuInfo = new CpuInfoModel
                        {
                            ClockSpeed = Convert.ToDecimal(cpuInfo.CpuBusSpeed),
                            CpuTemperature = Convert.ToDecimal(cpuInfo.CpuPackageTemperature),
                            FanSpeed = Convert.ToInt32(fanSpeed),
                            Name = cpuInfo.CpuName,
                            NumberOfCores = cpuInfo.CpuCoreInfos?.Count ?? 0,
                            UsagePercentage = Convert.ToDecimal(cpuInfo.CpuTotalUsedPercent),
                        },
                        MemoryInfo = new MemoryInfoModel
                        {
                            AvailableSizeBytes = memoryInfo.AvailableMemory,
                            UsedPercentage = Convert.ToDecimal(memoryInfo.UsedMemoryPercent),
                            MemoryRemaining = Convert.ToDecimal(memoryInfo.AvailableMemoryPercentage),
                            TotalSizeBytes = memoryInfo.UsedMemory + memoryInfo.AvailableMemory
                        },
                        GpuInfo = new GpuInfoModel
                        {
                            Name = primaryGpu?.Name,
                            UsagePercentage = primaryGpu?.Utilization ?? 0,
                        },
                        NetworkInfo = new NetworkInfoModel
                        {
                            DownloadSpeed = networkInfo.NetworkDownloadSpeed,
                            UploadSpeed = networkInfo.NetworkUploadSpeed,
                            IpAddress = networkInfo.IpAddress,
                        },
                        LocalNetworkConnectionInfos = localNetworkInfos?.Select(s =>
                            new LocalNetworkConnectionInfoModel
                            {
                                ConnectionName = s.ConnectionName,
                                DownloadSpeed = s.DownloadSpeed,
                                UploadSpeed = s.UploadSpeed,
                                Speed = s.Speed / 1000,
                                IsConnection = s.IsConnection,
                                Type = (NetworkType)s.Type
                            }).ToList() ?? new List<LocalNetworkConnectionInfoModel>(),
                        UpTime = TimeSpan.FromSeconds(counter.NextValue()),
                        SystemInfoString = systemInfoString
                    };

                    _computerInfoReporter.OnComputerInfoReceived(computerInfoModel);

                    if (computerInfoModel.CpuInfo.UsagePercentage >= 95)
                    {
                        PublishThrottledLog(
                            $"Cpu占用过高:{computerInfoModel.CpuInfo.UsagePercentage}%",
                            LogType.Warning, ref _lastCpuUsageWarning);
                    }

                    if (computerInfoModel.CpuInfo.CpuTemperature >= 85)
                    {
                        PublishThrottledLog(
                            $"Cpu温度过高:{computerInfoModel.CpuInfo.CpuTemperature}°",
                            LogType.Warning, ref _lastCpuTemperatureWarning);
                    }

                    if (computerInfoModel.MemoryInfo.UsedPercentage >= 90)
                    {
                        PublishThrottledLog(
                            $"内存占用过高:{computerInfoModel.MemoryInfo.UsedPercentage}%",
                            LogType.Warning, ref _lastMemoryWarning);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    PublishThrottledLog($"电脑状态采集异常:{exception.Message}",
                        LogType.Exception, ref _lastCollectionError);
                }
            }
        }

        private void PublishThrottledLog(string message, LogType type, ref long lastPublishedTimestamp)
        {
            var now = Stopwatch.GetTimestamp();
            if (lastPublishedTimestamp != 0 &&
                Stopwatch.GetElapsedTime(lastPublishedTimestamp, now) < WarningInterval)
            {
                return;
            }

            lastPublishedTimestamp = now;
            _eventBus.Publish(new AppLogInfoModel
            {
                CreateTime = DateTime.Now,
                Message = message,
                Type = type
            });
        }
    }
}
