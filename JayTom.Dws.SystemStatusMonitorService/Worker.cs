using Newtonsoft.Json;
using System.Diagnostics;
using JayTom.Dws.CrossCutting.SignalR;
using JayTom.Dws.Domain.Entities.SystemEntities;
using JayTom.Dws.SystemStatusMonitorService.Service;
using JayTom.Dws.SystemStatusMonitorService.SignalR;

namespace JayTom.Dws.SystemStatusMonitorService {

    public class Worker : BackgroundService {
        private readonly ILogger<Worker> _logger;
        private readonly ISystemStatusMonitorMessageHub _systemStatusMonitorMessageHub;

        private readonly IComputer _computer;

        public Worker(ILogger<Worker> logger, ISystemStatusMonitorMessageHub systemStatusMonitorMessageHub,
            IComputer computer) {
            _logger = logger;
            _systemStatusMonitorMessageHub = systemStatusMonitorMessageHub;

            _computer = computer;
            _systemStatusMonitorMessageHub.SetServerInfo("实时系统信息", string.Empty);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _systemStatusMonitorMessageHub.UserConnected += info => {
                _logger.LogInformation($"连接加入:{JsonConvert.SerializeObject(info)}");
                return Task.CompletedTask;
            };
            _systemStatusMonitorMessageHub.UserDisconnected += info => {
                _logger.LogInformation($"连接退出:{JsonConvert.SerializeObject(info)}");
                return Task.CompletedTask;
            };
            while (!stoppingToken.IsCancellationRequested) {
                var counter = new PerformanceCounter("System", "System Up Time");
                var systemInfo = _computer.GetSystemInfo();
                var systemInfoString = $"{systemInfo.OsVersion}-{systemInfo.SystemType}";
                try {
                    // 并行获取各项信息
                    var cpuInfoTask = _computer.GetCpuInfoAsync();
                    var fanSpeedTask = _computer.GetFanSpeedAsync();
                    var memoryInfoTask = _computer.GetMemoryInfoAsync();
                    var gpuInfosTask = _computer.GetGpuInformationAsync();
                    var networkInfoTask = _computer.GetNetworkInfoAsync();
                    var diskInfoTask = _computer.GetDiskInfoAsync();
                    var localNetworkConnectionInfosAsync = _computer.GetLocalNetworkConnectionInfosAsync();
                    var topCpuUsageProcessesAsync = _computer.GetTopCpuUsageProcessesAsync();
                    var topMemoryUsageProcessesAsync = _computer.GetTopMemoryUsageProcessesAsync();
                    await Task.WhenAll(cpuInfoTask, fanSpeedTask, localNetworkConnectionInfosAsync, memoryInfoTask,
                        gpuInfosTask, networkInfoTask, diskInfoTask, topCpuUsageProcessesAsync, topMemoryUsageProcessesAsync);
                    // 提取各项信息
                    var cpuInfoAsync = cpuInfoTask.Result;
                    var fanSpeed = fanSpeedTask.Result;
                    var memoryInfoAsync = memoryInfoTask.Result;
                    var gpuInfos = gpuInfosTask.Result;
                    var networkInfo = networkInfoTask.Result;
                    var diskInfoAsync = diskInfoTask.Result;
                    var localNetworkConnectionInfos = localNetworkConnectionInfosAsync.Result;
                    var result = topCpuUsageProcessesAsync.Result;
                    var list = topMemoryUsageProcessesAsync.Result;
                    //提交到事件
                    var computerInfoModel = new ComputerInfoModel() {
                        CpuInfo = new CpuInfoModel() {
                            ClockSpeed = cpuInfoAsync.CpuBusSpeed,
                            CpuTemperature = cpuInfoAsync.CpuPackageTemperature,
                            FanSpeed = fanSpeed,
                            Name = cpuInfoAsync.CpuName,
                            NumberOfCores = cpuInfoAsync.CpuCoreInfos?.Count ?? 0,
                            UsagePercentage = cpuInfoAsync.CpuTotalUsedPercent,
                        },
                        MemoryInfo = new MemoryInfoModel() {
                            AvailableSizeBytes = memoryInfoAsync.AvailableMemory,
                            UsedPercentage = memoryInfoAsync.UsedMemoryPercent,
                            MemoryRemaining = memoryInfoAsync.AvailableMemoryPercentage,
                            TotalSizeBytes = memoryInfoAsync.UsedMemory + memoryInfoAsync.AvailableMemory
                        },
                        GpuInfo = new GpuInfoModel() {
                            Name = gpuInfos?[0]?.Name,
                            UsagePercentage = gpuInfos?[0]?.Utilization ?? 0,
                            /*MemorySizeGb = (float)(gpuInfos?[1]?.TotalMemory ?? 0) / 1024 / 1024 / 1024,
                            UsedMemoryGb = (float)((gpuInfos?[1]?.TotalMemory ?? 0) - (gpuInfos?[0]?.FreeMemory ?? 0)) / 1024 / 1024 / 1024,
                            UsedMemoryPercentage = gpuInfos?[1]?.Utilization ?? 0,*/
                        },
                        NetworkInfo = new NetworkInfoModel() {
                            DownloadSpeed = networkInfo.NetworkDownloadSpeed,
                            UploadSpeed = networkInfo.NetworkUploadSpeed,
                            IpAddress = networkInfo.IpAddress,
                        },
                        HardDiskList = diskInfoAsync?.Select(s => new HardDiskInfoModel {
                            DiskName = s.Name,
                            FreeSpaceBytes = s.AvailableDiskSpace,
                            FreeSpacePercentage = s.AvailableDiskSpacePercentage,
                            UsedSpaceBytes = s.UsedDiskSpace,
                            UsedSpacePercentage = (float)s.UsedDiskSpacePercentage,
                        })?.ToList(),
                        LocalNetworkConnectionInfos = localNetworkConnectionInfos?.Select(s =>
                            new LocalNetworkConnectionInfoModel() {
                                ConnectionName = s.ConnectionName,
                                DownloadSpeed = s.DownloadSpeed,
                                UploadSpeed = s.UploadSpeed,
                                Speed = s.Speed / 1000,
                                IsConnection = s.IsConnection,
                                Type = s.Type
                            })?.ToList() ?? new List<LocalNetworkConnectionInfoModel>(),
                        UpTime = TimeSpan.FromSeconds(counter.NextValue()),
                        CpuUsageProcesses = result,
                        MemoryUsageProcesses = list,
                        SystemInfoString = systemInfoString
                    };

                    //推送事件
                    _systemStatusMonitorMessageHub.MessageAll("SystemInfo", computerInfoModel);
                }
                catch (Exception e) {
                    _logger.LogError($"{e}");
                    _logger.LogError($"{e}");
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}