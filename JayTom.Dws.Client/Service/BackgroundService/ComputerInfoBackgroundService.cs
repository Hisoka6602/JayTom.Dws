using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.Infrastructure.IComputer;
using NetworkType = JayTom.Dws.Client.Models.NetworkType;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class ComputerInfoBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IComputerInfoReporter _computerInfoReporter;
        private readonly IConfiguration _configuration;
        private readonly IComputer _computer;

        public ComputerInfoBackgroundService(IComputerInfoReporter computerInfoReporter,
            IConfiguration configuration, IComputer computer) {
            _computerInfoReporter = computerInfoReporter;
            _configuration = configuration;
            _computer = computer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var counter = new PerformanceCounter("System", "System Up Time");
            var systemInfo = _computer.GetSystemInfo();
            var systemInfoString = $"{systemInfo.OsVersion}-{systemInfo.SystemType}";
            while (!stoppingToken.IsCancellationRequested) {
                await Task.Run(async () => {
                    // 并行获取各项信息
                    var cpuInfoTask = _computer.GetCpuInfoAsync();
                    var fanSpeedTask = _computer.GetFanSpeedAsync();
                    var memoryInfoTask = _computer.GetMemoryInfoAsync();
                    var gpuInfosTask = _computer.GetGpuInformationAsync();
                    var networkInfoTask = _computer.GetNetworkInfoAsync();
                    var diskInfoTask = _computer.GetDiskInfoAsync();
                    var localNetworkConnectionInfosAsync = _computer.GetLocalNetworkConnectionInfosAsync();
                    await Task.WhenAll(cpuInfoTask, fanSpeedTask, localNetworkConnectionInfosAsync, memoryInfoTask, gpuInfosTask, networkInfoTask, diskInfoTask);
                    // 提取各项信息
                    var cpuInfoAsync = cpuInfoTask.Result;
                    var fanSpeed = fanSpeedTask.Result;
                    var memoryInfoAsync = memoryInfoTask.Result;
                    var gpuInfos = gpuInfosTask.Result;
                    var networkInfo = networkInfoTask.Result;
                    var diskInfoAsync = diskInfoTask.Result;
                    var localNetworkConnectionInfos = localNetworkConnectionInfosAsync.Result;
                    // 提交到事件
                    _computerInfoReporter.OnComputerInfoReceived(new ComputerInfoModel() {
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
                                Type = (NetworkType)s.Type
                            })?.ToList() ?? new List<LocalNetworkConnectionInfoModel>(),
                        UpTime = TimeSpan.FromSeconds(counter.NextValue()),
                        SystemInfoString = systemInfoString
                    });
                    if (memoryInfoAsync.UsedMemoryPercent >= 70) {
                        GC.Collect();
                    }
                }, stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}